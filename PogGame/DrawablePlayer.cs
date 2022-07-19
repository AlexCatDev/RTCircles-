using System;
using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;

namespace PogGame
{
    public class DrawablePlayer : Drawable
    {
        private static Texture weapon = new Texture(Utils.GetResource("Assets.Textures.bazooka.png"));

        private static Sound shootSound = new Sound(Utils.GetResource("Assets.Sounds.shoot.wav"));

        private Vector2 position;
        private Vector2 size = new Vector2(48);
        private Vector2 velocity;

        private float distCameraScale = 1f;
        private float shootSpeed = 0.140f;
        private float shootTimer;

        private Vector2 guideEnd, center;

        public override Rectangle Bounds => new Rectangle(position, size);

        public override void Render(Graphics g)
        {
            g.DrawRectangle(position, size, new Vector4(1f, 1.5f, 2.7f, 1f));

            Vector2 armPos = Bounds.Center;

            float angle = MathF.Atan2(Easy2D.Game.Input.MousePosition.Y - armPos.Y, Easy2D.Game.Input.MousePosition.X - armPos.X);

            g.DrawRectangle(position, new Vector2(100, 100 / weapon.Size.AspectRatio()), Colors.White, weapon, rotDegrees: MathHelper.RadiansToDegrees(angle));

            g.DrawRectangleCentered(guideEnd, new Vector2(4), Colors.Red);
            //g.DrawLine(center, guideEnd, new Vector4(2f, 0f, 0f, 1f), 10f);

            base.Render(g);
        }

        private int lastBeat = 0;
        public override void OnUpdate()
        {
            bool isTouchingGround = velocity.Y == 0;

            if (Easy2D.Game.Input.IsKeyDown(Key.D))
            {
                velocity.X = MathHelper.Lerp(velocity.X, 500, fDelta * 20);
            }else if (Easy2D.Game.Input.IsKeyDown(Key.A))
            {
                velocity.X = MathHelper.Lerp(velocity.X, -500, fDelta * 20);
            }
            else
            {
                velocity.X = MathHelper.Lerp(velocity.X, 0, fDelta * 20);
            }

            if (Easy2D.Game.Input.IsKeyDown(Key.Space) && isTouchingGround)
            {
                velocity.Y = -800f;
            }

            position += velocity * fDelta;
            
            velocity.Y += 1500 * fDelta;

            float barrierPos = EntryPoint.WindowHeight - size.Y;
            if (position.Y >= barrierPos)
            {
                position.Y = barrierPos;
                velocity.Y = 0;
            }

            EntryPoint.camera.Scale = MathHelper.Lerp(EntryPoint.camera.Scale, distCameraScale, 20f * fDelta);

            center = position + size / 2f;
            float length = 80f;
            float angle = MathF.Atan2(Easy2D.Game.Input.MousePosition.Y - center.Y, Easy2D.Game.Input.MousePosition.X - center.X);

            guideEnd = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * length;

            int floorBeat = (int)Math.Floor(AudioMain.CurrentBeat);
            if (Math.Abs(floorBeat - lastBeat) >= 1)
            {
                lastBeat = floorBeat;

                if (shoot)
                {
                    Add(new DrawableBullet(guideEnd, new Vector2(MathF.Cos(angle), MathF.Sin(angle)), 8, new Vector4(16f, 8f, 0f, 1f), 200) { Target = EntryPoint.Monster });
                    shootTimer = shootSpeed;
                    shootSound.Play(true);
                }
            }

            base.OnUpdate();
        }

        private bool shoot;
        public override bool OnMouseDown(MouseButton args)
        {
            if (args == MouseButton.Left)
                shoot = true;

            return base.OnMouseDown(args);
        }

        public override bool OnMouseUp(MouseButton args)
        {
            if (args == MouseButton.Left)
                shoot = false;

            return base.OnMouseUp(args);
        }

        public override bool OnMouseWheel(float delta)
        {
            if(delta > 0)
            {
                distCameraScale += 0.1f;
            }
            else
            {
                distCameraScale -= 0.1f;
            }
            Console.WriteLine(distCameraScale);

            return base.OnMouseWheel(delta);
        }
    }
}
