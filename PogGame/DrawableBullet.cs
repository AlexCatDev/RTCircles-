using System;
using Easy2D;
using OpenTK.Mathematics;

namespace PogGame
{
    public class DrawableBullet : Drawable
    {
        private static readonly Sound explodeSound = new Sound(Utils.GetResource("Assets.Sounds.explosion.wav"));

        private Vector2 pos;
        private Vector2 direction;
        private float length;
        private Vector4 color;
        private float thickness;

        public bool SpawnExplosion = true;
        public Drawable Target;

        public override Rectangle Bounds => new Rectangle(pos, new Vector2(thickness));

        private float speed = 2000;

        public DrawableBullet(Vector2 spawnPos, Vector2 direction, float thickness, Vector4 color, float length)
        {
            pos = spawnPos;
            this.direction = direction;
            this.color = color;
            this.length = length;
            this.thickness = thickness;
        }
        
        public override void Render(Graphics g)
        {
            //var tipColor = new Vector4(16f, 8f, 0f, 1f);
            //var behindColor = new Vector4(0f, 0f, 0f, 0f);

            var tip = pos;
            var behind = pos - direction * length;
            g.DrawLine(behind, tip, new Vector4(0), color, thickness);
            //g.DrawRectangleCentered(pos, size, new Vector4(16f, 8f, 0f, 1f));
            base.Render(g);
        }

        public override void OnUpdate()
        {
            pos += speed * direction * fDelta;

            if(pos.X <= 0 || pos.Y <= 0 || pos.X >= EntryPoint.WindowWidth || pos.Y >= EntryPoint.WindowHeight || Target?.Bounds.IntersectsWith(Bounds) == true)
                die();

            base.OnUpdate();
        }

        private void die()
        {
            IsDead = true;
            if (SpawnExplosion)
            {
                for (int i = 0; i < 1; i++)
                {
                    Parent.Add(new DrawableExplosion2(pos, MathHelper.RadiansToDegrees(MathF.Atan2(direction.Y, direction.X))));
                }
                explodeSound.Pan = pos.X.Map(0, EntryPoint.WindowWidth, -0.3f, 0.3f);
                explodeSound.Play(true);
            }
        }
    }
}
