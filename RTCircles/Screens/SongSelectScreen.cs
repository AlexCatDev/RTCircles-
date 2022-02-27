using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RTCircles
{
    public class SongSelectScreen : Screen
    {
        private FloatingScreen fs = new FloatingScreen();
        private MapBackground bg = new MapBackground();
        private Vector4 color = Colors.From255RGBA(37, 37, 37, 255);

        private FrameBuffer blurBuffer = new FrameBuffer(1, 1, Silk.NET.OpenGLES.FramebufferAttachment.ColorAttachment0, Silk.NET.OpenGLES.InternalFormat.Rgb, Silk.NET.OpenGLES.PixelFormat.Rgb);
        private bool shouldGenBlur = true;
        private bool shouldGenGraph = true;

        private GoodSliderBar testBar1 = new GoodSliderBar() { MaximumValue = 10 };

        public SongSelectScreen()
        {
            fs.SetTarget<OsuScreen>();

            bg.ParallaxAmount = 0;
            bg.ShowMenuFlash = false;
            bg.Opacity = 0.5f;
            bg.KiaiFlash = 0.75f;
            bg.TriggerFadeIn();
            Add(bg);

            OsuContainer.BeatmapChanged += () =>
            {
                if (OsuContainer.Beatmap.Background != null)
                    shouldGenBlur = true;

                shouldGenGraph = true;
            };

            //Add(new SongSelector());

            Add(testBar1);
        }

        public override void OnEntering()
        {
            ScreenManager.GetScreen<OsuScreen>().SyncObjectIndexToTime();
            base.OnEnter();
        }

        public override void OnKeyDown(Key key)
        {
            if(key == Key.Escape)
            {
                ScreenManager.GoBack();
            }

            base.OnKeyDown(key);
        }

        //private Texture floatingPanel = new Texture(File.OpenRead(@"C:\Users\user\Desktop\ui\FloatingScreen.png"));

        public override void Render(Graphics g)
        {
            base.Render(g);

            fs.Render(g);

            /*
            float height = 418 * MainGame.Scale;
            float spacing = 7 * MainGame.Scale;
            Vector2 panelSize = new Vector2(height * floatingPanel.Size.AspectRatio(), height);
            g.DrawRectangle(new Vector2(MainGame.WindowWidth - panelSize.X - spacing, spacing), panelSize, Colors.White, floatingPanel);
            */

            drawShit(g);

            renderBackground(bg, g);

            testBar1.Position = new Vector2(300, 300);
            testBar1.BackgroundColor = new Vector4(1f, 1f, 1f, 0.1f);
            testBar1.ForegroundColor = difficultyToColor(OsuContainer.Beatmap.OD * 0.5f + OsuContainer.Beatmap.CS * 0.25f + OsuContainer.Beatmap.AR * 0.25f);
            //testBar1.Value = 0.666;
            testBar1.Size = new Vector2(773, 34) / 2.25f;
        }

        private void renderBackground(MapBackground bg, Graphics g)
        {
            //shouldGenBlur = true;
            if (shouldGenBlur)
            {
                //Todo: Istedet for at resize blur buffer, så have blur bufferen relateret til screen resolution, og så render texture med offset i den og så blur den efterfølgende
                if (Blur.BlurTexture(OsuContainer.Beatmap.Background, blurBuffer, 2f, 6))
                {
                    bg.TextureOverride = blurBuffer.Texture;
                    shouldGenBlur = false;
                }
            }
        }

        private Vector2 convertSpace(Vector2 position, Vector2 baseSpace, Vector2 newSpace)
        {
            float x = position.X.Map(0, baseSpace.X, 0, newSpace.X);
            float y = position.Y.Map(0, baseSpace.Y, 0, newSpace.Y);

            return new Vector2(x, y);
        }

        private float frameThickness => 5f * MainGame.Scale;
        private Vector2 infoBarSize => new Vector2(fs.Size.X - fs.Size.X * 0.204f, 52 * MainGame.Scale);
        private Vector2 infoBarPosition => new Vector2(fs.Position.X + fs.Size.X - infoBarSize.X, fs.Position.Y + fs.Size.Y);

        private Vector2 triangleP1 => new Vector2(infoBarPosition.X - 52 * MainGame.Scale, infoBarPosition.Y);
        private Vector2 triangleP3 => new Vector2(infoBarPosition.X, infoBarPosition.Y + infoBarSize.Y);

        private void drawShit(Graphics g)
        {
            drawDifficultyAdjustPanel(g);
            drawFrame(g);

            g.DrawRectangle(infoBarPosition, infoBarSize, color);

            g.DrawRectangle(Vector2.Zero, new Vector2(68 * MainGame.Scale, MainGame.WindowHeight), color);

            g.DrawTriangle(triangleP1, infoBarPosition, triangleP3, color);

            g.DrawString($"{OsuContainer.Beatmap.Artist} - {OsuContainer.Beatmap.SongName}\nMapped by {OsuContainer.Beatmap.InternalBeatmap.MetadataSection.Creator}", Font.DefaultFont, infoBarPosition + new Vector2(8, 5) * MainGame.Scale, Vector4.One, 0.25f * MainGame.Scale, 15);

            //Button for drawDifficultyAdjustPanel ^^
            float rot = (1f - rotation) * 90;
            g.DrawRectangleCentered(fs.Position + fs.Size + new Vector2(-15 * MainGame.Scale, 52 * MainGame.Scale / 1.5f), new Vector2(48) * MainGame.Scale, Colors.White, Skin.Arrow, rotDegrees: rot);

            drawDifficultyBar(g);

            drawDifficultyGraph(g);
        }

        private SmoothFloat rotation = new SmoothFloat() { Value = 0 };
        private void drawDifficultyAdjustPanel(Graphics g)
        {
            if (rotation.Value > 0)
            {
                Vector2 size = new Vector2(500, 210 * rotation.Value) * MainGame.Scale;
                g.DrawRectangle(new Vector2((fs.Position.X + fs.Size.X) - size.X, infoBarPosition.Y + infoBarSize.Y), size, color);
            }
        }

        private FrameBuffer strainFB = new FrameBuffer(1, 1);
        private void drawDifficultyGraph(Graphics g)
        {
            if (OsuContainer.Beatmap?.DifficultyGraph.Count == 0)
                return;

            Vector2 size = new Vector2(MainGame.WindowWidth, 100);

            if (strainFB.Width != size.X || strainFB.Height != size.Y)
            {
                strainFB.EnsureSize(size.X, size.Y);
                shouldGenGraph = true;
            }

            if (shouldGenGraph)
            {
                List<Vector2> graph = new List<Vector2>();

                foreach (var item in OsuContainer.Beatmap.DifficultyGraph)
                {
                    graph.Add(new Vector2(0, (float)item));
                }

                Utils.Log($"Generating strain graph framebuffer!", LogLevel.Info);


                g.DrawInFrameBuffer(strainFB, () =>
                {
                    graph = PathApproximator.ApproximateCatmull(graph);

                    var vertices = g.VertexBatch.GetTriangleStrip(graph.Count * 2);

                    int vertexIndex = 0;

                    int textureSlot = g.GetTextureSlot(null);

                    float stepX = size.X / graph.Count;

                    Vector4 bottomColor = new Vector4(1f, 1f, 1f, 1f);
                    Vector4 peakColor = new Vector4(1f, 1f, 1f, 1f);

                    Vector2 movingPos = Vector2.Zero;

                    for (int i = 0; i < graph.Count; i++)
                    {
                        //float height = graph[i].Y.Map(0, 10, 0, size.Y);

                        float height = graph[i].Y.Map(0, 10000, 10, size.Y);

                        //Grundlinje
                        vertices[vertexIndex].TextureSlot = textureSlot;
                        vertices[vertexIndex].Color = bottomColor;
                        vertices[vertexIndex].Position = movingPos;

                        vertexIndex++;

                        movingPos.Y += height;

                        //TopLinje
                        vertices[vertexIndex].TextureSlot = textureSlot;
                        vertices[vertexIndex].Color = peakColor;
                        vertices[vertexIndex].Position = movingPos;

                        movingPos.Y -= height;
                        movingPos.X += stepX;

                        vertexIndex++;
                    }
                });

                shouldGenGraph = false;

            }

            Vector2 position = new Vector2(0, MainGame.WindowHeight - size.Y);

            //Vector2 songPosPos = new Vector2((float)OsuContainer.SongPosition.Map(OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime, OsuContainer.Beatmap.HitObjects[^1].BaseObject.StartTime, position.X, position.X + size.X), position.Y + poo.Y / 4);

            float songX = (float)OsuContainer.SongPosition.Map(OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime, OsuContainer.Beatmap.HitObjects[^1].BaseObject.StartTime, 0, 1).Clamp(0, 1);

            Vector4 progressColor = new Vector4(1f, 0f, 0f, 0.25f);
            Vector4 progressNotColor = new Vector4(1f, 1f, 1f, 0.25f);

            Rectangle texRectProgress = new Rectangle(0, 0, songX, 1);

            Vector2 sizeProgress = new Vector2(strainFB.Texture.Width * songX, strainFB.Texture.Height);

            //Progress
            g.DrawRectangle(position, sizeProgress, progressColor, strainFB.Texture, texRectProgress, true);

            //Not progress
            g.DrawRectangle(position, strainFB.Texture.Size, progressNotColor, strainFB.Texture);
        }

        private void drawDifficultyBar(Graphics g)
        {
            float difficultySize = 0.25f * MainGame.Scale;

            string difficultyString = OsuContainer.Beatmap.DifficultyName;

            Vector2 difficultyTextSize = Font.DefaultFont.MessureString(difficultyString, difficultySize);

            Vector2 difficultyBarSize = new Vector2(difficultyTextSize.X + 27 * MainGame.Scale, 23 * MainGame.Scale);

            Vector2 difficultyBarPosition = new Vector2(fs.Position.X + fs.Size.X - difficultyBarSize.X - frameThickness, fs.Position.Y + fs.Size.Y - difficultyBarSize.Y - frameThickness);

            g.DrawRectangle(difficultyBarPosition, difficultyBarSize, color);

            Vector2 triP1 = difficultyBarPosition - new Vector2(23 * MainGame.Scale, -difficultyBarSize.Y);
            Vector2 triP2 = difficultyBarPosition + new Vector2(0, difficultyBarSize.Y);
            Vector2 triP3 = difficultyBarPosition;

            g.DrawTriangle(triP1, triP2, triP3, color);

            Vector2 topRight = difficultyBarPosition;
            Vector2 bottomRight = triP1;

            Vector2 width = new Vector2(8 * MainGame.Scale, 0);

            g.DrawQuadrilateral(topRight - width, topRight, bottomRight + width, bottomRight, difficultyToColor(OsuContainer.Beatmap.OD * 0.5f + OsuContainer.Beatmap.CS * 0.25f + OsuContainer.Beatmap.AR * 0.25f));
            g.DrawString(difficultyString, Font.DefaultFont, difficultyBarPosition + difficultyBarSize / 2f - difficultyTextSize / 2f, Colors.White, difficultySize);
        }

        private static Texture shadowTexture = new Texture(File.OpenRead(@"C:\Users\user\Desktop\shadow.png"));
        private void drawFrame(Graphics g)
        {
            g.DrawOneSidedLine(fs.Bounds.TopLeft, fs.Bounds.BottomLeft, color, color, frameThickness);
            g.DrawOneSidedLine(fs.Bounds.BottomLeft, fs.Bounds.BottomRight, color, color, frameThickness);
            g.DrawOneSidedLine(fs.Bounds.BottomRight, fs.Bounds.TopRight, color, color, frameThickness);
            g.DrawOneSidedLine(fs.Bounds.TopRight, fs.Bounds.TopLeft, color, color, frameThickness);
            
            //Why am i doing this just for a fucking drop shadow

            float shadowThickness = frameThickness * 3.5f;
            Vector4 shadowColor = new Vector4(0, 0, 0, 1f);
            Rectangle rect = new Rectangle(0, 0, 1, 1);
            int segments = 4;

            g.DrawOneSidedLine(fs.Bounds.TopLeft, fs.Bounds.BottomLeft, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);
            g.DrawEllipse(fs.Bounds.TopLeft, 270, 180, shadowThickness, 0, shadowColor, shadowTexture, segments, false);


            //3 pixel overlap :tf:
            g.DrawOneSidedLine(fs.Bounds.BottomLeft, triangleP1, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);
            g.DrawEllipse(fs.Bounds.BottomLeft, 90, 180, shadowThickness, 0, shadowColor, shadowTexture, segments, false);

            g.DrawOneSidedLine(triangleP1, triangleP3, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);
            g.DrawEllipse(triangleP3, 135, 90, shadowThickness, 0, shadowColor, shadowTexture, segments, false);

            var p = fs.Bounds.BottomRight + new Vector2(0, infoBarSize.Y);
            g.DrawOneSidedLine(triangleP3, p, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);

            g.DrawEllipse(p, 0, 90, shadowThickness, 0, shadowColor, shadowTexture, segments, false);

            g.DrawOneSidedLine(p, fs.Bounds.TopRight, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);

            g.DrawOneSidedLine(fs.Bounds.TopRight, fs.Bounds.TopLeft, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);

            g.DrawEllipse(fs.Bounds.TopRight, 270, 360, shadowThickness, 0, shadowColor, shadowTexture, segments, false);
            
            if (OsuContainer.Beatmap != null)
            {
                g.DrawString(
                    "length\n" +
                    "bpm\n" +
                    "cs\n"  +
                    "ar\n"  +
                    "od\n"  +
                    "hp\n",
                    Font.DefaultFont, fs.Position + new Vector2(fs.Size.X * 0.02f, fs.Size.X * 0.02f), Colors.White, 0.3f * MainGame.Scale, 15);

                var timeSpan = TimeSpan.FromMilliseconds(OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime - OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime);
                var bpm = 60000 / OsuContainer.CurrentBeatTimingPoint?.BeatLength;

                string totalLength = (Math.Floor(timeSpan.TotalMinutes) + ":" + timeSpan.ToString("ss"));

                g.DrawString(
                    $"{totalLength}\n" +
                    $"{bpm:F0}\n" +
                    $"{OsuContainer.Beatmap.CS}\n" +
                    $"{OsuContainer.Beatmap.AR}\n" +
                    $"{OsuContainer.Beatmap.OD}\n" +
                    $"{OsuContainer.Beatmap.HP}\n",
                    Font.DefaultFont, fs.Position + new Vector2(fs.Size.X * 0.15f, fs.Size.X * 0.020f), Colors.White, 0.3f * MainGame.Scale, 15);
            }
        }

        private Vector3[] difficultyGradient = new Vector3[] { Vector3.One, new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 0) };
        private Vector4 difficultyToColor(float difficulty)
        {
            float index = difficulty.Clamp(0, 10).Map(0, 10, 0, difficultyGradient.Length - 1);

            int lowerIndex = (int)MathF.Floor(index);

            if (lowerIndex + 1 >= difficultyGradient.Length)
                return new Vector4(difficultyGradient[^1], 1);

            float blend = index - lowerIndex;

            return new Vector4(Vector3.Lerp(difficultyGradient[lowerIndex], difficultyGradient[lowerIndex + 1], blend), 1);
        }

        public override void OnMouseDown(MouseButton args)
        {
            if (rotation == 0)
            {
                rotation.TransformTo(1f, 0.25f, EasingTypes.OutQuart);
            }
            else if (rotation == 1)
            {
                rotation.TransformTo(0f, 0.25f, EasingTypes.InQuart);
            }
            base.OnMouseDown(args);
        }

        public override void Update(float delta)
        {
            base.Update(delta);

            rotation.Update(delta);

            float spacing = 14 * MainGame.Scale;
            float screenHeight = 420 * MainGame.Scale;
            float screenWidth = screenHeight * 1.77777777778f;

            fs.Size = new Vector2((int)screenWidth, (int)screenHeight);
            fs.Position = new Vector2((int)(MainGame.WindowWidth - fs.Size.X - spacing), (int)spacing);

            fs.Update(delta);
        }
    }
}
