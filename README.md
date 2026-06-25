# WBOX
This is a Windows Consolizer tool supporting Win10, Win11, x64 and AM64.<br>
This tool can significantly reduce memory (by GBs) and CPU/GPU cycles for games<br>
You can use this with any Windows flavor like Tiny10 or Tiny11 as well.<br>

Currently this is considered alpha BUT Steam is fully working.<br>
The plan is to allow you to run other non-Steam games/apps/launchers as well.

## How to use
* Place the files in a place where you will not move them
* Run WBOX.exe
* Click "Enable Game Mode" (This will modify a single reg value and reboot)
* NOTE: If the app fails to run you may need to disable "Smart App Control"

### Other info
The reg value changed is "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell" (yes thats it)<br>
If you ever need to manually revert simply do these steps
* Open Task-Manager (can be done with Ctrl+Alt+Del)
* Click "Run new task" and enter "regedit"
* Navigate to the "Shell" key listed above and change it to explorer.exe
* Click "Run new task" again and enter "explorer.exe"
* Everything should work as normal again.

![Screenshots](./WBOX/Logo.jpg)