@echo off
echo "Desktop Settings configuring..."

set "COLOR=%~1"
set "TRANSPARANCY=%~2"
set "WATERMARK=%~3"
set "WALLPAPER=%~4"
set "LIGHTTHEME=%~5"

:: echo "%COLOR%" %TRANSPARANCY% %WATERMARK% "%WALLPAPER%"
:: pause

reg.exe add "HKCU\Control Panel\Colors" /v Background /t REG_SZ /d "%COLOR%" /f
reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v EnableTransparency /t REG_DWORD /d "%TRANSPARANCY%" /f
reg.exe add "HKCU\Control Panel\Desktop" /v PaintDesktopVersion /t REG_DWORD /d "%WATERMARK%" /f
reg.exe add "HKCU\Control Panel\Desktop" /v Wallpaper /t REG_SZ /d "%WALLPAPER%" /f
reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v AppsUseLightTheme /t REG_SZ /d "%LIGHTTHEME%" /f
reg.exe add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v SystemUsesLightTheme /t REG_SZ /d "%LIGHTTHEME%" /f