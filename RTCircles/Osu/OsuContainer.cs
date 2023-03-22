﻿using Easy2D;
using Easy2D.Game;
using Newtonsoft.Json;
using System.Numerics;
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
        public static double CircleExplodeScale = 1.4;

        private static Vector2 lastWindowSize;

        //Cache of playfield
        private static Rectangle _playfield;
        public static Rectangle Playfield
        {
            get
            {
                var windowSize = MainGame.WindowSize;

                //If the viewport hasnt changed, return the cached value.
                if (lastWindowSize == windowSize)
                    return _playfield;

                //Else update the playfield and recache.
                lastWindowSize = windowSize;

                const float aspectRatio = 4f / 3f;

                //Playfield height is 80% of the window height
                float PlayfieldHeight = MainGame.WindowHeight * 0.8f;

                //Playfield width is playfield height * 4:3 aspect ratio
                float PlayfieldWidth = PlayfieldHeight * aspectRatio;

                //If there isn't enough space on screen for the playfield
                //Max pixel gap of 100 pixels or 50 on either side then haram and change viewport height instead to match
                if (PlayfieldWidth > MainGame.WindowWidth - 100)
                {
                    PlayfieldWidth = MainGame.WindowWidth - 100;
                    PlayfieldHeight = (PlayfieldWidth / aspectRatio);
                }

                OsuScale = PlayfieldHeight / 384;

                Vector2 PlayfieldTopLeft = new Vector2(MainGame.WindowCenter.X - PlayfieldWidth / 2f, MainGame.WindowCenter.Y - PlayfieldHeight / 2f);

                //Offset the playfield down by 2% of it's height like stable
                PlayfieldTopLeft.Y += PlayfieldHeight * 0.020f;

                //i should probably cast these to Vector2i
                _playfield = new Rectangle(PlayfieldTopLeft, new Vector2(PlayfieldWidth, PlayfieldHeight));

                return _playfield;
            }
        }

        public static float OsuScale { get; private set; }

        public static Rectangle FullPlayfield
        {
            get
            {
                //CS 0 circle size to screen size
                var magicOsuRadius = 70f * OsuScale;

                return new Rectangle(
                Playfield.Position - new Vector2(magicOsuRadius) / 2,
                Playfield.Size + new Vector2(magicOsuRadius));
            }
        }

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

        private static bool _muteHitsounds;
        public static bool MuteHitsounds
        {
            get
            {
                return ScreenManager.ActiveScreen is MenuScreen or SongSelectScreen ? true : _muteHitsounds;
            }
            set
            {
                _muteHitsounds = value;
            }
        }

        public static bool CookieziMode => ScreenManager.ActiveScreen is not OsuScreen || Beatmap.Mods.HasFlag(Mods.Auto);

        public static PlayableBeatmap Beatmap { get; private set; }

        public static event Action BeatmapChanged;
        public static event Action<PlayableBeatmap?> OnBeatmapChanged;

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
                    if (Beatmap?.Song != null)
                    {

                        Beatmap.Song.PlaybackPosition = value;
                        songPos = Beatmap.Song.PlaybackPosition;
                    }
                    else
                    {
                        songPos = value;
                    }

                    DeltaSongPosition = 0;
                    previousSongPos = songPos;
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

        public static double GetBeatCountFrom(double time, double scale = 1f)
        {
            if (CurrentBeatTimingPoint is null)
                return 0;

            return (songPos - time) / (CurrentBeatTimingPoint.BeatLength / scale);
        }

        public static double GetBeatProgressAt(double time, double scale = 1f, double? beatLength = null)
        {
            if (CurrentBeatTimingPoint is null)
                return 0;

            var beat = (songPos - time) / (beatLength ?? CurrentBeatTimingPoint.BeatLength * scale);
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
        }

        public static void SetMap(PlayableBeatmap beatmap)
        {
            var currentBeatmap = Beatmap;
            Beatmap?.Song?.Stop();

            Beatmap = beatmap;
            BeatmapChanged?.Invoke();
            OnBeatmapChanged?.Invoke(currentBeatmap);
        }

        public static bool SetMap(CarouselItem carouselItem, bool generateHitObjects = true, Mods mods = Mods.NM)
        {
            var currentBeatmap = Beatmap;

            var newBeatmap = PlayableBeatmap.FromCarouselItem(carouselItem);

            if (newBeatmap == null)
                return false;

            if (Beatmap?.AudioPath != newBeatmap.AudioPath)
            {
                //Fade out the current track
                ManagedBass.Bass.ChannelSlideAttribute(Beatmap.Song, ManagedBass.ChannelAttribute.Volume, 0, 250);
                //Beatmap?.Song.Stop();
            }

            if (generateHitObjects)
                newBeatmap.GenerateHitObjects(mods);

            Beatmap = newBeatmap;

            Utils.Log($"Map set to: {carouselItem.FullPath} GenObjects: {generateHitObjects} Mods: {mods}", LogLevel.Info);

            BeatmapChanged?.Invoke();
            OnBeatmapChanged?.Invoke(currentBeatmap);

            return true;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector2 MapToPlayfield(float x, float y, bool ignoreMods = false)
        {
            //If HR flip everything in the y direction
            if (ignoreMods == false && Beatmap.Mods.HasFlag(Mods.HR))
                y = 384 - y;

            //Osu hitobjects are in the coordinate system X: 0-512 and Y: 0-384, we need to map these to our playfield, which is based on the current screen size

            x = MathUtils.Map(x, 0, 512, Playfield.Left, Playfield.Right);
            y = MathUtils.Map(y, 0, 384, Playfield.Top, Playfield.Bottom);

            Vector2 pos = new Vector2(x, y);

            //pos = MathUtils.RotateAroundOrigin(pos, Playfield.Center, (float)MainGame.Instance.TotalTime);

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

        public static HitResult CheckHitResult(HitObject obj)
        {
            double t = Math.Abs(SongPosition - obj.StartTime);

            if (t < Beatmap.Window300)
                return HitResult.Max;

            if (t < Beatmap.Window100)
                return HitResult.Good;

            if (t < Beatmap.Window50)
                return HitResult.Meh;

            return HitResult.Miss;
        }

        public static Action<Vector2, HitResult> OnHitObjectHit;
        public static void ScoreHit(HitObject obj)
        {
            Update(0f);

            Vector2 position = CustomCursorPosition ?? Input.MousePosition;

            Vector2 objPos = MapToPlayfield(obj.Position.X, obj.Position.Y);

            //0 == outer edge, 1 == Spot on.
            float distanceCenter = (position - objPos).Length().Map(0, Beatmap.CircleRadius, 1, 0);

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

            OnHitObjectHit?.Invoke(position - objPos, timeJudgement);

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
                    beatmapHitsound.Pan = position.X.Map(0, MainGame.WindowWidth, -0.5f, 0.5f).Clamp(-0.5f, 0.5f);
                    beatmapHitsound.Play(true);
                }
                else if (skinHitsound is not null)
                {
                    skinHitsound.Pan = position.X.Map(0, MainGame.WindowWidth, -0.5f, 0.5f).Clamp(-0.5f, 0.5f);
                    skinHitsound.Play(true);
                }
                else
                {
                    Utils.Log($"Could not finding hitsound: <{hs},{set}>", LogLevel.Warning);

                    Skin.Hitsounds[HitSoundType.Normal, set]?.Play(true);
                }
            }
        }

        public static int GlobalOffset => GlobalOptions.SongOffsetMS.Value;

        private static int totalOffset => Sound.DeviceLatency + GlobalOffset;

        public static void Update(double delta)
        {
            if (Beatmap == null || Beatmap.Song == null || !Beatmap.Song.IsFunctional)
                songPos += delta;
            else
            {
                if (songPos < totalOffset)
                {
                    Beatmap.Song.Stop();
                    songPos = Math.Min(songPos + delta * Beatmap.Song.Tempo, totalOffset);
                    if (songPos == totalOffset)
                        Beatmap.Song.Play(true);
                }
                else if (songPos >= Beatmap.Song.Duration && Beatmap.Song.IsStopped)
                {
                    songPos += delta * Beatmap.Song.Tempo;
                }
                else
                {
                    songPos = Beatmap?.Song.PlaybackPosition + totalOffset ?? 0;
                }
            }

            if (CurrentTimingPoint?.Offset > songPos)
                CurrentTimingPoint = null;

            if (Beatmap != null)
            {
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

        public static OsuTexture CurrentRankingToTexture()
        {
            switch (GetCurrentRanking())
            {
                case OsuContainer.Ranking.X:
                    return Skin.RankingX;
                case OsuContainer.Ranking.XH:
                    return Skin.RankingXH;
                case OsuContainer.Ranking.S:
                    return Skin.RankingS;
                case OsuContainer.Ranking.SH:
                    return Skin.RankingSH;
                case OsuContainer.Ranking.A:
                    return Skin.RankingA;
                case OsuContainer.Ranking.B:
                    return Skin.RankingB;
                case OsuContainer.Ranking.C:
                    return Skin.RankingC;
                case OsuContainer.Ranking.D:
                    return Skin.RankingD;
                default:
                    throw new Exception("Wtf");
            }
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
