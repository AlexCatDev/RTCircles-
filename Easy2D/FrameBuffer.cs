using Silk.NET.OpenGLES;
using System;

namespace Easy2D
{
    public class FrameBuffer : GLObject
    {
        public static WeakReference<FrameBuffer> DefaultFrameBuffer { get; private set; } = new WeakReference<FrameBuffer>(null);

        public Texture Texture { get; private set; }

        public GLEnum Status { get; private set; }

        public FramebufferAttachment Attachment { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>
        /// Will not bind
        /// </summary>
        /// <param name="width">The width of the framebuffer</param>
        /// <param name="height">The height of the framebuffer</param>
        public FrameBuffer(int width, int height,
            FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0,
            InternalFormat textureComponentCount = InternalFormat.Rgba,
            PixelFormat pixelFormat = PixelFormat.Rgba,
            PixelType pixelType = PixelType.UnsignedByte)
        {
            Width = width;
            Height = height;

            Attachment = attachment;

            Texture = new Texture(Width, Height, textureComponentCount, pixelFormat, pixelType);
        }

        /// <summary>
        /// Will bind the underlying texture if initialised.
        /// Will only resize if theres a size difference 
        /// The width and height will not go under 1 even if 0 is inputed
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void EnsureSize(float fWidth, float fHeight)
        {
            int width = (int)MathF.Max(fWidth, 1);
            int height = (int)MathF.Max(fHeight, 1);

            if (width != Width || height != Height)
            {
                Width = width;
                Height = height;

                Texture.Resize(width, height);
            }
        }

        /// <summary>
        /// Actually just binds the default framebuffer
        /// </summary>
        public void Unbind()
        {
            //GL.Instance.InvalidateFramebuffer(FramebufferTarget.Framebuffer, new ReadOnlySpan<GLEnum>(new[] { (GLEnum)Attachment }));
            BindDefault();
        }

        public static void BindDefault(bool forceUseScreen = false)
        {
            if (DefaultFrameBuffer.TryGetTarget(out FrameBuffer defaultFrameBuffer) && !forceUseScreen)
                defaultFrameBuffer.Bind();
            else
                GL.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenFramebuffer();

            bind(null);

            Texture.Bind(0);

            GL.Instance.FramebufferTexture2D(FramebufferTarget.Framebuffer, Attachment, TextureTarget.Texture2D, Texture.Handle, 0);

            Status = GL.Instance.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            Utils.Log($"Created framebuffer {Handle} : {Status}", Status == GLEnum.FramebufferComplete ? LogLevel.Info : LogLevel.Error);
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        }

        protected override void delete()
        {
            //Native symbol not found? Multithreading bug
            GL.Instance.DeleteFramebuffer(Handle);
            Handle = uint.MaxValue;
            Texture.Delete();
        }
    }
}
