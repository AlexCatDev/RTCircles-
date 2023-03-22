using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;
using ManagedBass.Fx;

namespace Easy2D
{
    public struct SoundSync
    {
        private int handle;

        private SoundSync(int handle) { this.handle = handle; }

        public static implicit operator int (SoundSync sync) => sync.handle;
        public static implicit operator SoundSync(int sync) => new SoundSync(sync);
    }

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
            Bass.Init(device.Index, 44100, DeviceInitFlags.Latency | DeviceInitFlags.Stereo, windowPtr);
            for (int i = 0; i < allHandles.Count; i++)
            {
                Bass.ChannelSetDevice(allHandles[i], device.Index);
            }

            Bass.CurrentDevice = device.Index;

            DeviceLatency = Bass.Info.Latency;
            Utils.Log($"Audio device set to: {device.Info.Name} Latency: {DeviceLatency} Version: {Bass.Version}", LogLevel.Important);
        }
        #endregion
        public int Handle { get; private set; } = 0;

        /// <summary>
        /// Returns if the Sound is functional IE. if it has a valid handle, will be null if it fails to load
        /// </summary>
        public bool IsFunctional => Handle != 0;

        public bool IsPlaying => Bass.ChannelIsActive(Handle) == PlaybackState.Playing;
        public bool IsPaused => Bass.ChannelIsActive(Handle) == PlaybackState.Paused;

        public bool IsStopped => Bass.ChannelIsActive(Handle) == PlaybackState.Stopped;

        public bool IsStalled => Bass.ChannelIsActive(Handle) == PlaybackState.Stalled;

        public bool SupportsFX { get; private set; }

        public double DefaultFrequency { get; private set; }

        /// <summary>
        /// Get fft data from current audio
        /// </summary>
        /// <param name="data"></param>
        public void GetFFTData(float[] data, DataFlags flags)
        {
            Bass.ChannelGetData(Handle, data, (int)flags);
        }

        /// <summary>
        /// Get raw data from current audio
        /// </summary>
        /// <param name="data"></param>
        public void GetRawData(float[] data)
        {
            Bass.ChannelGetData(Handle, data, (data.Length * 4) + (int)DataFlags.Float);
        }

        public float[] GetLevels(float duration, LevelRetrievalFlags flags)
        {
            return Bass.ChannelGetLevel(Handle, duration.Clamp(0, 1), flags);
        }

        public int GetLevel()
        {
            return Bass.ChannelGetLevel(Handle);
        }

        public void SlideAttribute(ChannelAttribute attribute, float value, int time, bool logarithmic = false)
        {
            Bass.ChannelSlideAttribute(Handle, attribute, value, time, logarithmic);
        }

        public SoundSync SetSync(SyncFlags flag, long parameter, SyncProcedure procedure, IntPtr user = default)
        {
            return Bass.ChannelSetSync(Handle, flag, parameter, procedure, user);
        }

        public void RemoveSync(SoundSync sync)
        {
            Bass.ChannelRemoveSync(Handle, sync);
        }

        public bool AddFlag(BassFlags flag) => Bass.ChannelAddFlag(Handle, flag);

        public bool HasFlag(BassFlags flag) => Bass.ChannelHasFlag(Handle, flag);

        public bool RemoveFlag(BassFlags flag) => Bass.ChannelRemoveFlag(Handle, flag);

        public double Frequency
        {
            get
            {
                return Bass.ChannelGetAttribute(Handle, ChannelAttribute.Frequency);
            }
            set
            {
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Frequency, value);
            }
        }

        /// <summary>
        /// 0 = silent, 1 = full
        /// </summary>
        public double Volume
        {
            get
            {
                return Bass.ChannelGetAttribute(Handle, ChannelAttribute.Volume);
            }
            set
            {
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Volume, value);
            }
        }

        /// <summary>
        /// -1 Full Left, 0 Centre (Default), +1 Full Right
        /// </summary>
        public double Pan
        {
            get 
            { 
                return Bass.ChannelGetAttribute(Handle, ChannelAttribute.Pan); 
            }
            set
            {
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Pan, value.Clamp(-1, 1));
            }
        }

        /// <summary>
        /// The pitch in semitones -60 - 60
        /// </summary>
        public double Pitch
        {
            get
            {
                return Bass.ChannelGetAttribute(Handle, ChannelAttribute.Pitch);
            }
            set
            {
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Pitch, value.Clamp(-60, 60));
            }
        }

        /// <summary>
        /// Playback position in milliseconds
        /// </summary>
        public double PlaybackPosition
        {
            get
            {
                return Bass.ChannelBytes2Seconds(Handle, Bass.ChannelGetPosition(Handle, PositionFlags.Bytes)) * 1000d;
            }
            set
            {
                double clampedTime = Math.Max(value / 1000d, 0);
                var inBytes = Bass.ChannelSeconds2Bytes(Handle, clampedTime);

                if (inBytes != Bass.ChannelGetPosition(Handle))
                    Bass.ChannelSetPosition(Handle, inBytes);
            }
        }

        /// <summary>
        /// The duration of the sound in seconds
        /// </summary>
        public double Duration
        {
            get
            {
                return Bass.ChannelBytes2Seconds(Handle, Bass.ChannelGetLength(Handle)) * 1000d;
            }
        }

        /// <summary>
        /// Playback Speed 1 being normal speed, 0.5 being half and 2 being double speed
        /// </summary>
        public double Tempo
        {
            get
            {
                if (!SupportsFX)
                    return Frequency / DefaultFrequency;

                double speed = Bass.ChannelGetAttribute(Handle, ChannelAttribute.Tempo);
                speed /= 100;
                speed += 1;

                return speed;
            }
            set
            {
                if (!SupportsFX)
                {
                    Frequency = DefaultFrequency * value;
                    return;
                }

                double speed = value * 100 - 100;
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Tempo, speed);
            }
        }

        /// <summary>
        /// The real playback speed calculated using tempo and the frequency
        /// </summary>
        public double TotalPlaybackSpeed
        {
            get
            {
                double tempo = (Bass.ChannelGetAttribute(Handle, ChannelAttribute.Tempo) * 0.01) + 1;

                return tempo + Frequency / DefaultFrequency;
            }
        }

        public void Play(bool restart = false)
        {
            Bass.ChannelPlay(Handle, restart);
        }

        public void Pause()
        {
            Bass.ChannelPause(Handle);
        }

        public void Stop()
        {
            Bass.ChannelStop(Handle);
        }

        public void SetFX<T>(Effect<T> effect) where T : class, IEffectParameter, new() => effect.ApplyOn(Handle);

        public static implicit operator int (Sound sound) => sound.Handle;

        ~Sound()
        {
            Utils.Log($"Deleting Sound handle -> [{Handle}]", LogLevel.Debug);
            allHandles.Remove(Handle);
            bool success = Bass.StreamFree(Handle);
            if (success == false)
                Utils.Log($"Bass Sound handle deletion failed! -> {Bass.LastError}", LogLevel.Error);
        }

        private Sound() { }

        public static Sound FromFile(string file, int? bufferingLength = null)
        {
            Sound sound = new();

            sound.Handle = BassFx.TempoCreate(Bass.CreateStream(file, 0, 0, BassFlags.Decode | BassFlags.AsyncFile | BassFlags.Prescan), BassFlags.Default | BassFlags.FxFreeSource);

            if(bufferingLength.HasValue)
                Bass.ChannelSetAttribute(sound.Handle, ChannelAttribute.Buffer, bufferingLength.Value);

            return sound;
        }

        /// <summary>
        /// Create music woaaa
        /// </summary>
        /// <param name="stream">The input data to the file, which will be read to end and copied internally</param>
        /// <param name="useFX">Allow the usage of fx? this has some drawbacks, forexample, major delay on playing the sound and a lag spike</param>
        /// <param name="noBuffer">NoBuffer remedies this, but fft is broken among some other stuf probably</param>
        public Sound(Stream stream, bool useFX = false, bool noBuffer = false, BassFlags bassFlags = BassFlags.Default)
        {
            if (stream is null)
            {
                Utils.Log($"Error stream was null!", LogLevel.Error);
                GC.SuppressFinalize(this);
                return;
            }

            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            if (useFX)
                Handle = BassFx.TempoCreate(Bass.CreateStream(data, 0, data.Length, BassFlags.Decode | bassFlags), BassFlags.Default | BassFlags.FxFreeSource);
            else
                Handle = Bass.CreateStream(data, 0, data.Length, bassFlags);

            if (noBuffer)
                Bass.ChannelSetAttribute(Handle, ChannelAttribute.Buffer, 0);

            DefaultFrequency = Frequency;
            SupportsFX = useFX;

            if (Bass.LastError != Errors.OK)
                Utils.Log($"BASS error when loading audio: {Bass.LastError}", LogLevel.Error);
            else
                allHandles.Add(Handle);
        }
    }

}
