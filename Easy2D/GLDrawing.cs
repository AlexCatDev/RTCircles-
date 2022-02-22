using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using SixLabors.ImageSharp.PixelFormats;

namespace Easy2D
{
    public static class GLDrawing
    {
        struct SimpleVertex
        {
            public Vector2 Position;
            public Vector2 TexCoord;
        }

        private static GLBuffer<SimpleVertex> quadBuffer = new GLBuffer<SimpleVertex>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StreamDraw, 6);
        private static VertexArray<SimpleVertex> quadVao = new VertexArray<SimpleVertex>();

        /// <summary>
        /// Draw a quad to the currently bound framebuffer and shader
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public static void DrawQuad(Vector2 position, Vector2 size)
        {
            SimpleVertex[] quad = new SimpleVertex[6]
            {
                new SimpleVertex(){ Position = position, TexCoord = new Vector2(0, 1) },
                new SimpleVertex(){ Position = new Vector2(size.X, 0) + position, TexCoord = new Vector2(1, 1) },
                new SimpleVertex(){ Position = new Vector2(0, size.Y) + position, TexCoord = new Vector2(0, 0) },

                new SimpleVertex(){ Position = new Vector2(0, size.Y) + position, TexCoord = new Vector2(0, 0) },
                new SimpleVertex(){ Position = new Vector2(size.X, size.Y) + position, TexCoord = new Vector2(1, 0) },
                new SimpleVertex(){ Position = new Vector2(size.X, 0) + position, TexCoord = new Vector2(1, 1) },
            };

            quadBuffer.UploadData(0, quad.Length, quad);
            quadVao.Bind();

            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}
