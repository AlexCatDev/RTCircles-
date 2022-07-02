using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;

namespace RTCircles
{
    public class MenuLogo : Drawable
    {
        public static readonly Texture LogoTexture = new Texture(Utils.GetResource("Skin.logo.png")) { GenerateMipmaps = false };
        public static readonly Texture LogoTextTexture = new Texture(Utils.GetResource("UI.Assets.LogoText.png")) { GenerateMipmaps = false };

        private SmoothVector2 positionTransform = new SmoothVector2();
        private SmoothFloat rotationTransform = new SmoothFloat();
        private SmoothVector2 sizeTransform = new SmoothVector2();

        private SmoothVector4 colorTransform = new SmoothVector4();

        private Vector2 position = new Vector2();
        private Vector2 size => new Vector2(750) * MainGame.Scale + new Vector2(300) * visualizer.BeatValue * MainGame.Scale;

        private Vector2 parallaxPosition => mapBackground.ParallaxPosition * 2f;
        private Vector2 offset = Vector2.Zero;

        public override Rectangle Bounds => new Rectangle((Vector2)positionTransform * MainGame.Scale + position + parallaxPosition - (Vector2)sizeTransform / 2f - size / 2f + offset - IntroSizeAnimation.Value / 2f * MainGame.Scale, sizeTransform + size + IntroSizeAnimation.Value * MainGame.Scale);

        private SoundVisualizer visualizer = new SoundVisualizer();

        public SmoothVector2 IntroSizeAnimation = new SmoothVector2();

        private Button playButton = new Button();
        private Button multiPlayButton = new Button();
        private Button optionsButton = new Button();
        private Button exitButton = new Button();

        private SmoothVector2 buttonPosition = new SmoothVector2();
        private SmoothFloat buttonAlpha = new SmoothFloat();

        private MapBackground mapBackground;

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
                logoExplodeKiaiAnim.TransformTo(0f, 0.5f, EasingTypes.Out);
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
            playButton.OnClick += () =>
            {
                slideBack();
                ScreenManager.SetScreen<SongSelectScreen>();

                return true;
            };

            multiPlayButton.Layer = -69;
            multiPlayButton.Size = new Vector2(720, 120);
            multiPlayButton.Text = "Multiplayer";
            multiPlayButton.TextOffset = new Vector2(50, 0);
            multiPlayButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            multiPlayButton.TextColor = Colors.White;
            multiPlayButton.OnClick += () =>
            {
                slideBack();
                ScreenManager.SetScreen<MultiplayerScreen>();

                return true;
            };

            optionsButton.Layer = -69;
            optionsButton.Size = new Vector2(720, 120);
            optionsButton.TextOffset = new Vector2(50, 0);
            optionsButton.Text = "Options";
            optionsButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            optionsButton.TextColor = Colors.White;
            optionsButton.OnClick += () =>
            {
                slideBack();
                ScreenManager.SetScreen<OptionsScreen>();

                return true;
            };

            exitButton.Layer = -69;
            exitButton.Size = new Vector2(720, 120);
            exitButton.Text = "Pause";
            exitButton.TextOffset = new Vector2(50, 0);
            exitButton.Color = Colors.From255RGBA(37, 37, 37, 37);
            exitButton.TextColor = Colors.White;
            exitButton.OnClick += () =>
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

                return true;
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
            /*
            IntroSizeAnimation.Time = OsuContainer.Beatmap == null ? 
                MainGame.Instance.TotalTime * 1000 : OsuContainer.SongPosition;
            */

            IntroSizeAnimation.Update((float)OsuContainer.DeltaSongPosition);

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

            if (OsuContainer.Beatmap?.Song is not null)
            {
                visualizer.Sound = OsuContainer.Beatmap.Song;

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
            visualizer.Thickness = 28f * MainGame.Scale;
            visualizer.BarStartColor = new Vector4(Skin.Config.MenuGlow, 0.5f);
            visualizer.BarEndColor = visualizer.BarStartColor;

            if (OsuContainer.IsKiaiTimeActive)
                visualizer.FreckleSpawnRate = 0.01f;
            else
                visualizer.FreckleSpawnRate = float.MaxValue;
            
            if (OsuContainer.IsKiaiTimeActive && PostProcessing.Bloom)
            {
                visualizer.BarHighlight = new Vector3(5f);
            }
            else
            {
                visualizer.BarHighlight = Vector3.Lerp(visualizer.BarHighlight, new Vector3(0), 10f * delta);
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

            if (GlobalOptions.UseGameplayAsBackgroundSrc.Value)
                mapBackground.Zoom = 600 * MainGame.Scale;
        }
    }
}
