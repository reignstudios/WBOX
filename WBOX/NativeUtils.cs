using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace WBOX
{
	static unsafe class NativeUtils
	{
		private const string lib = "WBOX_NativeUtils.dll";

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern int WBOX_NativeUtils_InitInput();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_NativeUtils_DisposeInput();

		[DllImport(lib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		private static extern void WBOX_NativeUtils_UpdateInput(WBOX_Gamepad* gamepad);

		[StructLayout(LayoutKind.Sequential)]
		public struct WBOX_Gamepad
		{
			public int connected;
			public int a, b, x, y;
			public int dpadLeft, dpadRight, dpadDown, dpadUp;
			public int back, menu;
			public int bumperLeft, bumperRight;
			public int joystickButtonLeft, joystickButtonRight;
			public float joystickLeftX, joystickLeftY, joystickRightX, joystickRightY;
			public float triggerLeft, triggerRight;
		}

		public class Device
		{
			public bool Connected;

			public GamepadButton A = new GamepadButton();
			public GamepadButton B = new GamepadButton();
			public GamepadButton X = new GamepadButton();
			public GamepadButton Y = new GamepadButton();

			public GamepadButton DPadLeft = new GamepadButton();
			public GamepadButton DPadRight = new GamepadButton();
			public GamepadButton DPadDown = new GamepadButton();
			public GamepadButton DPadUp = new GamepadButton();

			public GamepadButton Back = new GamepadButton();
			public GamepadButton Menu = new GamepadButton();

			public GamepadButton BumperLeft = new GamepadButton();
			public GamepadButton BumperRight = new GamepadButton();

			public GamepadButton JoystickButtonLeft = new GamepadButton();
			public GamepadButton JoystickButtonRight = new GamepadButton();

			public Axis2D JoystickLeft = new Axis2D();
			public Axis2D JoystickRight = new Axis2D();

			public GamepadButton TriggerButtonLeft = new GamepadButton();
			public GamepadButton TriggerButtonRight = new GamepadButton();

			public Axis1D TriggerLeft = new Axis1D(Axis1DUpdateMode.Positive);
			public Axis1D TriggerRight = new Axis1D(Axis1DUpdateMode.Positive);

			public void Update(in WBOX_Gamepad gamepad)
			{
				Connected = gamepad.connected != 0;

				A.Update(gamepad.a != 0);
				B.Update(gamepad.b != 0);
				X.Update(gamepad.x != 0);
				Y.Update(gamepad.y != 0);

				DPadLeft.Update(gamepad.dpadLeft != 0);
				DPadRight.Update(gamepad.dpadRight != 0);
				DPadDown.Update(gamepad.dpadDown != 0);
				DPadUp.Update(gamepad.dpadUp != 0);

				Back.Update(gamepad.back != 0);
				Menu.Update(gamepad.menu != 0);

				BumperLeft.Update(gamepad.bumperLeft != 0);
				BumperRight.Update(gamepad.bumperRight != 0);

				JoystickButtonLeft.Update(gamepad.joystickButtonLeft != 0);
				JoystickButtonRight.Update(gamepad.joystickButtonRight != 0);

				JoystickLeft.Update(new Vec2(gamepad.joystickLeftX, gamepad.joystickLeftY));
				JoystickRight.Update(new Vec2(gamepad.joystickRightX, gamepad.joystickRightY));

				TriggerLeft.Update(gamepad.triggerLeft);
				TriggerRight.Update(gamepad.triggerRight);
				TriggerButtonLeft.Update(gamepad.triggerLeft >= .75f);// trigger button left
				TriggerButtonRight.Update(gamepad.triggerRight >= .75f);// trigger button right
			}
		}

		private static Device gamepadDevice;

		public static bool InitInput()
		{
			try
			{
				gamepadDevice = new Device();
				return WBOX_NativeUtils_InitInput() != 0;
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
			return false;
		}

		public static void DisposeInput()
		{
			try
			{
				WBOX_NativeUtils_DisposeInput();
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/
		}

		public static Device UpdateInput()
		{
			try
			{
				WBOX_Gamepad gamepad;
				WBOX_NativeUtils_UpdateInput(&gamepad);
				gamepadDevice.Update(gamepad);
				return gamepadDevice;
			}
			catch { }
			/*catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}*/

			return null;
		}
	}
}
