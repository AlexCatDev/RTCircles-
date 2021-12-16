using Easy2D;
using OpenTK.Mathematics;
using System;

namespace RTCircles
{
    public class ComboBurst : Drawable
    {
        public override Rectangle Bounds => throw new NotImplementedException();

        private SmoothVector4 color = new SmoothVector4();
        private SmoothFloat xOffset = new SmoothFloat();

        private Vector2 size
        {
            get 
            {
                float width = 500 * MainGame.Scale;
                float height  = width / Skin.ComboBurst.Texture.Size.AspectRatio();
                return new Vector2(width, height);
            }
        }

        private bool RightSide;

        public static bool CanSpawn = true;

        public ComboBurst()
        {
            CanSpawn = false;

            color.Value = new Vector4(1f, 1f, 1f, 0.8f);
            xOffset.Value = -1f;

            xOffset.TransformTo(0f, 0.7f, EasingTypes.Out, () =>
            {
                color.TransformTo(new Vector4(1f, 1f, 1f, 0f), 0.7f, EasingTypes.None, () => {
                    IsDead = true;
                    CanSpawn = true;
                });
            });

            RightSide = RNG.TryChance();
        }

        public override void Render(Graphics g)
        {
            var texRect = RightSide ? new Rectangle(1, 0, -1, 1) : new Rectangle(0, 0, 1, 1);
            g.DrawRectangle(new Vector2(RightSide ? MainGame.WindowWidth - size.X * (1f + xOffset) : size.X * xOffset, MainGame.WindowHeight - size.Y), size, color, Skin.ComboBurst, texRect, true);
        }

        public override void Update(float delta)
        {
            xOffset.Update(delta);
            color.Update(delta);
        }
    }
}
