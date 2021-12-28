using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Database.Objects;
using OsuParsers.Decoders;
using OsuParsers.Enums;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class PlayingBeatmap
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

        public List<double> DifficultyGraph = new List<double>();

        public PlayingBeatmap(Beatmap beatmap)
        {
            string folderPath = $"{BeatmapMirror.SongsFolder}/{beatmap.MetadataSection.BeatmapSetID}";

            InternalBeatmap = beatmap;

            Hitsounds = new HitsoundStore(folderPath, true);
            Hitsounds.SetVolume(0.6f);

            string audioPath = $"{folderPath}/{InternalBeatmap.GeneralSection.AudioFilename}";

            if (File.Exists(audioPath))
                Song = new Sound(File.OpenRead($"{folderPath}/{InternalBeatmap.GeneralSection.AudioFilename}"), true);
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
            CS = InternalBeatmap.DifficultySection.CircleSize / 2;
            AR = InternalBeatmap.DifficultySection.ApproachRate;
            OD = InternalBeatmap.DifficultySection.OverallDifficulty / 2;
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

            Utils.EndProfiling("StrainCalculation", false, true);
        }

        public void GenerateHitObjects(Mods mods = Mods.NM)
        {
            Utils.Log($"Generating Drawable Hitobjects for {InternalBeatmap.HitObjects.Count} hitobjects!", LogLevel.Info);

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

                int layer = 100000 + (InternalBeatmap.HitObjects.Count - i);

                switch (hitObject)
                {
                    case HitCircle circle:
                        DrawableHitCircle drawableCircle = new DrawableHitCircle(circle, colorIndex, combo++);
                        drawableCircle.Layer = layer;
                        HitObjects.Add(drawableCircle);
                        MaxCombo++;
                        break;
                    case Slider slider:
                        DrawableSlider drawableSlider = new DrawableSlider(slider, colorIndex, combo++);
                        drawableSlider.Layer = layer;
                        HitObjects.Add(drawableSlider);
                        MaxCombo += slider.Repeats + 1;
                        break;
                    case Spinner spinner:
                        DrawableSpinner drawableSpinner = new DrawableSpinner(spinner, colorIndex, combo++);
                        drawableSpinner.Layer = -layer;
                        HitObjects.Add(drawableSpinner);
                        MaxCombo++;
                        break;
                }
            }

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
}
