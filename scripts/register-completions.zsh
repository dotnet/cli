#compdef dotnet
local completions=("$(dotnet complete "$words")")
compadd "${(ps:\n:)completions}"
