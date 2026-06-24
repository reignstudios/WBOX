using System;
using System.Threading;
using NAudio.CoreAudioApi;

namespace WBOX
{
    static class Audio
    {
        public static float GetVolume()
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.AudioEndpointVolume.MasterVolumeLevelScalar;
        }

        public static void SetVolume(float volume)
        {
            volume = Math.Max(0f, volume);
            volume = Math.Min(1f, volume);
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
        }

        public static float AdjustVolume(float step)
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            float volume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
            volume += step;
            volume = Math.Max(0f, volume);
            volume = Math.Min(1f, volume);
            device.AudioEndpointVolume.MasterVolumeLevelScalar = volume;
            return volume;
        }

        public static bool MuteToggle()
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
        }
    }
}
