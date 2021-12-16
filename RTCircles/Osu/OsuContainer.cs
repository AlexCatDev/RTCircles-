using Easy2D;
using Easy2D.Game;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Database;
using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using OsuParsers.Enums;
using OsuParsers.Enums.Beatmaps;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RTCircles
{
    [Flags]
    public enum Mods
    {
        Null = -1, //Null mod (literally the absence of even Nomod)
        NM = 0, //Nomod
        NF = 1, //Nofail
        EZ = 2, //Easy
        TD = 4, //Touch device
        HD = 8, //Hidden
        HR = 16, //Hardrock
        SD = 32, //Sudden death
        DT = 64, //Double time
        RX = 128, //Relax
        HT = 256, //Half time
        NC = 512, //Nightcore Only set along with DoubleTime. i.e: NC only gives 
        FL = 1024, //Flashlight
        Auto = 2048,
        SO = 4096, //Spun out
        AP = 8192, // Autopilot
        PF = 16384, // Only set along with SuddenDeath. i.e: PF only gives 16416  
        Key4 = 32768,
        Key5 = 65536,
        Key6 = 131072,
        Key7 = 262144,
        Key8 = 524288,
        FadeIn = 1048576,
        Random = 2097152,
        Cinema = 4194304,
        Target = 8388608,
        Key9 = 16777216,
        KeyCoop = 33554432,
        Key1 = 67108864,
        Key3 = 134217728,
        Key2 = 268435456,
        ScoreV2 = 536870912,
        Mirror = 1073741824,
        KeyMod = Key1 | Key2 | Key3 | Key4 | Key5 | Key6 | Key7 | Key8 | Key9 | KeyCoop,
        FreeModAllowed = NF | EZ | HD | HR | SD | FL | FadeIn | RX | AP | SO | KeyMod,
        ScoreIncreaseMods = HD | HR | DT | FL | FadeIn
    }

    public static class OsuContainer
    {
        /// <summary>
        /// The time in milliseconds from when hitobjects are hit to they reach 0 opacity, and fully exploded
        /// </summary>
        public static double Fadeout = 240;
        /// <summary>
        /// The starting scale for approach circles
        /// </summary>
        public static double ApproachCircleScale = 4;
        /// <summary>
        /// The end scale for hitobjects when they have been hit
        /// </summary>
        public static double CircleExplode = 1.3;

        //Cache this?
        public static Rectangle Playfield
        {
            get
            {
                float aspectRatio = 4f / 3f;

                float PlayfieldHeight = MainGame.WindowHeight - (MainGame.WindowHeight * 0.2f);

                float PlayfieldWidth = (int)(PlayfieldHeight * aspectRatio);

                //Max pixel gap of 100 pixels or 50 on either side then haram and change viewport height instead to match
                if (PlayfieldWidth > MainGame.WindowWidth - 100)
                {
                    PlayfieldWidth = MainGame.WindowWidth - 100;
                    PlayfieldHeight = (PlayfieldWidth / aspectRatio);
                }

                Vector2 PlayfieldTopLeft = new Vector2(MainGame.WindowCenter.X - PlayfieldWidth / 2f, MainGame.WindowCenter.Y - PlayfieldHeight / 2f);

                return new Rectangle(PlayfieldTopLeft, new Vector2(PlayfieldWidth, PlayfieldHeight));
            }
        }

        public static Rectangle FullPlayfield => new Rectangle(
            Playfield.Position - new Vector2(Beatmap?.CircleRadius ?? 0) / 2,
            Playfield.Size + new Vector2(Beatmap?.CircleRadius ?? 0));

        public static OsuDatabase Database { get; private set; } = new OsuDatabase();

        public static HUD HUD { get; private set; } = new HUD();

        public static Key Key1 = Key.Z;
        public static Key Key2 = Key.X;
        public static Key SmokeKey = Key.Space;

        public static bool Key1Down { get; private set; }
        public static bool Key2Down { get; private set; }

        public static bool EnableMouseButtons = false;

        static OsuContainer()
        {
            Key[] keys = Settings.GetValue<Key[]>("OsuKeys", out bool exists);

            if (keys is not null)
            {
                Key1 = keys[0];
                Key2 = keys[1];
            }
        }

        public static uint Score;
        public static int MaxCombo;
        public static int Combo;
        public static int Count300;
        public static int Count100;
        public static int Count50;
        public static int CountMiss;

        public static bool MuteHitsounds { get; set; }

        public static bool CookieziMode => Beatmap.Mods.HasFlag(Mods.Auto);

        public static PlayingBeatmap Beatmap { get; private set; }

        public static event Action BeatmapChanged;

        public static event Action OnKiai;

        public static Vector2 CursorPosition => CustomCursorPosition ?? Input.MousePosition;
        public static Vector2? CustomCursorPosition;

        private static double songPos;

        private static double previousSongPos;

        public static double DeltaSongPosition { get; private set; }

        public static double SongPosition
        {
            get
            {
                return songPos;
            }
            set
            {
                if (value < 0)
                    songPos = value;
                else
                {
                    Beatmap.Song.PlaybackPosition = value;
                    songPos = Beatmap.Song.PlaybackPosition;
                    previousSongPos = songPos;
                    DeltaSongPosition = 0;
                }
            }
        }

        public static TimingPoint CurrentBeatTimingPoint { get; private set; }

        public static TimingPoint CurrentTimingPoint { get; private set; }

        public static double CurrentBeat => CurrentBeatTimingPoint is not null ? (songPos - CurrentBeatTimingPoint.Offset) / CurrentBeatTimingPoint.BeatLength : 0;

        /// <summary>
        /// Pulses from 1 to 0 every beat
        /// </summary>
        public static double BeatProgress
        {
            get
            {
                if (CurrentBeatTimingPoint is null)
                    return 0;

                return GetBeatProgressAt(CurrentBeatTimingPoint.Offset);
            }
        }

        public static double GetBeatProgressAt(double time, double scale = 1f)
        {
            if (CurrentBeatTimingPoint is null)
                return 0;

            var beat = (songPos - time) / (CurrentBeatTimingPoint.BeatLength * scale);
            var beatCeil = Math.Ceiling(beat);

            return beatCeil - beat;
        }

        public static double BeatProgressKiai => IsKiaiTimeActive ? BeatProgress : 0;

        public static bool IsKiaiTimeActive => CurrentTimingPoint?.Effects == Effects.Kiai;

        public static int TotalHits => Count300 + Count100 + Count50 + CountMiss;

        public static double Accuracy
        {
            get
            {
                if (TotalHits == 0)
                    return 1.0;

                var acc = ((Count300 * 300.0) + (Count100 * 100.0) + (Count50 * 50.0)) / (TotalHits * 300);

                return acc;
            } 
        }

        public static void SetMap(Beatmap beatmap, bool generateHitObjects = true, Mods mods = Mods.NM)
        {
            /*
            if (Beatmap?.FileInfo.Name == file.Name && generateHitObjects)
            {
                if (Beatmap.HitObjects.Count == 0 || Beatmap.Mods != mods)
                    Beatmap.GenerateHitObjects(mods);

                return;
            }
            */

            Beatmap?.Song.Stop();

            Beatmap = new PlayingBeatmap(beatmap);

            //FIIIIIIIIIIIX
            GC.Collect(2, GCCollectionMode.Forced, false);

            if(generateHitObjects)
                Beatmap.GenerateHitObjects(mods);

            Utils.Log($"Map set to: {beatmap} GenObjects: {generateHitObjects} Mods: {mods}", LogLevel.Info);
            Utils.Log($"Preempt: {Beatmap.Preempt} Fadein: {Beatmap.Fadein} AR: {Beatmap.AR}", LogLevel.Info);
            Utils.Log($"Window300 {Beatmap.Window300} Window100 {Beatmap.Window100} Window50 {Beatmap.Window50} OD: {Beatmap.OD}", LogLevel.Info);
            Utils.Log($"CircleRadius {Beatmap?.CircleRadius ?? 0} CS: {Beatmap.CS}", LogLevel.Info);
            Utils.Log($"PlayfieldWidth {Playfield.Width} PlayfieldHeight {Playfield.Height}", LogLevel.Info);
            
            BeatmapChanged?.Invoke();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector2 MapToPlayfield(float x, float y, bool ignoreMods = false)
        {
            if (Beatmap.Mods.HasFlag(Mods.HR) && ignoreMods == false)
                y = 384 - y;
            
            x = MathUtils.Map(x, 0, 512, Playfield.Left, Playfield.Right);

            y = MathUtils.Map(y, 0, 384, Playfield.Top, Playfield.Bottom);

            Vector2 pos = new Vector2(x, y);
            return pos;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector2 MapToPlayfield(Vector2 pos, bool ignoreMods = false) => MapToPlayfield(pos.X, pos.Y, ignoreMods);

        public static IEnumerable<T> GetFlags<T>(this T en) where T : struct, Enum
        {
            //return Enum.GetValues(typeof(T)).Where(member => en.HasFlag(member));
            var values = Enum.GetValues(typeof(T));

            foreach (T value in values)
            {
                if (en.HasFlag(value))
                    yield return value;
            }
        }

        public static void PlayHitsound(HitSoundType type, SampleSet set)
        {
            if (MuteHitsounds)
                return;

            foreach (var hitsound in type.GetFlags())
            {
                HitSoundType hs = hitsound;
                //Convert type none to normal
                if (hitsound == HitSoundType.None)
                    hs = HitSoundType.Normal;

                if (set == SampleSet.None)
                {
                    set = CurrentTimingPoint?.SampleSet ?? Beatmap.InternalBeatmap.GeneralSection.SampleSet;

                    if (set == SampleSet.None)
                        set = SampleSet.Normal;
                }

                //first look for the map hitsounds
                var beatmapHitsound = Beatmap.Hitsounds[hs, set];

                var skinHitsound = Skin.Hitsounds[hs, set];

                if (beatmapHitsound is not null)
                {
                    //Positional hitsounds
                    beatmapHitsound.Pan = Input.MousePosition.X.Map(0, 1920, -0.5f, 0.5f);
                    beatmapHitsound.Play(true);
                }
                else if (skinHitsound is not null)
                {
                    skinHitsound.Pan = Input.MousePosition.X.Map(0, 1920, -0.5f, 0.5f);
                    skinHitsound.Play(true);
                }
                else
                {
                    Utils.Log($"Could not finding hitsound: <{hs},{set}>", LogLevel.Warning);

                    Skin.Hitsounds[HitSoundType.Normal, set]?.Play(true);
                }
            }
        }

        public static void Update(float delta)
        {
            if (Beatmap is null)
                return;

            if (songPos < 0)
            {
                Beatmap.Song.Stop();
                songPos = Math.Min(songPos + delta * 1000d * Beatmap.Song.PlaybackSpeed, 0);
                if (songPos == 0)
                    Beatmap.Song.Play(true);
            }
            else
            {
                songPos = Beatmap?.Song.PlaybackPosition + Sound.DeviceLatency ?? 0;
                DeltaSongPosition = songPos - previousSongPos;
                previousSongPos = songPos;

                for (int i = 0; i < Beatmap.InternalBeatmap.TimingPoints.Count - 1; i++)
                {
                    var nowTiming = Beatmap.InternalBeatmap.TimingPoints[i];
                    var nextTiming = Beatmap.InternalBeatmap.TimingPoints[i + 1];

                    if (nowTiming.BeatLength > 0)
                        CurrentBeatTimingPoint = nowTiming;

                    if (songPos >= nowTiming.Offset && songPos < nextTiming.Offset)
                    {
                        if (nowTiming.Effects == Effects.Kiai && CurrentTimingPoint?.Effects != Effects.Kiai)
                            OnKiai?.Invoke();

                        CurrentTimingPoint = nowTiming;

                        break;
                    }
                }
            }
        }
        public static void MouseDown(MouseButton button)
        {
            if (button == MouseButton.Left && EnableMouseButtons)
                Key1Down = true;
        }

        public static void MouseUp(MouseButton button)
        {
            if (button == MouseButton.Left && EnableMouseButtons)
                Key1Down = false;
        }

        public static void KeyDown(Key e)
        {
            if (e == Key1)
                Key1Down = true;
            else if (e == Key2)
                Key2Down = true;

#if DEBUG
            if(e == Key.Space)
            {
                if (Beatmap.Song.IsPlaying)
                    Beatmap.Song.Pause();
                else
                    Beatmap.Song.Play(false);
            }
#endif
        }

        public static void KeyUp(Key e)
        {
            if (e == Key1)
                Key1Down = false;
            else if (e == Key2)
                Key2Down = false;
        }

        public static string GetRankingLetter(int count300, int count100, int count50, int countMiss)
        {
            int totalHits = count300 + count100 + count50 + countMiss;

            var acc = ((count300 * 300.0) + (count100 * 100.0) + (count50 * 50.0)) / (totalHits * 300);

            var percent50s = ((double)count50 / totalHits) * 100.0;
            var percent300s = ((double)count300 / totalHits) * 100.0;

            if (totalHits == 0)
                acc = 1.0;

            acc *= 100;

            //SH = Hidden/Flashlight S
            //XH = Hidden/Flashlight SS
            //X = SS

            if (countMiss == 0)
            {
                if (acc == 100)
                    return Beatmap.Mods.HasFlag(Mods.HD) ? "XH" : "X";

                if (percent50s < 1.0 && percent300s > 90.0)
                    return Beatmap.Mods.HasFlag(Mods.HD) ? "SH" : "S";
                else if (percent300s > 80.0)
                    return "A";
                else if (percent300s > 70.0)
                    return "B";
                else if (percent300s > 60.0)
                    return "C";
                else
                    return "D";
            }
            else 
            {
                if (percent300s > 90.0)
                    return "A";
                else if (percent300s > 80.0)
                    return "B";
                else if (percent300s > 60.0)
                    return "C";
                else
                    return "D";
            }
        }
    }
}
