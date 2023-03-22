using Easy2D;
using System.Numerics;
using System;

namespace RTCircles
{
    public class FloatingScreen : Drawable
    {
        public Vector2 Position;
        public Vector2 Size;

        public override Rectangle Bounds => new Rectangle(Position, Size);

        private Screen targetScreen;

        public void SetTarget<Type>() where Type : Screen
        {
            GPUSched.Instance.Enqueue(() => {
                targetScreen = ScreenManager.GetScreen<Type>();

                if (targetScreen == null)
                    throw new Exception("Target screen was not found?");
            });
        }

        private FrameBuffer screenFramebuffer = new FrameBuffer(1, 1, textureComponentCount: Silk.NET.OpenGLES.InternalFormat.Rgb, pixelFormat: Silk.NET.OpenGLES.PixelFormat.Rgb);
        private Graphics graphics = new Graphics(4000, 6000);
        public override void Render(Graphics g)
        {
            if (targetScreen == null)
                return;

            screenFramebuffer.EnsureSize(MainGame.WindowWidth, MainGame.WindowHeight);
            screenFramebuffer.BindDrawAction(() =>
            {
                GL.Instance.Clear(Silk.NET.OpenGLES.ClearBufferMask.ColorBufferBit);
                graphics.Projection = Matrix4x4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);

                MainGame.Instance.FakeWindowSize(Size, () =>
                {
                    targetScreen.Update((float)MainGame.Instance.DeltaTime);

                    targetScreen.Render(graphics);
                });

                graphics.EndDraw();
            });

            g.DrawRectangle(Position, Size, Colors.White, screenFramebuffer.Texture, new Rectangle(0, 1, 1, -1), Vector2.Zero, 0);
            /*
            g.EndDraw();
            var prevProj = g.Projection;
            var prevView = Viewport.CurrentViewport;

            Viewport.SetViewport((int)Position.X, (int)(MainGame.WindowHeight - Position.Y - Size.Y), (int)Size.X, (int)Size.Y);
            g.Projection = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);
            MainGame.Instance.FakeWindowSize(Size, () =>
            {
                targetScreen.Update((float)MainGame.Instance.DeltaTime);

                targetScreen.Render(g);
            });
            g.EndDraw();

            g.Projection = prevProj;
            Viewport.SetViewport(prevView);
            */
        }

        public override void Update(float delta) { }
    }
}
