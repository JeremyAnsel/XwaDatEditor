@echo off
setlocal

cd "%~dp0"

For %%a in (
"XwaDatEditor\bin\Release\*.dll"
"XwaDatEditor\bin\Release\*.exe"
"XwaDatEditor\bin\Release\*.config"
) do (
xcopy /s /d "%%~a" dist\
)

For %%a in (
"XwaDatExplorer\bin\Release\*.dll"
"XwaDatExplorer\bin\Release\*.exe"
"XwaDatExplorer\bin\Release\*.config"
) do (
xcopy /s /d "%%~a" dist\
)
