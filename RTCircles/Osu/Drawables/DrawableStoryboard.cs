using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Decoders;
using OsuParsers.Storyboards.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RTCircles
{
    /// <summary>
    /// All of this is so wrong, and idk how storyboards really work, and i dont care anymore
    /// </summary>
    public static class DrawableStoryboard
    {
        private static Dictionary<string, Texture> cachedTextures = new Dictionary<string, Texture>();

        public static float Alpha = 1;
        public static float Brightness = 0.5f;

        class DrawableStoryboardSprite
        {
            private Texture texture;

            private StoryboardSprite sSprite;

            public int StartTime { get; private set; } = int.MaxValue;
            public int EndTime { get; private set; } = int.MinValue;

            private AnimationTFloat fadeAnim = new AnimationTFloat() { DefaultValue = 0 };
            private AnimationTVec4 colorAnim = new AnimationTVec4() { DefaultValue = new Vector4(1f, 1f, 1f, 0f) };
            private AnimationTFloat xAnim = new AnimationTFloat();
            private AnimationTFloat yAnim = new AnimationTFloat();
            private AnimationTFloat rotationAnim = new AnimationTFloat() { DefaultValue = 0 };
            private AnimationTVec2 scaleAnim = new AnimationTVec2() { DefaultValue = Vector2.One };

            private Vector2 origin = Vector2.Zero;

            public DrawableStoryboardSprite(StoryboardSprite sSprite)
            {
                string texturePath = $"{OsuContainer.Beatmap.DirectoryPath}/{sSprite.FilePath}";

                if (DrawableStoryboard.cachedTextures.TryGetValue(texturePath, out var cachedTexture))
                {
                    texture = cachedTexture;
                }
                else
                {
                    if (File.Exists(texturePath))
                    {
                        texture = new Texture(File.OpenRead(texturePath));
                    }
                    else
                    {
                        if (texturePath.ToLower().Contains("hitcircle"))
                            texture = Skin.HitCircle;
                        else if (texturePath.ToLower().Contains("hitcircleoverlay"))
                            texture = Skin.HitCircleOverlay;
                        else if (texturePath.ToLower().Contains("approachcircle"))
                            texture = Skin.ApproachCircle;
                        else if (texturePath.ToLower().Contains("default-1"))
                            texture = Skin.CircleNumbers.Numbers[1];
                        else if (texturePath.ToLower().Contains("default-2"))
                            texture = Skin.CircleNumbers.Numbers[2];
                        else if (texturePath.ToLower().Contains("default-3"))
                            texture = Skin.CircleNumbers.Numbers[3];
                        else if (texturePath.ToLower().Contains("default-4"))
                            texture = Skin.CircleNumbers.Numbers[4];
                        else if (texturePath.ToLower().Contains("default-5"))
                            texture = Skin.CircleNumbers.Numbers[5];
                        else if (texturePath.ToLower().Contains("default-6"))
                            texture = Skin.CircleNumbers.Numbers[6];
                        else if (texturePath.ToLower().Contains("default-7"))
                            texture = Skin.CircleNumbers.Numbers[7];
                        else if (texturePath.ToLower().Contains("default-8"))
                            texture = Skin.CircleNumbers.Numbers[8];
                        else if (texturePath.ToLower().Contains("default-9"))
                            texture = Skin.CircleNumbers.Numbers[9];
                    }

                    if(texture == null)
                        Utils.Log($"Could not find storyboard texture: {texturePath}", LogLevel.Error);

                    DrawableStoryboard.cachedTextures.Add(texturePath, texture);

                    Utils.Log($"Loaded new storyboard sprite: {texturePath}", LogLevel.Important);
                }

                this.sSprite = sSprite;

                foreach (var cmd in sSprite.Commands.Commands)
                {
                    if (StartTime > cmd.StartTime)
                        StartTime = cmd.StartTime;

                    if (EndTime < cmd.EndTime)
                        EndTime = cmd.EndTime;

                    var easing = (EasingTypes)cmd.Easing;

                    switch (cmd.Type)
                    {
                        case OsuParsers.Enums.Storyboards.CommandType.None:
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.Movement:
                            xAnim.Add(cmd.StartTime, cmd.StartVector.X, easing);
                            xAnim.Add(cmd.EndTime, cmd.EndVector.X, easing);

                            yAnim.Add(cmd.StartTime, cmd.StartVector.Y, easing);
                            yAnim.Add(cmd.EndTime, cmd.EndVector.Y, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.MovementX:
                            xAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
                            xAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.MovementY:
                            yAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
                            yAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.Fade:
                            fadeAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
                            fadeAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.Scale:
                            scaleAnim.Add(cmd.StartTime, new Vector2(cmd.StartFloat), easing);
                            scaleAnim.Add(cmd.EndTime, new Vector2(cmd.EndFloat), easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.VectorScale:
                            scaleAnim.Add(cmd.StartTime, new Vector2(cmd.StartVector.X, cmd.StartVector.Y), easing);
                            scaleAnim.Add(cmd.EndTime, new Vector2(cmd.EndVector.X, cmd.EndVector.Y), easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.Rotation:
                            rotationAnim.Add(cmd.StartTime, cmd.StartFloat, easing);
                            rotationAnim.Add(cmd.EndTime, cmd.EndFloat, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.Colour:
                            colorAnim.Add(cmd.StartTime, Colors.From255RGBA(cmd.StartColour.R, cmd.StartColour.G, cmd.StartColour.B, 0f), easing);
                            colorAnim.Add(cmd.EndTime, Colors.From255RGBA(cmd.EndColour.R, cmd.EndColour.G, cmd.EndColour.B, 0f), easing);

                            fadeAnim.Add(cmd.StartTime, cmd.StartColour.A / 255f, easing);
                            fadeAnim.Add(cmd.EndTime, cmd.EndColour.A / 255f, easing);
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.FlipHorizontal:
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.FlipVertical:
                            break;
                        case OsuParsers.Enums.Storyboards.CommandType.BlendingMode:
                            break;
                        default:
                            break;
                    }
                }
                xAnim.DefaultValue = sSprite.X;
                yAnim.DefaultValue = sSprite.Y;
                
                colorAnim.Sort();
                fadeAnim.Sort();
                rotationAnim.Sort();
                scaleAnim.Sort();
                xAnim.Sort();
                yAnim.Sort();

                if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.TopLeft)
                    origin = new Vector2(-0.5f);
                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.BottomCentre)
                    origin.Y = 0.5f;
                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.TopCentre)
                    origin.Y = -0.5f;
                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.CentreRight)
                    origin.X = 0.5f;
                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.CentreLeft)
                    origin.X = -0.5f;
                else if (sSprite.Origin == OsuParsers.Enums.Storyboards.Origins.BottomRight)
                    origin = new Vector2(0.5f);
            }

            public void Render(Graphics g)
            {
                if (texture == null)
                    return;

                Vector4 color = colorAnim.GetOutputAtTime(OsuContainer.SongPosition);
                color.W = fadeAnim.GetOutputAtTime(OsuContainer.SongPosition) * DrawableStoryboard.Alpha;

                color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);

                color.Xyz *= Brightness;

                if (color.W < 0.001f)
                    return;

                float rotation = rotationAnim.GetOutputAtTime(OsuContainer.SongPosition);
                Vector2 vecScale = scaleAnim.GetOutputAtTime(OsuContainer.SongPosition);

                Vector2 position = new Vector2(xAnim.GetOutputAtTime(OsuContainer.SongPosition), yAnim.GetOutputAtTime(OsuContainer.SongPosition));

                Vector2 drawSize = (texture?.Size ?? Vector2.One) * vecScale * OsuContainer.OsuScale;
                Vector2 drawPos = OsuContainer.MapToPlayfield(position) - new Vector2(64, 57) * OsuContainer.OsuScale;

                float drawRotation = MathHelper.RadiansToDegrees(rotation);

                drawPos += drawSize * origin;

                //else if(sSprite.Origin != OsuParsers.Enums.Storyboards.Origins.Centre)
                //throw new Exception("cum");

                g.DrawRectangleCentered(drawPos, drawSize, color, texture, rotDegrees: drawRotation);
            }
        }

        private static List<DrawableStoryboardSprite> sprites = new List<DrawableStoryboardSprite>();

        private static string currentStoryboardFilename = "";

        static DrawableStoryboard()
        {
            string GetStoryboardPath(PlayableBeatmap beatmap)
            {
                return $"{beatmap.DirectoryPath}/" +
                            $"{beatmap.InternalBeatmap.MetadataSection.Artist} - {beatmap.InternalBeatmap.MetadataSection.Title}" +
                            $" ({beatmap.InternalBeatmap.MetadataSection.Creator}).osb";
            }


            OsuContainer.OnBeatmapChanged += (previousBeatmap) =>
            {
                var storyboardFilename = GetStoryboardPath(OsuContainer.Beatmap);

                var storyBoard = OsuContainer.Beatmap.InternalBeatmap.EventsSection.Storyboard;

                if (currentStoryboardFilename == storyboardFilename)
                    return;

                if (GlobalOptions.EnableStoryboard.Value == false)
                {
                    spriteIndex = 0;
                    activeSprites.Clear();
                    sprites.Clear();
                    cachedTextures.Clear();
                    return;
                }

                currentStoryboardFilename = storyboardFilename;

                spriteIndex = 0;
                activeSprites.Clear();
                sprites.Clear();
                cachedTextures.Clear();

                if (File.Exists(storyboardFilename))
                {
                    //Utils.BeginProfiling("Custom Storyboard");
                    using (FileStream fs = File.OpenRead(storyboardFilename))
                    {
                        var fileStoryBoard = StoryboardDecoder.Decode(fs);

                        if (fileStoryBoard.ForegroundLayer.Count > storyBoard.ForegroundLayer.Count)
                            storyBoard = fileStoryBoard;
                    }
                    //Utils.EndProfiling("Custom Storyboard");
                }

                Utils.Log($"Loading storyboard background layer : {storyBoard.BackgroundLayer.Count}", LogLevel.Info);
                foreach (var item in storyBoard.BackgroundLayer)
                {
                    if (item is StoryboardSprite sSprite)
                    {
                        sprites.Add(new DrawableStoryboardSprite(sSprite));
                    }
                }

                Utils.Log($"Loading storyboard fail layer layer : {storyBoard.FailLayer.Count}", LogLevel.Info);
                foreach (var item in storyBoard.FailLayer)
                {
                    if (item is StoryboardSprite sSprite)
                    {
                        sprites.Add(new DrawableStoryboardSprite(sSprite));
                    }
                }

                Utils.Log($"Loading storyboard foreground layer : {storyBoard.ForegroundLayer.Count}", LogLevel.Info);
                foreach (var item in storyBoard.ForegroundLayer)
                {
                    if (item is StoryboardSprite sSprite)
                    {
                        sprites.Add(new DrawableStoryboardSprite(sSprite));
                    }
                }

                Utils.Log($"Loading storyboard overlay layer : {storyBoard.OverlayLayer.Count}", LogLevel.Info);
                foreach (var item in storyBoard.OverlayLayer)
                {
                    if (item is StoryboardSprite sSprite)
                    {
                        sprites.Add(new DrawableStoryboardSprite(sSprite));
                    }
                }
                sprites.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            };
            
        }

        private static List<DrawableStoryboardSprite> activeSprites = new List<DrawableStoryboardSprite>();
        private static int spriteIndex = 0;

        public static void Render(Graphics g)
        {
            if(GlobalOptions.EnableStoryboard.Value == false)
                return;

            if (sprites.Count == 0)
                return;

            //Console.WriteLine(activeSprites.Count + " / " + sprites.Count);
            for (int i = 0; i < activeSprites.Count; i++)
            {
                activeSprites[i].Render(g);
            }

            if (spriteIndex < sprites.Count)
            {
                while (OsuContainer.SongPosition >= sprites[spriteIndex].StartTime)
                {
                    activeSprites.Add(sprites[spriteIndex]);
                    spriteIndex++;

                    if (spriteIndex == sprites.Count)
                        break;
                }
            }

            for (int i = activeSprites.Count - 1; i >= 0; i--)
            {
                if (OsuContainer.SongPosition > activeSprites[i].EndTime)
                    activeSprites.RemoveAt(i);
            }

            //If the storyboard is ahead of the current song time, then seek to the start, so it can sync from scratch
            if (sprites[(spriteIndex - 1).Clamp(0, sprites.Count - 1)].StartTime > OsuContainer.SongPosition)
            {
                activeSprites.Clear();
                spriteIndex = 0;
            }
            //System.Console.WriteLine($"Active sprites: {activeSprites.Count} Index: {spriteIndex}/{sprites.Count}");
        }
    }
}