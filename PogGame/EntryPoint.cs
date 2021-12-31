using System;
using System.Runtime.InteropServices;
using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;

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

    public class DrawableExplosion2 : Drawable
    {
        private static Texture texture;
        private static Tileset tileset;

        static DrawableExplosion2()
        {
            texture = new Texture(Utils.GetResource("Assets.Textures.explosion.png"));
            tileset = new Tileset(new Vector2(4096, 4096), new Vector2(512));
        }

        public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

        private Vector2 pos;
        private Vector2 size = new Vector2(8, 8);
        private float rotation;

        public DrawableExplosion2(Vector2 pos, float rotation)
        {
            this.pos = pos;
            this.rotation = rotation;
        }

        public override void Render(Graphics g)
        {
            var texCoords = tileset.GetRightThenWrapDown((int)time);
            g.DrawRectangleCentered(pos, new Vector2(256), new Vector4(10f, 10f, 10f, 1f), texture, new Rectangle(texCoords.X, texCoords.Y, texCoords.Z, texCoords.W), true, rotation);
        }

        private double time;
        public override void OnUpdate()
        {
            time += Delta * tileset.Count * 2;
            if (time > tileset.Count + 10)
                IsDead = true;
        }
    }

    public class DrawableExplosion : Drawable
    {
        public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

        private Vector2 pos;
        private Vector2 size = new Vector2(8, 8);
        private SmoothFloat alpha = new SmoothFloat();
        private Vector2 velocity;
        private float scale;

        public DrawableExplosion(Vector2 pos)
        {
            alpha.Value = 0f;
            alpha.TransformTo(1f, 0.25f, EasingTypes.Out);
            alpha.TransformTo(0f, 0.5f, EasingTypes.Out);

            float theta = RNG.Next(0, MathF.PI * 2);
            this.pos = pos + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * 1f;

            scale = RNG.Next(20f, 100f) / 100f;

            velocity.X = MathF.Cos(theta) * 1000f * scale;
            velocity.Y = MathF.Sin(theta) * 1000f * scale;
        }

        public override void Render(Graphics g)
        {
            float r = MathF.Abs((float)Math.Sin(Time)) * 5;
            float green = MathF.Abs((float)Math.Cos(Time)) * 5;
            float b = MathF.Abs(r - green) * 5;
            Vector4 color = new Vector4(r + 1, green + 1, b + 1, alpha);
            g.DrawRectangleCentered(pos, size, color, Texture.WhiteCircle);
        }

        public override void OnUpdate()
        {
            pos += velocity * fDelta;
            alpha.Update(fDelta);

            if (alpha.HasCompleted)
                IsDead = true;
        }
    }

    public class DrawableBullet : Drawable
    {
        private static Sound explodeSound;

        static DrawableBullet()
        {
            explodeSound = new Sound(Utils.GetResource("Assets.Sounds.explosion.wav"));
        }

        private Vector2 pos;
        private Vector2 direction;
        private float length;
        private Vector4 color;
        private float thickness;

        public bool SpawnExplosion = true;
        public Drawable Target;

        public override Rectangle Bounds => new Rectangle(pos, new Vector2(thickness));

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
            pos += direction * fDelta * 2000f;

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

    public class DrawablePlayer : Drawable
    {
        private static Texture weapon;

        static DrawablePlayer()
        {
            weapon = new Texture(Utils.GetResource("Assets.Textures.bazooka.png"));
        }

        public DrawablePlayer()
        {
            shootSound = new Sound(Utils.GetResource("Assets.Sounds.shoot.wav"));
        }

        private Vector2 position;
        private Vector2 size = new Vector2(48);
        private Vector2 velocity;

        private float distCameraScale = 1f;
        private float shootSpeed = 0.140f;
        private float shootTimer;

        private Vector2 guideEnd, center;

        private Sound shootSound;

        public override Rectangle Bounds => new Rectangle(position, size);

        public override void Render(Graphics g)
        {
            g.DrawRectangle(position, size, new Vector4(1f, 2.7f, 5f, 1f));

            Vector2 armPos = Bounds.Center;

            float angle = MathF.Atan2(Easy2D.Game.Input.MousePosition.Y - armPos.Y, Easy2D.Game.Input.MousePosition.X - armPos.X);

            g.DrawRectangle(position, new Vector2(100, 100 / weapon.Size.AspectRatio()), Colors.White, weapon, rotDegrees: MathHelper.RadiansToDegrees(angle));
            //g.DrawLine(center, guideEnd, new Vector4(2f, 0f, 0f, 1f), 10f);

            base.Render(g);
        }

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

            if (shoot)
            {
                shoot = false;
                shootTimer = 0;
                shootTimer -= fDelta;
                if(shootTimer <= 0)
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

    public class EntryPoint : Game
    {
        public static Camera camera = new Camera(1,1);
        private Graphics graphics;
        private Drawable container = new Drawable();

        public static DrawableMonster Monster = new DrawableMonster();
        public static DrawablePlayer Player = new DrawablePlayer();

        public override void OnImportFile(string path)
        {
            
        }

        public override void OnLoad()
        {
            graphics = new Graphics();
            container.Add(Player);
            container.Add(Monster);

            PostProcessing.Bloom = true;
            //PostProcessing.MotionBlur = true;
            //PostProcessing.MotionBlurScale = 20;

            Input.InputContext.Mice[0].Scroll += (s, e) =>
            {
                container.OnMouseWheel(e.Y);
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                container.OnMouseDown(e);
            };

            Input.InputContext.Mice[0].MouseUp += (s, e) =>
            {
                container.OnMouseUp(e);
            };
        }

        public override void OnRender(double delta)
        {
            graphics.Projection = camera.Projection;

            //PostProcessing.Use(((Vector2i)WindowSize), ((Vector2i)WindowSize));

            container.Render(graphics);

            renderStatistics(graphics);

            graphics.EndDraw();
            //PostProcessing.PresentFinalResult();
        }

        private ulong prevVertices, prevIndices, prevTriangles;
        private void renderStatistics(Graphics g)
        {
            ulong diffVertices = g.VerticesDrawn - prevVertices;
            prevVertices = g.VerticesDrawn;

            ulong diffIndices = g.IndicesDrawn - prevIndices;
            prevIndices = g.IndicesDrawn;

            ulong diffTriangles = g.TrianglesDrawn - prevTriangles;
            prevTriangles = g.TrianglesDrawn;

            double verticesPerSecond = (diffVertices) * (1.0 / RenderDeltaTime);

            double indicesPerSecond = (diffIndices) * (1.0 / RenderDeltaTime);

            double trianglesPerSecond = (diffTriangles) * (1.0 / RenderDeltaTime);

            float scale = 0.35f;
            string text = $"FPS: {FPS}/{1000.0 / FPS:F2}ms UPS: {UPS}/{1000.0 / UPS:F2}ms\nVertices: {Utils.ToKMB(verticesPerSecond)}/s\nIndices: {Utils.ToKMB(indicesPerSecond)}/s\nTris: {Utils.ToKMB(trianglesPerSecond)}/s\nFramework: {RuntimeInformation.FrameworkDescription}\nOS: {RuntimeInformation.OSDescription}";

            g.DrawString(text, Font.DefaultFont, new Vector2(20), Colors.Yellow, scale);
        }

        public static Vector2 WindowSize { get; private set; }

        public static Vector2 WindowCenter => WindowSize / 2f;

        public static float WindowWidth => WindowSize.X;
        public static float WindowHeight => WindowSize.Y;

        public static readonly Vector2 TargetResolution = new Vector2(1920, 1080);

        public static float Scale
        {
            get
            {
                //Calculate the aspectratio of our virtual resolution
                var aspectRatio = TargetResolution.X / TargetResolution.Y;

                //Store width of window width
                var width = WindowWidth;
                //calculate viewportheight by dividing window width with the virtual resolution aspect ratio
                var height = (int)(width / aspectRatio);
                //If the calculated viewport height is bigger than the window height then height is equals window height
                if (height > WindowHeight)
                {
                    height = (int)WindowHeight;
                    //Set viewport width to height times aspect ratio
                    width = (int)(height * aspectRatio);
                }

                return width / TargetResolution.X;
            }
        }

        public static Vector2 AbsoluteScale => new Vector2(WindowWidth / TargetResolution.X, WindowHeight / TargetResolution.Y);

        public override void OnResize(int width, int height)
        {
            GPUSched.Instance.Add(() =>
            {
                WindowSize = new Vector2(width, height);

                Viewport.SetViewport(0, 0, width, height);
                camera.Size = new Vector2(width, height);
            });
        }

        public override void OnUpdate(double delta)
        {
            camera.Update();
            container.Update(TotalTime);
        }
    }
}
