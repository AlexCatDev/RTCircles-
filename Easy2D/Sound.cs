using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;
using ManagedBass.Fx;
namespace Easy2D
{
    public class Sound
    {
        #region STATIC

        public struct AudioDevice
        {
            public DeviceInfo Info;
            public int Index;
        }

        public static double GlobalVolume
        {
            get { return Bass.GlobalStreamVolume / 10000d; }
            set { Bass.GlobalStreamVolume = (int)(10000d * value.Clamp(0, 1)); }
        }

        public static int DeviceLatency;

        private static List<int> allHandles = new List<int>();

        private static List<AudioDevice> allDevices = new List<AudioDevice>();

        public static IReadOnlyList<AudioDevice> GetDevices() => allDevices.AsReadOnly();

        private static IntPtr windowPtr;

        /// <summary>
        /// INIT MATE XD
        /// </summary>
        /// <param name="deviceIndex"></param>
        /// <param name="frequency"></param>
        /// <param name="flags"></param>
        /// <param name="window"></param>
        public static void Init(IntPtr window = default)
        {
            windowPtr = window;

            for (int i = 1; i < Bass.DeviceCount; i++)
            {
                DeviceInfo info;
                Bass.GetDeviceInfo(i, out info);
                allDevices.Add(new AudioDevice() { Info = info, Index = i});
            }

            foreach (var device in GetDevices())
            {
                string isDefault = device.Info.IsDefault ? "Default " : "";

                Utils.Log($"{isDefault}AudioDevice: {device.Index} : {device.Info.Name}", device.Info.IsDefault ? LogLevel.Important : LogLevel.Debug);

                if (device.Info.IsDefault)
                    SetDevice(device);
            }
        }

        public static void SetDevice(AudioDevice device)
        {
            Bass.Init(device.Index, 44100, DeviceInitFlags.Latency | DeviceInitFlags.Stereo | DeviceInitFlags.Bits16, windowPtr);

            for (int i = 0; i < allHandles.Count; i++)
            {
                Bass.ChannelSetDevice(allHandles[i], device.Index);
            }

            Bass.CurrentDevice = device.Index;

            DeviceLatency = Bass.Info.Latency;
            Utils.Log($"Audio device set to: {device.Info.Name} Latency: {DeviceLatency} Version: {Bass.Version}", LogLevel.Important);
        }
#endregion
        private int handle;

        public bool IsPlaying => Bass.ChannelIsActive(handle) == PlaybackState.Playing;
        public bool IsPaused => Bass.ChannelIsActive(handle) == PlaybackState.Paused;

        public bool IsStopped => Bass.ChannelIsActive(handle) == PlaybackState.Stopped;

        public bool IsStalled => Bass.ChannelIsActive(handle) == PlaybackState.Stalled;

        public double DefaultFrequency { get; private set; }

        /// <summary>
        /// Get fft data from current audio
        /// </summary>
        /// <param name="data"></param>
        public void GetFFTData(float[] data, DataFlags flags)
        {
            Bass.ChannelGetData(handle, data, (int)flags);
        }

        /// <summary>
        /// Get raw data from current audio
        /// </summary>
        /// <param name="data"></param>
        public void GetRawData(float[] data)
        {
            Bass.ChannelGetData(handle, data, (data.Length * 4) + (int)DataFlags.Float);
        }

        public float[] GetLevels(float duration, LevelRetrievalFlags flags)
        {
            return Bass.ChannelGetLevel(handle, duration.Clamp(0, 1), flags);
        }

        public int GetLevel()
        {
            return Bass.ChannelGetLevel(handle);
        }

        public double Frequency
        {
            get
            {
                return Bass.ChannelGetAttribute(handle, ChannelAttribute.Frequency);
            }
            set
            {
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Frequency, value);
            }
        }

        public double Volume
        {
            get
            {
                return Bass.ChannelGetAttribute(handle, ChannelAttribute.Volume);
            }
            set
            {
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Volume, value);
            }
        }

        /// <summary>
        /// -1 Full Left, 0 Centre (Default), +1 Full Right
        /// </summary>
        public double Pan
        {
            get 
            { 
                return Bass.ChannelGetAttribute(handle, ChannelAttribute.Pan); 
            }
            set
            {
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Pan, value.Clamp(-1, 1));
            }
        }

        public double PlaybackPosition
        {
            get
            {
                return Bass.ChannelBytes2Seconds(handle, Bass.ChannelGetPosition(handle, PositionFlags.Bytes)) * 1000d;
            }
            set
            {
                double clampedTime = Math.Max(value / 1000d, 0);
                var inBytes = Bass.ChannelSeconds2Bytes(handle, clampedTime);

                if (inBytes != Bass.ChannelGetPosition(handle))
                    Bass.ChannelSetPosition(handle, inBytes);
            }
        }

        public double PlaybackLength
        {
            get
            {
                return Bass.ChannelBytes2Seconds(handle, Bass.ChannelGetLength(handle)) * 1000d;
            }
        }

        /// <summary>
        /// Playback Speed 1 being normal speed, 0.5 being half and 2 being double speed
        /// </summary>
        public double PlaybackSpeed
        {
            get
            {
                double speed = Bass.ChannelGetAttribute(handle, ChannelAttribute.Tempo);
                speed /= 100;
                speed += 1;

                return speed;
            }
            set
            {
                double speed = value * 100 - 100;
                Bass.ChannelSetAttribute(handle, ChannelAttribute.Tempo, speed);
            }
        }

        public void Play(bool restart = false)
        {
            Bass.ChannelPlay(handle, restart);
        }

        public void Pause()
        {
            Bass.ChannelPause(handle);
        }

        public void Stop()
        {
            Bass.ChannelStop(handle);
        }

        public void SetFX<T>(Effect<T> effect) where T : class, IEffectParameter, new() => effect.ApplyOn(handle);

        ~Sound()
        {
            Utils.Log($"Deleting Sound handle -> [{handle}]", LogLevel.Info);
            allHandles.Remove(handle);
            bool success = Bass.StreamFree(handle);
            if (success == false)
                Utils.Log($"Bass Sound handle deletion failed! -> {Bass.LastError}", LogLevel.Error);
        }

        /// <summary>
        /// Create music woaaa
        /// </summary>
        /// <param name="stream">The input data to the file, which will be read to end and copied internally</param>
        /// <param name="useFX">Allow the usage of fx? this has some drawbacks, forexample, major delay on playing the sound and a lag spike</param>
        /// <param name="noBuffer">NoBuffer remidies this, but fft is broken among some other stuf probably</param>
        public Sound(Stream stream, bool useFX = false, bool noBuffer = false)
        {
            if(stream is null)
            {
                Utils.Log($"Error stream was null!", LogLevel.Error);
                return;
            }

            using (MemoryStream ms = new MemoryStream()) {
                stream.CopyTo(ms);
                byte[] data = ms.ToArray();
                if (useFX)
                {
                    handle = BassFx.TempoCreate(Bass.CreateStream(data, 0, data.Length, BassFlags.Decode | BassFlags.Prescan), BassFlags.Default | BassFlags.FxFreeSource);
                }
                else
                {
                    handle = Bass.CreateStream(data, 0, data.Length, BassFlags.Prescan);
                }

                if(noBuffer)
                    Bass.ChannelSetAttribute(handle, ChannelAttribute.NoBuffer, 1);
            }

            DefaultFrequency = Frequency;

            if (Bass.LastError != Errors.OK)
                Utils.Log($"BASS error when loading audio: {Bass.LastError}", LogLevel.Error);
            else
                allHandles.Add(handle);
        }
    }

}
