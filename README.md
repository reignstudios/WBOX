# WBOX
This is a Windows Consolizer tool supporting Win10, Win11, x64 and ARM64.<br>
This tool can significantly reduce memory (by GBs) and CPU/GPU cycles for games<br>
You can use this with any Windows flavor like Tiny10 or Tiny11 as well.<br>
Bypass explorer and even Win11 Xbox mode bloat if you only care about Steam for example.<br>

Steam, Playnite, GOG, Itch.io, Epic, Ubisoft, EA, Battlenet & Polymega should work.

## How to use
* Download and Install
* Click "Enable Game Mode" (This will modify a single reg value and reboot)
* NOTE: If the app fails to run you may need to disable "Smart App Control"
* You can config Windows to skip login etc
* Steam App Left/Right menus can be opened by holding Back/Select button then tapping Left/Right Bumper
* In Control-Center, you can use a Gamepad to navigate
* I suggest making Steam default boot into BigPicture mode (better after it updates). And disable Steams auto-start.

### Other info
The reg value changed is "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell" (yes thats it)<br>
If you ever need to manually revert simply do these steps (but Control Center does it for you)
* Open Task-Manager (can be done with Ctrl+Alt+Del)
* Click "Run new task" and enter "regedit"
* Navigate to the "Shell" key listed above and change it to explorer.exe
* Click "Run new task" again and enter "explorer.exe"
* Everything should work as normal again.

![Screenshots](./WBOX/Logo.jpg)