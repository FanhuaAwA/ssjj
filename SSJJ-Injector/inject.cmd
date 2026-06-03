@echo off
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [*] Requesting Administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"
title SSJJ Injector

echo ========================================
echo    SSJJ Plugin Injector - GameHelper
echo ========================================
echo.

set "DLL_PATH=%~dp0GameHelper.dll"

if not exist "%DLL_PATH%" (
    echo [!] GameHelper.dll not found at: %DLL_PATH%
    pause
    exit /b 1
)

echo [*] DLL: %DLL_PATH%
echo [*] Size:
for %%A in ("%DLL_PATH%") do echo     %%~zA bytes
echo.
echo [*] Mode: Pure Reflection (No MonoMod Hooks)
echo [*] Entry: SSJJPlugin.Loader.Load()
echo.

if exist "bin\Injector.exe" (
    echo [*] Launching Injector...
    bin\Injector.exe "%DLL_PATH%"
) else (
    echo [!] Injector.exe not found
)

echo.
pause
