@Echo OFF

REM Copyright (c) .NET Foundation and contributors. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

SETLOCAL
SET ERRORLEVEL=

"%~dp0dnx\dnx" "%~dp0dnx\lib\Microsoft.Dnx.Tooling\Microsoft.Dnx.Tooling.dll" %*

exit /b %ERRORLEVEL%
ENDLOCAL
