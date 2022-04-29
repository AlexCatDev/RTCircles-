using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTCircles
{
    public class CumSprayer : Drawable
    {
        class Cum : Drawable
        {
            private Vector2 size;
            private SmoothFloat alpha = new SmoothFloat() { Value = 1 };

            private Vector2 position;
            private Vector2 velocity;
            private float angle = 0;

            private float scale = 0;

            private static long layer = long.MaxValue;

            public Cum(Vector2 startPos, float angle)
            {
                Layer = layer--;

                position = startPos;

                alpha.Wait(0.1f);
                alpha.TransformTo(0f, 0.8f, EasingTypes.Out, () =>
                {
                    IsDead = true;
                });

                size = new Vector2(RNG.Next(24, 64));

                scale = size.X.Map(24, 64, 2000, 1500);

                velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * scale;
                velocity.X /= 1.5f;
                angle = RNG.Next(0, 360);
            }

            public override void Render(Graphics g)
            {
                var rgb = Skin.Config.ColorFromIndex(0);
                var color = new Vector4(rgb, alpha);

                g.DrawRectangleCentered(position, size * MainGame.Scale * Skin.GetScale(Skin.HitCircle), color, Skin.HitCircle, rotDegrees: angle);

                if (Skin.Config.HitCircleOverlayAboveNumber)
                    Skin.CircleNumbers.DrawCentered(g, position, size.Y * MainGame.Scale / 2.7f, new Vector4(1f, 1f, 1f, alpha), "727");

                g.DrawRectangleCentered(position, size * MainGame.Scale * Skin.GetScale(Skin.HitCircleOverlay), new Vector4(1f, 1f, 1f, alpha), Skin.HitCircleOverlay, rotDegrees: angle);

                if (!Skin.Config.HitCircleOverlayAboveNumber)
                    Skin.CircleNumbers.DrawCentered(g, position, size.Y * MainGame.Scale / 2.7f, new Vector4(1f, 1f, 1f, alpha), "727");
            }

            public override void Update(float delta)
            {
                position += velocity * delta * MainGame.Scale;
                angle += scale * delta / 10;

                velocity.Y += 2400f * delta;

                alpha.Update(delta);
            }
        }

        public CumSprayer()
        {
            OsuContainer.OnKiai += OsuContainer_OnKiai;
            Layer = Int32.MaxValue;
        }

        public Vector2? Position;

        private bool startSpawningCum;
        private float cumTimer;
        private float angle = 0;

        private void OsuContainer_OnKiai()
        {
            startSpawningCum = true;
            angle = 0;
        }

        public override void Render(Graphics g)
        {
            g.DrawString(angle.ToString(), Font.DefaultFont, Input.MousePosition, Colors.Blue, 1f);
        }

        public override void Update(float delta)
        {
            float spawnRate = 0.02f;
            float startAngle = MathF.PI + MathF.PI / 8;
            if (startSpawningCum)
            {
                cumTimer += delta;

                angle += 2f * delta;

                if (cumTimer >= spawnRate)
                {
                    cumTimer -= spawnRate;
                    Container.Add(new Cum(Position ?? Input.MousePosition, startAngle + angle));
                }

                if(angle >= MathF.PI - MathF.PI / 3)
                {
                    startSpawningCum = false;
                    angle = 0;
                }
            }
        }

        public override bool OnKeyDown(Key key)
        {
            OsuContainer.KeyDown(key);
            return base.OnKeyDown(key);
        }
    }

    public class MenuScreen : Screen
    {
        private MapBackground mapBG;
        private MenuLogo logo;

        public MenuScreen()
        {
            BeatmapMirror.Scheduler.Enqueue(() =>
            {
                ScreenManager.GetScreen<MapSelectScreen>().LoadCarouselItems();
                var carouselItems = BeatmapCollection.Items;
                if (BeatmapCollection.Items.Count > 0)
                {
                    var item = carouselItems[RNG.Next(0, carouselItems.Count - 1)];

                    OsuContainer.SetMap(item);
                    
                }
                else
                {
                    PlayableBeatmap playingBeatmap = new PlayableBeatmap(
                        BeatmapMirror.DecodeBeatmap(Utils.GetResource("Maps.BuildIn.map.osu")),
                        new Sound(Utils.GetResource("Maps.BuildIn.audio.mp3"), true),
                        new Texture(Utils.GetResource("Maps.BuildIn.bg.jpg")));

                    playingBeatmap.GenerateHitObjects(Mods.NM);

                    OsuContainer.SetMap(playingBeatmap);

                    NotificationManager.ShowMessage("You don't to have have any maps. Click here to get some", ((Vector4)Color4.Violet).Xyz, 10, () =>
                    {
                        ScreenManager.SetScreen<DownloadScreen>();
                    });
                }


                if (OsuContainer.Beatmap != null)
                {
                    OsuContainer.Beatmap.Song.Volume = 0;
                    OsuContainer.SongPosition = (OsuContainer.Beatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai))?.Offset - 500 ?? 0;
                    OsuContainer.Beatmap.Song.Play(false);
                }

                logo.soundFade.TransformTo((float)GlobalOptions.SongVolume.Value, 0.5f);

                logo.sizeTransform.TransformTo(Vector2.Zero, 0.5f, EasingTypes.InQuint, () =>
                {
                    logo.ToggleInput(true);
                    //Fade background in
                    mapBG.TriggerFadeIn();
                });
            });

            mapBG = new MapBackground() { BEAT_SIZE = 10 };
            logo = new MenuLogo(mapBG);

            logo.sizeTransform.Value = new Vector2(1000);
            logo.ToggleInput(false);

            Add(mapBG);
            Add(logo);
        }

        public override void OnMouseDown(MouseButton args)
        {
            base.OnMouseDown(args);
        }

        public override void Update(float delta)
        {
            mapBG.UseBluredGameplayAsBackgroundSource = GlobalOptions.UseGameplayAsBackgroundSrc.Value;
            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            base.Render(g);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }

    public class MapBackground : Drawable
    {
        public override Rectangle Bounds => new Rectangle(0, 0, 1920, 1080);

        private SmoothVector4 fadeColor = new SmoothVector4();

        public float Opacity = 0.5f;
        public float KiaiFlash = 2.0f;

        public static readonly Texture FlashTexture = new Texture(Utils.GetResource("Skin.menu-flash.png")) { GenerateMipmaps = false };

        public bool ShowMenuFlash = true;

        public Texture TextureOverride;

        public MapBackground()
        {
            Layer = -72727;
            OsuContainer.OnKiai += () =>
            {
                fadeColor.Value = new Vector4(KiaiFlash, KiaiFlash, KiaiFlash, 1f);
                fadeColor.TransformTo(new Vector4(Opacity, Opacity, Opacity, 1f), 1.5f, EasingTypes.OutSine);
            };
        }

        public void TriggerFadeIn()
        {
            fadeColor.TransformTo(new Vector4(Opacity, Opacity, Opacity, 1f), 0.25f, EasingTypes.Out);
        }

        private Vector2 position = Vector2.Zero;

        public Vector2 ParallaxPosition { get; private set; }

        public float ParallaxAmount = 10;
        public float ParallaxSmooth = 15f;

        public float BEAT_SIZE = 0f;
        public float Zoom = 50f;

        public float Rotation;

        private SmoothFloat beatFlash = new SmoothFloat();
        private SmoothFloat beatFlash2 = new SmoothFloat();
        private int previousBeat;

        private float beatScale = 0;

        public bool UseBluredGameplayAsBackgroundSource;

        private FrameBuffer fb = new FrameBuffer(1,1);
        private FrameBuffer bluredFB = new FrameBuffer(1, 1);
        private void drawBluredGameplay(Graphics g)
        {
            var osuScreen = ScreenManager.GetScreen<OsuScreen>();

            fb.EnsureSize(MainGame.WindowWidth/6, MainGame.WindowHeight/6);

            g.DrawInFrameBuffer(fb, () => {
                MainGame.Instance.FakeWindowSize(fb.Texture.Size, () =>
                {
                    osuScreen.SyncObjectIndexToTime();
                    osuScreen.Update((float)MainGame.Instance.DeltaTime);
                    osuScreen.Render(g);
                });
            });

            bluredFB.EnsureSize(fb.Width, fb.Height);

            Blur.BlurTexture(fb.Texture, bluredFB, 1, 2);
        }

        public override void Render(Graphics g)
        {
            if (OsuContainer.Beatmap is null)
                return;

            var color = fadeColor.Value;
            var textureRectangle = new Rectangle(0, 0, 1, 1);

            var tex = TextureOverride ?? OsuContainer.Beatmap.Background;

            if (UseBluredGameplayAsBackgroundSource)
            {
                drawBluredGameplay(g);
                textureRectangle = new Rectangle(0, 1, 1, -1);
                color.X += 1f;
                color.Y += 1f;
                color.Z += 1f;

                tex = bluredFB.Texture;
            }

            float aspectRatio = tex.Size.AspectRatio();

            beatScale = (float)Interpolation.Damp(beatScale, OsuContainer.BeatProgressKiai, 0.95, MainGame.Instance.DeltaTime * 1000);

            float width = MainGame.WindowWidth + (BEAT_SIZE * beatScale) + Zoom;
            float height = MainGame.WindowHeight + (BEAT_SIZE * beatScale) + Zoom;

            Vector2 bgSize;
            bgSize = new Vector2(width, width / aspectRatio) + new Vector2(ParallaxAmount * 2);

            if(bgSize.Y < MainGame.WindowHeight)
                bgSize = new Vector2(height * aspectRatio, height) + new Vector2(ParallaxAmount * 2);

            g.DrawRectangleCentered(MainGame.WindowCenter + ParallaxPosition, bgSize, color, tex, textureRectangle, true, Rotation);

            if (OsuContainer.IsKiaiTimeActive && ShowMenuFlash)
            {
                int beat = (int)Math.Floor(OsuContainer.CurrentBeat);

                if (previousBeat > beat)
                    previousBeat = beat;

                if (beat - previousBeat > 0)
                {
                    EasingTypes fadeOutEasing = EasingTypes.InQuad;
                    float fadeOutTime = 400f;

                    EasingTypes fadeInEasing = EasingTypes.OutQuad;
                    float fadeInTime = 50f;

                    if (OsuContainer.CurrentBeatTimingPoint != null)
                        fadeOutTime = (float)OsuContainer.CurrentBeatTimingPoint?.BeatLength;

                    fadeOutTime /= 1000;
                    fadeInTime /= 1000;

                    float fadeOutOpacity = 0.4f;

                    if (beat % 2 == 0)
                    {
                        beatFlash2.Value = 0;
                        beatFlash2.TransformTo(fadeOutOpacity, fadeInTime, fadeInEasing);
                        beatFlash2.TransformTo(0f, fadeOutTime, fadeOutEasing);
                    }
                    else
                    {
                        beatFlash.Value = 0;
                        beatFlash.TransformTo(fadeOutOpacity, fadeInTime, fadeInEasing);
                        beatFlash.TransformTo(0f, fadeOutTime, fadeOutEasing);
                    }

                    previousBeat = beat;
                }
            }

            if (beatFlash.HasCompleted == false || beatFlash2.HasCompleted == false)
            {
                Vector2 flashSize = new Vector2(MainGame.WindowWidth / 4, MainGame.WindowHeight);

                g.DrawRectangle(Vector2.Zero, flashSize, new Vector4(1f, 1f, 1f, beatFlash.Value), FlashTexture, new Rectangle(1, 0, -1, 1), true);

                g.DrawRectangle(new Vector2(MainGame.WindowWidth - flashSize.X, 0), flashSize, new Vector4(1f, 1f, 1f, beatFlash2.Value), FlashTexture);
            }
        }

        public override void Update(float delta)
        {
            beatFlash.Update(delta);
            beatFlash2.Update(delta);

            fadeColor.Update(delta);

            Vector2 mousePosition = Input.MousePosition;

            position = Vector2.Lerp(position, mousePosition, delta * ParallaxSmooth);

            ParallaxPosition = new Vector2(
                position.X.Map(0, MainGame.WindowWidth, ParallaxAmount, -ParallaxAmount),
                position.Y.Map(0, MainGame.WindowHeight, ParallaxAmount, -ParallaxAmount));
        }
    }

    public class MenuLogo : Drawable
    {
        public static readonly Texture LogoTexture = new Texture(Utils.GetResource("Skin.logo.png")) { GenerateMipmaps = false };
        public static readonly Texture LogoTextTexture = new Texture(Utils.GetResource("UI.Assets.LogoText.png")) { GenerateMipmaps = false };

        private SmoothVector2 positionTransform = new SmoothVector2();
        private SmoothFloat rotationTransform = new SmoothFloat();
        public SmoothVector2 sizeTransform = new SmoothVector2();

        private SmoothVector4 colorTransform = new SmoothVector4();

        private Vector2 position = new Vector2();
        private Vector2 size => new Vector2(750) * MainGame.Scale + new Vector2(300) * visualizer.BeatValue * MainGame.Scale;

        private Vector2 parallaxPosition => mapBackground.ParallaxPosition * 2f;
        private Vector2 offset = Vector2.Zero;

        public override Rectangle Bounds => new Rectangle((Vector2)positionTransform * MainGame.Scale + position + parallaxPosition - (Vector2)sizeTransform / 2f - size / 2f + offset, sizeTransform + size);

        private SoundVisualizer visualizer = new SoundVisualizer();

        private Button playButton = new Button();
        private Button multiPlayButton = new Button();
        private Button optionsButton = new Button();
        private Button exitButton = new Button();

        private SmoothVector2 buttonPosition = new SmoothVector2();
        private SmoothFloat buttonAlpha = new SmoothFloat();

        private MapBackground mapBackground;

        public SmoothFloat soundFade = new SmoothFloat();

        private bool hover => MathUtils.IsPointInsideRadius(Input.MousePosition, Bounds.Center, Bounds.Size.X / 2);
        private bool lastHover;

        private Vector4 visualizerColorAdditive = Vector4.Zero;

        private ScrollingTriangles triangles = new ScrollingTriangles(80);

        private SmoothFloat logoExplodeKiaiAnim = new SmoothFloat();

        private double logoShakeTime = 0;

        public MenuLogo(MapBackground mapBackground)
        {
            this.mapBackground = mapBackground;

            OsuContainer.OnKiai += () =>
            {
                logoExplodeKiaiAnim.Value = 1f;
                logoExplodeKiaiAnim.TransformTo(0f, 0.5f, EasingTypes.None);
            };

            buttonAlpha.Value = 0f;

            buttonPosition.Value = new Vector2(360, 270);

            positionTransform.Value = Vector2.Zero;
            rotationTransform.Value = 0;
            colorTransform.Value = Colors.White;

            visualizer.BarTexture = null;
            visualizer.Layer = -727;
            visualizer.MirrorCount = 2;
            visualizer.LerpSpeed = 40f;
            visualizer.Thickness = 25;
            visualizer.BarLength = 800f;
            visualizer.FreckleSpawnRate = float.MaxValue;
            visualizer.BarTexture = Skin.VisualizerBar;
            visualizer.StartRotation = -(MathF.PI / 4);
            visualizer.Style = SoundVisualizer.VisualizerStyle.Bars;

            //Rainbow color wow
            visualizer.ColorAt += (progress, volume) =>
            {
                var c = (Vector4)Color4.Crimson * volume * 25;
                c.W = 1f;
                return c;

                var currBeat = OsuContainer.IsKiaiTimeActive ? OsuContainer.CurrentBeat : 0;
                var beat = currBeat.Map(0, 1, 0, MathF.PI / 2);

                progress *= 3;

                float colorMultiplier = OsuContainer.IsKiaiTimeActive ? 2.2f : 1.5f;

                float r = (float)Math.Sin(beat + 0 + progress).Map(-1, 1, 0, 1);
                float g = (float)Math.Sin(beat + 2 + progress).Map(-1, 1, 0, 0.6);
                float b = (float)Math.Sin(beat + 4 + progress).Map(-1, 1, 0, 1);

                return Colors.Tint(new Vector4(r, g, b, 1f), 1.5f) + visualizerColorAdditive;
            };

            //buttonPosition.Value = new Vector2(-310, -270);

            playButton.Layer = -69;
            playButton.Size = new Vector2(720, 120);
            playButton.Text = "Play";
            playButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            playButton.TextColor = Colors.White;
            playButton.TextOffset = new Vector2(50, 0);
            playButton.OnClick += (s, e) =>
            {
                slideBack();
                ScreenManager.SetScreen<MapSelectScreen>();
            };

            multiPlayButton.Layer = -69;
            multiPlayButton.Size = new Vector2(720, 120);
            multiPlayButton.Text = "Multiplayer";
            multiPlayButton.TextOffset = new Vector2(50, 0);
            multiPlayButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            multiPlayButton.TextColor = Colors.White;
            multiPlayButton.OnClick += (s, e) =>
            {
                slideBack();
                ScreenManager.SetScreen<MultiplayerScreen>();
            };

            optionsButton.Layer = -69;
            optionsButton.Size = new Vector2(720, 120);
            optionsButton.TextOffset = new Vector2(50, 0);
            optionsButton.Text = "Options";
            optionsButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            optionsButton.TextColor = Colors.White;
            optionsButton.OnClick += (s, e) =>
            {
                slideBack();
                ScreenManager.SetScreen<OptionsScreen>();
            };

            exitButton.Layer = -69;
            exitButton.Size = new Vector2(720, 120);
            exitButton.Text = "Pause";
            exitButton.TextOffset = new Vector2(50, 0);
            exitButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            exitButton.TextColor = Colors.White;
            exitButton.OnClick += (s, e) =>
            {
                slideBack();
                //Game.Instance.Window.Close();

                //exitButton.Text = "ALT + F4 ty";

                if (OsuContainer.Beatmap is not null)
                {
                    if (OsuContainer.Beatmap.Song.IsPlaying)
                        OsuContainer.Beatmap.Song.Pause();
                    else
                        OsuContainer.Beatmap.Song.Play();
                }
            };

            triangles.Layer = -1;
            triangles.BaseColor = Colors.From255RGBA(37, 37, 37, 255);
        }

        public override void OnAdd()
        {
            Container.Add(visualizer);
            Container.Add(playButton);
            Container.Add(multiPlayButton);
            Container.Add(optionsButton);
            Container.Add(exitButton);

            Container.Add(triangles);
        }

        public override void Render(Graphics g)
        {
            colorTransform.Value = new Vector4(1);
            g.DrawRectangleCentered(visualizer.Position, Bounds.Size, colorTransform, LogoTexture, null, false, rotationTransform);

            if (!logoExplodeKiaiAnim.HasCompleted)
            {
                float logoExplodeScale = logoExplodeKiaiAnim.Value.Map(1f, 0f, 1f, 2f);
                g.DrawRectangleCentered(visualizer.Position, Bounds.Size * logoExplodeScale, new Vector4(colorTransform.Value.Xyz, LogoTextTexture.ImageDoneUploading ? logoExplodeKiaiAnim.Value : 0), LogoTextTexture);
            }
        }

        public void ToggleInput(bool input)
        {
            IsAcceptingInput = input;
            playButton.IsAcceptingInput = input;
            optionsButton.IsAcceptingInput = input;
            exitButton.IsAcceptingInput = input;
            multiPlayButton.IsAcceptingInput = input;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (hover && args == MouseButton.Left)
            {
                slideToSide();
                return true;
            }

            if (buttonAlpha.Value == 0)
                return true;

            return false;
        }

        public override bool OnMouseUp(MouseButton args)
        {
            return false;
        }

        private void slideToSide()
        {
            slideBackTimer = 0;
            positionTransform.ClearTransforms();
            positionTransform.TransformTo(-new Vector2(300, 0), 0.25f, EasingTypes.Out);

            buttonPosition.ClearTransforms();
            buttonPosition.TransformTo(new Vector2(30, 270), 0.25f, EasingTypes.Out);

            buttonAlpha.ClearTransforms();
            buttonAlpha.TransformTo(0.9f, 0.25f, EasingTypes.Out);
        }

        private void slideBack()
        {
            positionTransform.ClearTransforms();
            positionTransform.TransformTo(Vector2.Zero, 0.25f, EasingTypes.Out);

            buttonPosition.ClearTransforms();
            buttonPosition.TransformTo(new Vector2(360, 270), 0.25f, EasingTypes.Out);

            buttonAlpha.ClearTransforms();
            buttonAlpha.TransformTo(0f, 0.25f, EasingTypes.Out);
        }

        public override bool OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                slideBack();

            if (key == Key.Enter)
                slideToSide();

            return false;
        }

        private float slideBackTimer = 0;

        private void bopButtons(float delta)
        {
            if (playButton.Bounds.IntersectsWith(new Rectangle(Input.MousePosition, new Vector2(1))) && !hover)
                playButton.TextOffset = Vector2.Lerp(playButton.TextOffset, new Vector2(100, 0) * MainGame.Scale, delta * 10f);
            else
                playButton.TextOffset = Vector2.Lerp(playButton.TextOffset, new Vector2(50, 0) * MainGame.Scale, delta * 10f);

            if (multiPlayButton.Bounds.IntersectsWith(new Rectangle(Input.MousePosition, new Vector2(1))) && !hover)
                multiPlayButton.TextOffset = Vector2.Lerp(multiPlayButton.TextOffset, new Vector2(100, 0) * MainGame.Scale, delta * 10f);
            else
                multiPlayButton.TextOffset = Vector2.Lerp(multiPlayButton.TextOffset, new Vector2(50, 0) * MainGame.Scale, delta * 10f);

            if (optionsButton.Bounds.IntersectsWith(new Rectangle(Input.MousePosition, new Vector2(1))) && !hover)
                optionsButton.TextOffset = Vector2.Lerp(optionsButton.TextOffset, new Vector2(100, 0) * MainGame.Scale, delta * 10f);
            else
                optionsButton.TextOffset = Vector2.Lerp(optionsButton.TextOffset, new Vector2(50, 0) * MainGame.Scale, delta * 10f);

            if (exitButton.Bounds.IntersectsWith(new Rectangle(Input.MousePosition, new Vector2(1))) && !hover)
                exitButton.TextOffset = Vector2.Lerp(exitButton.TextOffset, new Vector2(100, 0) * MainGame.Scale, delta * 10f);
            else
                exitButton.TextOffset = Vector2.Lerp(exitButton.TextOffset, new Vector2(50, 0) * MainGame.Scale, delta * 10f);
        }

        public override void Update(float delta)
        {
            logoExplodeKiaiAnim.Update(delta);

            if (OsuContainer.IsKiaiTimeActive && PostProcessing.Bloom)
            {
                visualizerColorAdditive = Vector4.Lerp(visualizerColorAdditive, new Vector4(1f, 1f, 1f, 0f), delta * 10f);
            }
            else
            {
                visualizerColorAdditive = Vector4.Lerp(visualizerColorAdditive, Vector4.Zero, delta * 3f);
            }

            if (IsAcceptingInput)
            {
                if (hover && !lastHover)
                {
                    sizeTransform.TransformTo(new Vector2(70 * MainGame.Scale), 0.2f, EasingTypes.Out);
                    Skin.Hover.Play(true);
                }
                else if (!hover && lastHover)
                {
                    sizeTransform.TransformTo(new Vector2(0), 0.2f, EasingTypes.Out);
                }
                lastHover = hover;
            }

            //slideBackTimer += delta;
            if (slideBackTimer >= 7f)
                slideBack();

            buttonPosition.Update(delta);

            playButton.Position = MainGame.WindowCenter - (buttonPosition.Value * MainGame.Scale) + parallaxPosition;
            multiPlayButton.Position = MainGame.WindowCenter - (buttonPosition.Value * MainGame.Scale) + (new Vector2(0, 130) * MainGame.Scale) + parallaxPosition;
            optionsButton.Position = MainGame.WindowCenter - (buttonPosition.Value * MainGame.Scale) + (new Vector2(0, 260) * MainGame.Scale) + parallaxPosition;
            exitButton.Position = MainGame.WindowCenter - (buttonPosition.Value * MainGame.Scale) + (new Vector2(0, 390) * MainGame.Scale) + parallaxPosition;

            buttonAlpha.Update(delta);

            playButton.Color.W = buttonAlpha.Value;
            multiPlayButton.Color.W = buttonAlpha.Value;
            optionsButton.Color.W = buttonAlpha.Value;
            exitButton.Color.W = buttonAlpha.Value;

            playButton.TextColor.W = buttonAlpha.Value + 0.1f;
            multiPlayButton.TextColor.W = buttonAlpha.Value + 0.1f;
            optionsButton.TextColor.W = buttonAlpha.Value + 0.1f;
            exitButton.TextColor.W = buttonAlpha.Value + 0.1f;

            positionTransform.Update(delta);
            rotationTransform.Update(delta);
            sizeTransform.Update(delta);
            colorTransform.Update(delta);

            position = MainGame.WindowCenter;

            playButton.Size = new Vector2(720, 120) * MainGame.Scale;
            multiPlayButton.Size = new Vector2(720, 120) * MainGame.Scale;
            optionsButton.Size = new Vector2(720, 120) * MainGame.Scale;
            exitButton.Size = new Vector2(720, 120) * MainGame.Scale;

            bopButtons(delta);

            soundFade.Update(delta);
            if (OsuContainer.Beatmap?.Song is not null)
            {
                visualizer.Sound = OsuContainer.Beatmap.Song;

                if (soundFade.HasCompleted == false)
                    visualizer.Sound.Volume = soundFade.Value;

                if (OsuContainer.Beatmap.Song.IsStopped)
                    OsuContainer.Beatmap.Song.Play(true);
            }


            triangles.Radius = Bounds.Size.X / 2 - 20f * MainGame.Scale;
            triangles.Position = Bounds.Center;
            triangles.Speed = (50 + 1700 * visualizer.BeatValue) * (OsuContainer.IsKiaiTimeActive ? 1.75f : 1);

            visualizer.Position = Bounds.Center;
            visualizer.Radius = Bounds.Size.X / 2f - 20f * MainGame.Scale;
            visualizer.FreckleOffset = parallaxPosition;
            visualizer.BarLength = 800 * MainGame.Scale;
            visualizer.Thickness = 30f * MainGame.Scale;

            if (OsuContainer.IsKiaiTimeActive)
                visualizer.FreckleSpawnRate = 0.006f;
            else
                visualizer.FreckleSpawnRate = float.MaxValue;
            
            if (OsuContainer.IsKiaiTimeActive && PostProcessing.Bloom)
            {
                visualizer.BarHighlight = Vector3.Lerp(visualizer.BarHighlight, 
                    new Vector3((float)Math.Cos(OsuContainer.CurrentBeat).Map(-1, 1, 0, 2) + 1, 
                    (float)Math.Cos(OsuContainer.CurrentBeat + 2).Map(-1, 1, 0, 2) + 1,
                    (float)Math.Cos(OsuContainer.CurrentBeat + 4).Map(-1, 1, 0, 2) + 1), 
                    10f * delta);

                visualizer.BarStartColor = Vector4.Lerp(visualizer.BarStartColor, new Vector4(0.95f, 0.95f, 0.95f, 1f), delta * 10f);
                visualizer.BarEndColor = visualizer.BarStartColor;
            }
            else
            {
                visualizer.BarHighlight = Vector3.Lerp(visualizer.BarHighlight, new Vector3(0), 10f * delta);
                visualizer.BarStartColor = Vector4.Lerp(visualizer.BarStartColor, new Vector4(Skin.Config.MenuGlow, 0.5f), delta * 10f);
                visualizer.BarEndColor = visualizer.BarStartColor;
            }

            //BASS KIAI LOGO VIBRATION 2.0, buggy sometimes the logo glitches suddenly to a side for some reason???
            if (OsuContainer.IsKiaiTimeActive)
            {
                logoShakeTime += delta * visualizer.BeatValue * 30;
                /*
                float dirX = RNG.TryChance() ? -75 : 75;
                float dirY = RNG.TryChance() ? -75 : 75;

                //Pick a random direction from negative Value to positive Value
                Vector2 dist = new Vector2(dirX, dirY) * MainGame.Scale;

                //Set the offset to interpolate to this randomly chosen direction for this frame
                //Interpolate faster towards the destination by how big the beat currently is
                //fuck jeg er tørstig lol
                //use perlin noise
                var blend = (delta * 35f * visualizer.BeatValue).Clamp(0, 1);
                offset = Vector2.Lerp(offset, dist, blend);
                */

                Vector2 dist;

                float shakeAmount = 75 * visualizer.BeatValue * MainGame.Scale;

                dist.X = (float)Perlin.Instance.Noise(logoShakeTime, 0, 0) * shakeAmount;
                dist.Y = (float)Perlin.Instance.Noise(0, logoShakeTime, 0) * shakeAmount;
                dist.X -= shakeAmount / 2f;
                dist.Y -= shakeAmount / 2f;

                offset = dist;
            }
            else
                offset = Vector2.Lerp(offset, Vector2.Zero, delta * 10f);

            if (OsuContainer.IsKiaiTimeActive)
            {
                mapBackground.Zoom = 100f;

                //double cosHack = (OsuContainer.CurrentBeat / 16).Map(0f, 1f, -Math.PI, Math.PI);

                //mapBackground.Rotation = (float)Math.Cos(cosHack).Map(-1f, 1f, -4f, 4f);
            }
            else
            {
                mapBackground.Zoom = MathHelper.Lerp(mapBackground.Zoom, 0f, delta * 10f);
                mapBackground.Rotation = MathHelper.Lerp(mapBackground.Rotation, 0f, delta * 10f);
            }
        }
    }
}
