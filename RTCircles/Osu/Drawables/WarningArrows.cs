using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class WarningArrows : Drawable
    {
        public override Rectangle Bounds => OsuContainer.Playfield;

        private float alpha = 0f;
        private double spawnTime;
        private Vector2 size;

        public WarningArrows(double spawnTime)
        {
            this.spawnTime = spawnTime;

            Layer = 727691337;

            float aspectRatio = (float)Skin.WarningArrow.Texture.Width / Skin.WarningArrow.Texture.Height;

            float width = 180;

            size = new Vector2(width, width / aspectRatio) * Skin.GetScale(Skin.WarningArrow);
        }

        private Vector2 offset;

        public override void Render(Graphics g)
        {
            Vector4 color = new Vector4(1f, 1f, 1f, alpha);

            g.DrawRectangle(OsuContainer.FullPlayfield.TopLeft + offset, size, color, Skin.WarningArrow);

            g.DrawRectangle(OsuContainer.FullPlayfield.TopRight - new Vector2(size.X, 0) - offset, size, color, Skin.WarningArrow, new Rectangle(1, 0, -1, 1), true);

            g.DrawRectangle(OsuContainer.FullPlayfield.BottomLeft - new Vector2(0, size.Y) + offset, size, color, Skin.WarningArrow);

            g.DrawRectangle(OsuContainer.FullPlayfield.BottomRight - new Vector2(size.X, size.Y) - offset, size, color, Skin.WarningArrow, new Rectangle(1, 0, -1, 1), true);
        }

        private double lastBeat = 0;

        public override void Update(float delta)
        {
            if (OsuContainer.SongPosition < spawnTime && alpha == 0)
                return;

            if (Math.Abs(OsuContainer.CurrentBeat * 2 - lastBeat) >= 1f)
            {
                lastBeat = OsuContainer.CurrentBeat * 2;

                if (alpha == 1f)
                    alpha = 0f;
                else
                    alpha = 1f;

                if (OsuContainer.SongPosition > spawnTime + 2000)
                    IsDead = true;
            }
        }
    }

}
