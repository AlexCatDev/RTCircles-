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
        private Vector2 size
        {
            get {
                float height = 160 * MainGame.Scale;
                return new Vector2(height * Skin.WarningArrow.Texture.Size.AspectRatio(), height);
            }
        }

        public WarningArrows(double spawnTime)
        {
            this.spawnTime = spawnTime;

            Layer = 727691337;
        }

        public override void Render(Graphics g)
        {
            Vector4 color = new Vector4(1f, 1f, 1f, alpha);

            Vector2 warningTopLeft = new Vector2(OsuContainer.FullPlayfield.TopLeft.X - size.X, OsuContainer.FullPlayfield.Y);
            //g.DrawRectangle(warningTopLeft, size, Colors.Green);
            g.DrawRectangle(warningTopLeft, size, color, Skin.WarningArrow);

            Vector2 warningTopRight = new Vector2(OsuContainer.FullPlayfield.TopRight.X, OsuContainer.FullPlayfield.Y);
            //g.DrawRectangle(warningTopRight, size, Colors.Green);
            g.DrawRectangle(warningTopRight, size, color, Skin.WarningArrow, new Rectangle(1, 0, -1, 1), true);

            Vector2 warningBottomLeft = new Vector2(OsuContainer.FullPlayfield.X - size.X, OsuContainer.FullPlayfield.BottomLeft.Y - size.Y);
            //g.DrawRectangle(warningBottomLeft, size, Colors.Green);
            g.DrawRectangle(warningBottomLeft, size, color, Skin.WarningArrow);

            Vector2 warningBottomRight = new Vector2(OsuContainer.FullPlayfield.BottomRight.X, OsuContainer.FullPlayfield.BottomLeft.Y - size.Y);
            //g.DrawRectangle(warningBottomRight, size, Colors.Green);
            g.DrawRectangle(warningBottomRight, size, color, Skin.WarningArrow, new Rectangle(1, 0, -1, 1), true);
        }

        private double elapsedTime = 0;

        public override void Update(float delta)
        {
            if (OsuContainer.SongPosition < spawnTime && alpha == 0)
                return;

            elapsedTime += OsuContainer.DeltaSongPosition;

            if (elapsedTime >= 125)
            {
                elapsedTime -= 125;

                alpha = alpha == 1f ? 0 : 1f;
            }

            if (OsuContainer.SongPosition > spawnTime + 2000)
                IsDead = true;
        }
    }

}
