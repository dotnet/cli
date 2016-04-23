param( $versions)
$qmode = $false;

function cleanpath()
{
    $env:PATH=($env:PATH.Split(";") | Where { !$_.Contains("dotnet") }) -Join ";"
}
foreach($ver in $versions)
{
    cleanpath;
    if ($ver -ne "dev")
    {
        Write-Host -ForegroundColor Green "Installing $ver"
        $dotnetPath = "$PSScriptRoot\.dotnet\$ver"
        if (!(test-path ".dotnet"))
        {
            & ($PSScriptRoot+"\..\..\scripts\obtain\install.ps1") preview $ver $dotnetPath;
        }
        $env:PATH = "$dotnetPath;"+$env:PATH
    }
    else
    {
        Write-Host -ForegroundColor Green "Using dev"
        & ($PSScriptRoot+"\..\..\scripts\use-dev.ps1");
    }
    cmd /c "where dotnet" | Write-Host -ForegroundColor Green
    dotnet --version |  Write-Host -ForegroundColor Green
    if ($qmode)
    {
        dotnet test | Where {$_.startswith("[") } | Write-Host -ForegroundColor Blue
    }else {
        dotnet test
    }
}