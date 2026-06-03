@echo off
title SSJJ Injector
cd /d "%~dp0"
echo ========================================
echo        SSJJ Plugin Injector v2.0
echo ========================================
echo.
echo Make sure the game is running and you are in a match room.
echo.
pause
bin\Injector.exe "%~dp0GameHelper.dll"
