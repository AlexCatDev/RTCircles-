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
    public class PlayableBeatmap
    {
        public HitsoundStore Hitsounds { get; private set; }
        public Sound Song { get; private set; }

        private Guid backgroundGuid = Guid.NewGuid();
        private string backgroundPath = "";

        public bool IsNewBackground { get; private set; } = true;

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

        public string Hash { get; private set; }

        public CarouselItem CarouselItem { get; private set; }

        public float CircleRadius => (OsuContainer.Playfield.Width / 16f) * (1f - (0.7f * (CS - 5f) / 5f));
        public float CircleRadiusInOsuPixels => 54.4f - 4.48f * CS;

        public List<double> DifficultyGraph = new List<double>();

        public string AudioPath { get; private set; } = "";

        public readonly AutoGenerator AutoGenerator = new AutoGenerator();

        public PlayableBeatmap(Beatmap beatmap, Sound song, Texture background = null, HitsoundStore hitsounds = null)
        {
            InternalBeatmap = beatmap;
            Song = song;

            Background = background ?? Skin.DefaultBackground;
            Hitsounds = hitsounds;

            if (Song != null)
            {
                Song.Volume = 0;
                ManagedBass.Bass.ChannelSlideAttribute(Song, ManagedBass.ChannelAttribute.Volume, (float)GlobalOptions.SongVolume.Value, 500, false);
            }
        }

        private PlayableBeatmap() { }

        /// <param name="item"></param>
        /// <returns>Null if the beatmap couldn't be found.</returns>
        public static PlayableBeatmap FromCarouselItem(CarouselItem item)
        {
            PlayableBeatmap playableBeatmap = new PlayableBeatmap();

            string folderPath = $"{BeatmapMirror.SongsDirectory}/{item.Folder}";

            OsuParsers.Beatmaps.Beatmap beatmap;

            if (File.Exists(item.FullPath))
            {
                using (FileStream fs = File.OpenRead(item.FullPath))
                {
                    beatmap = BeatmapMirror.DecodeBeatmap(fs);
                }
            }
            else
            {
                NotificationManager.ShowMessage($"beatmap: '{item.FullPath}' not found", new Vector3(1f, 0.1f, 0.1f), 5f);
                return null;
            }

            playableBeatmap.CarouselItem = item;

            playableBeatmap.InternalBeatmap = beatmap;

            playableBeatmap.Hash = item.Hash;

            if (GlobalOptions.AllowMapHitSounds.Value)
            {
                playableBeatmap.Hitsounds = new HitsoundStore(folderPath, false);
                playableBeatmap.Hitsounds.SetVolume(GlobalOptions.SkinVolume.Value);
            }

            string audioPath = $"{folderPath}/{playableBeatmap.InternalBeatmap.GeneralSection.AudioFilename}";
            playableBeatmap.AudioPath = audioPath;

            //If the audio file is the same as the already set map, don't reinitialize it lol
            if(OsuContainer.Beatmap?.AudioPath == audioPath)
            {
                playableBeatmap.Song = OsuContainer.Beatmap.Song;
            }
            else if (File.Exists(audioPath))
            {
                using (FileStream fs = File.OpenRead(audioPath))
                {
                    Sound song = new Sound(fs, useFX: true, noBuffer: false, bassFlags: ManagedBass.BassFlags.Prescan);
                    playableBeatmap.Song = song;
                }
            }
            else
                playableBeatmap.Song = new Sound(null);

            playableBeatmap.Song.Volume = 0;
            ManagedBass.Bass.ChannelSlideAttribute(playableBeatmap.Song, ManagedBass.ChannelAttribute.Volume, (float)GlobalOptions.SongVolume.Value, 500, false);

            string bgPath = $"{folderPath}/{playableBeatmap.InternalBeatmap.EventsSection.BackgroundImage}";

            playableBeatmap.backgroundPath = bgPath;
            //Just get the cache since it was properly already loaded from beatmap carousel
            playableBeatmap.Background = DynamicTexureCache.AquireCache(playableBeatmap.backgroundGuid, playableBeatmap.backgroundPath);

            playableBeatmap.IsNewBackground = OsuContainer.Beatmap?.Background != playableBeatmap.Background;

            return playableBeatmap;
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

                //And all other difficulty attributes by 40%
                AR += InternalBeatmap.DifficultySection.ApproachRate * 0.4f;

                OD += InternalBeatmap.DifficultySection.OverallDifficulty * 0.4f;

                HP += InternalBeatmap.DifficultySection.HPDrainRate * 0.4f;
            }

            //DT makes song 1.5x times faster
            if (Song != null)
            {
                if (mods.HasFlag(Mods.DT) || mods.HasFlag(Mods.NC))
                    Song.PlaybackSpeed = 1.5;
                else
                    Song.PlaybackSpeed = 1;

                if (mods.HasFlag(Mods.NC))
                    Song.Pitch = 5;
                else
                    Song.Pitch = 0;
            }

            if (mods.HasFlag(Mods.EZ))
            {
                //The Easy mod decreases circle size (CS), approach rate (AR), overall difficulty (OD), and HP drain (HP) by half.
                CS = InternalBeatmap.DifficultySection.CircleSize / 2f;
                AR = InternalBeatmap.DifficultySection.ApproachRate / 2f;
                OD = InternalBeatmap.DifficultySection.OverallDifficulty / 2f;
                HP = InternalBeatmap.DifficultySection.HPDrainRate / 2f;
            }

            CS = CS.Clamp(0, 10);
            AR = AR.Clamp(0, 10);
            OD = OD.Clamp(0, 10);
            HP = HP.Clamp(0, 10);
        }

        public void SetOD(float od)
        {
            OD = od;
            Window50 = mapDifficultyRange(OD, 200, 150, 100);
            Window100 = mapDifficultyRange(OD, 140, 100, 60);
            Window300 = mapDifficultyRange(OD, 80, 50, 20);
        }

        public void SetAR(float ar)
        {
            AR = ar;
            Preempt = mapDifficultyRange(AR, 1800, 1200, 450);
            Fadein = 400 * Math.Min(1, Preempt / 450);
        }

        public void SetCS(float cs)
        {
            CS = cs;
        }

        public void SetHP(float hp)
        {
            HP = hp;
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

        private void applyStacking()
        {
            System.Numerics.Vector2 currentStack = System.Numerics.Vector2.Zero;
            double stackTimeThreshold = Preempt * InternalBeatmap.GeneralSection.StackLeniency;

            double stackBaseObjectEndTime = 0;

            void resetStacking()
            {
                currentStack = System.Numerics.Vector2.Zero;
            }

            for (int i = 0; i < InternalBeatmap.HitObjects.Count; i++)
            {
                var current = InternalBeatmap.HitObjects[i];

                if (current is Slider slider)
                {
                    //Since the slider gets rendered at its stacked position, the points needs to be unstacked

                    //Add no stacked position to slider points
                    slider.SliderPoints.Insert(0, slider.Position);
                }
                

                //If theres no next object just continue
                if (i + 1 >= InternalBeatmap.HitObjects.Count)
                    continue;

                var next = InternalBeatmap.HitObjects[i + 1];

                //Spinners apparently dont reset stacking
                if (next is Spinner)
                    continue;

                
                if (stackBaseObjectEndTime - current.StartTime > stackTimeThreshold)
                {
                    resetStacking();
                    continue;
                }                

                //Current may be modified if the previous object overlapped with it
                //So offset current with the current stacking amount
                //Then check if it stacks with the next object, which has not been stacked yet

                if ((current.Position - currentStack) == next.Position)
                {
                    if (currentStack.X == 0)
                        stackBaseObjectEndTime = current.EndTime;

                    //Keep making the stack bigger while they overlap
                    currentStack += new System.Numerics.Vector2(3, 3);
                    
                    //Next will be current in the next iteration
                    next.Position += currentStack;
                }
                else
                {
                    //Reset stacking when these objects no longer stack
                    resetStacking();
                }
            }
        }

        public void GenerateHitObjects(Mods mods = Mods.NM)
        {
            System.Diagnostics.Debug.Assert(!(HitObjects.Count > 0));

            if (InternalBeatmap.HitObjects.Count == 0 || HitObjects.Count > 0)
                return;

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

            applyStacking();

            int colorIndex = 0;
            int combo = 1;

            for (int i = 0; i < InternalBeatmap.HitObjects.Count; i++)
            {
                var hitObject = InternalBeatmap.HitObjects[i];

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

                            Vector2 spinPos = new Vector2(512/2, 384/2);

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

        ~PlayableBeatmap()
        {
            DynamicTexureCache.ReleaseCache(backgroundGuid, backgroundPath);
        }
    }
}