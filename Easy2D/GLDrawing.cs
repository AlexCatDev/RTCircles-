using System.Numerics;
using Silk.NET.OpenGLES;

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

        static GLDrawing()
        {
            SimpleVertex[] quad = new SimpleVertex[6]
            {
                new SimpleVertex(){ Position = Vector2.Zero, TexCoord = new Vector2(0, 1) },
                new SimpleVertex(){ Position = new Vector2(1, 0), TexCoord = new Vector2(1, 1) },
                new SimpleVertex(){ Position = new Vector2(0, 1), TexCoord = new Vector2(0, 0) },

                new SimpleVertex(){ Position = new Vector2(0, 1), TexCoord = new Vector2(0, 0) },
                new SimpleVertex(){ Position = new Vector2(1, 1), TexCoord = new Vector2(1, 0) },
                new SimpleVertex(){ Position = new Vector2(1, 0), TexCoord = new Vector2(1, 1) },
            };
            quadBuffer.UploadData(0, 6, quad);
            quadVao.Bind();
        }

        /// <summary>
        /// Draw a quad to the currently bound framebuffer and shader
        /// uniform vec2 u_QuadSize
        /// in vec2 a_Position
        /// in vec2 a_TexCoord
        /// </summary>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public static void DrawQuad()
        {
            quadVao.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}
