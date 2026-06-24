@echo off
echo "Init (Do not close this Window)"

:: Hide bat
:: if "%~1"=="hidden" goto :main
:: echo Set WshShell = CreateObject("WScript.Shell") > "%~n0.vbs"
:: echo WshShell.Run chr(34) ^& "%~f0" ^& Chr(34) ^& " hidden", 0 >> "%~n0.vbs"
:: echo Set WshShell = Nothing >> "%~n0.vbs"
:: cscript //nologo "%~n0.vbs"
:: del "%~n0.vbs"
:: exit
:: :main

:: Min bat window
if not "%~1"=="minimized" (
    start "" /min "%~f0" minimized
    exit
)

:: Relaunch loop
:loop
echo "Launching Control Center..."
"C:\Dev\WBOX\WBOX\bin\Debug\WBOX.exe"
echo "Control Center quit (will relaunch)"
timeout /t 2 >nul
goto loop