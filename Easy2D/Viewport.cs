using System.Numerics;

namespace Easy2D
{
    public static class Viewport
    {
        public static Vector4 CurrentViewport { get; private set; }

        public static Rectangle Area => 
            new Rectangle(CurrentViewport.X, CurrentViewport.Y, CurrentViewport.Z, CurrentViewport.W);

        public static int X => (int)CurrentViewport.X;
        public static int Y => (int)CurrentViewport.Y;

        public static int Width => (int)CurrentViewport.Z;
        public static int Height => (int)CurrentViewport.W;

        public static Vector2 Size => new Vector2(CurrentViewport.Z, CurrentViewport.W);
        public static Vector2 Position => new Vector2(CurrentViewport.X, CurrentViewport.Y);

        public static void SetViewport(int x, int y, int width, int height) => 
            SetViewport(new Vector4(x, y, width, height));

        public static void SetViewport(Vector4 dimensions)
        {
            if (CurrentViewport != dimensions)
            {
                CurrentViewport = dimensions;
                GL.Instance.Viewport((int)dimensions.X, (int)dimensions.Y, (uint)dimensions.Z, (uint)dimensions.W);
                GL.Instance.Scissor((int)dimensions.X, (int)dimensions.Y, (uint)dimensions.Z, (uint)dimensions.W);
            }
        }
    }
}
