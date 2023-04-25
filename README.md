# Unity-Markdown-Generator (WIP Under Heavy Development 4/25/23)
Generate .md Markdown Documentation from code comments in Unity script assets.

## Depencencies:
com.unity.nuget.mono-cecil (Docs: https://docs.unity3d.com/Packages/com.unity.nuget.mono-cecil@1.10/manual/index.html).

Should be a hidden internal package and installable by "Add package by name" > com.unity.nuget.mono-cecil 
via the package manager.

## Usage:

Right-click a script asset then click "Generate Markdown Documentation".

## Example:

#### The script should turn code comments structed like this:
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
