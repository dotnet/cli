# zsh parameter completion for the dotnet CLI

_dotnet_zsh_complete() 
{

    #compdef dotnet
    local completions=("$(dotnet complete "$words")")
    compadd "${(ps:\n:)completions}"
    
}

compctl -K _dotnet_zsh_complete dotnet
