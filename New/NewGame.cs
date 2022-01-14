using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace New
{
    class FancyCursorTrail : Drawable
    {
        class TrailPiece
        {
            public Vector2 Position;
            public Vector4 Color;
            public float Width;

            public bool RemoveMe;

            public TrailPiece(Vector2 Position)
            {
                this.Position = Position;

                this.Color = Vector4.One;
                this.Width = 10;
            }

            public void Update(float delta)
            {
                Width -= delta * 30;
                Color.W -= delta * 4;

                Width.ClampRef(0, 10000);
                Color.W.ClampRef(0, 1);

                float frequency1 = (float)NewGame.Instance.TotalTime;
                float frequency2 = (float)NewGame.Instance.TotalTime;
                float frequency3 = (float)NewGame.Instance.TotalTime;

                float phase1 = 0;
                float phase2 = 1;
                float phase3 = 5;

                float width = 2f;

                var red = 1 + MathF.Abs(MathF.Sin(frequency1 + phase1)) * width;
                var grn = 1 + MathF.Abs(MathF.Sin(frequency2 + phase2)) * width;
                var blu = 1 + MathF.Abs(MathF.Sin(frequency3 + phase3)) * width;

                Color.X = red;
                Color.Y = grn;
                Color.Z = blu;

                if (Color.W <= 0)
                    RemoveMe = true;
            }
        }

        List<TrailPiece> trailPieces = new List<TrailPiece>(); 

        public override void Render(FastGraphics g)
        {
            unsafe
            {
                if (trailPieces.Count > 2)
                {
                    var verts = g.VertexBatch.GetTriangleStrip((uint)(trailPieces.Count * 2) - 2);

                    if (verts != null)
                    {
                        int slot = g.GetTextureSlot(null);
                        for (int i = 1; i < trailPieces.Count; i++)
                        {
                            Vector2 perpen = trailPieces[i - 1].Position - trailPieces[i].Position;
                            perpen = perpen.PerpendicularLeft;
                            perpen.Normalize();

                            verts->Position = trailPieces[i - 1].Position - perpen * trailPieces[i - 1].Width;
                            verts->Color = trailPieces[i - 1].Color;
                            verts->Rotation = 0;
                            verts->TextureSlot = slot;
                            ++verts;

                            verts->Position = trailPieces[i].Position + perpen * trailPieces[i].Width;
                            verts->Color = trailPieces[i].Color;
                            verts->Rotation = 0;
                            verts->TextureSlot = slot;
                            ++verts;
                        }
                    }
                }
            }

            base.Render(g);
        }

        Vector2 lastMousePos;

        public override void OnUpdate()
        {
            for (int i = trailPieces.Count - 1; i >= 0; i--)
            {
                trailPieces[i].Update(fDelta);

                if (trailPieces[i].RemoveMe)
                    trailPieces.RemoveAt(i);
            }

            var mousePos = Easy2D.Game.Input.MousePosition;


            if (lastMousePos == Vector2.Zero)
                lastMousePos = mousePos;


            var length = (mousePos - lastMousePos).Length;

            if (length >= 5)
            {
                lastMousePos = mousePos;
                TrailPiece p = new TrailPiece(mousePos);

                trailPieces.Add(p);
            }

            base.OnUpdate();
        }
    }

    public class Player : Drawable
    {
        class Particle
        {
            public Vector2 Position;
            public Vector2 PositionOffset;
            public Vector4 Color;

            public Vector2 Velocity;

            public bool IsDead;

            public float OffsetX;
            public float OffsetY;

            public float Width = 10;

            public void Wake()
            {
                Position = Easy2D.Game.Input.MousePosition;
                Color = Vector4.One;

                Velocity.X = RNG.Next(-10, 10) * 50;
                Velocity.Y = RNG.Next(-10, 10) * 50;

                OffsetX = RNG.Next(0, 10f);
                OffsetY = RNG.Next(0, 10f);
            }

            public void Update(float delta)
            {
                Width -= delta * 30;
                Color.W -= delta * 4;

                float frequency1 = (float)NewGame.Instance.TotalTime;
                float frequency2 = (float)NewGame.Instance.TotalTime;
                float frequency3 = (float)NewGame.Instance.TotalTime;

                float phase1 = 0;
                float phase2 = 1;
                float phase3 = 5;

                float width = 2f;

                var red = 1 + MathF.Abs(MathF.Sin(frequency1 + phase1)) * width;
                var grn = 1 + MathF.Abs(MathF.Sin(frequency2 + phase2)) * width;
                var blu = 1 + MathF.Abs(MathF.Sin(frequency3 + phase3)) * width;

                Color.X = red;
                Color.Y = grn;
                Color.Z = blu;

                if (Color.W <= 0)
                    IsDead = true;
            }
        }

        List<Particle> Particles = new List<Particle>();

        float emitTimer = 0;

        //20 particlesPerSecond
        float emitRate = 1f / 90f;

        private Texture lineTexture = new Texture(Utils.GetResource("trail.png"));

        public override void Render(FastGraphics g)
        {
            /*
            for (int i = 0; i < Particles.Count; i++)
            {
                g.DrawRectangle(Particles[i].Position + Particles[i].PositionOffset, new Vector2(12, 12), Particles[i].Color, Texture.WhiteCircle);
            }
            */

            if (Particles.Count < 2)
                return;

            unsafe
            {
                var verts = g.VertexBatch.GetTriangleStrip((uint)(Particles.Count * 2) - 2);

                if (verts != null)
                {
                    int slot = g.GetTextureSlot(null);
                    for (int i = 1; i < Particles.Count; i++)
                    {
                        Vector2 perpen = Particles[i - 1].Position - Particles[i].Position;
                        perpen = perpen.PerpendicularLeft;
                        perpen.Normalize();

                        verts->Position = Particles[i - 1].Position - perpen * Particles[i - 1].Width;
                        verts->Color = Particles[i - 1].Color;
                        verts->Rotation = 0;
                        verts->TextureSlot = slot;

                        ++verts;


                        verts->Position = Particles[i].Position + perpen * Particles[i].Width;
                        verts->Color = Particles[i].Color;
                        verts->Rotation = 0;
                        verts->TextureSlot = slot;

                        ++verts;
                    }
                }
            }

            Console.WriteLine(g.VertexBatch.TriangleRenderCount);
            base.Render(g);
        }

        Vector2 lastMousePos;

        public override void OnUpdate()
        {
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                Particles[i].Update(fDelta);

                if (Particles[i].IsDead)
                    Particles.RemoveAt(i);
            }

            emitTimer += fDelta;

            if (emitTimer >= emitRate)
            {
                emitTimer -= emitRate;

                //Particle p = new Particle();
                //p.Wake();

                //Particles.Add(p);
            }

            var mousePos = Easy2D.Game.Input.MousePosition;
            var length = (mousePos - lastMousePos).Length;

            if (length >= 5)
            {
                lastMousePos = mousePos;
                Particle p = new Particle();
                p.Wake();

                Particles.Add(p);
            }

            base.OnUpdate();
        }
    }

    public class NewGame : Game
    {
        public new static NewGame Instance { get; private set; }

        public int Width;
        public int Height;
        public Vector2 Center;

        public override void OnImportFile(string path)
        {
            
        }

        private Drawable container = new Drawable();

        private FastGraphics graphics;
        private Sound sound;

        public override void OnLoad()
        {
            Instance = this;

            sound = new Sound(Utils.GetResource("hit.wav"));

            graphics = new FastGraphics(400_000 * 5, 600_000 * 5);

            //container.Add(new Test());
            //container.Add(new PerformanceCounter());

            Width = 1280;
            Height = 720;
            
            for (int i = 0; i < 100000; i++)
            {
                //container.Add(new BouncingCube());
            }

            container.Add(new FancyCursorTrail());

            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                sound.Play(true);
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                IsMultiThreaded = true;
            };

            Input.InputContext.Mice[0].MouseUp += (s, e) =>
            {
                IsMultiThreaded = false;
            };

            PrintFPS = true;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86 || RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                PostProcessing.Bloom = true;
                //PostProcessing.MotionBlur = true;
            }
            //PostProcessing.MotionBlur = true;
        }

        public (Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight) DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color1, Vector4 color2, float thickness, Texture texture = null)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topLeft = new Vector2(startPosition.X + perpen.X * thickness / 2f,
                startPosition.Y + perpen.Y * thickness / 2f);

            Vector2 topRight = new Vector2(startPosition.X - perpen.X * thickness / 2f,
                startPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomLeft = new Vector2(endPosition.X - perpen.X * thickness / 2f,
                endPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomRight = new Vector2(endPosition.X + perpen.X * thickness / 2f,
                endPosition.Y + perpen.Y * thickness / 2f);

            unsafe
            {
                var quad = graphics.VertexBatch.GetQuad();

                int slot = graphics.GetTextureSlot(texture);

                quad->Rotation = 0;
                quad->Color = color1;
                quad->Position = topLeft;
                quad->TexCoord = new Vector2(0, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color1;
                quad->Position = topRight;
                quad->TexCoord = new Vector2(0, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color2;
                quad->Position = bottomLeft;
                quad->TexCoord = new Vector2(1, 1);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
                ++quad;

                quad->Rotation = 0;
                quad->Color = color2;
                quad->Position = bottomRight;
                quad->TexCoord = new Vector2(1, 0);
                quad->TextureSlot = slot;
                quad->Rotation = 0;
            }

            return (topLeft,topRight,bottomLeft,bottomRight);
        }

        private void drawCircle(Vector2 pos, float radius, float startRotation, float endRotation)
        {
            const int CIRCLE_RESOLUTION = 20;
            unsafe
            {
                int slot = graphics.GetTextureSlot(null);
                var verts = graphics.VertexBatch.GetTriangleFan(CIRCLE_RESOLUTION);

                verts->Position = new Vector2(pos.X, pos.Y);
                verts->TextureSlot = slot;
                verts->Color = Colors.White;
                verts->Rotation = 0;
                ++verts;

                float theta = startRotation;
                float stepTheta = (endRotation - startRotation) / (CIRCLE_RESOLUTION - 2);

                Vector2 vertPos = new Vector2(0, 0);

                for (int i = 1; i < CIRCLE_RESOLUTION; i++)
                {
                    vertPos.X = MathF.Cos(theta) * radius + pos.X;
                    vertPos.Y = MathF.Sin(theta) * radius + pos.Y;

                    verts->Position = vertPos;
                    verts->TextureSlot = slot;
                    verts->Color = Colors.White;
                    verts->Rotation = 0;
                    ++verts;

                    theta += stepTheta;
                }
            }
        }

        public override void OnRender(double delta)
        {
            PostProcessing.Use(new Vector2i(Width, Height), new Vector2i(Width, Height));

            container.Render(graphics);

            Vector2 line1 = new Vector2(100, 100);
            Vector2 line1_2 = new Vector2(300, 300);

            Vector2 pos2 = Input.MousePosition;

            var k1 = DrawLine(line1, line1_2, Colors.Red, Colors.Red, 50f);
            var k2 = DrawLine(line1_2, pos2, Colors.Green, Colors.Green, 50f);

            float angle1 = MathUtils.AtanVec(line1_2, line1);
            float angle2 = MathUtils.AtanVec(line1_2, pos2);

            float angle3 = MathUtils.AtanVec(k1.bottomLeft, k2.topRight);

            float angleDegrees = MathHelper.RadiansToDegrees(angle1);
            float angle2Degrees = MathHelper.RadiansToDegrees(angle2);

            float angle3Degrees = MathHelper.RadiansToDegrees(angle3);

            graphics.DrawString(angleDegrees.ToString(), Font.DefaultFont, line1, Colors.Blue);
            graphics.DrawString(angle2Degrees.ToString(), Font.DefaultFont, pos2, Colors.Blue);

            graphics.DrawString(angle3Degrees.ToString(), Font.DefaultFont, line1_2, Colors.Blue);

            graphics.DrawRectangleCentered(line1, new Vector2(4), Colors.Yellow);
            graphics.DrawRectangleCentered(line1_2, new Vector2(4), Colors.Yellow);
            graphics.DrawRectangleCentered(pos2, new Vector2(4), Colors.Yellow);

            //graphics.DrawEllipse(line1_2, angleDegrees > angle2Degrees ? angle2Degrees : angleDegrees, angle2Degrees, 25, 0, Colors.Yellow, null, 50);

            //drawCircle(line1, 25, angle + MathF.PI, angle + MathF.PI * 2);

            //float angleEnd = MathUtils.AtanVec(k1, k2);

            //drawCircle(line1_2, 25, angle, angleEnd);

            //drawCircle(pos2, 25, angleEnd + MathF.PI / 2, angleEnd + MathF.PI + MathF.PI / 2);

            graphics.DrawString("k1 bl", Font.DefaultFont, k1.bottomLeft, Colors.White, 0.25f);
            graphics.DrawString("k2 tr", Font.DefaultFont, k2.topRight, Colors.White, 0.25f);
            graphics.EndDraw();
            PostProcessing.PresentFinalResult();
        }

        public override void OnResize(int width, int height)
        {
            Viewport.SetViewport(0, 0, width, height);
            graphics.Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            Width = width;
            Height = height;
            Center = new Vector2(Width, Height) / 2;
        }

        public override void OnUpdate(double delta)
        {
            container.Update(TotalTime);
        }
    }

    public class BouncingCube : Drawable
    {
        public BouncingCube()
        {
            position.X = RNG.Next(0, NewGame.Instance.Width);
            position.Y = RNG.Next(0, NewGame.Instance.Height);

            //velocity.X = RNG.TryChance() ? 1000f : -1000f;
            //velocity.Y = RNG.TryChance() ? 1000f : -1000f;
            velocity.X = RNG.Next(0, 500);
            velocity.Y = RNG.Next(0, 500);

            color.W = 1f;
            color.X = RNG.Next(0f, 1f);
            color.Y = RNG.Next(0f, 1f);
            color.Z = RNG.Next(0f, 1f);
        }

        private Vector2 position;
        private Vector2 size = new Vector2(2);
        private Vector2 velocity;
        private Vector4 color;
        private Vector4 texRect = new Vector4(0, 0, 1, 1);

        public override void Render(FastGraphics g)
        {
            g.DrawRectangle(position, size, (Vector4)color);
            //g.RawDrawRectangle(in position, in size, in color, null, in texRect, in Vector2.Zero, 0);
            //g.DrawEllipse(position, 0, 360, 10, 0, color, Texture.WhiteCircle);
            //g.DrawString("Cock", Font.DefaultFont, position, color);
            base.Render(g);
        }

        private float elapsed;

        public static int count;

        public override void OnUpdate()
        {
            position += velocity * fDelta;

            float maxX = NewGame.Instance.Width - size.X;
            float maxY = NewGame.Instance.Height - size.Y;

            position.X.ClampRef(0, maxX);
            position.Y.ClampRef(0, maxY);

            if (position.X == maxX || position.X == 0)
                velocity.X *= -1f;

            if (position.Y == maxY || position.Y == 0)
                velocity.Y *= -1f;

            elapsed += fDelta;
        }
    }

    public class PerformanceCounter : Drawable
    {
        private List<double> updateTimes = new List<double>();
        private List<double> frameTimes = new List<double>();

        public PerformanceCounter()
        {
            Layer = 90000;
        }


        private int frame;

        private float scale = 0.5f;

        private float height = 80;
        private double top = 0.020;

        private int maxSample = 1000;

        private Stopwatch sw = Stopwatch.StartNew();

        private int renderThreadID;

        public override void Render(FastGraphics g)
        {
            renderThreadID = Thread.CurrentThread.ManagedThreadId;

            frame++;

            double delta = ((double)sw.ElapsedTicks / Stopwatch.Frequency);
            sw.Restart();

            if (frameTimes.Count == maxSample)
                frameTimes.RemoveAt(0);
            frameTimes.Add(delta);

            Vector2 offset = new Vector2();
            offset.Y = height;

            for (int i = 0; i < frameTimes.Count; i++)
            {
                float barHeight = (float)frameTimes[i].Map(0, top, 0, height);
                float barWidth = (float)NewGame.Instance.Width / maxSample;

                g.DrawRectangle(offset - new Vector2(0, barHeight), new Vector2(barWidth, barHeight), Colors.Green);
                offset.X += barWidth;
            }

            Vector2 size = Font.DefaultFont.MessureString(drawString, scale);
            g.DrawString(drawString, Font.DefaultFont, new Vector2(NewGame.Instance.Center.X - size.X / 2f, 10), Colors.Yellow, scale);

            /*
            offset.Y = height * 2;
            offset.X = 0;
            for (int i = 0; i < updateTimes.Count; i++)
            {
                float barHeight = (float)updateTimes[i].Map(0, top, 0, height);
                float barWidth = (float)Program.Instance.Width / maxSample;

                g.DrawRectangle(offset - new Vector2(0, barHeight), new Vector2(barWidth, barHeight), Colors.Blue);
                offset.X += barWidth;
            }
            */
        }

        private string drawString = "";

        private int update;

        private double elapsed;

        public override void OnUpdate()
        {
            update++;

            elapsed += Delta;

            if (elapsed >= 1.0)
            {
                drawString = $"FPS: {frame} UPS: {update} Drawables: {Parent.ChildrenCount} Update: {Thread.CurrentThread.ManagedThreadId} Render: {renderThreadID}";
                elapsed -= 1.0;
                update = 0;
                frame = 0;
            }

            if (updateTimes.Count == maxSample)
                updateTimes.RemoveAt(0);

            updateTimes.Add(Delta);
        }
    }

    public class Test : Drawable
    {
        public Test()
        {

        }

        protected override void OnAdd()
        {

        }

        public override void Render(FastGraphics g)
        {
            //g.DrawRectangleCentered(Program.Instance.MousePosition, new Vector2(64, 64), new Vector4(1f,1f,1f,1f));
            g.DrawString($"Time: {Time:F2}", Font.DefaultFont, Easy2D.Game.Input.MousePosition, new Vector4(14.75f, 9.125f, 1.71f, 1f), 1f);

            base.Render(g);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

        }
    }
}
