![](https://kitbashery.com/assets/images/kitbashery-github-banner.jpg)

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity3d.com/get-unity/download)
[![GitHub](https://img.shields.io/github/license/kitbashery/modular-ai.svg)](https://github.com/Kitbashery/Unity-Markdown-Generator/blob/main/LICENSE)
[![Contributing](https://img.shields.io/badge/Contribute-guidelines-lightgrey)](https://github.com/Kitbashery/.github/blob/main/.github/CONTRIBUTING.md)

# Unity-Markdown-Generator
Generate .md Markdown Documentation from Unity C# script assets.

## Usage:

Right-click a script asset then click "Generate Markdown Documentation".

## Limitations:

- Only documents the class that shares the script file name.
- Only documents public properties, methods and fields.
- XML Summary comment support is WIP.

## Comment Support:

- Tooltip attributes for properties.
- Parameter XML comments for methods.

## Example:

#### The script should turn code structured like this:
```
Namespace myNamespace.space
{

/// <summary>
/// Test class summary.
/// </summary>
public class TestClass
{

 [Tooltip("My tooltip text.")]
    public int property = 0;

    /// <summary>
    /// Property2 summary.
    /// </summary>
    public int property2 = 1;

    /// <summary>
    /// Test method summary.
    /// </summary>
    /// <param name="param1">param1 summary</param>
    /// <param name="param2">param2 summary</param>
    /// <param name="param3">param3 summary</param>
    public void Test(int param1, string param2, int param3 = 1)
    {

    }
}
```

#### To this output:

```
# TestClass:
Test class summary.

### Namespace:
myNamespace.space

## Properties:

| Type        | Name | Description         | Default Value |
|:------------|:-----|:--------------------|:--------------|
|  `int` | property | My tooltip text. | 0 |
|  `int` | property2 |  Property2 summary. | 1 |

## Methods:

| Name | Summary      | Parameters | Returns |
|:-----|:-------------|:-----------|:--------|
| Test |  Test method summary. | `int` param1 "param1 summary" , `string` param2 "param2 summary", `int` param3 "param3 summary" = 1  | `Void` |
```

#### Rendered:

# TestClass:
Test class summary.

### Namespace:
myNamespace.space

## Properties:

| Type        | Name | Description         | Default Value |
|:------------|:-----|:--------------------|:--------------|
|  `int` | property | My tooltip text. | 0 |
|  `int` | property2 |  Property2 summary. | 1 |

## Methods:

| Name | Summary      | Parameters | Returns |
|:-----|:-------------|:-----------|:--------|
| Test |  Test method summary. | `int` param1 "param1 summary" , `string` param2 "param2 summary", `int` param3 "param3 summary" = 1  | `Void` |


---
The name Kitbashery & all associated images Copyright &copy; 2023 Kitbashery. All Rights Reserved.

<i>"Unity", Unity logos, and other Unity trademarks aretrademarks or registered trademarks of Unity Technologies. 
Notice: This script is an internal tool of Kitbashery and not part of Kitbashery's product library and is provided AS IS. The name Unity is only used to elaborate on what technology stack this parser is for.</i>
