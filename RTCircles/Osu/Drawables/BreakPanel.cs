using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class BreakPanel : Drawable
    {
        private AnimFloat scrollProgress = new AnimFloat();
        private AnimFloat sizeProgress = new AnimFloat();

        private double breakEnd;

        public BreakPanel()
        {
            OsuContainer.BeatmapChanged += () =>
            {
                Reset();
            };
        }

        public void Show(IDrawableHitObject current, IDrawableHitObject next)
        {
            scrollProgress.Value = 0f;
            scrollProgress.Wait(current.BaseObject.EndTime + 240f, () =>
            {
                scrollProgress.TransformTo(1f, OsuContainer.SongPosition, OsuContainer.SongPosition + 500f, EasingTypes.OutQuart);
            });

            sizeProgress.Value = 0f;
            sizeProgress.Wait(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500, () =>
            {
                sizeProgress.TransformTo(1f, OsuContainer.SongPosition, OsuContainer.SongPosition + 250f, EasingTypes.In);
            });

            breakEnd = next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 600f;
        }

        public void Reset()
        {
            scrollProgress.Value = 0f;
            sizeProgress.Value = 0f;
        }

        public override void Render(Graphics g)
        {
            if (scrollProgress.Value == 0 || ScreenManager.ActiveScreen is MenuScreen)
                return;

            Vector2 size = new Vector2(MainGame.WindowWidth, sizeProgress.Value.Map(0f, 1f, MainGame.WindowHeight / 3f, 0));
            Vector2 position = new Vector2(scrollProgress.Value.Map(0, 1, -MainGame.WindowWidth, 0), MainGame.WindowCenter.Y - size.Y / 2f);

            g.DrawRectangle(position, size, Colors.From255RGBA(37, 37, 37, 127));

            int remainingBreakTime = (int)Math.Max((breakEnd - OsuContainer.SongPosition) / 1000, 0);

            float textScale = size.Y / Font.DefaultFont.Size * 0.6f;

            Rectangle rect = new Rectangle(position, size);

            g.DrawStringCentered(remainingBreakTime.ToString(), Font.DefaultFont, rect.Center, new Vector4(1f, 1f, 1f, scrollProgress), textScale);
        }

        public override void Update(float delta)
        {
            scrollProgress.Time = OsuContainer.SongPosition;
            sizeProgress.Time = OsuContainer.SongPosition;
        }
    }
}
