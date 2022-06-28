using Easy2D;
using OpenTK.Mathematics;
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

        public override void Render(Graphics g)
        {
            if (targetScreen == null)
                return;

            g.EndDraw();
            var prevProj = g.Projection;
            var prevView = Viewport.CurrentViewport;

            Viewport.SetViewport((int)Position.X, (int)(MainGame.WindowHeight - Position.Y - Size.Y), (int)Size.X, (int)Size.Y);
            g.Projection = Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);
            MainGame.Instance.FakeWindowSize(Size, () =>
            {
                targetScreen.Update((float)MainGame.Instance.DeltaTime);

                g.DrawRectangle(Vector2.Zero, Size, Colors.Black);
                targetScreen.Render(g);

                g.EndDraw();
            });
            
            g.Projection = prevProj;
            Viewport.SetViewport(prevView);
        }

        public override void Update(float delta) { }
    }
}
