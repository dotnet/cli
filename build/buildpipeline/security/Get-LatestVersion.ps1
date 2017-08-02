<#
.SYNOPSIS
    Retrieves the latest commit SHA and the corresponding package Id for the specified branch of CLI. 
    This retrieval is achieved by downloading the latest.version file, which contains the commit SHA and package Id info.
    If retrieval succeeds, then the commit is set as $env:CliLatestCommitSha, and package Id is set as $env:CliLatestPackageId.
.PARAMETER $Branch
    Name of the CLI branch.
.PARAMETER $Filename
    Name of the file that contains latest version info i.e. commit SHA and package Id.
    If not specified, then the default value is latest.version
.PARAMETER $UrlPrefix
    URL prefix for $Filename.
    If not specified, then the default value is https://dotnetcli.blob.core.windows.net/dotnet/Sdk
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Branch,
    [string]$Filename="latest.version",
    [string]$UrlPrefix="https://dotnetcli.blob.core.windows.net/dotnet/Sdk"
)

$latestVersionUrl = "$UrlPrefix/$Branch/$Filename"
$latestVersionFilePath = ".\latest.version"
$env:CliLatestCommitSha = ""
$env:CliLatestPackageId = ""


function Get-VersionInfo
{
    Write-Host "Attempting to retrieve latest version info from $latestVersionUrl"
    $retries = 3
    $retryCount = 1
    $oldEap = $ErrorActionPreference

    while ($retryCount -le 3)
    {
        $ErrorActionPreference = "Stop"

        try
        {
            if(Test-Path "$latestVersionFilePath")
            {
                Remove-Item "$latestVersionFilePath" -Force
            }

            Invoke-WebRequest -Uri "$latestVersionUrl" -OutFile "$latestVersionFilePath"

            $latestVersionContent = Get-Content "$latestVersionFilePath"
            $env:CliLatestCommitSha = $latestVersionContent[0]
            $env:CliLatestPackageId = $latestVersionContent[1]

            break
        }
        catch
        {
            Sleep -Seconds (Get-Random -minimum 3 -maximum 10)
            Write-Host "Exception occurred while attempting to get latest version info from $latestVersionUrl. $_"
            Write-Host "Retry $retryCount of $retries"
        }
        finally
        {
            $ErrorActionPreference = $oldEap
        }

        $retryCount++
    }
}

Get-VersionInfo

if (-not [string]::IsNullOrWhiteSpace($env:CliLatestCommitSha) -and -not [string]::IsNullOrWhiteSpace($env:CliLatestPackageId))
{
    Write-Host "##vso[task.setvariable variable=CliLatestCommitSha;]$env:CliLatestCommitSha"
    Write-Host "##vso[task.setvariable variable=CliLatestPackageId;]$env:CliLatestPackageId"

    Write-Host "The latest commit SHA in CLI $Branch is $env:CliLatestCommitSha"
    Write-Host "The latest package Id in CLI $Branch is $env:CliLatestPackageId"
}
else
{
    Write-Error "Unable to get latest version info from $latestVersionUrl"
}
