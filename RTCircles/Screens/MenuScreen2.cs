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
    public class MenuScreen2 : Screen
    {
        private SmoothFloat introFade = new SmoothFloat() { Value = 0 };
        private SmoothFloat volumeFade = new SmoothFloat() { Value = 0 };
        public MenuScreen2()
        {
            introFade.TransformTo(1f, 0.3f, EasingTypes.Out, () =>
            {
                BeatmapMirror.Scheduler.Enqueue(() =>
                {
                    ScreenManager.GetScreen<MapSelectScreen>().LoadCarouselItems();

                    var carouselItems = BeatmapCarousel.Items;

                    if (BeatmapCarousel.Items.Count > 0)
                    {
                        var item = carouselItems[RNG.Next(0, carouselItems.Count - 1)];

                        OsuContainer.SetMap(BeatmapMirror.DecodeBeatmap(System.IO.File.OpenRead(item.FullPath)), true, Mods.Auto);
                    }
                    else
                    {
                        PlayableBeatmap builtInBeatmap = new PlayableBeatmap(
                            BeatmapMirror.DecodeBeatmap(Utils.GetResource("Maps.BuildIn.map.osu")),
                            new Sound(Utils.GetResource("Maps.BuildIn.audio.mp3"), true, false),
                            new Texture(Utils.GetResource("Maps.BuildIn.eleventea.jpg")));

                        builtInBeatmap.GenerateHitObjects(Mods.NM);
                        OsuContainer.SetMap(builtInBeatmap);

                        GPUSched.Instance.EnqueueAsync(() => { return (true, 0); }, (obj) => { ScreenManager.SetScreen<OsuScreen>(); }, 10000);
                    }

                    OsuContainer.Beatmap.Song.Volume = 0;
                    OsuContainer.SongPosition = (OsuContainer.Beatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai))?.Offset - 2500 ?? 0;
                    OsuContainer.Beatmap.Song.Play(false);
                });

                Add(new NewLogo());
                introFade.TransformTo(0f, 0.3f, EasingTypes.Out);
                volumeFade.TransformTo(1f, 1f);
            });
        }

        public override void Render(Graphics g)
        {
            g.DrawRectangleCentered(MainGame.WindowCenter, new Vector2(760) * MainGame.Scale * introFade.Value.Map(0, 1, 2, 1), new Vector4(1f, 1f, 1f, introFade.Value), MenuLogo.LogoTexture);
            base.Render(g);
        }

        public override void Update(float delta)
        {
            introFade.Update(delta);
            volumeFade.Update(delta);

            if (OsuContainer.Beatmap != null)
                OsuContainer.Beatmap.Song.Volume = volumeFade.Value * GlobalOptions.SongVolume.Value; 

            base.Update(delta);
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.O)
                ScreenManager.SetScreen<MapSelectScreen>(true);
            base.OnKeyDown(key);
        }
    }

    public class AudioGraph : Drawable
    {
        private List<Vector2> controlPoints = new List<Vector2>();

        public Vector2 PositionOffset;

        public override void Render(Graphics g)
        {
            controlPoints.Clear();

            for (int i = 0; i < smoothBuffer.Length; i++)
            {
                controlPoints.Add(new Vector2(0, smoothBuffer[i]));
            }

            var points = PathApproximator.ApproximateBezier(controlPoints);
            Vector4 innerColor = Colors.From255RGBA(37, 37, 37, 255);
            Vector4 outerColor = new Vector4(2f, 2f, 2f, 1f);
            float border = -0.01f;

            float minHeight = 5f * MainGame.Scale;

            PositionOffset=new Vector2(0, 0);
            drawGraph(g, points, Vector2.Zero + PositionOffset, new Vector2(MainGame.WindowWidth, 500 * MainGame.Scale), rainbowFunc, rainbowFunc, 0, minHeight);
            drawGraph(g, points, Vector2.Zero + PositionOffset, new Vector2(MainGame.WindowWidth, 500 * MainGame.Scale), (progress) => { return innerColor; }, (progress) => { return innerColor; }, border, minHeight);

            drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight) + PositionOffset, new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), rainbowFunc, rainbowFunc, 0, -minHeight);
            drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight) + PositionOffset, new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), (progress) => { return innerColor; }, (progress) => { return innerColor; }, border, -minHeight);

            //drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight), new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), (progress) => { return outerColor; }, (progress) => { return outerColor; });
            //drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight), new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), (progress) => { return innerColor; }, (progress) => { return innerColor; }, border);

            /*
            drawGraph(g, points, Vector2.Zero, new Vector2(MainGame.WindowWidth, 500 * MainGame.Scale), outerColor);
            drawGraph(g, points, Vector2.Zero, new Vector2(MainGame.WindowWidth, 500 * MainGame.Scale), innerColor, border);

            drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight), new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), outerColor);
            drawGraph(g, points, new Vector2(MainGame.WindowWidth, MainGame.WindowHeight), new Vector2(-MainGame.WindowWidth, 500 * -MainGame.Scale), innerColor, border);
            */

        }

        private Vector4 rainbowFunc(float progress)
        {
            var beat = OsuContainer.CurrentBeat.Map(0,1,0,MathF.PI/2);
            float colorMultiplier = OsuContainer.IsKiaiTimeActive ? 3 : 2;

            float r = (float)Math.Sin(beat + 0 + progress * 3f).Map(-1, 1, 0, 1);
            float g = (float)Math.Sin(beat + 2 + progress * 3f).Map(-1, 1, 0, 1);
            float b = (float)Math.Sin(beat + 4 + progress * 3f).Map(-1, 1, 0, 1);

            return Colors.Tint(new Vector4(r, g, b, 1f), PostProcessing.Bloom ? colorMultiplier : 1);
        }

        private void drawGraph(Graphics g, List<Vector2> points, Vector2 position, Vector2 size, Func<float, Vector4> startColorAt, Func<float, Vector4> endColorAt, float volumeOffset = 0, float addedHeight = 0)
        {
            float width = size.X / (points.Count - 1);

            var verts = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            int vertIndex = 0;

            int slot = g.GetTextureSlot(null);

            for (int i = 0; i < points.Count; i++)
            {
                float volume = (points[i].Y + volumeOffset) * size.Y;
                volume += addedHeight;

                verts[vertIndex].Position = new Vector2(position.X, position.Y);
                verts[vertIndex].Color = startColorAt(((float)i / points.Count));
                verts[vertIndex].TextureSlot = slot;
                ++vertIndex;

                verts[vertIndex].Position = new Vector2(position.X, position.Y + volume);
                verts[vertIndex].Color = endColorAt(((float)i / points.Count));
                verts[vertIndex].TextureSlot = slot;
                ++vertIndex;

                position.X += width;
            }
        }

        private float[] buffer = new float[4096];
        private float[] smoothBuffer = new float[33];

        public float this[int i] => buffer[i];

        public float BeatValue { get; private set; }
        public override void Update(float delta)
        {
            bool playing = true;

            if (OsuContainer.Beatmap != null)
                OsuContainer.Beatmap.Song.GetFFTData(buffer, ManagedBass.DataFlags.FFT8192);
            else
                playing = false;

            BeatValue = 0;
            for (int i = 0; i < smoothBuffer.Length; i++)
            {
                smoothBuffer[i] = playing ? MathHelper.Lerp(buffer[i + 5], 0, 20f * delta) : MathHelper.Lerp(smoothBuffer[i], 0, 1f * delta);

                BeatValue += (smoothBuffer[i] / smoothBuffer.Length) * Interpolation.ValueAt((float)i, 1, 0, 0, smoothBuffer.Length, EasingTypes.In);
            }
        }
    }

    public class NewLogo : Drawable
    {
        private static Texture singleplayTexture = new Texture(Utils.GetResource("Skin.text-singleplay.png"));
        private static Texture multiplayerTexture = new Texture(Utils.GetResource("Skin.text-multiplayer.png"));
        private static Texture optionsTexture = new Texture(Utils.GetResource("Skin.text-options.png"));
        private static Texture exitTexture = new Texture(Utils.GetResource("Skin.text-exit.png"));

        private Vector2 position => MainGame.WindowCenter + mapBackground.ParallaxPosition * 2 + new Vector2(-500, 0) * animPos.Value * MainGame.AbsoluteScale.X;
        private Vector2 size => rawSize * audioGraph.BeatValue.Map(0, 1, 1, 2f);

        private Vector2 rawSize => new Vector2(700 * animSize.Value * MainGame.Scale);

        private SmoothFloat animSize = new SmoothFloat() { Value = 1 };
        private SmoothFloat animPos = new SmoothFloat();

        private SmoothFloat barAnim = new SmoothFloat();

        private AudioGraph audioGraph = new AudioGraph();
        private ScrollingTriangles scrollingTriangles = new ScrollingTriangles(100) { BaseColor = Colors.From255RGBA(37,37,37,255)};
        private MapBackground mapBackground = new MapBackground();

        private BouncingButton singleplayBtn = new BouncingButton(singleplayTexture);
        private BouncingButton multiplayerBtn = new BouncingButton(multiplayerTexture);

        private BouncingButton optionsBtn = new BouncingButton(optionsTexture);
        private BouncingButton exitBtn = new BouncingButton(exitTexture);
        public NewLogo()
        {
            singleplayBtn.OnClick += () =>
            {
                ScreenManager.SetScreen<MapSelectScreen>();
            };

            optionsBtn.OnClick += () =>
            {
                ScreenManager.SetScreen<OptionsScreen>();
            };

            Layer = 1337;
        }

        public override void OnAdd()
        {
            Container.Add(audioGraph);

            mapBackground.TriggerFadeIn();
            Container.Add(mapBackground);
        }

        public override void Render(Graphics g)
        {
            Vector2 barSize = new Vector2((MainGame.WindowWidth * barAnim.Value), rawSize.Y / 1.5f);
            Vector4 barColor = Colors.From255RGBA(37, 37, 37, 225);

            Vector2 barPos1 = new Vector2(position.X, position.Y - barSize.Y / 2);
            Vector2 barPos2 = new Vector2(position.X - barSize.X, position.Y - barSize.Y / 2);

            g.DrawRectangle(barPos1, barSize, barColor);
            g.DrawRectangle(barPos2, barSize, barColor);

            singleplayBtn.Size = new Vector2(barSize.Y);
            singleplayBtn.Alpha = barAnim.Value;
            singleplayBtn.Position = barPos1 + new Vector2(300 * MainGame.AbsoluteScale.X, barSize.Y / 2);

            multiplayerBtn.Size = new Vector2(barSize.Y);
            multiplayerBtn.Alpha = barAnim.Value;
            multiplayerBtn.Position = barPos1 + new Vector2(340 * MainGame.AbsoluteScale.X + singleplayBtn.Size.X, barSize.Y / 2);

            optionsBtn.Size = new Vector2(barSize.Y);
            optionsBtn.Alpha = barAnim.Value;
            optionsBtn.Position = barPos1 + new Vector2(360 * MainGame.AbsoluteScale.X + singleplayBtn.Size.X * 2, barSize.Y / 2);

            exitBtn.Size = new Vector2(barSize.Y);
            exitBtn.Alpha = barAnim.Value;
            exitBtn.Position = barPos1 + new Vector2(700 * MainGame.AbsoluteScale.X + singleplayBtn.Size.X * 3, barSize.Y / 2);

            //g.DrawRectangleCentered(testButton.Position, testButton.Size * 2, new Vector4(barColor.Xyz, barAnim.Value));

            if (barAnim.Value > 0)
            {
                singleplayBtn.Render(g);
                multiplayerBtn.Render(g);
                optionsBtn.Render(g);
                exitBtn.Render(g);
            }

            scrollingTriangles.Render(g);

            g.DrawRectangleCentered(position, size, Colors.White, MenuLogo.LogoTexture);
        }

        private float idleTimer;
        private Vector2 lastMousePos;

        public override void Update(float delta)
        {
            float animDelta = delta * 1.1f;
            animPos.Update(animDelta);
            animSize.Update(animDelta);
            barAnim.Update(animDelta);

            idleTimer += delta;

            if((Input.MousePosition - lastMousePos).Length > 0)
            {
                idleTimer = 0;
                lastMousePos = Input.MousePosition;
            }

            if(idleTimer > 30)
            {
                unClickLogo();
                idleTimer = 0;
            }

            audioGraph.PositionOffset = mapBackground.ParallaxPosition;

            scrollingTriangles.Update(delta);
            scrollingTriangles.Speed = 100 + 3000 * audioGraph.BeatValue;

            scrollingTriangles.Position = position;
            scrollingTriangles.Radius = (size.X - 15 * MainGame.Scale) / 2;

            singleplayBtn.Update(delta);
            multiplayerBtn.Update(delta);
            optionsBtn.Update(delta);
            exitBtn.Update(delta);

            /*
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < 33; i++)
            {
                Console.WriteLine($"[{i}] {audioGraph[i]}        ");
            }
            */
            if (OsuContainer.IsKiaiTimeActive)
            {
                float beat = (audioGraph[2] + audioGraph[6] + audioGraph[9] + audioGraph[16]) / 4;
                mapBackground.Zoom = MathHelper.Lerp(mapBackground.Zoom, 50 + 3000 * beat, 20f * delta);
            }
            else
                mapBackground.Zoom = MathHelper.Lerp(mapBackground.Zoom, 50, 10f * delta);
        }

        private void clickedLogo()
        {
            idleTimer = 0;
            barAnim.ClearTransforms();
            animSize.ClearTransforms();
            animPos.ClearTransforms();

            animSize.TransformTo(0.4f, 0.75f, EasingTypes.OutElasticHalf);
            barAnim.Wait(0.1f).TransformTo(1f, 0.25f, EasingTypes.OutCirc);
            animPos.TransformTo(1f, 0.2f, EasingTypes.Out);
        }

        private void unClickLogo()
        {
            barAnim.TransformTo(0f, 0.25f, EasingTypes.Out, () => { 
                animSize.TransformTo(1f, 0.75f, EasingTypes.OutElasticHalf);
                animPos.TransformTo(0, 0.2f, EasingTypes.Out);
            });
        }

        public override bool OnKeyDown(Key key)
        {
            if(key == Key.Escape)
                unClickLogo();

            return base.OnKeyDown(key);
        }

        public override bool OnMouseDown(MouseButton button)
        {
            if(button == MouseButton.Left && MathUtils.PositionInsideRadius(Input.MousePosition, position, size.X))
                clickedLogo();

            if (barAnim.Value > 0)
            {
                singleplayBtn.OnMouseDown(button);
                multiplayerBtn.OnMouseDown(button);
                optionsBtn.OnMouseDown(button);
                exitBtn.OnMouseDown(button);
            }

            return base.OnMouseDown(button);
        }
    }
}
