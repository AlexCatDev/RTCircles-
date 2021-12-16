using Silk.NET.OpenGLES;
using OpenTK.Mathematics;
using System.IO;

namespace Easy2D
{
    /*
    public static class Blur
    {
        private static Shader blurShader = new Shader();

        static Blur()
        {
            blurShader.AttachShader(ShaderType.VertexShader, new FileInfo("./Shaders/Blur.vert"));
            blurShader.AttachShader(ShaderType.FragmentShader, new FileInfo("./Shaders/Blur.frag"));
        }

        public static FrameBuffer BlurTexture(Texture texture, float quality = 16f, float directions = 32f, float radius = 0.03f)
        {
            texture.UseAsyncLoading = false;
            texture.Bind(0);

            FrameBuffer frameBuffer = new FrameBuffer(texture.Width, texture.Height);
            frameBuffer.Bind();

            var prevView = Viewport.CurrentViewport;

            var projection = Matrix4.CreateOrthographicOffCenter(0, texture.Width, texture.Height, 0, 0, 1);
            var viewport = new Vector4i(0, 0, texture.Width, texture.Height);

            BlurDrawTexture(projection, viewport, Vector2.Zero, texture.Size, texture, quality, directions, radius);

            frameBuffer.Unbind();
            Viewport.SetViewport(prevView);

            return frameBuffer;
        }

        public static void BlurDrawTexture(Matrix4 projection, Vector4i viewport, Vector2 position, Vector2 size, Texture texture, float quality = 16f, float directions = 32f, float radius = 0.03f)
        {
            texture.UseAsyncLoading = false;
            texture.Bind(0);

            blurShader.Bind();
            blurShader.SetInt("u_Texture", 0);
            blurShader.SetMatrix("u_Projection", projection);

            blurShader.SetFloat("u_Quality", quality);
            blurShader.SetFloat("u_Directions", directions);
            blurShader.SetFloat("u_Radius", radius);

            Viewport.SetViewport(viewport);
            GLDrawing.DrawQuad(position, size);
        }
    }
    */
}
