using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class BreakPanel : Drawable
    {
        private SmoothFloat yPosition = new SmoothFloat() { Value = 0f };

        private Vector2 size => new Vector2(MainGame.WindowWidth, 400 * MainGame.Scale);

        private double? timeClose;
        private double showTime = 0;

        public void Show(double timeClose)
        {
            yPosition.ClearTransforms();
            yPosition.Wait(600);
            yPosition.TransformTo(1f, (float)OsuContainer.Beatmap.Preempt * 2f, EasingTypes.OutBack);

            this.timeClose = timeClose;

            showTime = OsuContainer.SongPosition;
        }

        private void hide()
        {
            yPosition.ClearTransforms();
            yPosition.TransformTo(0f, (float)OsuContainer.Beatmap.Preempt * 1.5f, EasingTypes.InBack);
        }

        public override void Render(Graphics g)
        {
            yPosition.Update((float)OsuContainer.DeltaSongPosition);

            float yPos = yPosition.Value.Map(0, 1, MainGame.WindowHeight + size.Y, MainGame.WindowCenter.Y);
            Vector2 pos = new Vector2(MainGame.WindowCenter.X, yPos);

            if (yPosition.Value > 0) {
                g.DrawRectangleCentered(pos, size, Colors.From255RGBA(37, 37, 37, 127));

                var val = timeClose ?? OsuContainer.SongPosition;

                string txt = $"{(val - OsuContainer.SongPosition) / 1000:F2}";

                Skin.ScoreNumbers.DrawCentered(g, pos, 100 * MainGame.Scale, Colors.White, txt);

                /*
                float txtScale = 2f * MainGame.Scale;

                Vector2 txtSize = Font.DefaultFont.MessureString(txt, txtScale);

                g.DrawString(txt, Font.DefaultFont, pos - txtSize / 2f, Colors.White, txtScale);
                */
            }

            if (OsuContainer.SongPosition < showTime)
            {
                timeClose = null;
                yPosition.Value = 0f;
            }

            if(OsuContainer.SongPosition >= timeClose)
            {
                timeClose = null;
                hide();
            }
        }

        public override void Update(float delta)
        {
            
        }
    }
}
