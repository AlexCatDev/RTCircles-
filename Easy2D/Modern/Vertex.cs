using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public struct Vertex
    {
        public Vector2 Position;

        public Vector2 TexCoord;

        public Vector2 RotationOrigin;

        public Vector4 Color;

        public float Rotation;

        //It's an int here, in the shader it's an int, but if i set the vertex attrib pointer to type int, or uint it doesnt work??? only if float it works???
        public int TextureSlot;

        public Vertex(Vector2 position, Vector2 texCoord, int textureSlot)
        {
            Position = position;
            TexCoord = texCoord;
            Color = Vector4.One;

            RotationOrigin = Vector2.Zero;
            Rotation = 0;
            TextureSlot = textureSlot;
        }

        public Vertex(Vector2 position, Vector2 texCoord, Vector4 color, int textureSlot)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;

            RotationOrigin = Vector2.Zero;
            Rotation = 0;
            TextureSlot = textureSlot;
        }

        public override string ToString()
        {
            return Position.ToString();
        }
    }
}
