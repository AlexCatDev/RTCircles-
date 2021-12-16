using OpenTK.Mathematics;

namespace Easy2D
{
    /// <summary>
    /// Orthographic 2D Camera
    /// </summary>
    public class Camera
    {
        public float Rotation = 0f;
        public float Scale = 1f;
        public Vector2 Position = Vector2.Zero;
        public Vector2 Size = new Vector2();

        public Matrix4 Projection { get; protected set; }

        /// <summary>
        /// Orthographic 2D Camera
        /// </summary>
        /// <param name="width">The view width</param>
        /// <param name="height">The view height</param>
        public Camera(int width, int height)
        {
            /*
            this.width = width;
            this.height = height;

            Position.X = -width / 2f;
            Position.Y = -height / 2f;

            Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
            */
        }

        public void Update()
        {
            Projection = Matrix4.CreateTranslation(Position.X, Position.Y, 0) * 
                Matrix4.CreateRotationZ(Rotation) * 
                Matrix4.CreateScale(Scale) * 
                //Matrix4.CreateTranslation(Size.X / 2, Size.Y / 2, 0) * 
                Matrix4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, 0, 1);
        }
    }
}
