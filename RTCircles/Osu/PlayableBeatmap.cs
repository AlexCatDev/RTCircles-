using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Database.Objects;
using OsuParsers.Enums;
using OsuParsers.Storyboards.Commands;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class AutoGenerator
    {
        public struct KeyFrame
        {
            public Vector2 Position;
            public double Time;
            public bool IsSliderSlide;
        }

        private List<KeyFrame> frames = new List<KeyFrame>();

        private int frameIndex = 0;
        private KeyFrame? first;

        public Vector2 CurrentPosition { get; private set; } = new Vector2(512, 384) / 2;

        private Vector2 onFrame(double time, KeyFrame from, KeyFrame to, int index)
        {
            bool cursorDance = GlobalOptions.AutoCursorDance.Value;

            if (!cursorDance && to.IsSliderSlide)
                return DrawableSlider.SliderBallPositionForAuto;

            float blend = Interpolation.ValueAt(time, 0, 1, from.Time, to.Time);

            var easing = (from.Position - to.Position).Length < OsuContainer.Beatmap.CircleRadiusInOsuPixels * 2 ? EasingTypes.None : EasingTypes.InOutQuad; 

            var vec2 = Vector2.Lerp(from.Position, to.Position, Interpolation.ValueAt(blend, 0, 1, 0, 1, easing));

            if (!cursorDance)
                return vec2;

            float length = (from.Position - to.Position).Length / 2;

            if (to.Time - from.Time < 20)
                return vec2;

            if (index % 2 == 0)
            {
                vec2.Y += (float)Math.Sin(blend.Map(0, 1, MathF.PI, 0)) * length;
                vec2.X += (float)Math.Cos(blend.Map(0, 1, MathF.PI / 2, -MathF.PI / 2)) * length;
            }
            else
            {
                vec2.Y -= (float)Math.Sin(blend.Map(0, 1, MathF.PI, 0)) * length;
                vec2.X -= (float)Math.Cos(blend.Map(0, 1, MathF.PI / 2, -MathF.PI / 2)) * length;
            }

            return vec2;
        }

        public void Sort()
        {
            frames.Sort((x, y) => { return x.Time.CompareTo(y.Time); });
        }

        public void SyncToTime(double time)
        {
            frameIndex = frames.FindIndex((o) => o.Time > time);

            if (frameIndex == -1)
                frameIndex = frames.Count - 1;
        }

        public void Reset()
        {
            frameIndex = 0;
            first = null;
        }

        public void Update(double currentTime)
        {
            if (frameIndex >= frames.Count)
                return;

            if (first == null)
                first = new KeyFrame() { Time = currentTime, Position = CurrentPosition };

            while (currentTime > frames[frameIndex].Time)
            {
                ++frameIndex;

                if (frameIndex >= frames.Count)
                    return;
            }

            bool hasPreviousIndex = frameIndex - 1 > -1;

            currentTime = Math.Min(currentTime, frames[frameIndex].Time);


            CurrentPosition = onFrame(currentTime, hasPreviousIndex ? frames[frameIndex - 1] : first.Value, frames[frameIndex], frameIndex);//OnTransformCursor(currentTime, hasPreviousIndex ? frames[frameIndex - 1] : first.Value, frames[frameIndex], frameIndex);
        }

        public void AddDestination(Vector2 destination, double time, bool isSliderSlide)
        {
            frames.Add(new KeyFrame() { Position = destination, Time = time, IsSliderSlide = isSliderSlide });
        }
    }

    public class PlayableBeatmap
    {
        public HitsoundStore Hitsounds { get; private set; }
        public Sound Song { get; private set; }

        public Texture Background { get; private set; }

        public Beatmap InternalBeatmap { get; private set; }

        public int MaxCombo { get; private set; }

        public string Artist => InternalBeatmap.MetadataSection.Artist;
        public string SongName => InternalBeatmap.MetadataSection.Title;
        public string DifficultyName => InternalBeatmap.MetadataSection.Version;

        public double Window50 { get; private set; }
        public double Window100 { get; private set; }
        public double Window300 { get; private set; }

        public double Fadein { get; private set; }
        public double Preempt { get; private set; }

        public List<IDrawableHitObject> HitObjects = new List<IDrawableHitObject>();

        public Mods Mods; //{ get; private set; }

        public float CS { get; private set; }
        public float AR { get; private set; }
        public float OD { get; private set; }
        public float HP { get; private set; }

        public float CircleRadius => (OsuContainer.Playfield.Width / 16f) * (1f - (0.7f * (CS - 5f) / 5f));
        public float CircleRadiusInOsuPixels => 54.4f - 4.48f * CS;

        public List<double> DifficultyGraph = new List<double>();

        public readonly AutoGenerator AutoGenerator = new AutoGenerator();

        public PlayableBeatmap(Beatmap beatmap, Sound song, Texture background = null, HitsoundStore hitsounds = null)
        {
            InternalBeatmap = beatmap;
            Song = song;

            Background = background ?? Skin.DefaultBackground;
            Hitsounds = hitsounds;
        }

        public PlayableBeatmap(Beatmap beatmap)
        {
            string folderPath = $"{BeatmapMirror.SongsFolder}/{beatmap.MetadataSection.BeatmapSetID}";

            InternalBeatmap = beatmap;

            if (GlobalOptions.AllowMapHitSounds.Value)
            {
                Hitsounds = new HitsoundStore(folderPath, false);
                Hitsounds.SetVolume(GlobalOptions.SkinVolume.Value);
            }

            string audioPath = $"{folderPath}/{InternalBeatmap.GeneralSection.AudioFilename}";

            if (File.Exists(audioPath))
            {
                Song = new Sound(File.OpenRead($"{folderPath}/{InternalBeatmap.GeneralSection.AudioFilename}"), true);
                Song.Volume = GlobalOptions.SongVolume.Value;
            }
            else
                Song = new Sound(null);

            string bgPath = $"{folderPath}/{InternalBeatmap.EventsSection.BackgroundImage}";

            if (File.Exists(bgPath))
                Background = new Texture(File.OpenRead(bgPath));
            else
                Background = Skin.DefaultBackground;
        }

        private void applyMods(Mods mods)
        {
            CS = InternalBeatmap.DifficultySection.CircleSize;
            AR = InternalBeatmap.DifficultySection.ApproachRate;
            OD = InternalBeatmap.DifficultySection.OverallDifficulty;
            HP = InternalBeatmap.DifficultySection.HPDrainRate;

            Mods = mods;

            if (mods.HasFlag(Mods.HR))
            {
                //HR increases CS by 30%
                CS += InternalBeatmap.DifficultySection.CircleSize * 0.3f;
                CS = CS.Clamp(0, 10);

                //And all other difficulty attributes by 40%
                AR += InternalBeatmap.DifficultySection.ApproachRate * 0.4f;
                AR = AR.Clamp(0, 10);

                OD += InternalBeatmap.DifficultySection.OverallDifficulty * 0.4f;
                OD = OD.Clamp(0, 10);

                HP += InternalBeatmap.DifficultySection.HPDrainRate * 0.4f;
                HP = HP.Clamp(0, 10);
            }

            //DT makes song 1.5x times faster
            //TODO: Fix nightcore pitch beat using bass_fx
            if (mods.HasFlag(Mods.DT))
                Song.PlaybackSpeed = 1.5;
            else
                Song.PlaybackSpeed = 1;

            if (mods.HasFlag(Mods.EZ))
            {
                //The Easy mod decreases circle size (CS), approach rate (AR), overall difficulty (OD), and HP drain (HP) by half.
                CS = InternalBeatmap.DifficultySection.CircleSize / 2f;
                AR = InternalBeatmap.DifficultySection.ApproachRate / 2f;
                OD = InternalBeatmap.DifficultySection.OverallDifficulty / 2f;
                HP = InternalBeatmap.DifficultySection.HPDrainRate / 2f;
            }
        }

        //split these up
        public void OverrideDifficulty(float cs, float ar, float od, float hp)
        {
            CS = cs;
            AR = ar;
            OD = od;
            HP = hp;

            Window50 = mapDifficultyRange(OD, 200, 150, 100);
            Window100 = mapDifficultyRange(OD, 140, 100, 60);
            Window300 = mapDifficultyRange(OD, 80, 50, 20);

            Preempt = mapDifficultyRange(AR, 1800, 1200, 450);
            Fadein = 400 * Math.Min(1, Preempt / 450);
        }

        private void generateStrainGraph()
        {
            if (DifficultyGraph.Count > 0)
                return;

            Utils.BeginProfiling("StrainCalculation");

            //the amount of time between each strain
            const int CHUNK_DURATION = 1000;

            int objectIndex = 0;

            int currentObjectCount = 0;

            int timer = CHUNK_DURATION;

            //the resolution of a tick
            int tick = 50;

            System.Numerics.Vector2? prevPos = null;

            float distance = 0;

            for (int i = HitObjects[0].BaseObject.StartTime; i < HitObjects[^1].BaseObject.StartTime; i += tick)
            {
                timer -= tick;

                while (i > HitObjects[objectIndex].BaseObject.StartTime - CHUNK_DURATION)
                {
                    var pos = HitObjects[objectIndex].BaseObject.Position;

                    if (prevPos is null)
                        prevPos = pos;

                    distance += System.Numerics.Vector2.Distance(pos, prevPos.Value);

                    prevPos = pos;

                    objectIndex++;
                    currentObjectCount++;

                    if (objectIndex >= HitObjects.Count)
                    {
                        timer = 0;
                        break;
                    }
                }

                if (timer <= 0)
                {
                    //Difficulty is:
                    //The sum of the distances of all objects in that time slice
                    //The amount of objects that occoured in that time slice
                    //Add the two together??? profit???

                    DifficultyGraph.Add(MathF.Pow(distance, 1.15f) + MathF.Pow(currentObjectCount * 100, 1.1f));

                    if (objectIndex >= HitObjects.Count)
                        break;

                    prevPos = null;
                    distance = 0;

                    timer = CHUNK_DURATION;
                    currentObjectCount = 0;
                }
            }

            Utils.EndProfiling("StrainCalculation");
        }

        public void GenerateHitObjects(Mods mods = Mods.NM)
        {
            Utils.Log($"Generating Drawable Hitobjects for {InternalBeatmap.HitObjects.Count} hitobjects!", LogLevel.Info);

            Utils.BeginProfiling("Generate HitObjects");

            HitObjects.Clear();

            //TODO: allow the game to process atleast a refresh rate worth of frames
            //Everytime a hitobject is created to allow for very smooth loading
            //måske idk ved ikk om det er det værd, det tager like 400 ms at loade et marathon map
            //vs osu der tager like 9 år nogen gange fordi den gør det der

            applyMods(mods);

            Window50 = mapDifficultyRange(OD, 200, 150, 100);
            Window100 = mapDifficultyRange(OD, 140, 100, 60);
            Window300 = mapDifficultyRange(OD, 80, 50, 20);

            Preempt = mapDifficultyRange(AR, 1800, 1200, 450);
            Fadein = 400 * Math.Min(1, Preempt / 450);

            int colorIndex = 0;
            int combo = 1;

            for (int i = 0; i < InternalBeatmap.HitObjects.Count; i++)
            {
                var hitObject = InternalBeatmap.HitObjects[i];

                //Fix this shitty stacking mechanism
                for (int j = i; j < InternalBeatmap.HitObjects.Count - 1; j++)
                {
                    var previous = InternalBeatmap.HitObjects[j];
                    var next = InternalBeatmap.HitObjects[j + 1];

                    if (next is Spinner)
                        break;

                    if (next.StartTime - previous.StartTime < 25)
                        break;

                    if (hitObject.Position == next.Position)
                    {
                        next.Position += new System.Numerics.Vector2(3, 3);

                        if(next is Slider nextSlider)
                        {
                            for (int k = 0; k < nextSlider.SliderPoints.Count; k++)
                            {
                                nextSlider.SliderPoints[k] += new System.Numerics.Vector2(3, 3);
                            }
                        }
                    }
                    else
                        break;
                }

                if (hitObject.IsNewCombo)
                {
                    combo = 1;
                    colorIndex++;
                }

                int layer = 1337_727 + (InternalBeatmap.HitObjects.Count - i);

                switch (hitObject)
                {
                    case HitCircle circle:
                        DrawableHitCircle drawableCircle = new DrawableHitCircle(circle, colorIndex, combo++);
                        drawableCircle.Layer = layer;
                        HitObjects.Add(drawableCircle);
                        MaxCombo++;

                        AutoGenerator.AddDestination(new Vector2(circle.Position.X, circle.Position.Y), circle.StartTime, false);
                        break;
                    case Slider slider:
                        //Cap aspire sliders!
                        if(slider.EndTime - slider.StartTime < 1)
                            slider.EndTime = slider.StartTime + 1;

                        DrawableSlider drawableSlider = new DrawableSlider(slider, colorIndex, combo++);
                        drawableSlider.Layer = layer;
                        HitObjects.Add(drawableSlider);
                        MaxCombo += slider.Repeats + 1;

                        AutoGenerator.AddDestination(new Vector2(slider.Position.X, slider.Position.Y), slider.StartTime, false);

                        if (slider.Repeats > 1)
                        {
                            //Make sure to add all the repeats aswell

                            double repeatDuration = (slider.EndTime - slider.StartTime) / (double)slider.Repeats;
                            double offset = repeatDuration;
                            for (int repeat = 0; repeat < slider.Repeats; repeat++)
                            {
                                if(repeat % 2 == 0)
                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.StartTime + offset, true);
                                else
                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(0f), slider.StartTime + offset, true);

                                offset += repeatDuration;
                            }
                        }
                        else
                        {
                            AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.EndTime, true);
                        }

                        break;
                    case Spinner spinner:
                        DrawableSpinner drawableSpinner = new DrawableSpinner(spinner, colorIndex, combo++);
                        drawableSpinner.Layer = -layer;
                        HitObjects.Add(drawableSpinner);
                        MaxCombo++;

                        for (int spinTime = spinner.StartTime; spinTime < spinner.EndTime; spinTime+=16)
                        {
                            if (spinTime >= spinner.EndTime)
                                spinTime = spinner.EndTime;

                            Vector2 spinPos = new Vector2(spinner.Position.X, spinner.Position.Y);

                            spinPos.X += MathF.Cos(spinTime / 20f) * 50;
                            spinPos.Y += MathF.Sin(spinTime / 20f) * 50;
                            AutoGenerator.AddDestination(spinPos, spinTime, false);
                        }
                        break;
                }
            }

            AutoGenerator.Sort();

            Utils.EndProfiling("Generate HitObjects");

            generateStrainGraph();
        }

        private double mapDifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5.0f)
                return mid + (max - mid) * (difficulty - 5.0f) / 5.0f;

            if (difficulty < 5.0f)
                return mid - (mid - min) * (5.0f - difficulty) / 5.0f;

            return mid;

        }
    }
    /*
    public class PlayableBeatmap
    {
        public HitsoundStore Hitsounds { get; private set; }
        public Sound Song { get; private set; }

        /// <summary>
        /// 1 = normal speed, 2 = double speed, 0.5 = half speed etc..
        /// </summary>
        public double PlaybackSpeed { get { return Song.PlaybackSpeed; } set { Song.PlaybackSpeed = value; } }

        public Texture Background { get; private set; }

        public Beatmap Beatmap { get; private set; }

        public int MaxCombo { get; private set; }

        public string Artist => Beatmap.MetadataSection.Artist;
        public string SongName => Beatmap.MetadataSection.Title;
        public string DifficultyName => Beatmap.MetadataSection.Version;

        public double Window50 { get; private set; }
        public double Window100 { get; private set; }
        public double Window300 { get; private set; }

        public double Fadein { get; private set; }
        public double Preempt { get; private set; }

        public List<IDrawableHitObject> HitObjects = new List<IDrawableHitObject>();

        public Mods Mods { get; private set; }

        public float CS { get; set; }

        private float _ar;
        public float AR
        {
            get
            {
                return _ar;
            }
            set
            {
                _ar = value;

                Preempt = mapDifficultyRange(_ar, 1800, 1200, 450);
                Fadein = 400 * Math.Min(1, Preempt / 450);
            }
        }

        private float _od;
        public float OD
        {
            get
            {
                return _od;
            }
            set
            {
                _od = value;

                Window50 = mapDifficultyRange(_od, 200, 150, 100);
                Window100 = mapDifficultyRange(_od, 140, 100, 60);
                Window300 = mapDifficultyRange(_od, 80, 50, 20);
            }
        }

        public float HP { get; set; }

        public float CircleRadius => (OsuContainer.Playfield.Width / 16f) * (1f - (0.7f * (CS - 5f) / 5f));
        public float CircleRadiusInOsuPixels => 54.4f - 4.48f * CS;

        public readonly AutoGenerator AutoGenerator = new AutoGenerator();

        public PlayableBeatmap(Beatmap beatmap, Sound song, Texture background = null, HitsoundStore hitsounds = null)
        {
            Beatmap = beatmap;
            Song = song;

            Background = background ?? Skin.DefaultBackground;
            Hitsounds = hitsounds;

            ApplyMods();
        }

        public PlayableBeatmap(Beatmap beatmap)
        {
            string folderPath = $"{BeatmapMirror.SongsFolder}/{beatmap.MetadataSection.BeatmapSetID}";

            Beatmap = beatmap;

            if (GlobalOptions.AllowMapHitSounds.Value)
            {
                Hitsounds = new HitsoundStore(folderPath, false);
                Hitsounds.SetVolume(GlobalOptions.SkinVolume.Value);
            }

            string audioPath = $"{folderPath}/{Beatmap.GeneralSection.AudioFilename}";

            if (File.Exists(audioPath))
            {
                Song = new Sound(File.OpenRead($"{folderPath}/{Beatmap.GeneralSection.AudioFilename}"), true);
                Song.Volume = GlobalOptions.SongVolume.Value;
            }
            else
                Song = new Sound(null);

            string bgPath = $"{folderPath}/{Beatmap.EventsSection.BackgroundImage}";

            if (File.Exists(bgPath))
                Background = new Texture(File.OpenRead(bgPath));
            else
                Background = Skin.DefaultBackground;

            ApplyMods();
        }

        public void ApplyMods(Mods mods = Mods.NM)
        {
            float cs = Beatmap.DifficultySection.CircleSize;
            float ar = Beatmap.DifficultySection.ApproachRate;
            float od = Beatmap.DifficultySection.OverallDifficulty;
            float hp = Beatmap.DifficultySection.HPDrainRate;

            Mods = mods;

            if (mods.HasFlag(Mods.HR))
            {
                //HR increases CS by 30%
                cs *= 1.3f;
                cs = cs.Clamp(0, 10);

                //And all other difficulty attributes by 40%
                ar *= 1.4f;
                ar = ar.Clamp(0, 10);

                od *= 1.4f;
                od = od.Clamp(0, 10);

                hp *= 1.4f;
                hp = hp.Clamp(0, 10);
            }

            //DT makes song 1.5x times faster
            //TODO: Fix nightcore pitch beat using bass_fx
            if (mods.HasFlag(Mods.DT))
                Song.PlaybackSpeed = 1.5;
            else if (mods.HasFlag(Mods.HT))
                Song.PlaybackSpeed = 0.75;
            else
                Song.PlaybackSpeed = 1;

            if (mods.HasFlag(Mods.EZ))
            {
                //The Easy mod decreases circle size (CS), approach rate (AR), overall difficulty (OD), and HP drain (HP) by half.
                cs /= 2f;
                ar /= 2f;
                od /= 2f;
                hp /= 2f;
            }

            CS = cs;
            AR = ar;
            OD = od;
            HP = hp;
        }

        public void GenerateDifficultyGraph(List<double> input)
        {
            if (HitObjects.Count == 0)
                throw new Exception("Can't generate difficulty graph for a beatmap with 0 objects.");

            Utils.BeginProfiling("StrainCalculation");

            input.Clear();

            //the amount of time between each strain
            const int CHUNK_DURATION = 1000;

            int objectIndex = 0;

            int currentObjectCount = 0;

            int timer = CHUNK_DURATION;

            //the resolution of a tick
            int tick = 50;

            System.Numerics.Vector2? prevPos = null;

            float distance = 0;

            for (int i = HitObjects[0].BaseObject.StartTime; i < HitObjects[^1].BaseObject.StartTime; i += tick)
            {
                timer -= tick;

                while (i > HitObjects[objectIndex].BaseObject.StartTime - CHUNK_DURATION)
                {
                    var pos = HitObjects[objectIndex].BaseObject.Position;

                    if (prevPos is null)
                        prevPos = pos;

                    distance += System.Numerics.Vector2.Distance(pos, prevPos.Value);

                    prevPos = pos;

                    objectIndex++;
                    currentObjectCount++;

                    if (objectIndex >= HitObjects.Count)
                    {
                        timer = 0;
                        break;
                    }
                }

                if (timer <= 0)
                {
                    //Difficulty is:
                    //The sum of the distances of all objects in that time slice
                    //The amount of objects that occoured in that time slice
                    //Add the two together??? profit???

                    input.Add(MathF.Pow(distance, 1.15f) + MathF.Pow(currentObjectCount * 100, 1.1f));

                    if (objectIndex >= HitObjects.Count)
                        break;

                    prevPos = null;
                    distance = 0;

                    timer = CHUNK_DURATION;
                    currentObjectCount = 0;
                }
            }

            Utils.EndProfiling("StrainCalculation", false, true);
        }

        public void GenerateHitObjects()
        {
            Utils.Log($"Generating Drawable Hitobjects for {Beatmap.HitObjects.Count} hitobjects!", LogLevel.Info);

            HitObjects.Clear();

            int colorIndex = 0;
            int combo = 1;

            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
            {
                var hitObject = Beatmap.HitObjects[i];

                //Fix this shitty stacking mechanism
                for (int j = i; j < Beatmap.HitObjects.Count - 1; j++)
                {
                    var previous = Beatmap.HitObjects[j];
                    var next = Beatmap.HitObjects[j + 1];

                    if (next is Spinner)
                        break;

                    if (next.StartTime - previous.StartTime < 25)
                        break;

                    if (hitObject.Position == next.Position)
                    {
                        next.Position += new System.Numerics.Vector2(3, 3);

                        if(next is Slider nextSlider)
                        {
                            for (int k = 0; k < nextSlider.SliderPoints.Count; k++)
                            {
                                nextSlider.SliderPoints[k] += new System.Numerics.Vector2(3, 3);
                            }
                        }
                    }
                    else
                        break;
                }

                if (hitObject.IsNewCombo)
                {
                    combo = 1;
                    colorIndex++;
                }

                int layer = 1337_727 + (Beatmap.HitObjects.Count - i);

                switch (hitObject)
                {
                    case HitCircle circle:
                        DrawableHitCircle drawableCircle = new DrawableHitCircle(circle, colorIndex, combo++);
                        drawableCircle.Layer = layer;
                        HitObjects.Add(drawableCircle);
                        MaxCombo++;

                        AutoGenerator.AddDestination(new Vector2(circle.Position.X, circle.Position.Y), circle.StartTime, false);
                        break;
                    case Slider slider:
                        DrawableSlider drawableSlider = new DrawableSlider(slider, colorIndex, combo++);
                        drawableSlider.Layer = layer;
                        HitObjects.Add(drawableSlider);
                        MaxCombo += slider.Repeats + 1;

                        AutoGenerator.AddDestination(new Vector2(slider.Position.X, slider.Position.Y), slider.StartTime, false);

                        if (slider.Repeats > 1)
                        {
                            //Make sure to get the repeats with

                            double repeatDuration = (slider.EndTime - slider.StartTime) / (double)slider.Repeats;
                            double offset = repeatDuration;
                            for (int repeat = 0; repeat < slider.Repeats; repeat++)
                            {
                                if(repeat % 2 == 0)
                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.StartTime + offset, true);
                                else
                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(0f), slider.StartTime + offset, true);

                                offset += repeatDuration;
                            }
                        }
                        else
                        {
                            AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.EndTime, true);
                        }

                        break;
                    case Spinner spinner:
                        DrawableSpinner drawableSpinner = new DrawableSpinner(spinner, colorIndex, combo++);
                        drawableSpinner.Layer = -layer;
                        HitObjects.Add(drawableSpinner);
                        MaxCombo++;

                        for (int spinTime = spinner.StartTime; spinTime < spinner.EndTime; spinTime+=16)
                        {
                            if (spinTime >= spinner.EndTime)
                                spinTime = spinner.EndTime;

                            Vector2 spinPos = new Vector2(spinner.Position.X, spinner.Position.Y);

                            spinPos.X += MathF.Cos(spinTime / 20f) * 50;
                            spinPos.Y += MathF.Sin(spinTime / 20f) * 50;
                            AutoGenerator.AddDestination(spinPos, spinTime, false);
                        }
                        break;
                }
            }

            AutoGenerator.Sort();
        }

        private double mapDifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5.0f)
                return mid + (max - mid) * (difficulty - 5.0f) / 5.0f;

            if (difficulty < 5.0f)
                return mid - (mid - min) * (5.0f - difficulty) / 5.0f;

            return mid;

        }
    }
    */
}