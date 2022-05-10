//using Easy2D;
//using OpenTK.Mathematics;
//using OsuParsers.Decoders;
//using OsuParsers.Storyboards.Objects;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace RTCircles
//{
//    /// <summary>
//    /// All of this is so wrong, and idk how storyboards really work, and i dont care anymore
//    /// </summary>
//    public static class DrawableStoryboard
//    {
//        private static Dictionary<string, Texture> cachedTextures = new Dictionary<string, Texture>();

//        class DrawableStoryboardSprite
//        {
//            private Texture texture;

//            private StoryboardSprite sSprite;

//            public int StartTime { get; private set; } = int.MaxValue;
//            public int EndTime { get; private set; } = int.MinValue;

//            private AnimationTFloat fadeAnim = new AnimationTFloat() { DefaultValue = 1 };
//            private AnimationTVec4 colorAnim = new AnimationTVec4() { DefaultValue = new Vector4(1f, 1f, 1f, 0f) };
//            private AnimationTFloat xAnim = new AnimationTFloat();
//            private AnimationTFloat yAnim = new AnimationTFloat();
//            private AnimationTFloat rotationAnim = new AnimationTFloat() { DefaultValue = 0 };
//            private AnimationTVec2 scaleAnim = new AnimationTVec2() { DefaultValue = Vector2.One };

//            public DrawableStoryboardSprite(StoryboardSprite sSprite)
//            {
//                string texturePath = $"{BeatmapMirror.SongsFolder}/{OsuContainer.Beatmap.InternalBeatmap.MetadataSection.BeatmapSetID}/{sSprite.FilePath}";

//                if (DrawableStoryboard.cachedTextures.TryGetValue(texturePath, out var texture))
//                {
//                    this.texture = texture;
//                }
//                else
//                {
//                    if (File.Exists(texturePath))
//                        texture = new Texture(File.OpenRead(texturePath));
//                    else
//                    {
//                        if (texturePath.ToLower().Contains("hitcircle"))
//                            texture = Skin.HitCircle;
//                        else if (texturePath.ToLower().Contains("hitcircleoverlay"))
//                            texture = Skin.HitCircleOverlay;
//                        else if (texturePath.ToLower().Contains("approachcircle"))
//                            texture = Skin.ApproachCircle;
//                        else if (texturePath.ToLower().Contains("default-1"))
//                            texture = Skin.CircleNumbers.Numbers[1];
//                        else if (texturePath.ToLower().Contains("default-2"))
//                            texture = Skin.CircleNumbers.Numbers[2];
//                        else if (texturePath.ToLower().Contains("default-3"))
//                            texture = Skin.CircleNumbers.Numbers[3];
//                        else if (texturePath.ToLower().Contains("default-4"))
//                            texture = Skin.CircleNumbers.Numbers[4];
//                        else if (texturePath.ToLower().Contains("default-5"))
//                            texture = Skin.CircleNumbers.Numbers[5];
//                        else if (texturePath.ToLower().Contains("default-6"))
//                            texture = Skin.CircleNumbers.Numbers[6];
//                        else if (texturePath.ToLower().Contains("default-7"))
//                            texture = Skin.CircleNumbers.Numbers[7];
//                        else if (texturePath.ToLower().Contains("default-8"))
//                            texture = Skin.CircleNumbers.Numbers[8];
//                        else if (texturePath.ToLower().Contains("default-9"))
//                            texture = Skin.CircleNumbers.Numbers[9];

//                        //texture = Skin.DefaultBackground;
//                        Utils.Log($"Could not find storyboard texture: {texturePath}", LogLevel.Error);
//                    }

//                    DrawableStoryboard.cachedTextures.Add(texturePath, texture);

//                    Utils.Log($"Loaded new storyboard sprite: {texturePath}", LogLevel.Important);
//                }

//                this.sSprite = sSprite;

//                foreach (var cmd in sSprite.Commands.Commands)
//                {
//                    if (StartTime > cmd.StartTime)
//                        StartTime = cmd.StartTime;

//                    if (EndTime < cmd.EndTime)
//                        EndTime = cmd.EndTime;

//                    var easing = (EasingTypes)cmd.Easing;

//                    switch (cmd.Type)
//                    {
//                        case OsuParsers.Enums.Storyboards.CommandType.None:
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.Movement:
//                            xAnim.Add(cmd.StartTime, cmd.StartVector.X, easing);
//                            xAnim.Add(cmd.EndTime, cmd.EndVector.X, easing);

//                            yAnim.Add(cmd.StartTime, cmd.StartVector.Y, easing);
//                            yAnim.Add(cmd.EndTime, cmd.EndVector.Y, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.MovementX:
//                            xAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
//                            xAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.MovementY:
//                            yAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
//                            yAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.Fade:
//                            fadeAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
//                            fadeAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.Scale:
//                            scaleAnim.Add(cmd.StartTime, new Vector2(cmd.StartFloat), easing);
//                            scaleAnim.Add(cmd.EndTime, new Vector2(cmd.EndFloat), easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.VectorScale:
//                            scaleAnim.Add(cmd.StartTime, new Vector2(cmd.StartVector.X, cmd.StartVector.Y), easing);
//                            scaleAnim.Add(cmd.EndTime, new Vector2(cmd.EndVector.X, cmd.EndVector.Y), easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.Rotation:
//                            rotationAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
//                            rotationAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.Colour:
//                            colorAnim.Add(cmd.StartTime, Colors.From255RGBA(cmd.StartColour.R, cmd.StartColour.G, cmd.StartColour.B, 0f), easing);
//                            colorAnim.Add(cmd.EndTime, Colors.From255RGBA(cmd.EndColour.R, cmd.EndColour.G, cmd.EndColour.B, 0f), easing);

//                            fadeAnim.Add(cmd.StartTime, cmd.StartColour.A / 255f, easing);
//                            fadeAnim.Add(cmd.EndTime, cmd.EndColour.A / 255f, easing);
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.FlipHorizontal:
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.FlipVertical:
//                            break;
//                        case OsuParsers.Enums.Storyboards.CommandType.BlendingMode:
//                            break;
//                        default:
//                            break;
//                    }
//                }
//                xAnim.DefaultValue = sSprite.X;
//                yAnim.DefaultValue = sSprite.Y;
//                /*
//                colorAnim.Sort();
//                fadeAnim.Sort();
//                rotationAnim.Sort();
//                scaleAnim.Sort();
//                xAnim.Sort();
//                yAnim.Sort();
//                */
//            }

//            public void Render(Graphics g)
//            {
//                Vector4 color = colorAnim.GetOutputAtTime(OsuContainer.SongPosition);
//                color.W = fadeAnim.GetOutputAtTime(OsuContainer.SongPosition);

//                color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);

//                if (color.W < 0.01f || texture == null)
//                    return;

//                float rotation = rotationAnim.GetOutputAtTime(OsuContainer.SongPosition);
//                Vector2 vecScale = scaleAnim.GetOutputAtTime(OsuContainer.SongPosition);

//                Vector2 position = new Vector2(xAnim.GetOutputAtTime(OsuContainer.SongPosition), yAnim.GetOutputAtTime(OsuContainer.SongPosition));

//                float osuScale = OsuContainer.Playfield.Width / 512f;

//                Vector2 drawSize = (texture?.Size ?? Vector2.One) * vecScale * osuScale;
//                Vector2 drawPos = OsuContainer.MapToPlayfield(position) - new Vector2(64, 57) * osuScale;

//                float drawRotation = MathHelper.RadiansToDegrees(rotation);

//                /*
//                if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.TopLeft)
//                    drawPos -= drawSize / 2f;
//                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.BottomCentre)
//                    drawPos += new Vector2(0, drawSize.Y / 2);
//                //else if(sSprite.Origin != OsuParsers.Enums.Storyboards.Origins.Centre)
//                    //throw new Exception("cum");
//                */
//                g.DrawRectangleCentered(drawPos, drawSize, color, texture, rotDegrees: drawRotation);
//            }
//        }

//        private static List<DrawableStoryboardSprite> sprites = new List<DrawableStoryboardSprite>();

//        static DrawableStoryboard()
//        {
//            /*
//            OsuContainer.BeatmapChanged += () =>
//            {
//                OsuContainer.Beatmap.Mods |= Mods.Auto;

//                spriteIndex = 0;
//                activeSprites.Clear();
//                sprites.Clear();
//                cachedTextures.Clear();

//                var storyboardFilename = $"{BeatmapMirror.SongsFolder}/{OsuContainer.Beatmap.InternalBeatmap.MetadataSection.BeatmapSetID}/" +
//                            $"{OsuContainer.Beatmap.InternalBeatmap.MetadataSection.Artist} - {OsuContainer.Beatmap.InternalBeatmap.MetadataSection.Title}" +
//                            $" ({OsuContainer.Beatmap.InternalBeatmap.MetadataSection.Creator}).osb";

//                var storyBoard = OsuContainer.Beatmap.InternalBeatmap.EventsSection.Storyboard;

//                if (File.Exists(storyboardFilename))
//                {
//                    //Utils.BeginProfiling("Custom Storyboard");
//                    using (FileStream fs = File.OpenRead(storyboardFilename))
//                        storyBoard = StoryboardDecoder.Decode(fs);
//                    //Utils.EndProfiling("Custom Storyboard");
//                }

//                Utils.Log($"Loading storyboard background layer : {storyBoard.BackgroundLayer.Count}", LogLevel.Info);
//                foreach (var item in storyBoard.BackgroundLayer)
//                {
//                    if (item is StoryboardSprite sSprite)
//                    {
//                        sprites.Add(new DrawableStoryboardSprite(sSprite));
//                    }
//                }

//                Utils.Log($"Loading storyboard fail layer layer : {storyBoard.FailLayer.Count}", LogLevel.Info);
//                foreach (var item in storyBoard.FailLayer)
//                {
//                    if (item is StoryboardSprite sSprite)
//                    {
//                        sprites.Add(new DrawableStoryboardSprite(sSprite));
//                    }
//                }

//                Utils.Log($"Loading storyboard foreground layer : {storyBoard.ForegroundLayer.Count}", LogLevel.Info);
//                foreach (var item in storyBoard.ForegroundLayer)
//                {
//                    if (item is StoryboardSprite sSprite)
//                    {
//                        sprites.Add(new DrawableStoryboardSprite(sSprite));
//                    }
//                }

//                Utils.Log($"Loading storyboard overlay layer : {storyBoard.OverlayLayer.Count}", LogLevel.Info);
//                foreach (var item in storyBoard.OverlayLayer)
//                {
//                    if (item is StoryboardSprite sSprite)
//                    {
//                        sprites.Add(new DrawableStoryboardSprite(sSprite));
//                    }
//                }
//                sprites.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

//            };
//            */
//        }

//        private static List<DrawableStoryboardSprite> activeSprites = new List<DrawableStoryboardSprite>();
//        private static int spriteIndex = 0;

//        public static void Render(Graphics g)
//        {
//            if (sprites.Count == 0)
//                return;

//            for (int i = 0; i < activeSprites.Count; i++)
//            {
//                activeSprites[i].Render(g);
//            }

//            if (spriteIndex < sprites.Count)
//            {
//                while (OsuContainer.SongPosition >= sprites[spriteIndex].StartTime)
//                {
//                    activeSprites.Add(sprites[spriteIndex]);
//                    spriteIndex++;

//                    if (spriteIndex == sprites.Count)
//                        break;
//                }
//            }

//            for (int i = activeSprites.Count - 1; i >= 0; i--)
//            {
//                if (OsuContainer.SongPosition > activeSprites[i].EndTime)
//                    activeSprites.RemoveAt(i);
//            }

//            //If the storyboard is ahead of the current song time, then seek to the start, so it can sync from scratch
//            if (sprites[(spriteIndex - 1).Clamp(0, sprites.Count - 1)].StartTime > OsuContainer.SongPosition)
//            {
//                activeSprites.Clear();
//                spriteIndex = 0;
//            }
//            //System.Console.WriteLine($"Active sprites: {activeSprites.Count} Index: {spriteIndex}/{sprites.Count}");
//        }
//    }
//    /*
//    public class PlayableBeatmap
//    {
//        public HitsoundStore Hitsounds { get; private set; }
//        public Sound Song { get; private set; }

//        /// <summary>
//        /// 1 = normal speed, 2 = double speed, 0.5 = half speed etc..
//        /// </summary>
//        public double PlaybackSpeed { get { return Song.PlaybackSpeed; } set { Song.PlaybackSpeed = value; } }

//        public Texture Background { get; private set; }

//        public Beatmap Beatmap { get; private set; }

//        public int MaxCombo { get; private set; }

//        public string Artist => Beatmap.MetadataSection.Artist;
//        public string SongName => Beatmap.MetadataSection.Title;
//        public string DifficultyName => Beatmap.MetadataSection.Version;

//        public double Window50 { get; private set; }
//        public double Window100 { get; private set; }
//        public double Window300 { get; private set; }

//        public double Fadein { get; private set; }
//        public double Preempt { get; private set; }

//        public List<IDrawableHitObject> HitObjects = new List<IDrawableHitObject>();

//        public Mods Mods { get; private set; }

//        public float CS { get; set; }

//        private float _ar;
//        public float AR
//        {
//            get
//            {
//                return _ar;
//            }
//            set
//            {
//                _ar = value;

//                Preempt = mapDifficultyRange(_ar, 1800, 1200, 450);
//                Fadein = 400 * Math.Min(1, Preempt / 450);
//            }
//        }

//        private float _od;
//        public float OD
//        {
//            get
//            {
//                return _od;
//            }
//            set
//            {
//                _od = value;

//                Window50 = mapDifficultyRange(_od, 200, 150, 100);
//                Window100 = mapDifficultyRange(_od, 140, 100, 60);
//                Window300 = mapDifficultyRange(_od, 80, 50, 20);
//            }
//        }

//        public float HP { get; set; }

//        public float CircleRadius => (OsuContainer.Playfield.Width / 16f) * (1f - (0.7f * (CS - 5f) / 5f));
//        public float CircleRadiusInOsuPixels => 54.4f - 4.48f * CS;

//        public readonly AutoGenerator AutoGenerator = new AutoGenerator();

//        public PlayableBeatmap(Beatmap beatmap, Sound song, Texture background = null, HitsoundStore hitsounds = null)
//        {
//            Beatmap = beatmap;
//            Song = song;

//            Background = background ?? Skin.DefaultBackground;
//            Hitsounds = hitsounds;

//            ApplyMods();
//        }

//        public PlayableBeatmap(Beatmap beatmap)
//        {
//            string folderPath = $"{BeatmapMirror.SongsFolder}/{beatmap.MetadataSection.BeatmapSetID}";

//            Beatmap = beatmap;

//            if (GlobalOptions.AllowMapHitSounds.Value)
//            {
//                Hitsounds = new HitsoundStore(folderPath, false);
//                Hitsounds.SetVolume(GlobalOptions.SkinVolume.Value);
//            }

//            string audioPath = $"{folderPath}/{Beatmap.GeneralSection.AudioFilename}";

//            if (File.Exists(audioPath))
//            {
//                Song = new Sound(File.OpenRead($"{folderPath}/{Beatmap.GeneralSection.AudioFilename}"), true);
//                Song.Volume = GlobalOptions.SongVolume.Value;
//            }
//            else
//                Song = new Sound(null);

//            string bgPath = $"{folderPath}/{Beatmap.EventsSection.BackgroundImage}";

//            if (File.Exists(bgPath))
//                Background = new Texture(File.OpenRead(bgPath));
//            else
//                Background = Skin.DefaultBackground;

//            ApplyMods();
//        }

//        public void ApplyMods(Mods mods = Mods.NM)
//        {
//            float cs = Beatmap.DifficultySection.CircleSize;
//            float ar = Beatmap.DifficultySection.ApproachRate;
//            float od = Beatmap.DifficultySection.OverallDifficulty;
//            float hp = Beatmap.DifficultySection.HPDrainRate;

//            Mods = mods;

//            if (mods.HasFlag(Mods.HR))
//            {
//                //HR increases CS by 30%
//                cs *= 1.3f;
//                cs = cs.Clamp(0, 10);

//                //And all other difficulty attributes by 40%
//                ar *= 1.4f;
//                ar = ar.Clamp(0, 10);

//                od *= 1.4f;
//                od = od.Clamp(0, 10);

//                hp *= 1.4f;
//                hp = hp.Clamp(0, 10);
//            }

//            //DT makes song 1.5x times faster
//            //TODO: Fix nightcore pitch beat using bass_fx
//            if (mods.HasFlag(Mods.DT))
//                Song.PlaybackSpeed = 1.5;
//            else if (mods.HasFlag(Mods.HT))
//                Song.PlaybackSpeed = 0.75;
//            else
//                Song.PlaybackSpeed = 1;

//            if (mods.HasFlag(Mods.EZ))
//            {
//                //The Easy mod decreases circle size (CS), approach rate (AR), overall difficulty (OD), and HP drain (HP) by half.
//                cs /= 2f;
//                ar /= 2f;
//                od /= 2f;
//                hp /= 2f;
//            }

//            CS = cs;
//            AR = ar;
//            OD = od;
//            HP = hp;
//        }

//        public void GenerateDifficultyGraph(List<double> input)
//        {
//            if (HitObjects.Count == 0)
//                throw new Exception("Can't generate difficulty graph for a beatmap with 0 objects.");

//            Utils.BeginProfiling("StrainCalculation");

//            input.Clear();

//            //the amount of time between each strain
//            const int CHUNK_DURATION = 1000;

//            int objectIndex = 0;

//            int currentObjectCount = 0;

//            int timer = CHUNK_DURATION;

//            //the resolution of a tick
//            int tick = 50;

//            System.Numerics.Vector2? prevPos = null;

//            float distance = 0;

//            for (int i = HitObjects[0].BaseObject.StartTime; i < HitObjects[^1].BaseObject.StartTime; i += tick)
//            {
//                timer -= tick;

//                while (i > HitObjects[objectIndex].BaseObject.StartTime - CHUNK_DURATION)
//                {
//                    var pos = HitObjects[objectIndex].BaseObject.Position;

//                    if (prevPos is null)
//                        prevPos = pos;

//                    distance += System.Numerics.Vector2.Distance(pos, prevPos.Value);

//                    prevPos = pos;

//                    objectIndex++;
//                    currentObjectCount++;

//                    if (objectIndex >= HitObjects.Count)
//                    {
//                        timer = 0;
//                        break;
//                    }
//                }

//                if (timer <= 0)
//                {
//                    //Difficulty is:
//                    //The sum of the distances of all objects in that time slice
//                    //The amount of objects that occoured in that time slice
//                    //Add the two together??? profit???

//                    input.Add(MathF.Pow(distance, 1.15f) + MathF.Pow(currentObjectCount * 100, 1.1f));

//                    if (objectIndex >= HitObjects.Count)
//                        break;

//                    prevPos = null;
//                    distance = 0;

//                    timer = CHUNK_DURATION;
//                    currentObjectCount = 0;
//                }
//            }

//            Utils.EndProfiling("StrainCalculation", false, true);
//        }

//        public void GenerateHitObjects()
//        {
//            Utils.Log($"Generating Drawable Hitobjects for {Beatmap.HitObjects.Count} hitobjects!", LogLevel.Info);

//            HitObjects.Clear();

//            int colorIndex = 0;
//            int combo = 1;

//            for (int i = 0; i < Beatmap.HitObjects.Count; i++)
//            {
//                var hitObject = Beatmap.HitObjects[i];

//                //Fix this shitty stacking mechanism
//                for (int j = i; j < Beatmap.HitObjects.Count - 1; j++)
//                {
//                    var previous = Beatmap.HitObjects[j];
//                    var next = Beatmap.HitObjects[j + 1];

//                    if (next is Spinner)
//                        break;

//                    if (next.StartTime - previous.StartTime < 25)
//                        break;

//                    if (hitObject.Position == next.Position)
//                    {
//                        next.Position += new System.Numerics.Vector2(3, 3);

//                        if(next is Slider nextSlider)
//                        {
//                            for (int k = 0; k < nextSlider.SliderPoints.Count; k++)
//                            {
//                                nextSlider.SliderPoints[k] += new System.Numerics.Vector2(3, 3);
//                            }
//                        }
//                    }
//                    else
//                        break;
//                }

//                if (hitObject.IsNewCombo)
//                {
//                    combo = 1;
//                    colorIndex++;
//                }

//                int layer = 1337_727 + (Beatmap.HitObjects.Count - i);

//                switch (hitObject)
//                {
//                    case HitCircle circle:
//                        DrawableHitCircle drawableCircle = new DrawableHitCircle(circle, colorIndex, combo++);
//                        drawableCircle.Layer = layer;
//                        HitObjects.Add(drawableCircle);
//                        MaxCombo++;

//                        AutoGenerator.AddDestination(new Vector2(circle.Position.X, circle.Position.Y), circle.StartTime, false);
//                        break;
//                    case Slider slider:
//                        DrawableSlider drawableSlider = new DrawableSlider(slider, colorIndex, combo++);
//                        drawableSlider.Layer = layer;
//                        HitObjects.Add(drawableSlider);
//                        MaxCombo += slider.Repeats + 1;

//                        AutoGenerator.AddDestination(new Vector2(slider.Position.X, slider.Position.Y), slider.StartTime, false);

//                        if (slider.Repeats > 1)
//                        {
//                            //Make sure to get the repeats with

//                            double repeatDuration = (slider.EndTime - slider.StartTime) / (double)slider.Repeats;
//                            double offset = repeatDuration;
//                            for (int repeat = 0; repeat < slider.Repeats; repeat++)
//                            {
//                                if(repeat % 2 == 0)
//                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.StartTime + offset, true);
//                                else
//                                    AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(0f), slider.StartTime + offset, true);

//                                offset += repeatDuration;
//                            }
//                        }
//                        else
//                        {
//                            AutoGenerator.AddDestination(drawableSlider.SliderPath.Path.CalculatePositionAtProgress(1f), slider.EndTime, true);
//                        }

//                        break;
//                    case Spinner spinner:
//                        DrawableSpinner drawableSpinner = new DrawableSpinner(spinner, colorIndex, combo++);
//                        drawableSpinner.Layer = -layer;
//                        HitObjects.Add(drawableSpinner);
//                        MaxCombo++;

//                        for (int spinTime = spinner.StartTime; spinTime < spinner.EndTime; spinTime+=16)
//                        {
//                            if (spinTime >= spinner.EndTime)
//                                spinTime = spinner.EndTime;

//                            Vector2 spinPos = new Vector2(spinner.Position.X, spinner.Position.Y);

//                            spinPos.X += MathF.Cos(spinTime / 20f) * 50;
//                            spinPos.Y += MathF.Sin(spinTime / 20f) * 50;
//                            AutoGenerator.AddDestination(spinPos, spinTime, false);
//                        }
//                        break;
//                }
//            }

//            AutoGenerator.Sort();
//        }

//        private double mapDifficultyRange(double difficulty, double min, double mid, double max)
//        {
//            if (difficulty > 5.0f)
//                return mid + (max - mid) * (difficulty - 5.0f) / 5.0f;

//            if (difficulty < 5.0f)
//                return mid - (mid - min) * (5.0f - difficulty) / 5.0f;

//            return mid;

//        }
//    }
//    */
//}