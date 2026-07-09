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
:: if not "%~1"=="minimized" (
::     start "" /min "%~f0" minimized
::     exit
:: )

:: Relaunch loop
:: :loop
:: echo "Launching Control Center..."
:: "D:\Dev\Reign\WBOX\WBOX\bin\Debug\WBOX.exe"
:: echo "Control Center quit (will relaunch)"
:: timeout /t 2 >nul
:: goto loop

:: Launch Explorer hidden
setlocal
echo "Launching Explorer...
start /min explorer.exe

:: Wait for Explorer boot
timeout /t 10 /nobreak >nul

:: Launch WBOX
echo "Launching WBOX...
start "D:\Dev\Reign\WBOX\WBOX\bin\Debug\WBOX.exe"

:: Keep this bat task alive
:loop
timeout /t 60 >nul
goto loop
endlocal