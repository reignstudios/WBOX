# WBOX
This is a Windows Consolizer tool supporting Win10, Win11, x86, x64, ARM32 and ARM64 (should also work on Win7 & Win8)<br>
This tool can significantly reduce memory (by GBs) and CPU/GPU cycles for games<br>
You can use this with any Windows flavor like Tiny10 or Tiny11 as well.<br>
Bypass explorer and even Win11 Xbox mode bloat if you only care about Steam for example.<br>

Steam, Playnite, GOG, Itch.io, Epic, Ubisoft, EA, Battlenet & Polymega should work.

## How to use
* You can config Windows to skip login etc (optional: go to <u>Settings→Accounts→Sign-in</u> options and disable login after sleep)
* Steam App Left/Right menus can be opened by holding Back/Select button then tapping Left/Right Bumper
* In Control-Center, you can use a Gamepad to navigate
* I suggest making Steam default boot into BigPicture mode (better after it updates). And disable Steams auto-start.

## Portable/Optimized Install (Win10, Win11 etc)
* Make sure VC++ redist is installed for your system architecture: [Download VC++ Redist](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-supported-redistributable-version)
* Download and Install (For upgrading, uninstall first)
* Click "Enable Game Mode" (This will modify a single reg value and reboot)
* NOTE: If the app fails to run you may need to disable "Smart App Control"
* Optional: Use HHC as an alternative for TDP control (no FSE requied) [HHC Download](https://github.com/Valkirie/HandheldCompanion)

## FSE [Full Screen Experience] / Xbox Mode (Win11 Only)
* You can install this tool in FSE mode giving you extended features (WinStore, Xbox bar etc)
* NOTE: If this feature is not enabled, you can enable it with this tool: [XboxFullscreenExperienceTool](https://github.com/8bit2qubit/XboxFullscreenExperienceTool)
* Make sure VC++ redist is installed for your system architecture: [Download VC++ Redist](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#latest-supported-redistributable-version)
* You must put Win11 into Developer Mode: Settings => "System/Advanced/Developer Mode"
* Download and Install (For upgrading, uninstall first)
* Then run "Install_WBOX_FSE.bat"
* Now got to: Settings => "Gaming/XBOX mode"
* Under "Choose home app" select WBOX.FSE
* Then enable "Enter XBOX app on startup"
* Reboot
* NOTE: Left/Right menu commands don't work from gamepad in FSE mode (Unless you use HHC) [HHC Download](https://github.com/Valkirie/HandheldCompanion)

### Gotchas
* If Steams window is not fully in focus it may not detect virtual menu buttons (just click/tap it to focus it)

## HDR
* WBOX has a Auto-HDR tool which will read EDID and create a calibration for you
* You can test its results using this tool: https://www.wide-gamut.com/test/image-hdr

## Optional: Virtual HID
* If Left/Right menus are not working well you can install: libvirtualhid (v2026.901.116.32)
* Download and install the driver: [VirtualHID Download](https://github.com/LizardByte/libvirtualhid/releases#release-v2026.901.116.32)
* WBOX will auto detect and use it if available

### Other info
The reg value changed is "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\Shell" (yes thats it, unless you're using FSE)<br>
If you ever need to manually revert simply do these steps (but Control Center does it for you)
* Open Task-Manager (can be done with Ctrl+Alt+Del)
* Click "Run new task" and enter "regedit"
* Navigate to the "Shell" key listed above and change it to explorer.exe
* Click "Run new task" again and enter "explorer.exe"
* Everything should work as normal again.

### Build
* VS 2026
* .NETFW 4.7.2
* VC++
* For VirtualHID
    - Create a folder next to WBOX called "WBOX_VirtualDriver" then open terminal
    - git clone --recurse-submodules --branch v2026.829.2338.54 --depth 1 https://github.com/LizardByte/libvirtualhid.git
    - cmake -S . -B build-vs2026 -G "Visual Studio 18 2026" -A x64 -DBUILD_TESTS=OFF -DBUILD_EXAMPLES=ON -DBUILD_DOCS=OFF -DLIBVIRTUALHID_BUILD_TOOLS=ON -DLIBVIRTUALHID_BUILD_WINDOWS_DRIVER=OFF -DLIBVIRTUALHID_INSTALL=ON
    - cmake --build build-vs2026 --config Debug --target libvirtualhid
    - cmake --build build-vs2026 --config Release --target libvirtualhid

![Screenshots](./WBOX/Logo.jpg)