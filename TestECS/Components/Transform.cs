using OpenTK.Mathematics;

namespace TestECS
{
    public class Transform : Component
    {
        public Vector2 Position;
        public Vector2 Size;
        public float Rotation;

        public override string ToString()
        {
            return $"Pos: {Position} Size: {Size} Rot: {Rotation}";
        }
    }
}