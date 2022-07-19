using Easy2D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PogGame
{
    public static class AudioMain
    {
        public static Sound Song = 
            new Sound(File.OpenRead(@"C:\Users\user\Desktop\osu!\Songs\1604940 Hoshimachi Suisei - comet [no video]\audio.mp3"));

        static AudioMain()
        {
            Song?.Play(true);
        }

        public static double BPM = 162;

        public static int Offset = 20;

        public static double BeatLength => 60000 / BPM;

        public static double PlaybackPosition => Song?.PlaybackPosition ?? 0;

        public static double CurrentBeat
        {
            get
            {
                if(Song == null)
                    return 0;

                return (Song.PlaybackPosition - Offset) / BeatLength;
            }
        }

        public static double BeatProgress
        {
            get
            {
                if(Song == null)
                    return 0;

                return ((PlaybackPosition - Offset) % BeatLength) / BeatLength;
            }
        }
    }
}
