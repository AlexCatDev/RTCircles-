using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace New
{
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

        private Graphics graphics;
        private Sound sound;

        public override void OnLoad()
        {
            Instance = this;

            sound = new Sound(Utils.GetResource("hit.wav"));

            graphics = new Graphics();

            container.Add(new Test());
            container.Add(new PerformanceCounter());
            
            
            for (int i = 0; i < 100000; i++)
            {
                container.Add(new BouncingCube());
            }
            
            container.Add(new BouncingCube());

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

            //PostProcessing.MotionBlur = true;
        }

        public override void OnRender(double delta)
        {
            PostProcessing.Use(new Vector2i(Width, Height), new Vector2i(Width, Height));

            container.Render(graphics);
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

            velocity.X = RNG.TryChance() ? 1000f : -1000f;
            velocity.Y = RNG.TryChance() ? 1000f : -1000f;
        }

        private Vector2 position;
        private Vector2 size = new Vector2(2);
        private Vector2 velocity;

        public override void Render(Graphics g)
        {
            g.DrawRectangle(position, size, new Vector4(2f, 2f, 2f, 1f));
            base.Render(g);
        }

        private float elapsed;

        public static int count;

        public override void OnUpdate()
        {
            position += velocity * fDelta;

            position.X = position.X.Clamp(0, NewGame.Instance.Width - size.X);
            position.Y = position.Y.Clamp(0, NewGame.Instance.Height - size.Y);

            if (position.X == NewGame.Instance.Width - size.X || position.X == 0)
                velocity.X *= -1f;

            if (position.Y == NewGame.Instance.Height - size.Y || position.Y == 0)
                velocity.Y *= -1f;

            elapsed += fDelta;
            /*
            if (elapsed >= 0.1f && count < 10000)
            {
                Add(new BouncingCube());
                count++;
            }
            */
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

        public override void Render(Graphics g)
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

        public override void Render(Graphics g)
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
