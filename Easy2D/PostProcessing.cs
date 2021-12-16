using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public static class PostProcessing
    {
        //Float pixel type for dat hdr
        private static FrameBuffer MainFrameBuffer = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer1 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer2 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer3 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer4 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer5 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);
        private static FrameBuffer blurBuffer6 = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, (PixelType)GLEnum.HalfFloat);

        private static Shader bloomShader = new Shader();
        private static Shader motionBlurShader = new Shader();

        public static bool MotionBlur = false;
        public static bool Bloom = false;

        /// <summary>
        /// Higher value = less motion blur, Default = 200
        /// </summary>
        public static float MotionBlurScale = 200f;

        private static bool Enabled => MotionBlur || Bloom;

        private static Vector2i drawSize;

        public static float BloomThreshold = 1f;

        private static Vector4i viewPort;

        static PostProcessing()
        {
            /*
#if RELEASE
            bloomShader.AttachShader(ShaderType.VertexShader, new FileInfo("./Shaders/Bloom.vert"));
            bloomShader.AttachShader(ShaderType.FragmentShader, new FileInfo("./Shaders/Bloom.frag"));
            motionBlurShader.AttachShader(ShaderType.VertexShader, new FileInfo("./Shaders/MotionBlur.vert"));
            motionBlurShader.AttachShader(ShaderType.FragmentShader, new FileInfo("./Shaders/MotionBlur.frag"));
#else
            */
            bloomShader.AttachShader(ShaderType.VertexShader, Utils.GetInternalResource("Shaders.Bloom.vert"));
            bloomShader.AttachShader(ShaderType.FragmentShader, Utils.GetInternalResource("Shaders.Bloom.frag"));

            motionBlurShader.AttachShader(ShaderType.VertexShader, Utils.GetInternalResource("Shaders.MotionBlur.vert"));
            motionBlurShader.AttachShader(ShaderType.FragmentShader, Utils.GetInternalResource("Shaders.MotionBlur.frag"));

            /*
            bloomShader.AttachShader(ShaderType.VertexShader, new FileInfo(@"C:\Users\user\Desktop\CSharp\RTCircles\Easy2D\Shaders\Bloom.vert"));
            bloomShader.AttachShader(ShaderType.FragmentShader, new FileInfo(@"C:\Users\user\Desktop\CSharp\RTCircles\Easy2D\Shaders\Bloom.frag"));

            motionBlurShader.AttachShader(ShaderType.VertexShader, new FileInfo(@"C:\Users\user\Desktop\CSharp\RTCircles\Easy2D\Shaders\MotionBlur.vert"));
            motionBlurShader.AttachShader(ShaderType.FragmentShader, new FileInfo(@"C:\Users\user\Desktop\CSharp\RTCircles\Easy2D\Shaders\MotionBlur.frag"));
            */
//#endif
        }

        public static void Use(Vector2i windowSize, Vector2i drawSize)
        {
            if (Enabled == false)
            {
                FrameBuffer.DefaultFrameBuffer.SetTarget(null);
                return;
            }
            else
            {
                FrameBuffer.DefaultFrameBuffer.SetTarget(MainFrameBuffer);
            }

            PostProcessing.drawSize = drawSize;

            viewPort = Viewport.CurrentViewport;
            MainFrameBuffer.Resize(windowSize.X, windowSize.Y);
            MainFrameBuffer.Bind();
            Viewport.SetViewport(0, 0, MainFrameBuffer.Width, MainFrameBuffer.Height);
            GL.Instance.Clear(ClearBufferMask.ColorBufferBit);
        }

        private static void blitFramebuffer(FrameBuffer src, FrameBuffer dst)
        {
            src.Texture.Bind(0);

            if (dst is not null)
            {
                bloomShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, dst.Width, dst.Height, 0, -1f, 1f));
                dst.Bind();
                Viewport.SetViewport(0, 0, dst.Width, dst.Height);

                GLDrawing.DrawQuad(Vector2.Zero, dst.Texture.Size);
            }
            else
            {
                bloomShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, drawSize.X, drawSize.Y, 0, -1f, 1f));
                FrameBuffer.BindDefault(forceUseScreen: true);
                Viewport.SetViewport(viewPort);
                GLDrawing.DrawQuad(Vector2.Zero, new Vector2(drawSize.X, drawSize.Y));
            }
        }

        public static void PresentFinalResult()
        {
            if (Enabled == false)
                return;

            bloomShader.Bind();
            bloomShader.SetInt("u_Texture", 0);

            if (Bloom)
            {
                bloomShader.SetFloat("u_BloomThreshold", BloomThreshold);

                //Draw Framebuffer 1 to framebuffer 2, subtract color in shader
                blurBuffer1.Resize(MainFrameBuffer.Width / 2, MainFrameBuffer.Height / 2);

                bloomShader.SetBoolean("u_Subtract", true);
                blitFramebuffer(MainFrameBuffer, blurBuffer1);
                bloomShader.SetBoolean("u_Subtract", false);

                bloomShader.SetBoolean("u_Blur", true);
                blurBuffer2.Resize(blurBuffer1.Width / 2, blurBuffer1.Height / 2);

                blitFramebuffer(blurBuffer1, blurBuffer2);
                blitFramebuffer(blurBuffer2, blurBuffer2);

                blurBuffer3.Resize(blurBuffer2.Width / 2, blurBuffer2.Height / 2);

                blitFramebuffer(blurBuffer2, blurBuffer3);
                blitFramebuffer(blurBuffer3, blurBuffer3);

                blurBuffer4.Resize(blurBuffer3.Width / 2, blurBuffer3.Height / 2);

                blitFramebuffer(blurBuffer3, blurBuffer4);
                blitFramebuffer(blurBuffer4, blurBuffer4);

                blurBuffer5.Resize(blurBuffer4.Width / 2, blurBuffer4.Height / 2);

                blitFramebuffer(blurBuffer4, blurBuffer5);
                blitFramebuffer(blurBuffer5, blurBuffer5);

                blurBuffer6.Resize(blurBuffer5.Width / 2, blurBuffer5.Height / 2);

                blitFramebuffer(blurBuffer5, blurBuffer6);
                blitFramebuffer(blurBuffer6, blurBuffer6);

                bloomShader.SetBoolean("u_Blur", false);
                bloomShader.SetBoolean("u_Combine", true);
                bloomShader.SetInt("u_CombineTexture", 5);

                blurBuffer5.Texture.Bind(5);
                blitFramebuffer(blurBuffer6, blurBuffer5);

                blurBuffer4.Texture.Bind(5);
                blitFramebuffer(blurBuffer5, blurBuffer4);
                
                blurBuffer3.Texture.Bind(5);
                blitFramebuffer(blurBuffer4, blurBuffer3);

                blurBuffer2.Texture.Bind(5);
                blitFramebuffer(blurBuffer3, blurBuffer2);

                blurBuffer1.Texture.Bind(5);
                blitFramebuffer(blurBuffer2, blurBuffer1);

                bloomShader.SetBoolean("u_Final", true);
                bloomShader.SetBoolean("u_Combine", false);
                blurBuffer1.Texture.Bind(5);
                blitFramebuffer(MainFrameBuffer, MotionBlur ? MainFrameBuffer : null);

                bloomShader.SetBoolean("u_Final", false);
            }

            if (MotionBlur)
                motionBlur();
            
        }

        private static FrameBuffer motionBlurBuffer = new FrameBuffer(1, 1, FramebufferAttachment.ColorAttachment0, InternalFormat.Rgb16f, PixelFormat.Rgb, PixelType.Float);

        private static void motionBlur()
        {
            motionBlurBuffer.Resize(MainFrameBuffer.Width, MainFrameBuffer.Height);
            motionBlurBuffer.Bind();

            motionBlurShader.Bind();
            motionBlurShader.SetInt("u_Texture", 0);
            motionBlurShader.SetInt("u_CombineTexture", 5);
            motionBlurShader.SetFloat("u_NewPercentage", (DeltaTime * MotionBlurScale).Clamp(0, 1));
            motionBlurBuffer.Texture.Bind(0);
            MainFrameBuffer.Texture.Bind(5);

            motionBlurShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, motionBlurBuffer.Width, motionBlurBuffer.Height, 0, -1f, 1f));
            Viewport.SetViewport(0, 0, motionBlurBuffer.Width, motionBlurBuffer.Height);
            GLDrawing.DrawQuad(Vector2.Zero, new Vector2(motionBlurBuffer.Width, motionBlurBuffer.Height));

            bloomShader.Bind();
            blitFramebuffer(motionBlurBuffer, null);
        }

        private static float DeltaTime;
        public static void Update(float delta)
        {
            DeltaTime = delta;
        }

        public static void Recompile()
        {
            bloomShader.Delete();
            motionBlurShader.Delete();
        }
    }
}
