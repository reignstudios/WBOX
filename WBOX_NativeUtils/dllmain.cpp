// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <GameInput.h>
#include <wrl/client.h>

#ifndef GAMEINPUT_API_VERSION
#define GAMEINPUT_API_VERSION 0
#endif

#if GAMEINPUT_API_VERSION == 1
using namespace GameInput::v1;
#elif GAMEINPUT_API_VERSION == 2
using namespace GameInput::v2;
#elif GAMEINPUT_API_VERSION == 3
using namespace GameInput::v3;
#elif GAMEINPUT_API_VERSION > 0
#define GI_NS_JOIN2(a, b) a##b
#define GI_NS_JOIN(a, b) GI_NS_JOIN2(a, b)
using namespace GameInput::GI_NS_JOIN(v, GAMEINPUT_API_VERSION);
#endif

using Microsoft::WRL::ComPtr;

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
//IGameInput* gi = nullptr;
ComPtr<IGameInput> g_gameInput;
ComPtr<IGameInputDevice> g_gamepad;

// ===========================================
// C interop
// ===========================================
typedef struct WBOX_Gamepad
{
    int connected;
    int a, b, x, y;
    int dpadLeft, dpadRight, dpadDown, dpadUp;
    int back, menu;
    int bumperLeft, bumperRight;
    int joystickButtonLeft, joystickButtonRight;
    float joystickLeftX, joystickLeftY, joystickRightX, joystickRightY;
    float triggerLeft, triggerRight;
}WBOX_Gamepad;

extern "C"
{
    __declspec(dllexport) int WBOX_NativeUtils_InitInput()
    {
        //if (!gi) return;
        //gi = nullptr;

        // init
        HRESULT hr = GameInputCreate(&g_gameInput);
        if (FAILED(hr)) return 0;

        // enable background usage
        g_gameInput->SetFocusPolicy(GameInputEnableBackgroundInput | GameInputEnableBackgroundGuideButton | GameInputEnableBackgroundShareButton);

        return 1;
    }

    __declspec(dllexport) void WBOX_NativeUtils_DisposeInput()
    {
        g_gameInput->Release();
        /*if (gi)
        {
            gi->Release();
            gi = nullptr;
        }*/
    }

    __declspec(dllexport) void WBOX_NativeUtils_UpdateInput(WBOX_Gamepad* gamepad)
    {
        //if (!gi) return;

        // check for gamepads
        ComPtr<IGameInputReading> reading;
        HRESULT hr = g_gameInput->GetCurrentReading(GameInputKindGamepad, g_gamepad.Get(), &reading);
        if (FAILED(hr))
        {
            g_gamepad.Reset();
            gamepad->connected = 0;
            return;
        }

        // grab current gamepad
        if (!g_gamepad) reading->GetDevice(&g_gamepad);
        if (!g_gamepad)
        {
            gamepad->connected = 0;
            return;
        }

        // read gamepad state
        GameInputGamepadState state{};
        if (!reading->GetGamepadState(&state))
        {
            gamepad->connected = 0;
            return;
        }

        // copy state
        gamepad->connected = 1;

        gamepad->a = (state.buttons & GameInputGamepadA) ? 1 : 0;
        gamepad->b = (state.buttons & GameInputGamepadB) ? 1 : 0;
        gamepad->x = (state.buttons & GameInputGamepadX) ? 1 : 0;
        gamepad->y = (state.buttons & GameInputGamepadY) ? 1 : 0;

        gamepad->dpadLeft = (state.buttons & GameInputGamepadDPadLeft) ? 1 : 0;
        gamepad->dpadRight = (state.buttons & GameInputGamepadDPadRight) ? 1 : 0;
        gamepad->dpadDown = (state.buttons & GameInputGamepadDPadDown) ? 1 : 0;
        gamepad->dpadUp = (state.buttons & GameInputGamepadDPadUp) ? 1 : 0;

        gamepad->back = (state.buttons & GameInputGamepadView) ? 1 : 0;
        gamepad->menu = (state.buttons & GameInputGamepadMenu) ? 1 : 0;

        gamepad->bumperLeft = (state.buttons & GameInputGamepadLeftShoulder) ? 1 : 0;
        gamepad->bumperRight = (state.buttons & GameInputGamepadRightShoulder) ? 1 : 0;

        gamepad->joystickButtonLeft = (state.buttons & GameInputGamepadLeftThumbstick) ? 1 : 0;
        gamepad->joystickButtonRight = (state.buttons & GameInputGamepadRightThumbstick) ? 1 : 0;

        gamepad->joystickLeftX = state.leftThumbstickX;
        gamepad->joystickLeftY = state.leftThumbstickY;
        gamepad->joystickRightX = state.rightThumbstickX;
        gamepad->joystickRightY = state.rightThumbstickY;

        gamepad->triggerLeft = state.leftTrigger;
        gamepad->triggerRight = state.rightTrigger;
    }
}