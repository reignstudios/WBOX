// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <iostream>
#include <chrono>
#include <thread>
#include <libvirtualhid/libvirtualhid.hpp>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
    return TRUE;
}

// ===========================================
// C++ handles
// ===========================================
std::unique_ptr<lvh::Runtime> runtime;
lvh::KeyboardCreationResult created;

static const lvh::KeyboardKeyCode kVkLControl = 0xA2;
static const lvh::KeyboardKeyCode kVk1 = 0x31;
static const lvh::KeyboardKeyCode kVk2 = 0x32;
static const lvh::KeyboardKeyCode kVkTab = 0x09;

static const std::uint16_t kScanLControl = 0x1D;
static const std::uint16_t kScan1 = 0x02;
static const std::uint16_t kScan2 = 0x03;
static const std::uint16_t kScanTab = 0x0F;

static void SleepMs(int ms)
{
    if (ms > 0) std::this_thread::sleep_for(std::chrono::milliseconds(ms));
}

static bool Submit(lvh::Keyboard& kb, lvh::KeyboardKeyCode vk, std::uint16_t scan, bool pressed)
{
    lvh::KeyboardEvent ev;
    ev.key_code = vk;
    ev.pressed = pressed;
    ev.scan_code = scan;
    ev.uses_normalized_key_code = true;
    ev.prefer_native_scan_code = true;

    lvh::OperationStatus st = kb.submit(ev);
    return st.ok();
}

static bool SendChord(lvh::Keyboard& kb, lvh::KeyboardKeyCode secondVk, std::uint16_t secondScan, int delayMs)
{
    if (!Submit(kb, kVkLControl, kScanLControl, true)) return false;
    SleepMs(delayMs);

    if (!Submit(kb, secondVk, secondScan, true)) return false;
    SleepMs(delayMs);

    if (!Submit(kb, secondVk, secondScan, false)) return false;
    SleepMs(delayMs);

    if (!Submit(kb, kVkLControl, kScanLControl, false)) return false;
    SleepMs(delayMs);
    return true;
}

// ===========================================
// C interop
// ===========================================
extern "C"
{
    __declspec(dllexport) int WBOX_VirtualHID_Init()
    {
        // init runtime
        lvh::RuntimeOptions opt;
        opt.backend = lvh::BackendKind::platform_default;
        runtime = lvh::Runtime::create(opt);
        if (!runtime) return 0;

        // validate virtual keyboard is supported
        const lvh::BackendCapabilities& caps = runtime->capabilities();
        if (!caps.supports_keyboard) return 0;

        // init virtual keyboard
        lvh::CreateKeyboardOptions kbdOpt;
        lvh::KeyboardCreationResult created = runtime->create_keyboard(kbdOpt);
        if (!created) return 0;

        return 1;
    }

    __declspec(dllexport) void WBOX_VirtualHID_Dispose()
    {
        if (runtime)
        {
            runtime->close_all();
            runtime = nullptr;
        }
    }

    __declspec(dllexport) void WBOX_VirtualHID_TriggerLeftMenu()
    {
        lvh::Keyboard& kb = *created.keyboard;
        SendChord(kb, kVk1, kScan1, 100);
    }

    __declspec(dllexport) void WBOX_VirtualHID_TriggerLeftInGameMenu()
    {
        lvh::Keyboard& kb = *created.keyboard;
        SendChord(kb, kVk1, kScanTab, 100);
    }

    __declspec(dllexport) void WBOX_VirtualHID_TriggerRightMenu()
    {
        lvh::Keyboard& kb = *created.keyboard;
        SendChord(kb, kVk2, kScan1, 100);
    }
}