﻿using System.Numerics;

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

        public Matrix4x4 Projection { get; protected set; }

        public void Update()
        {
            Projection = Matrix4x4.CreateTranslation(Position.X, Position.Y, 0) *
                Matrix4x4.CreateRotationZ(Rotation) *
                Matrix4x4.CreateScale(Scale) *
                //Matrix4.CreateTranslation(Size.X / 2, Size.Y / 2, 0) * 
                Matrix4x4.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, -1, 1);
        }
    }
}
