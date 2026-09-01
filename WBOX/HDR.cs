using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

// NOTE: generated from Grok

namespace WBOX
{
	static class HDR
	{
		public static void SetHdr(bool enable, string monitorNameContains = null)
        {
            Check(GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out int pathCount, out int modeCount));

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            Check(QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero));

            bool any = false;
            for (int i = 0; i < pathCount; i++)
            {
                DISPLAYCONFIG_PATH_INFO path = paths[i];
                string name = GetTargetName(path);
                if (monitorNameContains != null &&
                    name.IndexOf(monitorNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO info = GetAdvancedColor(path);
                if ((info.value & AdvancedColorSupported) == 0)
                    continue;

                any = true;

                // Win11 24H2+: SET_HDR_STATE (type 16). Needed when ACM keeps Advanced Color always on.
                var setHdr = new DISPLAYCONFIG_SET_HDR_STATE();
                setHdr.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE;
                setHdr.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_HDR_STATE>();
                setHdr.header.adapterId = path.targetInfo.adapterId;
                setHdr.header.id = path.targetInfo.id;
                setHdr.value = enable ? 1u : 0u; // bit 0 = enableHdr
                if (DisplayConfigSetDeviceInfo(ref setHdr) == 0)
                    continue;

                var setColor = new DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE();
                setColor.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
                setColor.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE>();
                setColor.header.adapterId = path.targetInfo.adapterId;
                setColor.header.id = path.targetInfo.id;
                setColor.value = enable ? 1u : 0u; // bit 0 = enableAdvancedColor
                Check(DisplayConfigSetDeviceInfo(ref setColor));
            }

            if (!any)
                throw new InvalidOperationException("No matching HDR-capable display found.");
        }

        public static void Dump()
        {
            Check(GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out int pathCount, out int modeCount));
            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
            Check(QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero));

            for (int i = 0; i < pathCount; i++)
            {
                DISPLAYCONFIG_PATH_INFO path = paths[i];
                DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO info = GetAdvancedColor(path);
                Console.WriteLine(GetTargetName(path));
                Console.WriteLine(
                    "  supported={0} enabled={1} bpc={2}",
                    (info.value & AdvancedColorSupported) != 0,
                    (info.value & AdvancedColorEnabled) != 0,
                    info.bitsPerColorChannel);
            }
        }

        static string GetTargetName(DISPLAYCONFIG_PATH_INFO path)
        {
            var name = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            name.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            name.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
            name.header.adapterId = path.targetInfo.adapterId;
            name.header.id = path.targetInfo.id;
            Check(DisplayConfigGetDeviceInfo(ref name));
            return name.monitorFriendlyDeviceName ?? string.Empty;
        }

        static DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO GetAdvancedColor(DISPLAYCONFIG_PATH_INFO path)
        {
            var info = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
            info.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
            info.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
            info.header.adapterId = path.targetInfo.adapterId;
            info.header.id = path.targetInfo.id;
            Check(DisplayConfigGetDeviceInfo(ref info));
            return info;
        }

        static void Check(int err)
        {
            if (err != 0)
                throw new Win32Exception(err);
        }

        const int QDC_ONLY_ACTIVE_PATHS = 0x00000002;

        const int DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;
        const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
        const int DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE = 10;
        const int DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE = 16;

        const uint AdvancedColorSupported = 1u << 0;
        const uint AdvancedColorEnabled   = 1u << 1;

        [DllImport("user32.dll")]
        static extern int GetDisplayConfigBufferSizes(int flags, out int pathCount, out int modeCount);

        [DllImport("user32.dll")]
        static extern int QueryDisplayConfig(
            int flags,
            ref int pathCount,
            [In, Out] DISPLAYCONFIG_PATH_INFO[] paths,
            ref int modeCount,
            [In, Out] DISPLAYCONFIG_MODE_INFO[] modes,
            IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO requestPacket);

        [DllImport("user32.dll")]
        static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME requestPacket);

        [DllImport("user32.dll")]
        static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE requestPacket);

        [DllImport("user32.dll")]
        static extern int DisplayConfigSetDeviceInfo(ref DISPLAYCONFIG_SET_HDR_STATE requestPacket);

        [StructLayout(LayoutKind.Sequential)]
        struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_RATIONAL
        {
            public uint Numerator;
            public uint Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_2DREGION
        {
            public uint cx;
            public uint cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public int type;     // DISPLAYCONFIG_DEVICE_INFO_TYPE
            public uint size;    // UINT32
            public LUID adapterId;
            public uint id;      // UINT32
        }

        // union { bitfields; UINT32 value; }
        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;              // bit0 supported, bit1 enabled, bit2 wideColorEnforced, bit3 forceDisabled
            public int colorEncoding;       // DISPLAYCONFIG_COLOR_ENCODING
            public uint bitsPerColorChannel;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;              // bit0 enableAdvancedColor
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_SET_HDR_STATE
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint value;              // bit0 enableHdr
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public int outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string monitorDevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public int outputTechnology;
            public int rotation;
            public int scaling;
            public DISPLAYCONFIG_RATIONAL refreshRate;
            public int scanLineOrdering;
            public int targetAvailable;     // BOOL (Win32 4-byte), not C# bool
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
        {
            public ulong pixelRate;
            public DISPLAYCONFIG_RATIONAL hSyncFreq;
            public DISPLAYCONFIG_RATIONAL vSyncFreq;
            public DISPLAYCONFIG_2DREGION activeSize;
            public DISPLAYCONFIG_2DREGION totalSize;
            public uint videoStandard;
            public int scanLineOrdering;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_TARGET_MODE
        {
            public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_SOURCE_MODE
        {
            public uint width;
            public uint height;
            public int pixelFormat;
            public POINTL position;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
        {
            public POINTL PathSourceSize;
            public RECT DesktopImageRegion;
            public RECT DesktopImageClip;
        }

        // C union at offset 16: targetMode / sourceMode / desktopImageInfo
        [StructLayout(LayoutKind.Explicit)]
        struct DISPLAYCONFIG_MODE_INFO
        {
            [FieldOffset(0)] public int infoType;
            [FieldOffset(4)] public uint id;
            [FieldOffset(8)] public LUID adapterId;
            [FieldOffset(16)] public DISPLAYCONFIG_TARGET_MODE targetMode;
            [FieldOffset(16)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
            [FieldOffset(16)] public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
        }
	}
}
