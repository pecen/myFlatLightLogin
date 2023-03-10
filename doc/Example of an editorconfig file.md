
If an editorconfig file should be used to control what editor settings in Visual Studio developers should be bound to, do the following before you create the main solution in Visual Studio:

1. In the root folder for the solution, i.e. the src folder, create a file with the name .editorconfig (don't miss the dot in the beginning of the filename)
2. Copy and paste the following (beware that this is only an example and that you might have other requirements):

```
root = true

[*]
end_of_line = crlf

[*.cs]
indent_style = tab
indent_size = 2
csharp_new_line_before_catch = true
csharp_new_line_before_else = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_anonymous_types = false
csharp_new_line_before_members_in_object_initializers = false
csharp_new_line_before_open_brace = none
csharp_new_line_between_query_expression_clauses = false

[*.xaml]
indent_style = tab
indent_size = 2

[*.xml]
indent_style = tab
indent_size = 2
```

3. Save and close the file
