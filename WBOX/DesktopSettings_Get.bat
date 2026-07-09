@echo off

for /f "tokens=2,*" %%A in ('reg.exe query "HKCU\Control Panel\Colors" /v Background') do set "COLOR=%%B"
for /f "tokens=2,*" %%A in ('reg.exe query "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v EnableTransparency') do set "TRANSPARANCY=%%B"
for /f "tokens=2,*" %%A in ('reg.exe query "HKCU\Control Panel\Desktop" /v PaintDesktopVersion') do set "WATERMARK=%%B"
for /f "tokens=2,*" %%A in ('reg.exe query "HKCU\Control Panel\Desktop" /v Wallpaper') do set "WALLPAPER=%%B"

echo "%COLOR%" "%TRANSPARANCY%" "%WATERMARK%" "%WALLPAPER%"