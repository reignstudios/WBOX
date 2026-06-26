using System;
    using System.Runtime.InteropServices;
    using System.Threading;

namespace WBOX
{
    static class MouseSimulator
    {
        [Flags]
        private enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_ABSOLUTE = 0x8000,
            MOUSEEVENTF_WHEEL = 0x0800
        }

        public static void MoveMouse(int deltaX, int deltaY)//, int steps = 1)//, int delayMs = 5)
        {
            //if (steps <= 1)
            //{
            //    SendMouseInput(deltaX, deltaY, MouseEventFlags.MOUSEEVENTF_MOVE);
            //    return;
            //}

            // Smooth movement with velocity feel
            //int stepX = deltaX / steps;
            //int stepY = deltaY / steps;
            SendMouseInput(deltaX, deltaY, MouseEventFlags.MOUSEEVENTF_MOVE);

            /*for (int i = 0; i < steps; i++)
            {
                SendMouseInput(stepX, stepY, MouseEventFlags.MOUSEEVENTF_MOVE);
                if (delayMs > 0)
                    Thread.Sleep(delayMs);
            }*/

            // Final correction step
            /*int remainderX = deltaX - (stepX * steps);
            int remainderY = deltaY - (stepY * steps);
            if (remainderX != 0 || remainderY != 0) SendMouseInput(remainderX, remainderY, MouseEventFlags.MOUSEEVENTF_MOVE);*/
        }

        public static void MoveMouseAbsolute(int x, int y)
        {
            SendMouseInput(x, y, MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE);
        }

        public static void LeftClick()
        {
            SendMouseInput(0, 0, MouseEventFlags.MOUSEEVENTF_LEFTDOWN);
            Thread.Sleep(10);                    // Small delay for realism
            SendMouseInput(0, 0, MouseEventFlags.MOUSEEVENTF_LEFTUP);
        }

        public static void LeftDown()
        {
            SendMouseInput(0, 0, MouseEventFlags.MOUSEEVENTF_LEFTDOWN);
        }

        public static void LeftUp()
        {
            SendMouseInput(0, 0, MouseEventFlags.MOUSEEVENTF_LEFTUP);
        }

        private static void SendMouseInput(int dx, int dy, MouseEventFlags flags)
        {
            INPUT[] input = new INPUT[1];
            input[0].type = InputType.INPUT_MOUSE;
            input[0].U.mi.dx = dx;
            input[0].U.mi.dy = dy;
            input[0].U.mi.dwFlags = (uint)flags;
            input[0].U.mi.time = 0;
            input[0].U.mi.dwExtraInfo = IntPtr.Zero;

            KeyboardSimulator.SendInput(1, input, INPUT.Size);
        }

        // === Keep your existing Keyboard methods (KeyDown, KeyUp, PressKey, etc.) ===
    }
}
