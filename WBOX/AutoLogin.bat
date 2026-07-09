@echo off
echo "Auto Login configuring..."

set "ENABLE_BOOL=%~1"
set "USER_NAME=%~2"
set "USER_PASS=%~3"

reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v AutoAdminLogon /t REG_SZ /d "%ENABLE_BOOL%" /f
reg.exe add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v DisableAutomaticRestartSignOn /t REG_SZ /d "%ENABLE_BOOL%" /f
reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultUserName /t REG_SZ /d "%USER_NAME%" /f
reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon" /v DefaultPassword /t REG_SZ /d "%USER_PASS%" /f