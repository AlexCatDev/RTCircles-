using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class BreakPanel : Drawable
    {
        private Vector2 size => new Vector2(MainGame.WindowWidth, 400 * MainGame.Scale);

        private double? timeClose;
        private double showTime = 0;

        private AnimationTFloat tree = new AnimationTFloat();

        private string breakText = "";

        public BreakPanel()
        {
            OsuContainer.BeatmapChanged += () =>
            {
                showTime = double.MaxValue;
            };
        }

        public void Show(IDrawableHitObject current, IDrawableHitObject next)
        {
            tree.Clear();

            //Starting State
            tree.Add(current.BaseObject.EndTime + OsuContainer.Fadeout, 0f);

            tree.Add(current.BaseObject.EndTime + OsuContainer.Fadeout + 500, 1f, EasingTypes.OutQuart);

            tree.Wait(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500);

            tree.Add(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt, 2f, EasingTypes.InQuart);

            showTime = OsuContainer.SongPosition;

            timeClose = next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500;
        }

        private float scaledBeatProgress;

        public override void Render(Graphics g)
        {
            double output = tree.GetOutputAtTime(OsuContainer.SongPosition);

            float xPos = (float)output.Map(0, 1, -size.X / 2f, size.X / 2f);

            Vector2 pos = new Vector2(xPos, MainGame.WindowCenter.Y);

            if (output > 0 && output < 2)
            {
                g.DrawRectangleCentered(pos, size, Colors.From255RGBA(37, 37, 37, 127));

                var val = timeClose ?? OsuContainer.SongPosition;

                var remainingTime = (val - OsuContainer.SongPosition) / 1000;

                if (remainingTime < 0)
                    remainingTime = 0;

                string txt = $"{remainingTime:F2}";

                scaledBeatProgress = MathHelper.Lerp(scaledBeatProgress, MainGame.Scale * (float)OsuContainer.BeatProgress.Map(0, 1, 0.9, 1), (float)MainGame.Instance.DeltaTime * 60f);

                float txtYSize = 100 * scaledBeatProgress;

                Skin.ScoreNumbers.DrawCentered(g, pos, txtYSize, Colors.White, txt);

                var breakTxtSize = Font.DefaultFont.MessureString(breakText, 1f);

                g.DrawString(breakText, Font.DefaultFont, pos - new Vector2(breakTxtSize.X / 2, breakTxtSize.Y * 3.5f), Colors.White, 1f);
            }

            if (OsuContainer.SongPosition < showTime)
            {
                timeClose = null;
                tree.Clear();
            }
        }

        public override void Update(float delta)
        {
            
        }
    }
}
