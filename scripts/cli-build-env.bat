@echo off
REM Copyright (c) .NET Foundation and contributors. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

REM Get normalized version of parent path
for %%i in (%~dp0..\) DO (
    SET CLI_REPO_ROOT=%%~dpi
)

title CLI Build (%CLI_REPO_ROOT%)

REM Add Stage 0 CLI to path
set PATH=%CLI_REPO_ROOT%.dotnet_stage0\x64;%PATH%

set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_MULTILEVEL_LOOKUP=0
set NUGET_PACKAGES=%CLI_REPO_ROOT%.nuget\packages