﻿using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTCircles
{
    public class MenuScreen : Screen
    {
        private SmoothVector4 introFade = new SmoothVector4();

        public MenuScreen()
        {
            //Fade loader in
            introFade.TransformTo(new Vector4(0f, 0f, 0f, 1f), 0.3f, EasingTypes.Out, () =>
            {
                BeatmapMirror.Scheduler.Run(new(() =>
                {
                   ScreenManager.GetScreen<MapSelectScreen>().LoadCarouselItems();

                   var carouselItems = BeatmapCarousel.Items;
                   if (BeatmapCarousel.Items.Count > 0)
                   {
                       var item = carouselItems[RNG.Next(0, carouselItems.Count - 1)];

                       OsuContainer.SetMap(BeatmapMirror.DecodeBeatmap(System.IO.File.OpenRead(item.FullPath)), true, Mods.Auto);
                       OsuContainer.Beatmap.Song.Volume = 0;
                       OsuContainer.SongPosition = (OsuContainer.Beatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai))?.Offset - 2500 ?? 0;
                       OsuContainer.Beatmap.Song.Play(false);
                   }

                    //When everything has been loaded, add the ui items
                    MapBackground mapBackground = new MapBackground();

                    Add(mapBackground);
                    Add(new MenuLogo(mapBackground));

                    //and fade the loading animation out
                    introFade.TransformTo(new Vector4(0f, 0f, 0f, 0f), 0.3f, EasingTypes.Out, () =>
                    {
                        //Fade background in
                        mapBackground.TriggerFadeIn();
                    });
                }));
                //OsuContainer.SetMap(BeatmapMirror.DecodeBeatmap(Utils.GetResource("Maps.SliderBenchmark.map.osu")), true, Mods.Auto);
            });
        }

        public override void Update(float delta)
        {
            introFade.Update(delta);

            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            base.Render(g);

            float alpha = introFade.Value.W;

            float explode = (1f - alpha) * (float)OsuContainer.CircleExplode;

            if(alpha > 0f)
                g.DrawRectangleCentered(MainGame.WindowCenter, new Vector2(800) + new Vector2(800) * explode, new Vector4(1f, 1f, 1f, alpha), MenuLogo.LogoTexture);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }

    public class MapBackground : Drawable
    {
        public override Rectangle Bounds => new Rectangle(0, 0, 1920, 1080);

        public SmoothVector4 fadeColor = new SmoothVector4();

        public float Opacity = 0.5f;
        public float KiaiFlash = 2.0f;

        public static Texture menuFlash;

        static MapBackground()
        {
            menuFlash = new Texture(Utils.GetResource("Skin.menu-flash.png"));
        }

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

        public override void Render(Graphics g)
        {
            if (OsuContainer.Beatmap is null)
                return;

            float aspectRatio = (float)OsuContainer.Beatmap.Background.Width / OsuContainer.Beatmap.Background.Height;

            float beatScale = (float)Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0, 1, 0, 1, EasingTypes.InOutSine);

            float width = MainGame.WindowWidth + (BEAT_SIZE * beatScale) + Zoom;
            float height = MainGame.WindowHeight + (BEAT_SIZE * beatScale) + Zoom;

            Vector2 bgSize;
            bgSize = new Vector2(width, width / aspectRatio) + new Vector2(ParallaxAmount * 2);

            if(bgSize.Y < MainGame.WindowHeight)
                bgSize = new Vector2(height * aspectRatio, height) + new Vector2(ParallaxAmount * 2);

            g.DrawRectangleCentered(MainGame.WindowCenter + ParallaxPosition, bgSize, fadeColor, OsuContainer.Beatmap.Background, null, false, Rotation);

            if (OsuContainer.IsKiaiTimeActive)
            {
                int beat = (int)Math.Floor(OsuContainer.CurrentBeat);

                if(previousBeat > beat)
                    previousBeat = beat;

                if (beat - previousBeat > 0)
                {
                    float waitTime = 0.1f;
                    float fadeInTime = 0.12f;
                    float fadeOutTime = 0.3f;

                    if (beat % 2 == 0)
                    {
                        beatFlash2.ClearTransforms();
                        beatFlash2.TransformTo(0.35f, fadeInTime, EasingTypes.Out);
                        beatFlash2.Wait(waitTime);
                        beatFlash2.TransformTo(0f, fadeOutTime, EasingTypes.Out);
                    }
                    else
                    {
                        beatFlash.ClearTransforms();
                        beatFlash.TransformTo(0.35f, fadeInTime, EasingTypes.Out);
                        beatFlash.Wait(waitTime);
                        beatFlash.TransformTo(0f, fadeOutTime, EasingTypes.Out);
                    }

                    previousBeat = beat;
                }
            }

            if (beatFlash.HasCompleted == false || beatFlash2.HasCompleted == false)
            {
                Vector2 flashSize = new Vector2(MainGame.WindowWidth / 6, MainGame.WindowHeight);

                g.DrawRectangle(Vector2.Zero, flashSize, new Vector4(1f, 1f, 1f, beatFlash.Value), menuFlash, new Rectangle(1, 0, -1, 1), true);

                g.DrawRectangle(new Vector2(MainGame.WindowWidth - flashSize.X, 0), flashSize, new Vector4(1f, 1f, 1f, beatFlash2.Value), menuFlash);
            }
            //if (Input.IsKeyDown(Key.ControlLeft))
            //    g.DrawString(OsuContainer.Beatmap.FileInfo.FullName, Font.DefaultFont, new Vector2(20), Colors.LightGray, 0.25f);
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
        public static Texture LogoTexture { get; private set; }

        static MenuLogo()
        {
            LogoTexture = new Texture(Utils.GetResource("Skin.logo.png"));
        }

        private SmoothVector2 positionTransform = new SmoothVector2();
        private SmoothFloat rotationTransform = new SmoothFloat();
        private SmoothVector2 sizeTransform = new SmoothVector2();

        private SmoothVector4 colorTransform = new SmoothVector4();

        private Vector2 position = new Vector2();
        private Vector2 size => new Vector2(800) * MainGame.Scale + new Vector2(300) * visualizer.BeatValue * MainGame.Scale;
        private Vector2 logoSize;

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

        private SmoothFloat soundFade = new SmoothFloat();

        private bool hover => MathUtils.PositionInsideRadius(Input.MousePosition, Bounds.Center, Bounds.Size.X);
        private bool lastHover;

        private bool sizeFadedIn;

        private Vector4 visualizerColorAdditive = Vector4.Zero;

        private ScrollingTriangles triangles = new ScrollingTriangles(60);

        public MenuLogo(MapBackground mapBackground)
        {
            this.mapBackground = mapBackground;

            sizeTransform.Value = -size;
            sizeTransform.TransformTo(new Vector2(0), 0.25f, EasingTypes.Out, () => { sizeFadedIn = true; });

            soundFade.Value = 0f;
            soundFade.TransformTo(1f, 1f);

            buttonAlpha.Value = 0f;

            buttonPosition.Value = new Vector2(360, 270);

            positionTransform.Value = Vector2.Zero;
            rotationTransform.Value = 0;
            colorTransform.Value = Colors.White;

            visualizer.BarTexture = null;
            visualizer.Layer = -727;
            visualizer.MirrorCount = 2;
            visualizer.LerpSpeed = 40f;
            visualizer.Thickness = 20f;
            visualizer.BarLength = 800f;
            visualizer.FreckleSpawnRate = float.MaxValue;
            visualizer.BarTexture = Skin.VisualizerBar;
            visualizer.StartRotation = -(MathF.PI / 4);
            visualizer.Style = SoundVisualizer.VisualizerStyle.Smooth;


            //Rainbow color wow
            visualizer.ColorAt += (pos) =>
            {
                Vector4 col = Vector4.Zero;
                col.X = pos.X.Map(-visualizer.Radius, visualizer.Radius, 0f, 1f);
                col.Z = pos.Y.Map(visualizer.Radius, -visualizer.Radius, 0f, 1f);

                col.Y = 1f - col.X;

                col = Colors.Tint(col, 1.2f);

                col += visualizerColorAdditive * 0.25f;

                col.W = 1.0f;

                return col;
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
                //ScreenManager.SetScreen<MultiConnectScreen>();
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
            exitButton.Text = "Exit";
            exitButton.TextOffset = new Vector2(50, 0);
            exitButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            exitButton.TextColor = Colors.White;
            exitButton.OnClick += (s, e) =>
            {
                slideBack();
                //Game.Instance.Window.Close();

                exitButton.Text = "ALT + F4 ty";

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
            Vector4 beatFlash = new Vector4(0.7f, 0.7f, 0.7f, 0f) * Interpolation.ValueAt(OsuContainer.BeatProgressKiai, 0f, 1f, 0f, 1f, EasingTypes.Out);
            g.DrawRectangleCentered(visualizer.Position, logoSize, colorTransform + beatFlash, LogoTexture, null, false, rotationTransform);
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

        /*
        public override bool OnMouseMove(float x, float y)
        {
            slideBackTimer = 0;

            return hover;
        }
        */

        private void slideToSide()
        {
            slideBackTimer = 0;
            positionTransform.ClearTransforms();
            positionTransform.TransformTo(-new Vector2(300, 0), 0.25f, EasingTypes.Out);

            buttonPosition.ClearTransforms();
            buttonPosition.TransformTo(new Vector2(30, 270), 0.25f, EasingTypes.Out);

            buttonAlpha.ClearTransforms();
            buttonAlpha.TransformTo(1f, 0.25f, EasingTypes.Out);
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
            if (OsuContainer.IsKiaiTimeActive && PostProcessing.Bloom)
            {
                visualizerColorAdditive = Vector4.Lerp(visualizerColorAdditive, new Vector4(1f), delta * 10f);
            }
            else
            {
                visualizerColorAdditive = Vector4.Lerp(visualizerColorAdditive, Vector4.Zero, delta * 3f);
            }

                if (hover && !lastHover)
            {
                Skin.Hover.Play(true);
                if (sizeFadedIn)
                    sizeTransform.ClearTransforms();
                sizeTransform.TransformTo(new Vector2(100) * MainGame.Scale, 0.2f, EasingTypes.OutElasticHalf);
            }
            else if (!hover && lastHover)
            {
                if (sizeFadedIn)
                    sizeTransform.ClearTransforms();
                sizeTransform.TransformTo(new Vector2(0), 0.2f, EasingTypes.Out);
            }
            lastHover = hover;

            slideBackTimer += delta;
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

            playButton.TextColor.W = buttonAlpha.Value;
            multiPlayButton.TextColor.W = buttonAlpha.Value;
            optionsButton.TextColor.W = buttonAlpha.Value;
            exitButton.TextColor.W = buttonAlpha.Value;

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
            triangles.Speed = 50 + 600 * visualizer.BeatValue;

            visualizer.Position = Bounds.Center;
            visualizer.Radius = Bounds.Size.X / 2f - 20f * MainGame.Scale;
            visualizer.FreckleOffset = parallaxPosition;
            visualizer.BarLength = 800 * MainGame.Scale;

            logoSize = Bounds.Size;

            if (OsuContainer.IsKiaiTimeActive)
                visualizer.FreckleSpawnRate = 0.006f;
            else
                visualizer.FreckleSpawnRate = float.MaxValue;

            if (OsuContainer.IsKiaiTimeActive && PostProcessing.Bloom)
            {
                visualizer.StartColor = Vector4.Lerp(visualizer.StartColor, new Vector4(Skin.Config.MenuGlow + new Vector3(1f, 1f, 1f), 0.7f), delta * 10f);
                visualizer.EndColor = visualizer.StartColor;
            }
            else
            {
                visualizer.StartColor = Vector4.Lerp(visualizer.StartColor, new Vector4(Skin.Config.MenuGlow * 2f, 0.7f), delta * 10f);
                visualizer.EndColor = Vector4.Lerp(visualizer.StartColor, new Vector4(Skin.Config.MenuGlow * 2f, 0.7f), delta * 10f);
            }

            //BASS KIAI LOGO VIBRATION 2.0, buggy sometimes the logo glitches suddenly to a side for some reason???
            if (OsuContainer.IsKiaiTimeActive)
            {
                float dirX = RNG.TryChance() ? -40 : 40;
                float dirY = RNG.TryChance() ? -40 : 40;
                //Pick a random direction from negative Value to positive Value
                Vector2 dist = new Vector2(dirX, dirY) * MainGame.Scale;

                //Set the offset to interpolate to this randomly chosen direction for this frame
                //Interpolate faster towards the destination by how big the beat currently is
                //fuck jeg er tørstig lol
                //cock
                //use perlin noise
                var blend = (delta * 30f * visualizer.BeatValue).Clamp(0, 1);
                offset = Vector2.Lerp(offset, dist, blend);
            }
            else
                offset = Vector2.Lerp(offset, Vector2.Zero, delta * 10f);

            if (OsuContainer.IsKiaiTimeActive)
            {
                mapBackground.Zoom = 100f;

                double cosHack = (OsuContainer.CurrentBeat / 8).Map(0f, 1f, -Math.PI, Math.PI);

                //mapBackground.Rotation = (float)Math.Cos(cosHack).Map(-1f, 1f, -2f, 2f);
            }
            else
            {
                mapBackground.Zoom = MathHelper.Lerp(mapBackground.Zoom, 0f, delta * 10f);
                mapBackground.Rotation = MathHelper.Lerp(mapBackground.Rotation, 0f, delta * 10f);
            }
        }
    }
}