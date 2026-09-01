/*
 * HdrEdidProfile.c
 *
 * Windows 11 console tool:
 *   1. Enumerate active displays (CCD / QueryDisplayConfig)
 *   2. Read full EDID from the monitor's device-parameters registry key
 *   3. Parse CTA-861 HDR static metadata + chromaticity
 *   4. Write an ICC v4 display profile with a Microsoft MHC2 tag
 *   5. Install it and set it as the Advanced Color (HDR) default
 *   6. Optionally enable HDR so Windows actually uses the profile
 *
 * Build (x64 Native Tools / Developer Command Prompt):
 *   build.bat
 *
 *   HdrEdidProfile.exe --list
 *   HdrEdidProfile.exe --index 0 --apply
 *   HdrEdidProfile.exe --index 0 --out MyDisplay.icc
 */

// NOTE: generated with Grok build mode

#define UNICODE
#define _UNICODE
#define WIN32_LEAN_AND_MEAN
#define _WIN32_WINNT 0x0A00

#include <windows.h>
#include <setupapi.h>
#include <initguid.h>
#include <icm.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include <wchar.h>
#include <stdint.h>

#pragma comment(lib, "user32.lib")
#pragma comment(lib, "gdi32.lib")
#pragma comment(lib, "setupapi.lib")
#pragma comment(lib, "mscms.lib")
#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "ole32.lib")

DEFINE_GUID(GUID_DEVINTERFACE_MONITOR,
    0xe6f07b5f, 0xee97, 0x4a90, 0xb0, 0x76, 0x33, 0xf5, 0x7b, 0xf4, 0xea, 0xa7);

#ifndef DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO
#define DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO ((DISPLAYCONFIG_DEVICE_INFO_TYPE)9)
#endif
#ifndef DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE
#define DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE ((DISPLAYCONFIG_DEVICE_INFO_TYPE)10)
#endif
#ifndef DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2
#define DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2 ((DISPLAYCONFIG_DEVICE_INFO_TYPE)15)
#endif
#ifndef DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE
#define DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE ((DISPLAYCONFIG_DEVICE_INFO_TYPE)16)
#endif

#ifndef CPST_STANDARD_DISPLAY_COLOR_MODE
#define CPST_STANDARD_DISPLAY_COLOR_MODE ((COLORPROFILESUBTYPE)7)
#endif
#ifndef CPST_EXTENDED_DISPLAY_COLOR_MODE
#define CPST_EXTENDED_DISPLAY_COLOR_MODE ((COLORPROFILESUBTYPE)8)
#endif

typedef struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2_LOCAL {
    DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    union {
        struct {
            UINT32 advancedColorSupported : 1;
            UINT32 advancedColorActive : 1;
            UINT32 reserved1 : 2;
            UINT32 highDynamicRangeSupported : 1;
            UINT32 highDynamicRangeUserEnabled : 1;
            UINT32 wideColorSupported : 1;
            UINT32 wideColorUserEnabled : 1;
            UINT32 reserved2 : 24;
        };
        UINT32 value;
    };
    DISPLAYCONFIG_COLOR_ENCODING colorEncoding;
    UINT32 bitsPerColorChannel;
    UINT32 activeColorMode;
} DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2_LOCAL;

typedef struct DISPLAYCONFIG_SET_HDR_STATE_LOCAL {
    DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    union {
        struct {
            UINT32 enableHdr : 1;
            UINT32 reserved : 31;
        };
        UINT32 value;
    };
} DISPLAYCONFIG_SET_HDR_STATE_LOCAL;

typedef struct { double x, y; } Chroma;

typedef struct {
    int hdr_supported;
    int hdr_enabled;
    int advanced_supported;
    int advanced_enabled;
} HdrState;

typedef struct {
    int valid;
    char manufacturer[8];
    unsigned product;
    unsigned serial;
    int week, year;
    char name[64];
    char serial_str[64];
    int width_cm, height_cm;
    double gamma;
    int native_w, native_h;
    Chroma red, green, blue, white;
    int ext_count;
    int has_hdr_block;
    int eotf_pq, eotf_hlg;
    double max_nits;
    double max_fall_nits;
    double min_nits;
    BYTE *raw;
    DWORD raw_len;
} EdidInfo;

typedef struct {
    DISPLAYCONFIG_PATH_INFO path;
    wchar_t gdi_name[64];
    wchar_t friendly[64];
    wchar_t device_path[256];
    LUID adapter;
    UINT32 source_id;
    UINT32 target_id;
    HdrState hdr;
    EdidInfo edid;
} DisplayInfo;

static int g_list_only = 0;
static int g_apply = 1;
static int g_enable_hdr = 1;
static int g_index = -1;
static int g_system_scope = 0;
static wchar_t g_out_path[MAX_PATH];
static wchar_t g_dump_edid[MAX_PATH];
static double g_override_peak = 0;
static double g_override_min = 0;

static void die(const char *msg) {
    fprintf(stderr, "error: %s\n", msg);
    exit(1);
}

static void winerr(const char *what) {
    fprintf(stderr, "error: %s failed (GetLastError=%lu)\n", what, (unsigned long)GetLastError());
}

static void wr_u32(BYTE *p, uint32_t v) {
    p[0] = (BYTE)(v >> 24); p[1] = (BYTE)(v >> 16); p[2] = (BYTE)(v >> 8); p[3] = (BYTE)v;
}

static void wr_u16(BYTE *p, uint16_t v) {
    p[0] = (BYTE)(v >> 8); p[1] = (BYTE)v;
}

static void wr_s15(BYTE *p, double v) {
    if (v > 32767.99998) v = 32767.99998;
    if (v < -32768.0) v = -32768.0;
    int32_t s = (int32_t)(v * 65536.0 + (v >= 0 ? 0.5 : -0.5));
    wr_u32(p, (uint32_t)s);
}

static void wr_sig(BYTE *p, const char *s) { memcpy(p, s, 4); }

static int checksum_ok(const BYTE *b) {
    unsigned s = 0;
    for (int i = 0; i < 128; i++) s += b[i];
    return (s & 0xFF) == 0;
}

static double cta_max_nits(unsigned cv) {
    if (cv == 0) return 0;
    return 50.0 * pow(2.0, cv / 32.0);
}

static void utf8_from_wide(const wchar_t *w, char *out, int cap) {
    if (!w || !*w) { out[0] = 0; return; }
    if (WideCharToMultiByte(CP_UTF8, 0, w, -1, out, cap, NULL, NULL) <= 0) out[0] = 0;
}

static void read_desc_text(const BYTE *b, int off, char *out, int cap) {
    int n = 0;
    for (int i = 5; i < 18 && n < cap - 1; i++) {
        BYTE c = b[off + i];
        if (c == 0x0A || c == 0x00) break;
        if (c >= 32 && c < 127) out[n++] = (char)c;
    }
    while (n > 0 && out[n - 1] == ' ') n--;
    out[n] = 0;
}

static void parse_cta(const BYTE *ext, EdidInfo *e) {
    if (ext[0] != 0x02) return;
    int dtd = ext[2] ? ext[2] : 4;
    int i = 4;
    while (i < dtd && i < 127) {
        int tag = ext[i] >> 5;
        int len = ext[i] & 0x1F;
        if (len == 0 && tag == 0) break;
        if (i + 1 + len > 128) break;
        if (tag == 7 && len >= 2 && ext[i + 1] == 0x06) {
            e->has_hdr_block = 1;
            unsigned eotf = ext[i + 2];
            e->eotf_pq = (eotf & 4) != 0;
            e->eotf_hlg = (eotf & 8) != 0;
            if (len >= 5) {
                e->max_nits = cta_max_nits(ext[i + 4]);
                e->max_fall_nits = (len >= 6) ? cta_max_nits(ext[i + 5]) : 0;
                if (len >= 6 && ext[i + 6] && e->max_nits > 0) {
                    double t = ext[i + 6] / 255.0;
                    e->min_nits = e->max_nits * t * t / 100.0;
                }
            }
        }
        i += 1 + len;
    }
}

static int parse_edid(const BYTE *raw, DWORD len, EdidInfo *e) {
    memset(e, 0, sizeof(*e));
    if (len < 128) return 0;
    static const BYTE hdr[8] = { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };
    if (memcmp(raw, hdr, 8) != 0) return 0;
    e->raw = (BYTE *)raw;
    e->raw_len = len;
    e->valid = checksum_ok(raw);

    unsigned id = (raw[8] << 8) | raw[9];
    e->manufacturer[0] = (char)(64 + ((id >> 10) & 31));
    e->manufacturer[1] = (char)(64 + ((id >> 5) & 31));
    e->manufacturer[2] = (char)(64 + (id & 31));
    e->manufacturer[3] = 0;
    e->product = raw[10] | (raw[11] << 8);
    e->serial = raw[12] | (raw[13] << 8) | (raw[14] << 16) | (raw[15] << 24);
    e->week = raw[16];
    e->year = 1990 + raw[17];
    e->width_cm = raw[21];
    e->height_cm = raw[22];
    e->gamma = (raw[23] == 0xFF) ? 2.2 : (raw[23] + 100) / 100.0;
    e->ext_count = raw[126];

    unsigned b25 = raw[25], b26 = raw[26];
    e->red.x   = ((raw[27] << 2) | ((b25 >> 6) & 3)) / 1024.0;
    e->red.y   = ((raw[28] << 2) | ((b25 >> 4) & 3)) / 1024.0;
    e->green.x = ((raw[29] << 2) | ((b25 >> 2) & 3)) / 1024.0;
    e->green.y = ((raw[30] << 2) | ( b25       & 3)) / 1024.0;
    e->blue.x  = ((raw[31] << 2) | ((b26 >> 6) & 3)) / 1024.0;
    e->blue.y  = ((raw[32] << 2) | ((b26 >> 4) & 3)) / 1024.0;
    e->white.x = ((raw[33] << 2) | ((b26 >> 2) & 3)) / 1024.0;
    e->white.y = ((raw[34] << 2) | ( b26       & 3)) / 1024.0;

    for (int d = 0; d < 4; d++) {
        int off = 54 + d * 18;
        if (raw[off] == 0 && raw[off + 1] == 0 && raw[off + 2] == 0) {
            if (raw[off + 3] == 0xFC) read_desc_text(raw, off, e->name, sizeof(e->name));
            if (raw[off + 3] == 0xFF) read_desc_text(raw, off, e->serial_str, sizeof(e->serial_str));
        } else if (e->native_w == 0) {
            e->native_w = raw[off + 2] | ((raw[off + 4] & 0xF0) << 4);
            e->native_h = raw[off + 5] | ((raw[off + 7] & 0xF0) << 4);
        }
    }
    if (!e->name[0]) snprintf(e->name, sizeof(e->name), "%s %04X", e->manufacturer, e->product);

    int blocks = (int)(len / 128);
    for (int n = 1; n < blocks; n++) parse_cta(raw + n * 128, e);
    return 1;
}

static int get_edid_from_device_path(const wchar_t *device_path, BYTE **out, DWORD *out_len) {
    HDEVINFO set = SetupDiGetClassDevsW(&GUID_DEVINTERFACE_MONITOR, NULL, NULL,
                                        DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
    if (set == INVALID_HANDLE_VALUE) return 0;

    SP_DEVICE_INTERFACE_DATA ifd;
    ifd.cbSize = sizeof(ifd);
    int found = 0;
    BYTE *edid = NULL;
    DWORD edid_len = 0;

    for (DWORD i = 0; SetupDiEnumDeviceInterfaces(set, NULL, &GUID_DEVINTERFACE_MONITOR, i, &ifd); i++) {
        DWORD need = 0;
        SetupDiGetDeviceInterfaceDetailW(set, &ifd, NULL, 0, &need, NULL);
        if (need == 0) continue;
        SP_DEVICE_INTERFACE_DETAIL_DATA_W *detail = (SP_DEVICE_INTERFACE_DETAIL_DATA_W *)calloc(1, need);
        if (!detail) continue;
        detail->cbSize = sizeof(*detail);
        SP_DEVINFO_DATA dev;
        dev.cbSize = sizeof(dev);
        if (!SetupDiGetDeviceInterfaceDetailW(set, &ifd, detail, need, NULL, &dev)) {
            free(detail);
            continue;
        }
        if (_wcsicmp(detail->DevicePath, device_path) != 0) {
            free(detail);
            continue;
        }
        HKEY key = SetupDiOpenDevRegKey(set, &dev, DICS_FLAG_GLOBAL, 0, DIREG_DEV, KEY_READ);
        free(detail);
        if (key == INVALID_HANDLE_VALUE) break;
        DWORD type = 0, size = 0;
        if (RegQueryValueExW(key, L"EDID", NULL, &type, NULL, &size) == ERROR_SUCCESS && size >= 128) {
            edid = (BYTE *)malloc(size);
            if (edid && RegQueryValueExW(key, L"EDID", NULL, &type, edid, &size) == ERROR_SUCCESS) {
                edid_len = size;
                found = 1;
            } else {
                free(edid);
                edid = NULL;
            }
        }
        RegCloseKey(key);
        break;
    }
    SetupDiDestroyDeviceInfoList(set);
    *out = edid;
    *out_len = edid_len;
    return found;
}

static HdrState read_hdr_state(const DISPLAYCONFIG_PATH_INFO *path) {
    HdrState s = { 0 };
    DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2_LOCAL i2;
    memset(&i2, 0, sizeof(i2));
    i2.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO_2;
    i2.header.size = sizeof(i2);
    i2.header.adapterId = path->targetInfo.adapterId;
    i2.header.id = path->targetInfo.id;
    if (DisplayConfigGetDeviceInfo(&i2.header) == ERROR_SUCCESS) {
        s.hdr_supported = i2.highDynamicRangeSupported;
        s.hdr_enabled = i2.highDynamicRangeUserEnabled;
        s.advanced_supported = i2.advancedColorSupported || i2.highDynamicRangeSupported || i2.wideColorSupported;
        s.advanced_enabled = i2.advancedColorActive || i2.highDynamicRangeUserEnabled;
        return s;
    }
    DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO i1;
    memset(&i1, 0, sizeof(i1));
    i1.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
    i1.header.size = sizeof(i1);
    i1.header.adapterId = path->targetInfo.adapterId;
    i1.header.id = path->targetInfo.id;
    if (DisplayConfigGetDeviceInfo(&i1.header) == ERROR_SUCCESS) {
        s.advanced_supported = i1.advancedColorSupported;
        s.advanced_enabled = i1.advancedColorEnabled;
        s.hdr_supported = i1.advancedColorSupported;
        s.hdr_enabled = i1.advancedColorEnabled;
    }
    return s;
}

static int set_hdr_on(const DISPLAYCONFIG_PATH_INFO *path) {
    DISPLAYCONFIG_SET_HDR_STATE_LOCAL st;
    memset(&st, 0, sizeof(st));
    st.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_HDR_STATE;
    st.header.size = sizeof(st);
    st.header.adapterId = path->targetInfo.adapterId;
    st.header.id = path->targetInfo.id;
    st.enableHdr = 1;
    if (DisplayConfigSetDeviceInfo(&st.header) == ERROR_SUCCESS) return 1;

    DISPLAYCONFIG_SET_ADVANCED_COLOR_STATE ac;
    memset(&ac, 0, sizeof(ac));
    ac.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_ADVANCED_COLOR_STATE;
    ac.header.size = sizeof(ac);
    ac.header.adapterId = path->targetInfo.adapterId;
    ac.header.id = path->targetInfo.id;
    ac.enableAdvancedColor = 1;
    return DisplayConfigSetDeviceInfo(&ac.header) == ERROR_SUCCESS;
}

static int enumerate_displays(DisplayInfo **out, int *count) {
    UINT32 nPath = 0, nMode = 0;
    if (GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, &nPath, &nMode) != ERROR_SUCCESS)
        return 0;
    DISPLAYCONFIG_PATH_INFO *paths = (DISPLAYCONFIG_PATH_INFO *)calloc(nPath, sizeof(*paths));
    DISPLAYCONFIG_MODE_INFO *modes = (DISPLAYCONFIG_MODE_INFO *)calloc(nMode, sizeof(*modes));
    if (!paths || !modes) { free(paths); free(modes); return 0; }
    if (QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, &nPath, paths, &nMode, modes, NULL) != ERROR_SUCCESS) {
        free(paths); free(modes); return 0;
    }

    DisplayInfo *list = (DisplayInfo *)calloc(nPath, sizeof(DisplayInfo));
    int n = 0;
    for (UINT32 i = 0; i < nPath; i++) {
        if (!(paths[i].flags & DISPLAYCONFIG_PATH_ACTIVE)) continue;
        DisplayInfo *d = &list[n];
        memset(d, 0, sizeof(*d));
        d->path = paths[i];
        d->adapter = paths[i].sourceInfo.adapterId;
        d->source_id = paths[i].sourceInfo.id;
        d->target_id = paths[i].targetInfo.id;

        DISPLAYCONFIG_SOURCE_DEVICE_NAME src;
        memset(&src, 0, sizeof(src));
        src.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
        src.header.size = sizeof(src);
        src.header.adapterId = paths[i].sourceInfo.adapterId;
        src.header.id = paths[i].sourceInfo.id;
        if (DisplayConfigGetDeviceInfo(&src.header) == ERROR_SUCCESS)
            wcsncpy(d->gdi_name, src.viewGdiDeviceName, 63);

        DISPLAYCONFIG_TARGET_DEVICE_NAME tgt;
        memset(&tgt, 0, sizeof(tgt));
        tgt.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        tgt.header.size = sizeof(tgt);
        tgt.header.adapterId = paths[i].targetInfo.adapterId;
        tgt.header.id = paths[i].targetInfo.id;
        if (DisplayConfigGetDeviceInfo(&tgt.header) == ERROR_SUCCESS) {
            wcsncpy(d->friendly, tgt.monitorFriendlyDeviceName, 63);
            wcsncpy(d->device_path, tgt.monitorDevicePath, 255);
        }

        d->hdr = read_hdr_state(&paths[i]);

        BYTE *edid = NULL;
        DWORD elen = 0;
        if (d->device_path[0] && get_edid_from_device_path(d->device_path, &edid, &elen)) {
            parse_edid(edid, elen, &d->edid);
            d->edid.raw = edid;
            d->edid.raw_len = elen;
        }
        n++;
    }
    free(paths);
    free(modes);
    *out = list;
    *count = n;
    return 1;
}

typedef struct { double X, Y, Z; } XYZ;
typedef struct { double m[3][3]; } Mat3;

static XYZ xyY(Chroma c, double Y) {
    XYZ o = { 0, 0, 0 };
    if (c.y <= 1e-12) return o;
    o.X = (c.x / c.y) * Y;
    o.Y = Y;
    o.Z = ((1.0 - c.x - c.y) / c.y) * Y;
    return o;
}

static int invert3(const Mat3 *in, Mat3 *out) {
    const double a = in->m[0][0], b = in->m[0][1], c = in->m[0][2];
    const double d = in->m[1][0], e = in->m[1][1], f = in->m[1][2];
    const double g = in->m[2][0], h = in->m[2][1], i = in->m[2][2];
    double det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
    if (fabs(det) < 1e-12) return 0;
    double id = 1.0 / det;
    out->m[0][0] = (e * i - f * h) * id; out->m[0][1] = (c * h - b * i) * id; out->m[0][2] = (b * f - c * e) * id;
    out->m[1][0] = (f * g - d * i) * id; out->m[1][1] = (a * i - c * g) * id; out->m[1][2] = (c * d - a * f) * id;
    out->m[2][0] = (d * h - e * g) * id; out->m[2][1] = (b * g - a * h) * id; out->m[2][2] = (a * e - b * d) * id;
    return 1;
}

static Mat3 mul3(const Mat3 *A, const Mat3 *B) {
    Mat3 R = { 0 };
    for (int r = 0; r < 3; r++)
        for (int c = 0; c < 3; c++)
            R.m[r][c] = A->m[r][0] * B->m[0][c] + A->m[r][1] * B->m[1][c] + A->m[r][2] * B->m[2][c];
    return R;
}

static XYZ mul3v(const Mat3 *A, XYZ v) {
    XYZ o;
    o.X = A->m[0][0] * v.X + A->m[0][1] * v.Y + A->m[0][2] * v.Z;
    o.Y = A->m[1][0] * v.X + A->m[1][1] * v.Y + A->m[1][2] * v.Z;
    o.Z = A->m[2][0] * v.X + A->m[2][1] * v.Y + A->m[2][2] * v.Z;
    return o;
}

static Mat3 bradford_chad(XYZ src, XYZ dst) {
    Mat3 B = {{
        {  0.8951,  0.2664, -0.1614 },
        { -0.7502,  1.7135,  0.0367 },
        {  0.0389, -0.0685,  1.0296 }
    }};
    Mat3 Bi;
    invert3(&B, &Bi);
    XYZ cs = mul3v(&B, src);
    XYZ cd = mul3v(&B, dst);
    Mat3 S = {{ { cd.X / cs.X, 0, 0 }, { 0, cd.Y / cs.Y, 0 }, { 0, 0, cd.Z / cs.Z } }};
    Mat3 tmp = mul3(&S, &B);
    return mul3(&Bi, &tmp);
}

static int rgb_xyz(Chroma r, Chroma g, Chroma b, Chroma w, XYZ *R, XYZ *G, XYZ *Bcol, XYZ *W) {
    XYZ r1 = xyY(r, 1), g1 = xyY(g, 1), b1 = xyY(b, 1);
    *W = xyY(w, 1);
    Mat3 M = {{ { r1.X, g1.X, b1.X }, { r1.Y, g1.Y, b1.Y }, { r1.Z, g1.Z, b1.Z } }};
    Mat3 Minv;
    if (!invert3(&M, &Minv)) return 0;
    XYZ S = mul3v(&Minv, *W);
    R->X = r1.X * S.X; R->Y = r1.Y * S.X; R->Z = r1.Z * S.X;
    G->X = g1.X * S.Y; G->Y = g1.Y * S.Y; G->Z = g1.Z * S.Y;
    Bcol->X = b1.X * S.Z; Bcol->Y = b1.Y * S.Z; Bcol->Z = b1.Z * S.Z;
    return 1;
}

typedef struct {
    char sig[5];
    DWORD size;
    BYTE *data;
} IccTag;

static BYTE *mluc_tag(const wchar_t *text, DWORD *sz) {
    int n = (int)wcslen(text);
    DWORD strb = (DWORD)(n * 2);
    DWORD size = (28 + strb + 3) & ~3u;
    BYTE *buf = (BYTE *)calloc(1, size);
    wr_sig(buf, "mluc");
    wr_u32(buf + 8, 1);
    wr_u32(buf + 12, 12);
    buf[16] = 'e'; buf[17] = 'n'; buf[18] = 'U'; buf[19] = 'S';
    wr_u32(buf + 20, strb);
    wr_u32(buf + 24, 28);
    for (int i = 0; i < n; i++) {
        buf[28 + i * 2] = (BYTE)(text[i] >> 8);
        buf[28 + i * 2 + 1] = (BYTE)text[i];
    }
    *sz = size;
    return buf;
}

static BYTE *xyz_tag(XYZ v, DWORD *sz) {
    BYTE *buf = (BYTE *)calloc(1, 20);
    wr_sig(buf, "XYZ ");
    wr_s15(buf + 8, v.X);
    wr_s15(buf + 12, v.Y);
    wr_s15(buf + 16, v.Z);
    *sz = 20;
    return buf;
}

static BYTE *para_gamma(double g, DWORD *sz) {
    BYTE *buf = (BYTE *)calloc(1, 16);
    wr_sig(buf, "para");
    wr_u16(buf + 8, 0);
    wr_s15(buf + 12, g);
    *sz = 16;
    return buf;
}

static BYTE *mhc2_tag(double min_nits, double peak_nits, DWORD *sz) {
    DWORD header = 36, matrix = 48, lut = 16;
    DWORD red = header + matrix;
    DWORD green = red + lut;
    DWORD blue = green + lut;
    DWORD size = blue + lut;
    BYTE *buf = (BYTE *)calloc(1, size);
    wr_sig(buf, "MHC2");
    wr_u32(buf + 8, 2);
    wr_s15(buf + 12, min_nits);
    wr_s15(buf + 16, peak_nits);
    wr_u32(buf + 20, header);
    wr_u32(buf + 24, red);
    wr_u32(buf + 28, green);
    wr_u32(buf + 32, blue);
    double ident[12] = { 1,0,0,0,  0,1,0,0,  0,0,1,0 };
    for (int i = 0; i < 12; i++) wr_s15(buf + header + i * 4, ident[i]);
    for (int c = 0; c < 3; c++) {
        BYTE *p = buf + red + c * lut;
        wr_sig(p, "sf32");
        wr_s15(p + 8, 0.0);
        wr_s15(p + 12, 1.0);
    }
    *sz = size;
    return buf;
}

static int write_icc(const EdidInfo *e, const wchar_t *path, double peak, double minn, double fall) {
    static const XYZ D50 = { 0.96420288, 1.0, 0.82490540 };
    Chroma r = e->red, g = e->green, b = e->blue, w = e->white;
    if (r.y < 0.01) {
        r.x = 0.64; r.y = 0.33; g.x = 0.30; g.y = 0.60;
        b.x = 0.15; b.y = 0.06; w.x = 0.3127; w.y = 0.3290;
    }
    XYZ R, G, B, W;
    if (!rgb_xyz(r, g, b, w, &R, &G, &B, &W)) return 0;
    Mat3 chad = bradford_chad(W, D50);
    XYZ Ra = mul3v(&chad, R), Ga = mul3v(&chad, G), Ba = mul3v(&chad, B);

    wchar_t desc[128], cprt[128], namew[64];
    MultiByteToWideChar(CP_ACP, 0, e->name, -1, namew, 64);
    swprintf(desc, 128, L"%s HDR from EDID", namew);
    wcscpy(cprt, L"Generated by HdrEdidProfile from display EDID");

    IccTag tags[12];
    int nt = 0;
#define ADD(sigstr, ptr) do { strncpy(tags[nt].sig, sigstr, 4); tags[nt].sig[4]=0; tags[nt].data = (ptr); nt++; } while (0)
    ADD("desc", mluc_tag(desc, &tags[nt].size));
    ADD("cprt", mluc_tag(cprt, &tags[nt].size));
    ADD("wtpt", xyz_tag(D50, &tags[nt].size));
    ADD("rXYZ", xyz_tag(Ra, &tags[nt].size));
    ADD("gXYZ", xyz_tag(Ga, &tags[nt].size));
    ADD("bXYZ", xyz_tag(Ba, &tags[nt].size));
    DWORD gsz = 0;
    BYTE *trc = para_gamma(e->gamma > 1.0 ? e->gamma : 2.2, &gsz);
    ADD("rTRC", trc); tags[nt - 1].size = gsz;
    BYTE *trc2 = (BYTE *)malloc(gsz); memcpy(trc2, trc, gsz);
    BYTE *trc3 = (BYTE *)malloc(gsz); memcpy(trc3, trc, gsz);
    ADD("gTRC", trc2); tags[nt - 1].size = gsz;
    ADD("bTRC", trc3); tags[nt - 1].size = gsz;
    BYTE *chadb = (BYTE *)calloc(1, 44);
    wr_sig(chadb, "sf32");
    for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
            wr_s15(chadb + 8 + (i * 3 + j) * 4, chad.m[i][j]);
    ADD("chad", chadb); tags[nt - 1].size = 44;
    XYZ lumi = { 0, fall, 0 };
    ADD("lumi", xyz_tag(lumi, &tags[nt].size));
    ADD("MHC2", mhc2_tag(minn, peak, &tags[nt].size));
#undef ADD

    DWORD header = 128;
    DWORD table = 4 + nt * 12;
    DWORD off = header + table;
    DWORD offsets[12];
    for (int i = 0; i < nt; i++) {
        offsets[i] = off;
        off = (off + tags[i].size + 3) & ~3u;
    }
    BYTE *icc = (BYTE *)calloc(1, off);
    wr_u32(icc, off);
    wr_sig(icc + 4, "none");
    wr_u32(icc + 8, 0x04400000);
    wr_sig(icc + 12, "mntr");
    wr_sig(icc + 16, "RGB ");
    wr_sig(icc + 20, "XYZ ");
    SYSTEMTIME st; GetSystemTime(&st);
    wr_u16(icc + 24, st.wYear); wr_u16(icc + 26, st.wMonth); wr_u16(icc + 28, st.wDay);
    wr_u16(icc + 30, st.wHour); wr_u16(icc + 32, st.wMinute); wr_u16(icc + 34, st.wSecond);
    wr_sig(icc + 36, "acsp");
    wr_sig(icc + 40, "MSFT");
    wr_u32(icc + 64, 1);
    wr_s15(icc + 68, D50.X); wr_s15(icc + 72, D50.Y); wr_s15(icc + 76, D50.Z);
    wr_sig(icc + 80, "xAI ");
    wr_u32(icc + 128, (uint32_t)nt);
    for (int i = 0; i < nt; i++) {
        BYTE *ent = icc + 132 + i * 12;
        wr_sig(ent, tags[i].sig);
        wr_u32(ent + 4, offsets[i]);
        wr_u32(ent + 8, tags[i].size);
        memcpy(icc + offsets[i], tags[i].data, tags[i].size);
        free(tags[i].data);
    }

    HANDLE h = CreateFileW(path, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (h == INVALID_HANDLE_VALUE) { free(icc); return 0; }
    DWORD written = 0;
    BOOL ok = WriteFile(h, icc, off, &written, NULL);
    CloseHandle(h);
    free(icc);
    return ok && written == off;
}

static int is_elevated(void) {
    BOOL elevated = FALSE;
    HANDLE token = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token)) {
        TOKEN_ELEVATION te;
        DWORD sz = sizeof(te);
        if (GetTokenInformation(token, TokenElevation, &te, sizeof(te), &sz))
            elevated = te.TokenIsElevated;
        CloseHandle(token);
    }
    return elevated;
}

static int install_and_set(const DisplayInfo *d, const wchar_t *icc_path) {
    if (!InstallColorProfileW(NULL, icc_path)) {
        winerr("InstallColorProfileW");
        fprintf(stderr, "       Run from an elevated console, or install the .icc via Color Management.\n");
        return 0;
    }
    const wchar_t *name = wcsrchr(icc_path, L'\\');
    name = name ? name + 1 : icc_path;

    WCS_PROFILE_MANAGEMENT_SCOPE scope = g_system_scope
        ? WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE
        : WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER;

    HRESULT hr = ColorProfileAddDisplayAssociation(
        scope, name, d->adapter, d->source_id, TRUE, TRUE);
    if (FAILED(hr)) {
        fprintf(stderr, "error: ColorProfileAddDisplayAssociation failed (hr=0x%08lX)\n", (unsigned long)hr);
    }

    hr = ColorProfileSetDisplayDefaultAssociation(
        scope, name, CPT_ICC, CPST_EXTENDED_DISPLAY_COLOR_MODE,
        d->adapter, d->source_id);
    if (FAILED(hr)) {
        fprintf(stderr, "error: ColorProfileSetDisplayDefaultAssociation (EXTENDED) hr=0x%08lX\n",
                (unsigned long)hr);
        return 0;
    }
    char utf[MAX_PATH];
    utf8_from_wide(name, utf, sizeof(utf));
    printf("  installed and set as HDR (EXTENDED) default: %s\n", utf);
    return 1;
}

static void usage(void) {
    printf("HdrEdidProfile — read EDID, write an MHC2 HDR ICC, set it as Windows default\n\n");
    printf("Usage:\n");
    printf("  HdrEdidProfile.exe [options]\n\n");
    printf("Options:\n");
    printf("  --list              list displays / EDID / HDR state and exit\n");
    printf("  --index N           only process display N (from --list)\n");
    printf("  --out PATH          write the .icc here (no install)\n");
    printf("  --apply             install and set as the HDR default (default if no --out)\n");
    printf("  --no-apply          do not install\n");
    printf("  --enable-hdr        turn HDR on so Windows uses the EXTENDED profile (default)\n");
    printf("  --no-enable-hdr     leave HDR toggle alone\n");
    printf("  --system            associate system-wide (needs elevation)\n");
    printf("  --peak NITS         override EDID peak luminance\n");
    printf("  --min NITS          override EDID min luminance\n");
    printf("  --dump-edid PATH    write raw EDID bytes of the selected display\n");
    printf("  --help              this text\n");
}

static void print_display(int i, const DisplayInfo *d) {
    char friendly[256], gdi[128];
    utf8_from_wide(d->friendly, friendly, sizeof(friendly));
    utf8_from_wide(d->gdi_name, gdi, sizeof(gdi));
    printf("[%d] %s\n", i, friendly[0] ? friendly : "(unnamed monitor)");
    printf("    GDI:     %s\n", gdi);
    printf("    HDR:     supported=%s  enabled=%s\n",
           d->hdr.hdr_supported ? "yes" : "no",
           d->hdr.hdr_enabled ? "yes" : "no");
    if (!d->edid.valid && !d->edid.raw) {
        printf("    EDID:    (not readable)\n\n");
        return;
    }
    const EdidInfo *e = &d->edid;
    printf("    EDID:    %s  product %04X  %dx%d  %d bytes  ext=%d\n",
           e->name, e->product, e->native_w, e->native_h, (int)e->raw_len, e->ext_count);
    printf("    chroma:  R %.4f,%.4f  G %.4f,%.4f  B %.4f,%.4f  W %.4f,%.4f\n",
           e->red.x, e->red.y, e->green.x, e->green.y, e->blue.x, e->blue.y, e->white.x, e->white.y);
    if (e->has_hdr_block) {
        printf("    CTA HDR: PQ=%s HLG=%s  peak=%.0f nits  maxFALL=%.0f  min=%.6f\n",
               e->eotf_pq ? "yes" : "no", e->eotf_hlg ? "yes" : "no",
               e->max_nits, e->max_fall_nits, e->min_nits);
    } else {
        printf("    CTA HDR: (no static metadata block in EDID)\n");
    }
    printf("\n");
}

int wmain(int argc, wchar_t **argv) {
    SetConsoleOutputCP(CP_UTF8);
    for (int i = 1; i < argc; i++) {
        if (!wcscmp(argv[i], L"--help") || !wcscmp(argv[i], L"-h")) { usage(); return 0; }
        else if (!wcscmp(argv[i], L"--list")) { g_list_only = 1; g_apply = 0; }
        else if (!wcscmp(argv[i], L"--apply")) g_apply = 1;
        else if (!wcscmp(argv[i], L"--no-apply")) g_apply = 0;
        else if (!wcscmp(argv[i], L"--enable-hdr")) g_enable_hdr = 1;
        else if (!wcscmp(argv[i], L"--no-enable-hdr")) g_enable_hdr = 0;
        else if (!wcscmp(argv[i], L"--system")) g_system_scope = 1;
        else if (!wcscmp(argv[i], L"--index") && i + 1 < argc) g_index = _wtoi(argv[++i]);
        else if (!wcscmp(argv[i], L"--out") && i + 1 < argc) { wcsncpy(g_out_path, argv[++i], MAX_PATH - 1); g_apply = 0; }
        else if (!wcscmp(argv[i], L"--peak") && i + 1 < argc) g_override_peak = _wtof(argv[++i]);
        else if (!wcscmp(argv[i], L"--min") && i + 1 < argc) g_override_min = _wtof(argv[++i]);
        else if (!wcscmp(argv[i], L"--dump-edid") && i + 1 < argc) wcsncpy(g_dump_edid, argv[++i], MAX_PATH - 1);
        else {
            fprintf(stderr, "unknown argument\n");
            usage();
            return 2;
        }
    }

    DisplayInfo *list = NULL;
    int n = 0;
    if (!enumerate_displays(&list, &n) || n == 0)
        die("no active displays found");

    printf("HdrEdidProfile — %d active display%s\n\n", n, n == 1 ? "" : "s");
    for (int i = 0; i < n; i++) print_display(i, &list[i]);
    if (g_list_only) return 0;

    int start = 0, end = n;
    if (g_index >= 0) {
        if (g_index >= n) die("index out of range");
        start = g_index;
        end = g_index + 1;
    }

    wchar_t tmpdir[MAX_PATH];
    GetTempPathW(MAX_PATH, tmpdir);

    int processed = 0;
    for (int i = start; i < end; i++) {
        DisplayInfo *d = &list[i];
        if (!d->edid.raw) {
            printf("[%d] skip — no EDID\n", i);
            continue;
        }
        if (g_dump_edid[0] && (g_index >= 0 || n == 1)) {
            HANDLE h = CreateFileW(g_dump_edid, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
            if (h != INVALID_HANDLE_VALUE) {
                DWORD wr = 0;
                WriteFile(h, d->edid.raw, d->edid.raw_len, &wr, NULL);
                CloseHandle(h);
                char dump[MAX_PATH];
                utf8_from_wide(g_dump_edid, dump, sizeof(dump));
                printf("  wrote EDID (%lu bytes) to %s\n", (unsigned long)d->edid.raw_len, dump);
            }
        }

        double peak = g_override_peak > 0 ? g_override_peak : (d->edid.max_nits > 0 ? d->edid.max_nits : 400.0);
        double minn = g_override_min > 0 ? g_override_min : (d->edid.min_nits > 0 ? d->edid.min_nits : 0.005);
        double fall = d->edid.max_fall_nits > 0 ? d->edid.max_fall_nits : peak * 0.4;
        if (fall < 80) fall = (peak < 80) ? peak : 80;

        wchar_t icc_path[MAX_PATH];
        if (g_out_path[0] && (end - start) == 1) {
            wcsncpy(icc_path, g_out_path, MAX_PATH - 1);
        } else {
            wchar_t safe[64];
            int k = 0;
            for (char *p = d->edid.name; *p && k < 60; p++) {
                char c = *p;
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    safe[k++] = (wchar_t)c;
                else
                    safe[k++] = L'_';
            }
            safe[k] = 0;
            swprintf(icc_path, MAX_PATH, L"%s%s_HDR_from_EDID.icc", tmpdir, safe);
        }

        char icc_utf[MAX_PATH];
        utf8_from_wide(icc_path, icc_utf, sizeof(icc_utf));
        printf("[%d] writing MHC2 ICC  peak=%.0f  min=%.6f  maxFALL=%.0f\n    %s\n",
               i, peak, minn, fall, icc_utf);
        if (!write_icc(&d->edid, icc_path, peak, minn, fall)) {
            fprintf(stderr, "error: failed to write ICC\n");
            continue;
        }

        if (g_apply) {
            if (!is_elevated() && g_system_scope)
                fprintf(stderr, "warning: --system usually needs an elevated console\n");
            if (!install_and_set(d, icc_path)) {
                printf("  profile saved — install it from Color Management if association failed\n");
            } else if (g_enable_hdr && d->hdr.hdr_supported && !d->hdr.hdr_enabled) {
                if (set_hdr_on(&d->path))
                    printf("  HDR enabled on this display\n");
                else
                    printf("  could not enable HDR (toggle it in Settings > System > Display)\n");
            } else if (g_enable_hdr && d->hdr.hdr_enabled) {
                printf("  HDR already on — Windows should load the new EXTENDED profile now\n");
            }
        }
        processed++;
    }

    if (!processed) {
        fprintf(stderr, "nothing to do — no EDID on the selected display(s)\n");
        return 1;
    }
    return 0;
}
