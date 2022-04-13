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
            scrollProgress.Time = OsuContainer.SongPosition;
            sizeProgress.Time = OsuContainer.SongPosition;

            if (!(scrollProgress.Value > 0f))
                return;

            Vector2 size = new Vector2(MainGame.WindowWidth, sizeProgress.Value.Map(0f, 1f, MainGame.WindowHeight / 3f, 0));
            Vector2 position = new Vector2(scrollProgress.Value.Map(0, 1, -MainGame.WindowWidth, 0), MainGame.WindowCenter.Y - size.Y / 2f);

            g.DrawRectangle(position, size, Colors.From255RGBA(37, 37, 37, 127));

            int remainingBreakTime = (int)Math.Max((breakEnd - OsuContainer.SongPosition) / 1000, 0);

            float textScale = size.Y / Font.DefaultFont.Size * 0.6f;

            Rectangle rect = new Rectangle(position, size);

            g.DrawStringCentered(remainingBreakTime.ToString(), Font.DefaultFont, rect.Center, new Vector4(1f, 1f, 1f, scrollProgress), textScale);

            /*
            var remainingTime = (breakEnd - OsuContainer.SongPosition) / 1000;

            if (remainingTime < 0)
                remainingTime = 0;

            string txt = $"{(int)remainingTime}";

            float textScale = size.Y / Font.DefaultFont.Size * 0.6f;
            Vector2 textSize = Font.DefaultFont.MessureString("00", textScale);
            Vector2 textPos = new Vector2(position.X / 2f + size.X / 2 - textSize.X / 2, position.Y + size.Y / 2f - textSize.Y);

            g.DrawStringNoAlign(txt, Font.DefaultFont, textPos, new Vector4(1f,1f,1f,scrollProgress), textScale);
            */
        }

        public override void Update(float delta)
        {
            
        }
    }

    //public class BreakPanel : Drawable
    //{
    //    private Vector2 size => new Vector2(MainGame.WindowWidth, 400 * MainGame.Scale);

    //    private double? timeClose;
    //    private double showTime = 0;

    //    private AnimationTFloat tree = new AnimationTFloat();

    //    private string breakText = "";

    //    public BreakPanel()
    //    {
    //        OsuContainer.BeatmapChanged += () =>
    //        {
    //            showTime = double.MaxValue;
    //        };
    //    }

    //    public void Show(IDrawableHitObject current, IDrawableHitObject next)
    //    {
    //        tree.Clear();

    //        Starting State
    //        tree.Add(current.BaseObject.EndTime + OsuContainer.Fadeout, 0f);

    //        tree.Add(current.BaseObject.EndTime + OsuContainer.Fadeout + 500, 1f, EasingTypes.OutQuart);

    //        tree.Wait(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500);

    //        tree.Add(next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt, 2f, EasingTypes.InQuart);

    //        showTime = OsuContainer.SongPosition;

    //        timeClose = next.BaseObject.StartTime - OsuContainer.Beatmap.Preempt - 500;
    //    }

    //    private float scaledBeatProgress;

    //    public override void Render(Graphics g)
    //    {
    //        double output = tree.GetOutputAtTime(OsuContainer.SongPosition);

    //        float xPos = (float)output.Map(0, 1, -size.X / 2f, size.X / 2f);

    //        Vector2 pos = new Vector2(xPos, MainGame.WindowCenter.Y);

    //        if (output > 0 && output < 2)
    //        {
    //            g.DrawRectangleCentered(pos, size, Colors.From255RGBA(37, 37, 37, 127));

    //            var val = timeClose ?? OsuContainer.SongPosition;

    //            var remainingTime = (val - OsuContainer.SongPosition) / 1000;

    //            if (remainingTime < 0)
    //                remainingTime = 0;

    //            string txt = $"{remainingTime:F2}";

    //            scaledBeatProgress = MathHelper.Lerp(scaledBeatProgress, MainGame.Scale * (float)OsuContainer.BeatProgress.Map(0, 1, 0.9, 1), (float)MainGame.Instance.DeltaTime * 60f);

    //            float txtYSize = 100 * scaledBeatProgress;

    //            Skin.ScoreNumbers.DrawCentered(g, pos, txtYSize, Colors.White, txt);

    //            var breakTxtSize = Font.DefaultFont.MessureString(breakText, 1f);

    //            g.DrawString(breakText, Font.DefaultFont, pos - new Vector2(breakTxtSize.X / 2, breakTxtSize.Y * 3.5f), Colors.White, 1f);
    //        }

    //        if (OsuContainer.SongPosition < showTime)
    //        {
    //            timeClose = null;
    //            tree.Clear();
    //        }
    //    }

    //    public override void Update(float delta)
    //    {

    //    }
    //}
}
