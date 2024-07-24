using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
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
        Type scriptType = FindTypeInAssemblies(scriptName);

        if (scriptType != null)
        {
            string markdown = "# " + scriptName + ":\n";
            // Get class summary.
            markdown += GetClassSummary(scriptText, scriptName) + "\n";

            // Get class namespace.
            string scriptNamespace = scriptType.Namespace;
            if (!string.IsNullOrEmpty(scriptNamespace))
            {
                markdown += "### Namespace:\n";
                markdown += scriptNamespace + "\n\n";
            }

            markdown += "## Properties:\n\n";
            markdown += GetPropertiesTable(scriptType, scriptName, scriptText) + "\n\n";

            markdown += "## Methods:\n\n";
            markdown += GetMethodTable(scriptType, scriptName, scriptText) + "\n";

            string outputPath = Path.GetDirectoryName(scriptPath) + "/" + scriptName + ".md";
            File.WriteAllText(outputPath, markdown);
            AssetDatabase.Refresh();

            Debug.Log("Markdown documentation generated at " + outputPath);
        }
        else
        {
            Debug.LogWarningFormat("Script type: {0} not found in any loaded assembly.", scriptName);
        }
    }

    public static string GetClassSummary(string scriptText, string className)
    {
        return GetSummaryCommentAboveMatch(scriptText, Regex.Match(scriptText, $@"\bclass\s+{Regex.Escape(className)}\b", RegexOptions.Multiline));
    }

    public static Type FindTypeInAssemblies(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (type != null)
            {
                return type;
            }
        }
        return null;
    }

    public static string GetPropertiesTable(Type scriptType, string className, string scriptText)
    {
        var properties = scriptType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

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

        var fields = scriptType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
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

            var defaultValueMatch = Regex.Match(scriptText, $@"\b{fieldName}\b\s*=\\s*(?<value>\S+)\s*;");
            if (string.IsNullOrEmpty(summary))
            {
                summary = GetSummaryCommentAboveMatch(scriptText, defaultValueMatch);
            }
            var defaultValue = defaultValueMatch.Success ? defaultValueMatch.Groups["value"].Value : "";

            markdownTable += $"| `{fieldType}` | {fieldName} | {summary} | {defaultValue} |\n";
        }

        return markdownTable;
    }

    public static string GetMethodTable(Type scriptType, string className, string scriptText)
    {
        // Get methods
        MethodInfo[] methods = scriptType.GetMethods();

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

    public static string GetSummaryCommentAboveMatch(string text, Match regexMatch)
    {
        string[] lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        int matchLine = regexMatch.Index;
        int maxLinesAbove = Math.Min(matchLine, 4);
        int startLine = matchLine - maxLinesAbove;

        for (int i = startLine; i < matchLine; i++)
        {
            if (i >= 0 && i < lines.Length && lines[i].Contains("</summary>"))
            {
                int summaryIndex = lines[i].IndexOf("<summary>");
                if (summaryIndex != -1)
                {
                    return lines[i].Substring(summaryIndex + "<summary>".Length);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        return string.Empty;
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

    public static string ConvertToMarkdownTable(string[] array)
    {
        StringBuilder table = new StringBuilder();
        table.AppendLine("| Index | Value |");
        table.AppendLine("| ----- | ----- |");
        for (int i = 0; i < array.Length; i++)
        {
            table.AppendLine($"| {i} | {array[i]} |");
        }
        Debug.Log(table.ToString());
        return table.ToString();
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
    /// property summary.
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
