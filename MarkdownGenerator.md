# MarkdownGenerator:

## Properties:

| Type | Property Name | Summary | Default Value |
| --- | --- | --- | --- |
| String | test3 |  |  |
| `Int32` | test1 | tooltip 1 | 0 |
| `Int32` | test2 |  | 1 |


## Methods:

| Method | Summary | Parameters | Returns |
| --- | --- | --- | --- |
| GetClassSummary |  | `String` scriptText, `String` className | `String` |
| GetScriptAssembly |  | `String` assetPath | `Assembly` |
| GetPropertiesTable |  | `Assembly` assembly, `String` className, `String` scriptText | `String` |
| GetMethodTable |  | `Assembly` assembly, `String` className, `String` scriptText | `String` |
| GetSummaryCommentAboveMatch |  | `String` text, `Match` regexMatch | `String` |
| TrimNamespace |  | `String` typeName "The type name to trim a namespace from." | `String` |
| TestMethod |  | `Int32` testInt "Test int summary." | `Int32` |
| get_test3 |  |  | `String` |
| set_test3 |  | `String` value | `Void` |
| set_test4 |  | `String` value | `Void` |
| set_test5 |  | `String` value | `Void` |
| Equals |  | `Object` obj | `Boolean` |
| GetHashCode |  |  | `Int32` |
| GetType |  |  | `Type` |
| ToString |  |  | `String` |

