using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WBOX
{
	static class KeyboardSimulator
	{
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public const ushort VK_1 = 0x31;
        public const ushort VK_2 = 0x32;
        public const ushort VK_LCONTROL = 0xA2;
        public const ushort VK_LSHIFT = 0xA0;
        public const ushort VK_TAB = 0x09;
        public const ushort VK_LWIN = 0x5B;
        public const ushort VK_G = 0x47;

        public const ushort SC_1 = 0x02;
        public const ushort SC_2 = 0x03;
        public const ushort SC_LCONTROL = 0x1D;
        public const ushort SC_LSHIFT = 0x2A;
        public const ushort SC_TAB = 0x0F;

        public static void KeyDown(ushort virtualKey, bool extended = false)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wVk = virtualKey;
            if (extended) input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput(1, input, INPUT.Size);
        }

        public static void KeyUp(ushort virtualKey, bool extended = false)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wVk = virtualKey;
            input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_KEYUP;
            if (extended) input[0].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput(1, input, INPUT.Size);
        }

        // Virtual Key Codes: https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        public static void PressKey(ushort virtualKey, bool extended = false)
        {
            var input = new INPUT[2];

            // Key down
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wVk = virtualKey;
            if (extended) input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            // Key up
            input[1].type = InputType.INPUT_KEYBOARD;
            input[1].U.ki.wVk = virtualKey;
            input[1].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_KEYUP;
            if (extended) input[1].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput((uint)input.Length, input, INPUT.Size);
        }

        public static void KeyDownScan(ushort scanKey, bool extended = false)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wScan = scanKey;
            input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_SCANCODE;
            if (extended) input[0].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput(1, input, INPUT.Size);
        }

        public static void KeyUpScan(ushort scanKey, bool extended = false)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wScan = scanKey;
            input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_KEYUP | KeyEventFlags.KEYEVENTF_SCANCODE;
            if (extended) input[0].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput(1, input, INPUT.Size);
        }

        // Virtual Key Codes: https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        public static void PressKeyScan(ushort scanKey, bool extended = false)
        {
            var input = new INPUT[2];

            // Key down
            input[0].type = InputType.INPUT_KEYBOARD;
            input[0].U.ki.wScan = scanKey;
            input[0].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_SCANCODE;
            if (extended) input[0].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            // Key up
            input[1].type = InputType.INPUT_KEYBOARD;
            input[1].U.ki.wScan = scanKey;
            input[1].U.ki.dwFlags = KeyEventFlags.KEYEVENTF_KEYUP | KeyEventFlags.KEYEVENTF_SCANCODE;
            if (extended) input[1].U.ki.dwFlags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

            SendInput((uint)input.Length, input, INPUT.Size);
        }

        public static void SendText(string text)
        {
            foreach (char c in text)
            {
                // For characters, you can use scan codes or Unicode, but simple keys via VK is often easier
                ushort vk = (ushort)char.ToUpper(c); // Rough mapping for basic chars
                PressKey(vk);
            }
        }
	}

    [Flags]
    enum InputType : uint
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }

    [Flags]
    enum KeyEventFlags : uint
    {
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        KEYEVENTF_KEYUP = 0x0002,
        KEYEVENTF_SCANCODE = 0x0008,
        KEYEVENTF_UNICODE = 0x0004
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public InputType type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;      // Virtual key code
        public ushort wScan;    // Scan code
        public KeyEventFlags dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
