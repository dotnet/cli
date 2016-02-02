dotnet-mcg
===========

# NAME
dotnet-mcg -- Run marshalling code generator

# SYNOPSIS
dotnet compile-native --enablemcg

# DESCRIPTION
The mcg generate marshalling interop code to enable complex interop other wise not supported. 
This command is invoked in the context of  native compilation and when invoked will generate 
interop code , which is then compiled to "projectname".mcginterop.dll. 

# EXAMPLES

`dotnet compile-native --enableinterop`

# SEE ALSO
dotnet compile-native(1)
