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

                scrollProgress.Wait(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500, () =>
                {
                    scrollProgress.TransformTo(2f, OsuContainer.SongPosition, OsuContainer.SongPosition + 500f, EasingTypes.InQuart, () =>
                    {
                        scrollProgress.Value = 0;
                    });
                });
            });

            breakEnd = next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 600f;
        }

        public void Reset()
        {
            scrollProgress.Value = 0f;
        }

        public override void Render(Graphics g)
        {
            if (scrollProgress.Value == 0 || ScreenManager.ActiveScreen is MenuScreen)
                return;

            Vector2 size = new Vector2(MainGame.WindowWidth / 2f, MainGame.WindowHeight / 3.5f);
            Vector2 position = new Vector2(scrollProgress.Value.Map(0, 1, -size.X, MainGame.WindowWidth/2 - size.X/2), MainGame.WindowCenter.Y - size.Y / 2f);

            float alpha = MathUtils.OscillateValue(scrollProgress.Value, 0, 1);

            g.DrawRoundedRect(position + size / 2, size, Colors.From255RGBA(37, 37, 37, alpha * 127), size.Y / 2f);
            //g.DrawRectangle(position, size, Colors.From255RGBA(37, 37, 37, 127));

            int remainingBreakTime = (int)Math.Max((breakEnd - OsuContainer.SongPosition) / 1000, 0);

            Vector4 textColor = new Vector4(1f, 1f, 1f, alpha);

            float textScale = size.Y / Font.DefaultFont.Size*0.5f;

            Rectangle rect = new Rectangle(position, size);

            g.DrawStringCentered(remainingBreakTime.ToString(), Font.DefaultFont, rect.Center, textColor, textScale);

            const string subText = "Enjoy this little break, although i bet it won't be lasting long";

            var subTextScale = textScale / 6;
            var subTextSize = Font.DefaultFont.MessureString(subText, subTextScale);
            var subTextBottomMargin = 10 * MainGame.Scale;

            g.DrawString(subText, Font.DefaultFont, 
                new Vector2(rect.Center.X - subTextSize.X / 2, position. Y + size.Y - subTextSize.Y - subTextBottomMargin),
                textColor, subTextScale);
        }

        public override void Update(float delta)
        {
            scrollProgress.Time = OsuContainer.SongPosition;
        }
    }
}
