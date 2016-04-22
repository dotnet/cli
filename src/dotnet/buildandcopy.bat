@echo off
set clirepo=x:\oss\cli-2343-redo
set cliver=1.0.0-rc2-002481
dotnet build && copy %clirepo%\src\dotnet\bin\Debug\netcoreapp1.0\* %clirepo%\artifacts\win10-x64\stage2\sdk\%cliver%\ /Y
