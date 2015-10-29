@echo off

echo Build Number - %BUILD_NUMBER%

CALL %~dp0..\build.cmd %*

exit /b %errorlevel%
