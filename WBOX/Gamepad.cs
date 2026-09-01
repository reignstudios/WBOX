using System.Runtime.InteropServices;
using System.Text;

using HMODULE = System.IntPtr;
using FARPROC = System.IntPtr;
using DWORD = System.UInt32;
using WORD = System.UInt16;
using BYTE = System.Byte;
using System.Collections.Generic;
using System;

namespace WBOX
{
	public enum InstanceVersion
	{
		/// <summary>
		/// Legacy version
		/// </summary>
		XInput_1_1,

		/// <summary>
		/// Legacy version
		/// </summary>
		XInput_1_2,

		/// <summary>
		/// Support started with Windows XP
		/// </summary>
		XInput_1_3,

		/// <summary>
		/// Support started with Windows 8
		/// </summary>
		XInput_1_4,

		/// <summary>
		/// Limited feature lib starting in Vista for broad compatibility
		/// </summary>
		XInput_9_1_0,
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XINPUT_GAMEPAD
	{
		public ushort wButtons;
		public byte bLeftTrigger;
		public byte bRightTrigger;
		public short sThumbLX;
		public short sThumbLY;
		public short sThumbRX;
		public short sThumbRY;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XINPUT_STATE
	{
		public DWORD dwPacketNumber;
		public XINPUT_GAMEPAD Gamepad;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XINPUT_VIBRATION
	{
		public WORD wLeftMotorSpeed;
		public WORD wRightMotorSpeed;
	}

	enum XINPUT_DEVSUBTYPE : byte
	{
		UNKNOWN = 0,
		GAMEPAD,
		WHEEL,
		ARCADE_STICK,
		FLIGHT_STICK,
		DANCE_PAD,
		GUITAR,
		GUITAR_ALTERNATE,
		GUITAR_BASS,
		DRUM_KIT,
		ARCADE_PAD
	}

	[StructLayout(LayoutKind.Sequential)]
	struct XINPUT_CAPABILITIES
	{
		public BYTE Type;
		public BYTE SubType;
		public WORD Flags;
		public XINPUT_GAMEPAD Gamepad;
		public XINPUT_VIBRATION Vibration;
	}

	public sealed unsafe class XInputInstance
	{
		private const string lib_1_1 = "xinput1_1.dll";
		private const string lib_1_2 = "xinput1_2.dll";
		private const string lib_1_3 = "xinput1_3.dll";
		private const string lib_1_4 = "xinput1_4.dll";
		private const string lib_9_1_0 = "XInput9_1_0.dll";

		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern HMODULE LoadLibraryW(char* lpLibFileName);

		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
		private static extern FARPROC GetProcAddress(HMODULE hModule, byte* lpProcName);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		internal delegate DWORD XInputGetState_Method(DWORD dwUserIndex, XINPUT_STATE* pState);
		internal XInputGetState_Method XInputGetState;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		internal delegate DWORD XInputSetState_Method(DWORD dwUserIndex, XINPUT_VIBRATION* pVibration);
		internal XInputSetState_Method XInputSetState;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		internal delegate DWORD XInputGetCapabilities_Method(DWORD dwUserIndex, DWORD dwFlags, XINPUT_CAPABILITIES* pCapabilities);
		internal XInputGetCapabilities_Method XInputGetCapabilities;

		public InstanceVersion version { get; private set; }

		/// <summary>
		/// 4 devices max
		/// </summary>
		public XInputDevice[] devices;

		public XInputInstance()
		{
			devices = new XInputDevice[4];
			for (int i = 0; i != devices.Length; ++i) devices[i] = new XInputDevice(this, i);
		}

		public bool Init()
		{
			// test for v1.4
			fixed (char* libName = lib_1_4)
			{
				var library = LoadLibraryW(libName);
				if (library != HMODULE.Zero)
				{
					version = InstanceVersion.XInput_1_4;
					return LoadMethods(library);
				}
			}

			// test for v1.3
			fixed (char* libName = lib_1_3)
			{
				var library = LoadLibraryW(libName);
				if (library != HMODULE.Zero)
				{
					version = InstanceVersion.XInput_1_3;
					return LoadMethods(library);
				}
			}

			// test for v1.2
			fixed (char* libName = lib_1_2)
			{
				var library = LoadLibraryW(libName);
				if (library != HMODULE.Zero)
				{
					version = InstanceVersion.XInput_1_2;
					return LoadMethods(library);
				}
			}

			// test for v1.1
			fixed (char* libName = lib_1_1)
			{
				var library = LoadLibraryW(libName);
				if (library != HMODULE.Zero)
				{
					version = InstanceVersion.XInput_1_1;
					return LoadMethods(library);
				}
			}

			// test for v9.1.0
			fixed (char* libName = lib_9_1_0)
			{
				var library = LoadLibraryW(libName);
				if (library != HMODULE.Zero)
				{
					version = InstanceVersion.XInput_9_1_0;
					return LoadMethods(library);
				}
			}

			return false;
		}

		private bool LoadMethods(HMODULE library)
		{
			FARPROC address;
			byte[] name;

			name = Encoding.ASCII.GetBytes("XInputGetState");
			fixed (byte* namePtr = name) address = GetProcAddress(library, namePtr);
			if (address == FARPROC.Zero) return false;
			XInputGetState = Marshal.GetDelegateForFunctionPointer<XInputGetState_Method>(address);

			name = Encoding.ASCII.GetBytes("XInputSetState");
			fixed (byte* namePtr = name) address = GetProcAddress(library, namePtr);
			if (address == FARPROC.Zero) return false;
			XInputSetState = Marshal.GetDelegateForFunctionPointer<XInputSetState_Method>(address);

			// NOTE: only some APIs support this function
			name = Encoding.ASCII.GetBytes("XInputGetCapabilities");
			fixed (byte* namePtr = name) address = GetProcAddress(library, namePtr);
			if (address != FARPROC.Zero) XInputGetCapabilities = Marshal.GetDelegateForFunctionPointer<XInputGetCapabilities_Method>(address);

			return true;
		}

		public void Update()
		{
			foreach (var device in devices)
			{
				device.Update();
			}
		}
	}

	public sealed unsafe class XInputDevice
	{
		public XInputInstance instance { get; private set; }
		public int typeID;
		public bool connected;

		/// <summary>
		/// The device index
		/// </summary>
		public int index { get; private set; }

		public GamepadButton A, B, X, Y;
		public GamepadButton DpadLeft, DpadRight, DpadDown, DpadUp;
		public GamepadButton Menu, Back;
		public GamepadButton BumperLeft, BumperRight;
		public GamepadButton TriggerButtonLeft, TriggerButtonRight;
		public GamepadButton JoystickButtonLeft, JoystickButtonRight;
		public Axis1D TriggerLeft, TriggerRight;
		public Axis2D JoystickLeft, JoystickRight;

		public XInputDevice(XInputInstance instance, int index)
		{
			this.instance = instance;
			this.index = index;

			A = new GamepadButton();
			B = new GamepadButton();
			X = new GamepadButton();
			Y = new GamepadButton();

			DpadLeft = new GamepadButton();
			DpadRight = new GamepadButton();
			DpadDown = new GamepadButton();
			DpadUp = new GamepadButton();

			Menu = new GamepadButton();
			Back = new GamepadButton();

			BumperLeft = new GamepadButton();
			BumperRight = new GamepadButton();

			TriggerButtonLeft = new GamepadButton();
			TriggerButtonRight = new GamepadButton();

			JoystickButtonLeft = new GamepadButton();
			JoystickButtonRight = new GamepadButton();

			TriggerLeft = new Axis1D(Axis1DUpdateMode.Positive);
			TriggerRight = new Axis1D(Axis1DUpdateMode.Positive);

			JoystickLeft = new Axis2D();
			JoystickRight = new Axis2D();
		}

		public void Update()
		{
			// get device state
			XINPUT_STATE state;
			bool connected = instance.XInputGetState((DWORD)index, &state) == 0;

			// validate is connected
			if (connected != this.connected)
			{
				if (connected) RefreshDeviceInfo();
			}
			this.connected = connected;
			if (!connected) return;

			// grab gamepad state
			var gamepad = state.Gamepad;

			// primary buttons
			A.Update((gamepad.wButtons & 0x1000) != 0);// 1
			B.Update((gamepad.wButtons & 0x2000) != 0);// 2
			X.Update((gamepad.wButtons & 0x4000) != 0);// 3
			Y.Update((gamepad.wButtons & 0x8000) != 0);// 4

			// dpad
			DpadLeft.Update((gamepad.wButtons & 0x0004) != 0);// left
			DpadRight.Update((gamepad.wButtons & 0x0008) != 0);// right
			DpadDown.Update((gamepad.wButtons & 0x0002) != 0);// down
			DpadUp.Update((gamepad.wButtons & 0x0001) != 0);// up

			// options
			Menu.Update((gamepad.wButtons & 0x0010) != 0);// menu
			Back.Update((gamepad.wButtons & 0x0020) != 0);// back

			// bumbers
			BumperLeft.Update((gamepad.wButtons & 0x0100) != 0);// bumper left
			BumperRight.Update((gamepad.wButtons & 0x0200) != 0);// bumper right

			// trigger buttons
			float triggerLeftValue = gamepad.bLeftTrigger / 255f;
			float triggerRightValue = gamepad.bRightTrigger / 255f;
			TriggerButtonLeft.Update(triggerLeftValue >= .75f);// trigger button left
			TriggerButtonRight.Update(triggerRightValue >= .75f);// trigger button right

			// joystick buttons
			JoystickButtonLeft.Update((gamepad.wButtons & 0x0040) != 0);// joystick button left
			JoystickButtonRight.Update((gamepad.wButtons & 0x0080) != 0);// joystick button right

			// triggers
			TriggerLeft.Update(triggerLeftValue);// trigger left
			TriggerRight.Update(triggerRightValue);// trigger right

			// joysticks
			JoystickLeft.Update(new Vec2(gamepad.sThumbLX / (float)short.MaxValue, gamepad.sThumbLY / (float)short.MaxValue));// joystick left
			JoystickRight.Update(new Vec2(gamepad.sThumbRX / (float)short.MaxValue, gamepad.sThumbRY / (float)short.MaxValue));// joystick right
		}

		private void RefreshDeviceInfo()
		{
			if
			(
				instance.XInputGetCapabilities != null &&
				instance.version != InstanceVersion.XInput_1_1 &&
				instance.version != InstanceVersion.XInput_1_2 &&
				instance.version != InstanceVersion.XInput_1_3
			)
			{
				var caps = new XINPUT_CAPABILITIES();
				if (instance.XInputGetCapabilities((DWORD)index, 0, &caps) == 0)
				{
					typeID = caps.Type;
				}
				else
				{
					typeID = (int)XINPUT_DEVSUBTYPE.GAMEPAD;
				}
			}
			else
			{
				typeID = (int)XINPUT_DEVSUBTYPE.GAMEPAD;
			}
		}

		public void SetRumble(float value)
		{
			if (value > 1) value = 1;
			if (value < 0) value = 0;
			var desc = new XINPUT_VIBRATION()
			{
				wLeftMotorSpeed = (WORD)(WORD.MaxValue * value),
				wRightMotorSpeed = (WORD)(WORD.MaxValue * value)
			};
			instance.XInputSetState((DWORD)index, &desc);
		}

		public void SetRumble(float leftValue, float rightValue)
		{
			if (leftValue > 1) leftValue = 1;
			if (leftValue < 0) leftValue = 0;
			if (rightValue > 1) rightValue = 1;
			if (rightValue < 0) rightValue = 0;
			var desc = new XINPUT_VIBRATION()
			{
				wLeftMotorSpeed = (WORD)(WORD.MaxValue * leftValue),
				wRightMotorSpeed = (WORD)(WORD.MaxValue * rightValue)
			};
			instance.XInputSetState((DWORD)index, &desc);
		}

		public void SetRumble(float value, int motorIndex)
		{
			if (value > 1) value = 1;
			if (value < 0) value = 0;
			var desc = new XINPUT_VIBRATION();
			if (motorIndex == 0) desc.wLeftMotorSpeed = (WORD)(WORD.MaxValue * value);
			if (motorIndex == 1) desc.wRightMotorSpeed = (WORD)(WORD.MaxValue * value);
			instance.XInputSetState((DWORD)index, &desc);
		}
	}

	public class GamepadButton
	{
		/// <summary>
		/// Button actively being pressed
		/// </summary>
		public bool on { get; private set; }

		/// <summary>
		/// Button was pressed
		/// </summary>
		public bool down { get; private set; }

		/// <summary>
		/// Button was released
		/// </summary>
		public bool up { get; private set; }

		public void Update(bool on)
		{
			down = false;
			up = false;
			if (this.on != on)
			{
				if (on) down = true;
				else up = true;
			}
			this.on = on;
		}
	}

	public enum Axis1DUpdateMode
	{
		/// <summary>
		/// Value can be positive or negitive
		/// </summary>
		Bidirectional,

		/// <summary>
		/// (0)-(+1) values
		/// </summary>
		Positive,

		/// <summary>
		/// (0)-(-1) values
		/// </summary>
		Negitive,

		/// <summary>
		/// (-1)-(+1) shifted into range of (0)-(+1)
		/// </summary>
		FullRange_ShiftedPositive
	}

	public class Axis1D
	{
		/// <summary>
		/// How the update values are processed
		/// </summary>
		public readonly Axis1DUpdateMode updateMode;

		/// <summary>
		/// Any input under talerance will be forced to 0
		/// </summary>
		public float tolerance = .2f;

		/// <summary>
		/// 0-1 smoothing value
		/// </summary>
		public float smoothing = .75f;

		/// <summary>
		/// Value of the axis input
		/// </summary>
		public float value { get; private set; }

		public Axis1D(Axis1DUpdateMode mode)
		{
			this.updateMode = mode;
		}

		public void Update(float value)
		{
			if (updateMode == Axis1DUpdateMode.Bidirectional)
			{
				if (Math.Abs(value) <= tolerance) value = 0;
			}
			else if (updateMode == Axis1DUpdateMode.Positive)
			{
				if (value < 0) value = 0;
				if (value <= tolerance) value = 0;
			}
			else if (updateMode == Axis1DUpdateMode.Negitive)
			{
				if (value > 0) value = 0;
				value = Math.Abs(value);
				if (value <= tolerance) value = 0;
			}
			else if (updateMode == Axis1DUpdateMode.FullRange_ShiftedPositive)
			{
				value += 1f;
				value *= .5f;
				if (value <= tolerance) value = 0;
			}

			this.value += (value - this.value) * smoothing;
		}
	}

	public class Axis2D
	{
		/// <summary>
		/// Any input under talerance will be forced to Vec2.zero
		/// </summary>
		public float tolerance = .2f;

		/// <summary>
		/// 0-1 smoothing value
		/// </summary>
		public float smoothing = .75f;

		/// <summary>
		/// Value of the axis input
		/// </summary>
		public Vec2 value { get; private set; }

		public void Update(Vec2 value)
		{
			if (value.Length() <= tolerance) value = Vec2.zero;
			this.value += (value - this.value) * smoothing;
		}
	}

	public struct Vec2
	{
		#region Properties
		public float x, y;

		public static readonly Vec2 one = new Vec2(1);
		public static readonly Vec2 minusOne = new Vec2(-1);
		public static readonly Vec2 zero = new Vec2();
		public static readonly Vec2 right = new Vec2(1, 0);
		public static readonly Vec2 left = new Vec2(-1, 0);
		public static readonly Vec2 up = new Vec2(0, 1);
		public static readonly Vec2 down = new Vec2(0, -1);
		#endregion

		#region Constructors
		public Vec2(float value)
		{
			x = value;
			y = value;
		}

		public Vec2(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
		#endregion

		#region Operators
		// +
		public static Vec2 operator+(Vec2 p1, Vec2 p2)
		{
			return new Vec2(p1.x + p2.x, p1.y + p2.y);
		}

		public static Vec2 operator+(Vec2 p1, float p2)
		{
			return new Vec2(p1.x + p2, p1.y + p2);
		}

		public static Vec2 operator+(float p1, Vec2 p2)
		{
			return new Vec2(p1 + p2.x, p1 + p2.y);
		}

		// -
		public static Vec2 operator-(Vec2 p1, Vec2 p2)
		{
			return new Vec2(p1.x - p2.x, p1.y - p2.y);
		}

		public static Vec2 operator-(Vec2 p1, float p2)
		{
			return new Vec2(p1.x - p2, p1.y - p2);
		}

		public static Vec2 operator-(float p1, Vec2 p2)
		{
			return new Vec2(p1 - p2.x, p1 - p2.y);
		}

		public static Vec2 operator-(Vec2 p1)
		{
			return new Vec2(-p1.x, -p1.y);
		}

		// *
		public static Vec2 operator*(Vec2 p1, Vec2 p2)
		{
			return new Vec2(p1.x * p2.x, p1.y * p2.y);
		}

		public static Vec2 operator*(Vec2 p1, float p2)
		{
			return new Vec2(p1.x * p2, p1.y * p2);
		}

		public static Vec2 operator*(float p1, Vec2 p2)
		{
			return new Vec2(p1 * p2.x, p1 * p2.y);
		}

		// /
		public static Vec2 operator/(Vec2 p1, Vec2 p2)
		{
			return new Vec2(p1.x / p2.x, p1.y / p2.y);
		}

		public static Vec2 operator/(Vec2 p1, float p2)
		{
			return new Vec2(p1.x / p2, p1.x / p2);
		}

		public static Vec2 operator/(float p1, Vec2 p2)
		{
			return new Vec2(p1 / p2.x, p1 / p2.y);
		}

		// ==
		public static bool operator==(Vec2 p1, Vec2 p2) {return p1.x==p2.x && p1.y==p2.y;}
		public static bool operator!=(Vec2 p1, Vec2 p2) {return p1.x!=p2.x || p1.y!=p2.y;}
		#endregion

		#region Methods
		public float Length()
		{
			return (float)Math.Sqrt((x*x) + (y*y));
		}
		#endregion
	}
}
