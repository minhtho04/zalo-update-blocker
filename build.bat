@echo off
setlocal
cd /d "%~dp0"

echo ========================================================
echo   Compiling Zalo Update Blocker - Terminal Edition
echo ========================================================

set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [!] C# compiler csc.exe not found at: %CSC%
    pause
    exit /b 1
)

echo [*] Compiling ZaloBlocker.exe ...
"%CSC%" /target:exe /optimize+ /win32icon:block.ico /win32manifest:app.manifest /r:System.dll /r:System.Core.dll /r:System.Management.dll /r:System.Web.Extensions.dll /out:ZaloBlocker.exe Program.cs Properties\AssemblyInfo.cs

if %ERRORLEVEL% EQU 0 (
    echo [OK] Build SUCCESS! Generated file: ZaloBlocker.exe
) else (
    echo [ERROR] Build failed with code: %ERRORLEVEL%
)

echo ========================================================
pause
