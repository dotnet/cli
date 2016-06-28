@echo off

REM Copyright (c) .NET Foundation and contributors. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

REM The Architecture parameter is the 4th item in the space delimited list, so check for it
IF "%4"=="" (
  set ARCHITECTURE="%PROCESSOR_ARCHITECTURE%"
) else (
  set ARCHITECTURE=%4
)

%~dp0init-tools.cmd %ARCHITECTURE%
if %errorlevel% neq 0 exit /b %errorlevel%

powershell -NoProfile -NoLogo -Command "%~dp0build_projects\dotnet-cli-build\build.ps1 %*; exit $LastExitCode;"
if %errorlevel% neq 0 exit /b %errorlevel%
