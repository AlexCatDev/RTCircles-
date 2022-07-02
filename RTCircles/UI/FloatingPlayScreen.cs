using Easy2D;
using OpenTK.Mathematics;
using System;

namespace RTCircles
{
    public class FloatingPlayScreen : Drawable
    {
        private FloatingScreen floatingScreen = new FloatingScreen();

        public override Rectangle Bounds => floatingScreen.Bounds;

        private Button difficultAdjustPanelBtn = new Button() { Texture = Skin.Arrow, IsVisible = false };
        private SmoothFloat adjustPanelPopProgress = new SmoothFloat();

        public Vector2 Position
        {
            get => floatingScreen.Position;
            set => floatingScreen.Position = value;
        }

        public Vector2 Size
        {
            get => floatingScreen.Size;
            set => floatingScreen.Size = value;
        }

        public FloatingPlayScreen()
        {
            floatingScreen.SetTarget<OsuScreen>();
        }

        private DifficultyAdjuster difficultyAdjuster = new DifficultyAdjuster() { Layer = 6969, IsVisible = false };

        public override void OnAdd()
        {
            Container.Add(difficultAdjustPanelBtn);
            Container.Add(difficultyAdjuster);

            difficultyAdjuster.ToggleInput(false);

            difficultAdjustPanelBtn.OnClick += () =>
            {
                if (adjustPanelPopProgress.Value == 0f)
                {
                    difficultyAdjuster.ToggleVisibility(true);
                    difficultyAdjuster.ToggleInput(true);
                    adjustPanelPopProgress.TransformTo(1f, 0.25f, EasingTypes.OutQuint);
                }
                else if (adjustPanelPopProgress.Value == 1f)
                {
                    adjustPanelPopProgress.TransformTo(0f, 0.25f, EasingTypes.InBack, () => { difficultyAdjuster.ToggleVisibility(false); });
                    difficultyAdjuster.ToggleInput(false);
                }

                return true;
            };
        }

        public override void Render(Graphics g)
        {
            floatingScreen.Render(g);
            drawFrame(g);

            //difficultyAdjuster.Render(g);
            difficultAdjustPanelBtn.Render(g);
        }

        private void drawFrame(Graphics g)
        {
            /*
            var color = Colors.From255RGBA(61, 61, 61 ,255);
            float frameThickness = 4f * MainGame.Scale;

            g.DrawOneSidedLine(Bounds.TopLeft, Bounds.BottomLeft, color, color, frameThickness);
            g.DrawOneSidedLine(Bounds.BottomLeft, Bounds.BottomRight, color, color, frameThickness);
            g.DrawOneSidedLine(Bounds.BottomRight, Bounds.TopRight, color, color, frameThickness);
            g.DrawOneSidedLine(Bounds.TopRight, Bounds.TopLeft, color, color, frameThickness);
            
            //Why am i doing this just for a fucking drop shadow
            
            float shadowThickness = frameThickness * 3.5f;
            Vector4 shadowColor = new Vector4(0, 0, 0, 1f);
            Rectangle rect = new Rectangle(0, 0, 1, 1);
            int segments = 4;

            g.DrawOneSidedLine(fs.Bounds.TopLeft, Bounds.BottomLeft, shadowColor, shadowColor, -shadowThickness, shadowTexture, rect);
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
            */
            if (OsuContainer.Beatmap != null)
            {
                g.DrawString(
                    "length\n" +
                    "bpm\n" +
                    "cs\n" +
                    "ar\n" +
                    "od\n" +
                    "hp\n",
                    Font.DefaultFont, Position + new Vector2(Size.X * 0.018f, Size.X * 0.018f), Colors.White, 0.3f * MainGame.Scale, 15);

                var timeSpan = new TimeSpan(0);

                if(OsuContainer.Beatmap.HitObjects.Count > 1)
                    timeSpan = TimeSpan.FromMilliseconds(OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime - OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime);

                var bpm = 60000 / OsuContainer.CurrentBeatTimingPoint?.BeatLength;

                string totalLength = (Math.Floor(timeSpan.TotalMinutes) + ":" + timeSpan.ToString("ss"));

                g.DrawString(
                    $"{totalLength}\n" +
                    $"{bpm:F0}\n" +
                    $"{OsuContainer.Beatmap.CS}\n" +
                    $"{OsuContainer.Beatmap.AR}\n" +
                    $"{OsuContainer.Beatmap.OD}\n" +
                    $"{OsuContainer.Beatmap.HP}\n",
                    Font.DefaultFont, Position + new Vector2(Size.X * 0.18f, Size.X * 0.018f), Colors.White, 0.3f * MainGame.Scale, 15);
            }
        }

        public override void Update(float delta)
        {
            adjustPanelPopProgress.Update(delta);

            difficultAdjustPanelBtn.Size = new Vector2(64) * MainGame.Scale;
            difficultAdjustPanelBtn.Position = Position + new Vector2(Size.X - difficultAdjustPanelBtn.Size.X, 
                Size.Y - difficultAdjustPanelBtn.Size.Y);
            difficultAdjustPanelBtn.TextureRotation = adjustPanelPopProgress.Value.Map(0, 1, 90, 0);

            difficultyAdjuster.Size = new Vector2(Size.X, Size.Y / 2f);
            difficultyAdjuster.Position = new Vector2(Position.X, Position.Y + Size.Y * adjustPanelPopProgress.Value);
        }
    }
}

