using System;
using Easy2D;
using OpenTK.Mathematics;

namespace PogGame
{
    public class DrawableMonster : Drawable
    {
        private static Texture texture;

        static DrawableMonster()
        { 
            texture = new Texture(Utils.GetResource("Assets.Textures.evil_monster.png"));
        }

        public DrawableMonster()
        {
            Layer = -69;
        }

        private const float width = 200;
        private float rotation = 0f;
        private Vector2 position = new Vector2(0, 20);
        private Vector2 size => new Vector2(width, width / texture.Size.AspectRatio());

        public override Rectangle Bounds => new Rectangle(position, size);

        public override void Render(Graphics g)
        {
            g.DrawRectangle(position, size, Colors.White, texture, rotDegrees: rotation);
            base.Render(g);
        }
        
        private Vector2 mult(Vector2 pos, Matrix4 matrix) => (new Vector4(pos.X, pos.Y, 0, 1.0f) * matrix).Xy;

        private Vector2 velocity;

        public override void OnUpdate()
        {
            position += velocity * fDelta;
            
            if (position.X <= 0)
                velocity.X = 500;
            else if (position.X >= EntryPoint.WindowWidth - size.X)
                velocity.X = -500;
            
            //rotation = (float)Math.Cos(Time * 5) * 25;
            laserEyes();

            base.OnUpdate();
        }

        private void laserEyes()
        {
            return;

            //0.35 0.68310076
            Vector2 eye1Pos = new Vector2(position.X + (0.34f * size.X), position.Y + (0.69f * size.Y));
            //0.705 0.6980592
            Vector2 eye2Pos = new Vector2(position.X + (0.705f * size.X), position.Y + (0.698f * size.Y));

            Vector4 color = new Vector4(4f, 0f, 0f, 1f);

            //Console.WriteLine(Easy2D.Game.Input.MousePosition.X / size.X + " " + Easy2D.Game.Input.MousePosition.Y / size.Y);

            float anglePlayer = MathF.Atan2(EntryPoint.Player.Bounds.Center.Y - eye1Pos.Y, EntryPoint.Player.Bounds.Center.X - eye1Pos.X);
            Parent.Add(new DrawableBullet(eye1Pos, new Vector2(MathF.Cos(anglePlayer), MathF.Sin(anglePlayer)), 8f, color, 8) { SpawnExplosion = false, Target = EntryPoint.Player });

            anglePlayer = MathF.Atan2(EntryPoint.Player.Bounds.Center.Y - eye2Pos.Y, EntryPoint.Player.Bounds.Center.X - eye2Pos.X);
            Parent.Add(new DrawableBullet(eye2Pos, new Vector2(MathF.Cos(anglePlayer), MathF.Sin(anglePlayer)), 8f, color, 8) { SpawnExplosion = false, Target = EntryPoint.Player });
        }
    }
}
