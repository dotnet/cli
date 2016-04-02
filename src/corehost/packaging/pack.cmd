@echo off
setlocal EnableDelayedExpansion

set __ProjectDir=%~dp0
set __ThisScriptShort=%0
set __ThisScriptFull="%~f0"

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Adding environment variables to workaround the "Argument Escape" problem with passing arguments to
:: .cmd calls from dotnet-cli-build scripts.
::
set __BuildArch=%__WorkaroundCliCoreHostBuildArch%
set __DotNetHostBinDir=%__WorkaroundCliCoreHostBinDir%
set __HostVer=%__WorkaroundCliCoreHostVer%
set __FxrVer=%__WorkaroundCliCoreHostFxrVer%
set __PolicyVer=%__WorkaroundCliCoreHostPolicyVer%
set __BuildMajor=%__WorkaroundCliCoreHostBuildMajor%
set __VersionTag=%__WorkaroundCliCoreHostVersionTag%
::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "x64"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArch=%1&shift&goto Arg_Loop)
if /i "%1" == "/hostbindir"         (set __DotNetHostBinDir=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/hostver"            (set __HostVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/fxrver"             (set __FxrVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/policyver"          (set __PolicyVer=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/build"              (set __BuildMajor=%2&shift&shift&goto Arg_Loop)
if /i "%1" == "/vertag"             (set __VersionTag=%2&shift&shift&goto Arg_Loop)

echo Invalid command line argument: %1
goto Usage

:ArgsDone

if [%__BuildArch%]==[] (goto Usage)
if [%__DotNetHostBinDir%]==[] (goto Usage)

:: Initialize the MSBuild Tools
call "%__ProjectDir%\init-tools.cmd"

:: Setup deps
pushd "%__ProjectDir%\deps"
set __RuntimeJsonVersion=1.0.1-rc2-23931
set __ProjectJsonContents={ "dependencies": { "Microsoft.NETCore.Platforms": "%__RuntimeJsonVersion%" }, "frameworks": { "dnxcore50": { "imports": "portable-net45+win8" } } }

:: Restore deps
echo %__ProjectJsonContents% > "project.json"
"%__ProjectDir%\Tools\dotnetcli\dotnet.exe" restore --source "https://dotnet.myget.org/F/dotnet-core" --packages "%__ProjectDir%\packages"

:: Copy runtime.json
set "__RuntimeJsonFile=%__ProjectDir%\Tools\runtime.json"
if not exist "%__RuntimeJsonFile%" (copy /y "%__ProjectDir%\packages\Microsoft.NETCore.Platforms\%__RuntimeJsonVersion%\runtime.json" "%__RuntimeJsonFile%")
popd

:: Clean up existing nupkgs
if exist "%__ProjectDir%\bin" (rmdir /s /q "%__ProjectDir%\bin")

:: Package the assets using Tools

"%__ProjectDir%\Tools\corerun" "%__ProjectDir%\Tools\MSBuild.exe" "%__ProjectDir%\projects\Microsoft.NETCore.DotNetHostPolicy.builds" /p:Platform=%__BuildArch% /p:DotNetHostBinDir=%__DotNetHostBinDir% /p:TargetsWindows=true /p:HostVersion=%__HostVer% /p:HostResolverVersion=%__FxrVer% /p:HostPolicyVersion=%__PolicyVer% /p:BuildNumberMajor=%__BuildMajor% /p:PreReleaseLabel=%__VersionTag% /verbosity:minimal
if not ERRORLEVEL 0 goto :Error

"%__ProjectDir%\Tools\corerun" "%__ProjectDir%\Tools\MSBuild.exe" "%__ProjectDir%\projects\Microsoft.NETCore.DotNetHostResolver.builds" /p:Platform=%__BuildArch% /p:DotNetHostBinDir=%__DotNetHostBinDir% /p:TargetsWindows=true /p:HostVersion=%__HostVer% /p:HostResolverVersion=%__FxrVer% /p:HostPolicyVersion=%__PolicyVer% /p:BuildNumberMajor=%__BuildMajor% /p:PreReleaseLabel=%__VersionTag% /verbosity:minimal
if not ERRORLEVEL 0 goto :Error

"%__ProjectDir%\Tools\corerun" "%__ProjectDir%\Tools\MSBuild.exe" "%__ProjectDir%\projects\Microsoft.NETCore.DotNetHost.builds" /p:Platform=%__BuildArch% /p:DotNetHostBinDir=%__DotNetHostBinDir% /p:TargetsWindows=true /p:HostVersion=%__HostVer% /p:HostResolverVersion=%__FxrVer% /p:HostPolicyVersion=%__PolicyVer% /p:BuildNumberMajor=%__BuildMajor% /p:PreReleaseLabel=%__VersionTag% /verbosity:minimal
if not ERRORLEVEL 0 goto :Error

exit /b 0

:Usage
echo.
echo Package the dotnet host artifacts
echo.
echo Usage:
echo     %__ThisScriptShort% [x64/x86/arm]  /hostbindir path-to-binaries /hostver /fxrver /policyver /build /vertag
echo.
echo./? -? /h -h /help -help: view this message.

:Error
echo An error occurred during packing.
exit /b 1
