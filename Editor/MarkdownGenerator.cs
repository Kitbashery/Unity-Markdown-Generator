using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates Github Markdown from a script.
/// </summary>
public static class MarkdownGenerator
{
    [MenuItem("Assets/Generate Markdown Documentation")]
    private static void GenerateMarkdownDocumentation()
    {
        string scriptPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!scriptPath.EndsWith(".cs"))
        {
            Debug.LogWarning("Selected asset is not a C# script.");
            return;
        }

        string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        string scriptText = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptPath).text;
        Assembly scriptAssembly = GetScriptAssembly(scriptPath);

        string markdown = "# " + scriptName + ":\n";
        markdown += GetClassSummary(scriptText) + "\n";
        markdown += "### Namespace:\n";  
        markdown += scriptAssembly.GetType(scriptName).Namespace + "\n";
        markdown += "## Properties:\n\n";
        markdown += GetPropertiesTable(scriptAssembly, scriptName, scriptText) + "\n\n";
        markdown += "## Methods:\n\n";
        markdown += GetMethodTable(scriptAssembly, scriptName, scriptText) + "\n";

        string outputPath = Path.GetDirectoryName(scriptPath) + "/" + scriptName + ".md";
        File.WriteAllText(outputPath, markdown);
        AssetDatabase.Refresh();

        Debug.Log("Markdown documentation generated at " + outputPath);
    }

    private static string GetClassSummary(string scriptText)
    {
        var regex = new Regex(@"///\s*<summary>(?<summary>[\s\S]*?)</summary>");
        var summary = string.Empty;

        var summaryMatch = regex.Match(scriptText);
        if (summaryMatch.Success)
        {
            summary = summaryMatch.Groups["summary"].Value.Trim();
            summary = summary.TrimStart('/', ' ', '\t');
            summary = summary.TrimEnd('/', ' ', '\t');
        }
        return summary;
    }

    public static Assembly GetScriptAssembly(string assetPath)
    {
        // Load the asset at the given path
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

        // Cast the asset to a MonoScript
        var monoScript = asset as MonoScript;

        if (monoScript == null)
        {
            Debug.LogError($"Asset at path {assetPath} is not a MonoScript.");
            return null;
        }

        // Get the full type name from the MonoScript
        var typeName = monoScript.GetClass().FullName;

        // Check if the type is in a custom namespace assembly
        var customAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetTypes().Any(t => t.FullName == typeName));
        if (customAssembly != null)
        {
            // Get the path of the assembly file
            var path = customAssembly.Location;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Assembly for type {typeName} was not loaded from a file.");
                return null;
            }
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load assembly from path {path}: {e.Message}");
                return null;
            }
        }

        // Get the default assembly for the type
        var defaultAssembly = Type.GetType(typeName)?.Assembly;
        if (defaultAssembly != null)
        {
            // Get the path of the assembly file
            var path = defaultAssembly.Location;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Assembly for type {typeName} was not loaded from a file.");
                return null;
            }
            try
            {
                return Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load assembly from path {path}: {e.Message}");
                return null;
            }
        }

        // If we couldn't find the assembly, log an error and return null
        Debug.LogError($"Could not find assembly for type {typeName}.");
        return null;
    }

    public static string GetPropertiesTable(Assembly assembly, string className, string scriptText)
    {
        var type = assembly.GetType(className);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        var regex = new Regex(@"///\s*<summary>(?<summary>[\s\S]*?)</summary>");

        var markdownTable = "| Type | Property Name | Summary | Default Value |\n| --- | --- | --- | --- |\n";
        foreach (var property in properties)
        {
            if (!property.CanRead || property.GetMethod.IsPrivate)
            {
                continue; // skip properties that can't be read or are private
            }

            var propertyType = property.PropertyType.Name;
            var propertyName = property.Name;

            var tooltipAttribute = (TooltipAttribute)property.GetCustomAttribute(typeof(TooltipAttribute));
            var summary = tooltipAttribute != null ? tooltipAttribute.tooltip.TrimStart('/', ' ', '\t') : "";

            var defaultValueMatch = Regex.Match(scriptText, $@"\b{propertyName}\b\s*=\s*(?<value>\S+)\s*;");
            if (string.IsNullOrEmpty(summary))
            {
                summary = GetSummaryCommentAboveMatch(scriptText, defaultValueMatch);
            }
            var defaultValue = defaultValueMatch.Success ? defaultValueMatch.Groups["value"].Value : "";

            markdownTable += $"| {propertyType} | {propertyName} | {summary} | {defaultValue} |\n";
        }

        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.IsPrivate || field.IsInitOnly)
            {
                continue; // skip private and readonly fields
            }

            var fieldType = field.FieldType.Name;
            var fieldName = field.Name;

            var tooltipAttribute = (TooltipAttribute)field.GetCustomAttribute(typeof(TooltipAttribute));
            var summary = tooltipAttribute != null ? tooltipAttribute.tooltip.TrimStart('/', ' ', '\t') : "";

            var defaultValueMatch = Regex.Match(scriptText, $@"\b{fieldName}\b\s*=\s*(?<value>\S+)\s*;");
            if (string.IsNullOrEmpty(summary))
            {
                summary = GetSummaryCommentAboveMatch(scriptText, defaultValueMatch);
            }
            var defaultValue = defaultValueMatch.Success ? defaultValueMatch.Groups["value"].Value : "";



            markdownTable += $"| `{fieldType}` | {fieldName} | {summary} | {defaultValue} |\n";
        }

        return markdownTable;
    }

    public static string GetMethodTable(Assembly assembly, string className, string scriptText)
    {
        // Get class type
        Type type = assembly.GetType(className);

        // Get methods
        MethodInfo[] methods = type.GetMethods();

        // Build table header
        StringBuilder table = new StringBuilder("| Method | Summary | Parameters | Returns |\n");
        table.Append("| --- | --- | --- | --- |\n");

        // Process each method
        foreach (MethodInfo method in methods)
        {
            // Skip non-public methods
            if (!method.IsPublic)
            {
                continue;
            }

            // TODO: get method by type and name then:
            string summary = string.Empty;// GetSummaryCommentAboveMatch(scriptText, defaultValueMatch);

            // Build parameter list
            StringBuilder parameters = new StringBuilder();
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                // Add parameter type and name
                parameters.Append($"`{TrimNamespace(parameter.ParameterType.ToString())}` {parameter.Name}");

                // Get parameter summary comment
                string parameterSummary = string.Empty;
                foreach (var line in scriptText.Split('\n'))
                {
                    // Match parameter summary comment using regex
                    var match = Regex.Match(line.Trim(), $@"^///\s*<param\s+name=""{parameter.Name}"">\s*(?<summary>.*)\s*</param>\s*$");
                    if (match.Success)
                    {
                        parameterSummary = match.Groups["summary"].Value.Trim();
                        break;
                    }
                }

                // Add parameter summary comment (if exists)
                if (!string.IsNullOrEmpty(parameterSummary))
                {
                    parameters.Append($" \"{parameterSummary}\"");
                }

                // Add default value (if exists)
                if (parameter.HasDefaultValue)
                {
                    parameters.Append($" = {TrimNamespace(parameter.DefaultValue.ToString())}");
                }

                // Add separator
                parameters.Append(", ");
            }

            // Remove trailing separator
            if (parameters.Length > 0)
            {
                parameters.Length -= 2;
            }

            // Add method row to table
            table.Append($"| {method.Name} | {summary} | {parameters} | `{TrimNamespace(method.ReturnType.ToString())}` |\n");
        }

        // Return table
        return table.ToString();
    }

    public static string GetSummaryCommentAboveMatch(string source, Match match)
    {
        string pattern = @"///\s*<summary>(?<text>[\s\S]*?)\s*///\s*</summary>";
        string summaryText = "";

        int matchLine = source.Substring(0, match.Index).Count(c => c == '\n'); // convert match index to line number
        int searchStartLine = matchLine - 1; // start search from the line above the match
        string[] lines = source.Split('\n');

        // find the first instance of a non-comment line above the match
        int firstNonCommentLine = searchStartLine;
        for (int i = searchStartLine; i >= 0; i--)
        {
            string line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("//"))
            {
                firstNonCommentLine = i;
                break;
            }
        }

        // search for the nearest preceding summary comment above the first non-comment line
        for (int i = firstNonCommentLine - 1; i >= 0; i--)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("/// <summary>"))
            {
                summaryText = Regex.Match(line, pattern).Groups["text"].Value.Trim();
                break;
            }
        }

        return summaryText;
    }

    /// <summary>
    /// Method to trim the namespace from a type name.
    /// </summary>
    /// <param name="typeName">The type name to trim a namespace from.</param>
    /// <returns>The trimmed name.</returns>
    public static string TrimNamespace(string typeName)
    {
        if (typeName == null)
        {
            return null;
        }

        int index = typeName.LastIndexOf('.');
        if (index >= 0)
        {
            return typeName.Substring(index + 1);
        }
        else
        {
            return typeName;
        }
    }

    #region Test Data:

    /// <summary>
    /// This is a test summary.
    /// </summary>
    /// <param name="testInt">Test int summary.</param>
    /// <returns>Returns summary.</returns>
    public static int TestMethod(int testInt)
    {
        return 0;
    }

    [Tooltip("tooltip 1")]
    public static int test1 = 0;

    /// <summary>
    ///  property summary.
    /// </summary>
    public static int test2 = 1;

    /// <summary>
    /// get set test
    /// </summary>
    public static string test3 { get; set; }

    /// <summary>
    /// get set test 2
    /// </summary>
    public static string test4 { private get; set; }

    [Tooltip("test")]
    public static string test5 { private get; set; }

    #endregion
}
