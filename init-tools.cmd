@if "%_echo%" neq "on" echo off
setlocal

:CheckOS
IF "%PROCESSOR_ARCHITECTURE%"=="x86" (set bit=x86) else (set bit=x64)

set INIT_TOOLS_LOG=%~dp0init-tools.log
if [%PACKAGES_DIR%]==[] set PACKAGES_DIR=%~dp0packages\
if [%TOOLRUNTIME_DIR%]==[] set TOOLRUNTIME_DIR=%~dp0build_tools
set DOTNET_PATH=%~dp0.dotnet_stage0\Windows\%bit%\
if [%DOTNET_CMD%]==[] set DOTNET_CMD=%DOTNET_PATH%dotnet.exe
if [%BUILDTOOLS_SOURCE%]==[] set BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json
set /P BUILDTOOLS_VERSION=< "%~dp0BuildToolsVersion.txt"
set BUILD_TOOLS_PATH=%PACKAGES_DIR%Microsoft.DotNet.BuildTools\%BUILDTOOLS_VERSION%\lib\
set PROJECT_JSON_PATH=%TOOLRUNTIME_DIR%\%BUILDTOOLS_VERSION%
set PROJECT_JSON_FILE=%PROJECT_JSON_PATH%\project.json
set PROJECT_JSON_CONTENTS={ "dependencies": { "Microsoft.DotNet.BuildTools": "%BUILDTOOLS_VERSION%" }, "frameworks": { "netcoreapp1.0": { } } }
set BUILD_TOOLS_SEMAPHORE=%PROJECT_JSON_PATH%\init-tools.completed
set DOTNET_INSTALL_DIR=%~dp0.dotnet_stage0

if DEFINED "%CHANNEL%" (
  set SCRIPT="%~dp0scripts\obtain\dotnet-install.ps1 -Channel %CHANNEL% -Architecture %bit% -InstallDir \"%DOTNET_PATH%\""
) else (
  set SCRIPT="%~dp0scripts\obtain\dotnet-install.ps1 -Architecture %bit% -InstallDir \"%DOTNET_PATH%\""
)

:: if force option is specified then clean the tool runtime and build tools package directory to force it to get recreated
if [%1]==[force] (
  if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"
  if exist "%PACKAGES_DIR%Microsoft.DotNet.BuildTools" rmdir /S /Q "%PACKAGES_DIR%Microsoft.DotNet.BuildTools"
)

:: If sempahore exists do nothing
if exist "%BUILD_TOOLS_SEMAPHORE%" (
  echo Tools are already initialized.
  goto :EOF
)

if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"

if NOT exist "%PROJECT_JSON_PATH%" mkdir "%PROJECT_JSON_PATH%"
echo %PROJECT_JSON_CONTENTS% > "%PROJECT_JSON_FILE%"
echo Running %0 > "%INIT_TOOLS_LOG%"

if NOT exist "%DOTNET_CMD%" (
  if NOT exist "%DOTNET_INSTALL_DIR%" mkdir "%DOTNET_INSTALL_DIR%"
  if NOT exist "%PACKAGES_DIR%" mkdir "%PACKAGES_DIR%"

  echo %SCRIPT%
  powershell -NoProfile -NoLogo -Command %SCRIPT% "-Verbose; exit $LastExitCode;"
  if %errorlevel% neq 0 exit /b %errorlevel%
  setx DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1
)

:afterdotnetrestore

if exist "%BUILD_TOOLS_PATH%" goto :afterbuildtoolsrestore
echo Restoring BuildTools version %BUILDTOOLS_VERSION%...
echo Running: "%DOTNET_CMD%" restore "%PROJECT_JSON_FILE%" --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" >> "%INIT_TOOLS_LOG%"
call "%DOTNET_CMD%" restore "%PROJECT_JSON_FILE%" --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" >> "%INIT_TOOLS_LOG%"
if NOT exist "%BUILD_TOOLS_PATH%init-tools.cmd" (
  echo ERROR: Could not restore build tools correctly. See '%INIT_TOOLS_LOG%' for more details.
  exit /b 1
)

:afterbuildtoolsrestore

echo Initializing BuildTools ...
echo Running: "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
call "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
set INIT_TOOLS_ERRORLEVEL=%ERRORLEVEL%
if not [%INIT_TOOLS_ERRORLEVEL%]==[0] (
	echo ERROR: An error occured when trying to initialize the tools. Please check '%INIT_TOOLS_LOG%' for more details.
	exit /b %INIT_TOOLS_ERRORLEVEL%
)

:: Create sempahore file
echo Done initializing tools.
echo Init-Tools.cmd completed for BuildTools Version: %BUILDTOOLS_VERSION% > "%BUILD_TOOLS_SEMAPHORE%"