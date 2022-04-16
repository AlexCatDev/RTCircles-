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
        public static double CircleExplodeScale = 1.5;

        private static Vector2 lastViewport;

        private static Rectangle _playfield;
        //Cache this?
        public static Rectangle Playfield
        {
            get
            {
                var windowSize = MainGame.WindowSize;
                if (lastViewport != windowSize)
                {
                    //Console.WriteLine("Cached viewport.");
                    lastViewport = windowSize;

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

                    _playfield = new Rectangle(PlayfieldTopLeft, new Vector2(PlayfieldWidth, PlayfieldHeight));
                }

                return _playfield;
            }
        }

        public static Rectangle FullPlayfield => new Rectangle(
            Playfield.Position - new Vector2(Beatmap?.CircleRadius ?? 0) / 2,
            Playfield.Size + new Vector2(Beatmap?.CircleRadius ?? 0));

        public static HUD HUD { get; private set; } = new HUD();

        public static Key Key1 = Key.Z;
        public static Key Key2 = Key.X;
        public static Key SmokeKey = Key.Space;

        public static bool Key1Down { get; private set; }
        public static bool Key2Down { get; private set; }

        public static bool EnableMouseButtons => GlobalOptions.EnableMouseButtons.Value;

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

        public static bool CookieziMode => ScreenManager.ActiveScreen is SongSelectScreen || ScreenManager.ActiveScreen is MapSelectScreen || Beatmap.Mods.HasFlag(Mods.Auto);

        public static PlayableBeatmap Beatmap { get; private set; }

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

        public static void UnloadMap()
        {
            Beatmap = null;
            //GC.Collect(2, GCCollectionMode.Forced, false);
        }

        public static void SetMap(PlayableBeatmap beatmap)
        {
            Beatmap = beatmap;
            BeatmapChanged?.Invoke();
        }

        public static void SetMap(Beatmap beatmap, bool generateHitObjects = true, Mods mods = Mods.NM)
        {
            Beatmap?.Song.Stop();

            Beatmap = new PlayableBeatmap(beatmap);

            //FIIIIIIIIIIIX
            //GC.Collect(2, GCCollectionMode.Forced, false);

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
            if (ignoreMods == false && Beatmap.Mods.HasFlag(Mods.HR))
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

        public static void ScoreHit(HitObject obj)
        {
            Update(0f);

            Vector2 position = CustomCursorPosition ?? Input.MousePosition;

            Vector2 objPos = MapToPlayfield(obj.Position.X, obj.Position.Y);

            //0 == outer edge, 1 == Spot on.
            float distanceCenter = (position - objPos).Length.Map(0, Beatmap.CircleRadius, 1, 0);

            double timeDiff = Math.Abs(obj.StartTime - SongPosition);

            HitResult timeJudgement;
            HitResult hitJudgement;

            if (distanceCenter > 0.66)
                hitJudgement = HitResult.Max;
            else if (distanceCenter > 0.33)
                hitJudgement = HitResult.Good;
            else
                hitJudgement = HitResult.Meh;

            //0 == Badest possible value, right on the edge of a miss, 1 = perfectly on time
            double distanceNextJudgement = 0;

            if (timeDiff < Beatmap.Window300)
            {
                timeJudgement = HitResult.Max;
                distanceNextJudgement = timeDiff.Map(Beatmap.Window300, 0, 0, 1);
            }
            else if (timeDiff < Beatmap.Window100)
            {
                timeJudgement = HitResult.Good;
                distanceNextJudgement = timeDiff.Map(Beatmap.Window100, Beatmap.Window300, 0, 1);
            }
            else if (timeDiff < Beatmap.Window50)
            {
                timeJudgement = HitResult.Meh;
                distanceNextJudgement = timeDiff.Map(Beatmap.Window50, Beatmap.Window100, 0, 1);
            }
            else
                timeJudgement = HitResult.Miss;

            //distanceCenter: 0 == outer edge of circle, 1 == Spot on.
            //distanceNextJudgement: The more towards an upgrade to the next hitjudgement, the closer to 1, if judgement is 300, 1 is perfectly on time

            int score = (int)Math.Floor(((double)timeJudgement * 0.90) + ((double)hitJudgement * 0.10) + (26 * distanceNextJudgement));

            //Console.WriteLine($"Scored hit! Result: {timeJudgement} centerHit: {distanceCenter:F4} perfectMS: {distanceNextJudgement:F4} score: {score}");
        }

        //Ad
        public static void PlayHitsound(HitSoundType type, SampleSet set)
        {
            if (MuteHitsounds)
                return;

            Vector2 position = CustomCursorPosition ?? Input.MousePosition;

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

                int sampleSetIndex = CurrentTimingPoint?.CustomSampleSet ?? 0;

                //first look for the map hitsounds
                var beatmapHitsound = Beatmap.Hitsounds?[hs, set, sampleSetIndex];

                var skinHitsound = Skin.Hitsounds[hs, set, 0];

                if (beatmapHitsound is not null)
                {
                    //Positional hitsounds
                    beatmapHitsound.Pan = position.X.Map(0, MainGame.WindowWidth, -0.5f, 0.5f);
                    beatmapHitsound.Play(true);
                }
                else if (skinHitsound is not null)
                {
                    skinHitsound.Pan = position.X.Map(0, MainGame.WindowWidth, -0.5f, 0.5f);
                    skinHitsound.Play(true);
                }
                else
                {
                    Utils.Log($"Could not finding hitsound: <{hs},{set}>", LogLevel.Warning);

                    Skin.Hitsounds[HitSoundType.Normal, set]?.Play(true);
                }
            }
        }

        public static int GlobalOffset => 0;

        private static int totalOffset => Sound.DeviceLatency + GlobalOffset;

        public static void Update(float delta)
        {
            if (Beatmap is null)
                return;

            if (songPos < totalOffset)
            {
                Beatmap.Song.Stop();
                songPos = Math.Min(songPos + delta * 1000d * Beatmap.Song.PlaybackSpeed, totalOffset);
                if (songPos == totalOffset)
                    Beatmap.Song.Play(true);
            }
            else if (songPos >= Beatmap.Song.PlaybackLength && Beatmap.Song.IsStopped)
            {
                songPos += delta * 1000f * Beatmap.Song.PlaybackSpeed;
            }
            else
            {
                songPos = Beatmap?.Song.PlaybackPosition + totalOffset ?? 0;
            }

            if (CurrentTimingPoint?.Offset > songPos)
                CurrentTimingPoint = null;

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

            DeltaSongPosition = songPos - previousSongPos;
            previousSongPos = songPos;
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
            if(e == Key.End)
            {
                if (Beatmap.Song.IsPlaying)
                    Beatmap.Song.Pause();
                else
                    Beatmap.Song.Play(false);
            }

            if (e == Key.Pause)
            {
                OnKiai?.Invoke();
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

        public enum Ranking
        {
            /// <summary>
            /// SS
            /// </summary>
            X,
            /// <summary>
            /// SS with hidden mod
            /// </summary>
            XH,
            S,
            /// <summary>
            /// S with hidden mod
            /// </summary>
            SH,
            A,
            B,
            C,
            D
        }

        public static Ranking GetCurrentRanking()
        {
            int totalHits = Count300 + Count100 + Count50 + CountMiss;

            var acc = ((Count300 * 300.0) + (Count100 * 100.0) + (Count50 * 50.0)) / (totalHits * 300);

            var percent50s = ((double)Count50 / totalHits) * 100.0;
            var percent300s = ((double)Count300 / totalHits) * 100.0;

            if (totalHits == 0)
                acc = 1.0;

            acc *= 100;

            //SH = Hidden/Flashlight S
            //XH = Hidden/Flashlight SS
            //X = SS

            if (CountMiss == 0)
            {
                if (acc == 100)
                    return Beatmap.Mods.HasFlag(Mods.HD) ? Ranking.XH : Ranking.X;

                if (percent50s < 1.0 && percent300s > 90.0)
                    return Beatmap.Mods.HasFlag(Mods.HD) ? Ranking.SH : Ranking.S;
                else if (percent300s > 80.0)
                    return Ranking.A;
                else if (percent300s > 70.0)
                    return Ranking.B;
                else if (percent300s > 60.0)
                    return Ranking.C;
                else
                    return Ranking.D;
            }
            else
            {
                if (percent300s > 90.0)
                    return Ranking.A;
                else if (percent300s > 80.0)
                    return Ranking.B;
                else if (percent300s > 60.0)
                    return Ranking.C;
                else
                    return Ranking.D;
            }
        }
    }
}
