using OpenTK.Mathematics;
using Silk.NET.OpenGLES;

namespace Easy2D
{
    public static class Blur
    {
        private static Shader blurShader = new Shader();
        
        static Blur()
        {
            blurShader.AttachShader(Silk.NET.OpenGLES.ShaderType.VertexShader, Utils.GetInternalResource("Shaders.Blur.vert"));
            blurShader.AttachShader(Silk.NET.OpenGLES.ShaderType.FragmentShader, Utils.GetInternalResource("Shaders.Blur.frag"));
        }

        private static FrameBuffer destPong = new FrameBuffer(1,1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb, PixelFormat.Rgb);

        public static void BlurTexture(Texture texture, FrameBuffer dest, float radius, int iterations)
        {
            //texture.Bind(0);

            //var startViewport = Viewport.CurrentViewport;

            //int w = dest.Width;
            //int h = dest.Height;

            //Vector2 quadSize = new Vector2(w, h);

            //Vector2 horizontalBlur = new Vector2(radius, 0);
            //Vector2 verticalBlur = new Vector2(0, radius);

            //destPong.EnsureSize(w, h);

            //dest.Bind();

            //Viewport.SetViewport(0, 0, w, h);

            //texture.Bind(0);

            //blurShader.Bind();
            //blurShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, w, h, 0, -1, 1));
            //blurShader.SetInt("u_SrcTexture", 0);

            //blurShader.SetVector("u_Direction", horizontalBlur);
            //GLDrawing.DrawQuad(Vector2.Zero, quadSize);

            //var writeBuffer = destPong;
            //var readBuffer = dest;

            //blurShader.SetVector("u_Direction", horizontalBlur);



            ////-1 because we already blured once into the dest buffer ^
            //for (int i = 0; i < iterations - 1; i++)
            //{
            //    writeBuffer.Bind();
            //    Viewport.SetViewport(0, 0, w, h);

            //    readBuffer.Texture.Bind(0);

            //    GLDrawing.DrawQuad(Vector2.Zero, quadSize);

            //    swap(ref writeBuffer, ref readBuffer);
            //}

            //blurShader.SetVector("u_Direction", verticalBlur);
            //for (int i = 0; i < iterations; i++)
            //{
            //    writeBuffer.Bind();
            //    Viewport.SetViewport(0, 0, w, h);

            //    readBuffer.Texture.Bind(0);

            //    GLDrawing.DrawQuad(Vector2.Zero, quadSize);

            //    swap(ref writeBuffer, ref readBuffer);
            //}

            ///*
            //if (writeBuffer == dest)
            //    throw new System.Exception("Dude, du manger lige en ekstra pong her");
            //*/

            //dest.Unbind();
            //Viewport.SetViewport(startViewport);

            texture.Bind(0);

            var startViewport = Viewport.CurrentViewport;

            int w = dest.Width;
            int h = dest.Height;

            Vector2 quadSize = new Vector2(w, h);

            Vector2 horizontalBlur = new Vector2(radius, 0);
            Vector2 verticalBlur = new Vector2(0, radius);

            destPong.EnsureSize(w, h);

            dest.Bind();

            Viewport.SetViewport(0, 0, w, h);

            texture.Bind(0);

            blurShader.Bind();
            blurShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, w, h, 0, -1, 1));
            blurShader.SetInt("u_SrcTexture", 0);

            blurShader.SetVector("u_Direction", horizontalBlur);
            GLDrawing.DrawQuad(Vector2.Zero, quadSize);

            var writeBuffer = destPong;
            var readBuffer = dest;

            var blurDirection = horizontalBlur;
            //-1 because we already blured once into the dest buffer ^
            for (int i = 0; i < (iterations * 2) - 1; i++)
            {
                blurDirection = blurDirection == horizontalBlur ? verticalBlur : horizontalBlur;

                writeBuffer.Bind();
                Viewport.SetViewport(0, 0, w, h);

                readBuffer.Texture.Bind(0);

                blurShader.SetVector("u_Direction", blurDirection);
                GLDrawing.DrawQuad(Vector2.Zero, quadSize);

                swap(ref writeBuffer, ref readBuffer);
            }

            
            //if (writeBuffer == dest)
            //    throw new System.Exception("Dude, du manger lige en ekstra pong her");
            

            dest.Unbind();
            Viewport.SetViewport(startViewport);

            //Utils.Log($"Blured {w}x{h} texture Radius: {radius} Iterations: {iterations}", LogLevel.Info);
        }

        private static void swap(ref FrameBuffer a, ref FrameBuffer b)
        {
            var c = a;

            a = b;
            b = c;
        }
    }
}
