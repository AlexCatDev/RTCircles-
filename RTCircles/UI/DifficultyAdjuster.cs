using Easy2D;
using OpenTK.Mathematics;

namespace RTCircles
{
    public class DifficultyAdjuster : Drawable
    {
        public Vector2 Position;
        public Vector2 Size;

        public override Rectangle Bounds => new Rectangle(Position, Size);

        private GoodSliderBar csSliderBar = new() { IsVisible = false, MaximumValue = 10, MinimumValue = 0, StepDecimals = 1 };
        private GoodSliderBar arSliderBar = new() { IsVisible = false, MaximumValue = 10, MinimumValue = 0, StepDecimals = 1 };
        private GoodSliderBar odSliderBar = new() { IsVisible = false, MaximumValue = 10, MinimumValue = 0, StepDecimals = 1 };
        private GoodSliderBar hpSliderBar = new() { IsVisible = false, MaximumValue = 10, MinimumValue = 0, StepDecimals = 1 };
        private GoodSliderBar speedSliderBar = new() { IsVisible = false, MaximumValue = 3, MinimumValue = 0.1, StepDecimals = 1 };

        private Button csSliderLockBtn = new Button() { Texture = unlockTexture, Color = new Vector4(0.5f), AnimationColor = new Vector4(1f) };
        private Button arSliderLockBtn = new Button() { Texture = unlockTexture, Color = new Vector4(0.5f), AnimationColor = new Vector4(1f) };
        private Button odSliderLockBtn = new Button() { Texture = unlockTexture, Color = new Vector4(0.5f), AnimationColor = new Vector4(1f) };
        private Button hpSliderLockBtn = new Button() { Texture = unlockTexture, Color = new Vector4(0.5f), AnimationColor = new Vector4(1f) };

        private bool csSliderLocked, arSliderLocked, odSliderLocked, hpSliderLocked;

        private static Texture lockTexture = new Texture(Utils.GetResource("UI.Assets.LockClosed.png"));
        private static Texture unlockTexture = new Texture(Utils.GetResource("UI.Assets.LockOpen.png"));

        public void ToggleInput(bool canAcceptInput)
        {
            csSliderBar.IsAcceptingInput = canAcceptInput;
            odSliderBar.IsAcceptingInput = canAcceptInput;
            arSliderBar.IsAcceptingInput = canAcceptInput;
            hpSliderBar.IsAcceptingInput = canAcceptInput;
            speedSliderBar.IsAcceptingInput = canAcceptInput;

            csSliderLockBtn.IsAcceptingInput = canAcceptInput;
            arSliderLockBtn.IsAcceptingInput = canAcceptInput;
            odSliderLockBtn.IsAcceptingInput = canAcceptInput;
            hpSliderLockBtn.IsAcceptingInput = canAcceptInput;
        }

        public void ToggleVisibility(bool isVisible)
        {
            IsVisible = isVisible;
            arSliderLockBtn.IsVisible = isVisible;
            hpSliderLockBtn.IsVisible = isVisible;
            odSliderLockBtn.IsVisible = isVisible;
            csSliderLockBtn.IsVisible = isVisible;
        }

        public DifficultyAdjuster()
        {
            OsuContainer.BeatmapChanged += () =>
            {
                if (csSliderLocked)
                    OsuContainer.Beatmap.SetCS((float)csSliderBar.Value);

                if (arSliderLocked)
                    OsuContainer.Beatmap.SetAR((float)arSliderBar.Value);

                if (odSliderLocked)
                    OsuContainer.Beatmap.SetOD((float)odSliderBar.Value);

                if (hpSliderLocked)
                    OsuContainer.Beatmap.SetHP((float)hpSliderBar.Value);
            };
        }

        public override void OnAdd()
        {
            csSliderBar.ValueChanged += (csValue) =>
            { 
                OsuContainer.Beatmap?.SetCS((float)csValue);
            };

            arSliderBar.ValueChanged += (arValue) =>
            {
                OsuContainer.Beatmap?.SetAR((float)arValue);
            };

            odSliderBar.ValueChanged += (odValue) =>
            {
                OsuContainer.Beatmap?.SetOD((float)odValue);
            };

            hpSliderBar.ValueChanged += (hpValue) =>
            {
                OsuContainer.Beatmap?.SetHP((float)hpValue);
            };

            speedSliderBar.ValueChanged += (speedValue) =>
            {
                if(OsuContainer.Beatmap != null)
                    OsuContainer.Beatmap.Song.PlaybackSpeed = speedValue;
            };

            Container.Add(speedSliderBar);
            Container.Add(csSliderBar);
            Container.Add(arSliderBar);
            Container.Add(odSliderBar);
            Container.Add(hpSliderBar);

            Container.Add(csSliderLockBtn);
            csSliderLockBtn.OnClick += () =>
            {
                csSliderLocked = !csSliderLocked;

                csSliderBar.IsLocked = csSliderLocked;

                csSliderLockBtn.Texture = csSliderLocked ? lockTexture : unlockTexture;
                csSliderLockBtn.Color = csSliderLocked ? Vector4.One : new Vector4(0.5f);

                return true;
            };

            Container.Add(arSliderLockBtn);
            arSliderLockBtn.OnClick += () =>
            {
                arSliderLocked = !arSliderLocked;

                arSliderBar.IsLocked = arSliderLocked;

                arSliderLockBtn.Texture = arSliderLocked ? lockTexture : unlockTexture;
                arSliderLockBtn.Color = arSliderLocked ? Vector4.One : new Vector4(0.5f);

                return true;
            };

            Container.Add(odSliderLockBtn);
            odSliderLockBtn.OnClick += () =>
            {
                odSliderLocked = !odSliderLocked;

                odSliderBar.IsLocked = odSliderLocked;

                odSliderLockBtn.Texture = odSliderLocked ? lockTexture : unlockTexture;
                odSliderLockBtn.Color = odSliderLocked ? Vector4.One : new Vector4(0.5f);

                return true;
            };

            Container.Add(hpSliderLockBtn);
            hpSliderLockBtn.OnClick += () =>
            {
                hpSliderLocked = !hpSliderLocked;

                hpSliderBar.IsLocked = hpSliderLocked;

                hpSliderLockBtn.Texture = hpSliderLocked ? lockTexture : unlockTexture;
                hpSliderLockBtn.Color = hpSliderLocked ? Vector4.One : new Vector4(0.5f);

                return true;
            };
        }

        public override void Render(Graphics g)
        {
            float padding = 5f;

            Vector2 lockBtnSize = new Vector2(Size.X / 13 - padding, Size.X / 13 - padding);

            Vector2 sliderSize = new Vector2(Size.X - lockBtnSize.X - padding, Size.Y / 4);
            float textScale = sliderSize.Y / Font.DefaultFont.Size;
            Vector2 sliderPos = Position;
            sliderPos.Y += padding;

            csSliderBar.Position = sliderPos;
            csSliderBar.Size = sliderSize;
            csSliderBar.BackgroundColor = Colors.From255RGBA(31, 31, 31, 127);
            csSliderBar.ForegroundColor = Colors.From255RGBA(255, 10, 127, 255);

            csSliderLockBtn.Size = lockBtnSize;
            csSliderLockBtn.Position = sliderPos + new Vector2(sliderSize.X + padding, 0);

            sliderPos.Y += sliderSize.Y + padding;

            arSliderBar.Position = sliderPos;
            arSliderBar.Size = sliderSize;
            arSliderBar.BackgroundColor = Colors.From255RGBA(31, 31, 31, 127);
            arSliderBar.ForegroundColor = Colors.From255RGBA(127, 0, 255, 255);

            arSliderLockBtn.Size = lockBtnSize;
            arSliderLockBtn.Position = sliderPos + new Vector2(sliderSize.X + padding, 0);

            sliderPos.Y += sliderSize.Y + padding;

            odSliderBar.Position = sliderPos;
            odSliderBar.Size = sliderSize;
            odSliderBar.BackgroundColor = Colors.From255RGBA(31, 31, 31, 127);
            odSliderBar.ForegroundColor = Colors.From255RGBA(0, 127, 255, 255);

            odSliderLockBtn.Size = lockBtnSize;
            odSliderLockBtn.Position = sliderPos + new Vector2(sliderSize.X + padding, 0);

            sliderPos.Y += sliderSize.Y + padding;

            hpSliderBar.Position = sliderPos;
            hpSliderBar.Size = sliderSize;
            hpSliderBar.BackgroundColor = Colors.From255RGBA(31, 31, 31, 127);
            hpSliderBar.ForegroundColor = Colors.From255RGBA(0, 255, 127, 255);

            hpSliderLockBtn.Size = lockBtnSize;
            hpSliderLockBtn.Position = sliderPos + new Vector2(sliderSize.X + padding, 0);

            sliderPos.Y += sliderSize.Y + padding;

            speedSliderBar.Position = sliderPos;
            speedSliderBar.Size = sliderSize;
            speedSliderBar.BackgroundColor = Colors.From255RGBA(31, 31, 31, 127);
            speedSliderBar.ForegroundColor = Colors.From255RGBA(0, 255, 127, 255);

            if (OsuContainer.Beatmap != null)
            {
                csSliderBar.Value = OsuContainer.Beatmap.CS;
                csSliderBar.Render(g);

                arSliderBar.Value = OsuContainer.Beatmap.AR;
                arSliderBar.Render(g);

                odSliderBar.Value = OsuContainer.Beatmap.OD;
                odSliderBar.Render(g);

                hpSliderBar.Value = OsuContainer.Beatmap.HP;
                hpSliderBar.Render(g);

                speedSliderBar.Value = OsuContainer.Beatmap.Song.PlaybackSpeed;
                speedSliderBar.Render(g);

                g.DrawStringCentered($"CS: {OsuContainer.Beatmap.CS}", Font.DefaultFont, csSliderBar.Position + csSliderBar.Size / 2, Colors.White, textScale);
                g.DrawStringCentered($"AR: {OsuContainer.Beatmap.AR}", Font.DefaultFont, arSliderBar.Position + arSliderBar.Size / 2, Colors.White, textScale);
                g.DrawStringCentered($"OD: {OsuContainer.Beatmap.OD}", Font.DefaultFont, odSliderBar.Position + odSliderBar.Size / 2, Colors.White, textScale);
                g.DrawStringCentered($"HP: {OsuContainer.Beatmap.HP}", Font.DefaultFont, hpSliderBar.Position + hpSliderBar.Size / 2, Colors.White, textScale);

                var playbackSpeed = OsuContainer.Beatmap.Song.PlaybackSpeed;
                var currentBPM = (60000 / OsuContainer.CurrentBeatTimingPoint?.BeatLength) * playbackSpeed;

                g.DrawStringCentered($"Song Speed: {playbackSpeed:F2}x ({currentBPM:F0} BPM)", Font.DefaultFont, speedSliderBar.Position + speedSliderBar.Size / 2, Colors.White, textScale);
            }
        }

        public override void Update(float delta)
        {
            
        }
    }
}

