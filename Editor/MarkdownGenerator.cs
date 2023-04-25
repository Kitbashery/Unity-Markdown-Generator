using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection;
using System;

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

        string markdown = "# " + scriptName + ":\n";
        markdown += GetClassSummary(scriptPath) + "\n";
        markdown += "### Namespace:\n";
        markdown += GetNamespace(scriptText) + "\n\n";
        markdown += "## Properties:\n\n";
        markdown += "| Type        | Name | Description         | Default Value |\n";
        markdown += "|:------------|:-----|:--------------------|:--------------|\n";
        markdown += GetPropertiesTable(scriptPath) + "\n\n";
        markdown += "## Methods:\n\n";
        markdown += "| Name | Summary      | Parameters | Returns |\n";
        markdown += "|:-----|:-------------|:-----------|:--------|\n";
        markdown += GetMethodsTable(scriptPath) + "\n";

        string outputPath = Path.GetDirectoryName(scriptPath) + "/" + scriptName + ".md";
        File.WriteAllText(outputPath, markdown);
        AssetDatabase.Refresh();

        Debug.Log("Markdown documentation generated at " + outputPath);
    }

    private static string GetClassSummary(string scriptPath)
    {
        string summaryPattern = @"\/\*\*\s*\n\s*\*\s+<summary>\s*\n\s*\*\s+(.*?)\n\s*\*\s+<\/summary>\s*\n\s*\*\s+\*\/";
        string tooltipPattern = @"\[\s*Tooltip\s*\(\s*\""(.*?)\""\s*\)\s*\]";

        string scriptText = File.ReadAllText(scriptPath);
        string summary = Regex.Match(scriptText, summaryPattern, RegexOptions.Singleline).Groups[1].Value.Trim();
        string tooltip = Regex.Match(scriptText, tooltipPattern).Groups[1].Value.Trim();

        return summary + (string.IsNullOrEmpty(tooltip) ? "" : "\n\n" + tooltip);
    }

    private static string GetNamespace(string scriptText)
    {
        string namespacePattern = @"(?<=namespace\s+)[\w\.]+";
        Match match = Regex.Match(scriptText, namespacePattern);

        if (match.Success)
        {
            return match.Value;
        }

        return "";
    }

    private static string GetPropertiesTable(string scriptPath)
    {
        string propertyPattern = @"(?<!\/\/\s*)\[\s*Tooltip\s*\(\s*\""(.*?)\""\s*\)\s*\]\s*public\s+(\S+)\s+(\S+)\s*{\s*get;\s*set;\s*}";

        string scriptText = File.ReadAllText(scriptPath);
        MatchCollection matches = Regex.Matches(scriptText, propertyPattern);

        string table = "";
        foreach (Match match in matches)
        {
            string tooltip = match.Groups[1].Value.Trim();
            string type = match.Groups[2].Value.Trim();
            string name = match.Groups[3].Value.Trim();
            string defaultValue = GetPropertyDefaultValue(scriptPath, name);

            table += "| " + type + " | " + name + " | " + tooltip + " | " + defaultValue + " |\n";
        }

        return table;
    }

    private static string GetMethodsTable(string scriptPath)
    {
        string methodPattern = @"(?<!\/\/\s*)\[\s*Tooltip\s*\(\s*\""(.*?)\""\s*\)\s*\]\s*(public|private|protected)\s+([\w<>, ]+)\s+(\w+)\s*\((.*?)\)\s*";

        string scriptText = File.ReadAllText(scriptPath);
        MatchCollection matches = Regex.Matches(scriptText, methodPattern);

        string table = "";
        foreach (Match match in matches)
        {
            string summary = match.Groups[1].Value.Trim();
            string accessModifier = match.Groups[2].Value.Trim();
            string returnType = match.Groups[3].Value.Trim();
            string methodName = match.Groups[4].Value.Trim();
            string parameters = match.Groups[5].Value.Trim();

            parameters = Regex.Replace(parameters, @"^\s*void\s*$", "");
            parameters = Regex.Replace(parameters, @"\s*\(\s*\)", "");
            parameters = Regex.Replace(parameters, @"\s+", " ");
            parameters = Regex.Replace(parameters, @"(?<!\w)out\s+", "");
            parameters = Regex.Replace(parameters, @"(?<!\w)ref\s+", "");
            parameters = Regex.Replace(parameters, @"(?<=\w)\[\]\s+", "[] ");

            table += "| " + methodName + " | " + summary + " | " + parameters + " | " + returnType + " |\n";
        }

        return table;
    }

    private static string GetPropertyDefaultValue(string scriptPath, string propertyName)
    {
        using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(scriptPath))
        {
            TypeDefinition type = assembly.MainModule.Types[0];
            FieldDefinition field = type.Fields.FirstOrDefault(f => f.Name == propertyName);

            if (field != null && field.HasConstant)
            {
                object value = field.Constant;
                if (value != null)
                {
                    return value.ToString();
                }
            }

            return "";
        }
    }
}
