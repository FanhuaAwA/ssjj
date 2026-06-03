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
echo    SSJJ Injector - Protected Mode
echo ========================================
echo.

set "DLL_PATH=%~dp0UnityEngine.Components.dll"
set "MONO_DLL=D:\SSJJ-4399\battle\21_64\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll"

if not exist "%DLL_PATH%" (
    echo [!] DLL not found: %DLL_PATH%
    pause
    exit /b 1
)

echo [*] Target: UnityEngine.Components.dll (%DLL_PATH%)
for %%A in ("%DLL_PATH%") do echo     Size: %%~zA bytes
echo [*] Entry: UnityEngine.Components.SystemInitializer.Run()
echo.

REM Step 1: Patch GG's hook
echo ========================================
echo [1/3] Patching GG hook on mono_image_open_from_data...
echo ========================================
if exist "MonoHookPatcher.exe" (
    MonoHookPatcher.exe -d "%DLL_PATH%" -m "%MONO_DLL%"
    echo.
) else (
    echo [!] MonoHookPatcher.exe not found
    pause
    exit /b 1
)

REM Step 2: Inject managed DLL
echo ========================================
echo [2/3] Injecting DLL...
echo ========================================
if exist "bin\Injector.exe" (
    start "" "bin\Injector.exe" "%DLL_PATH%"
    echo [*] Injector launched - watch the game window
) else (
    echo [!] bin\Injector.exe not found
)

echo.
echo [*] Check if menu appears in game (Home key to toggle)
echo.
echo ========================================
echo [3/3] Press any key AFTER injection to restore GG hook...
echo        (this keeps GG heartbeats working normally)
echo ========================================
pause >nul

REM Step 3: Restore GG's hook
echo.
echo [*] Restoring GG hook...
MonoHookPatcher.exe -r
echo [*] Done.
