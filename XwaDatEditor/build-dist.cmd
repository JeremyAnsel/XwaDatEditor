@echo off
setlocal

cd "%~dp0"

For %%a in (
"XwaDatEditor\bin\Release\net48\*.dll"
"XwaDatEditor\bin\Release\net48\*.exe"
"XwaDatEditor\bin\Release\net48\*.config"
) do (
xcopy /s /d "%%~a" dist\
)

For %%a in (
"XwaDatExplorer\bin\Release\net48\*.dll"
"XwaDatExplorer\bin\Release\net48\*.exe"
"XwaDatExplorer\bin\Release\net48\*.config"
) do (
xcopy /s /d "%%~a" dist\
)
