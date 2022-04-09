using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Easy2D
{
    /// <summary>
    /// TODO: REDO THIS MESS
    /// </summary>
    public class Texture : GLObject, IEqualityComparer<Texture>
    {
        public static int TextureCount { get; private set; }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public InternalFormat InternalFormat { get; private set; }
        public PixelFormat PixelFormat { get; private set; }
        public PixelType PixelType { get; private set; }

        public Vector2 Size => new Vector2(Width, Height);

        public bool GenerateMipmaps { get; set; } = true;

        public static Texture WhiteSquare { get; private set; }
        public static Texture WhiteCircle { get; private set; }
        public static Texture WhiteFlatCircle { get; private set; }

        public TextureMinFilter MinFilter { get; set; } = TextureMinFilter.Linear;
        public TextureMagFilter MagFilter { get; set; } = TextureMagFilter.Linear;

        public bool ImageDoneUploading { get; private set; } 

        public bool UseAsyncLoading { get; set; } = true;

        private Stream stream;

        static Texture()
        {
            WhiteSquare = new Texture(Utils.GetInternalResource("Textures.square.png"));
            WhiteCircle = new Texture(Utils.GetInternalResource("Textures.circle.png"));
            WhiteFlatCircle = new Texture(Utils.GetInternalResource("Textures.flatcircle.png"));
        }

        public Texture(int width, int height, InternalFormat componentCount = InternalFormat.Rgba, 
            PixelFormat pixelFormat = PixelFormat.Rgba, PixelType pixelType = PixelType.UnsignedByte)
        {
            Width = width;
            Height = height;

            InternalFormat = componentCount;
            PixelFormat = pixelFormat;
            PixelType = pixelType;
        }

        public Texture(Stream stream)
        {
            this.stream = stream;

            InternalFormat = InternalFormat.Rgba;
            PixelFormat = PixelFormat.Rgba;
            PixelType = PixelType.UnsignedByte;
        }

        public Texture(Image<Rgba32> image, bool genMips)
        {
            InternalFormat = InternalFormat.Rgba;
            PixelFormat = PixelFormat.Rgba;
            PixelType = PixelType.UnsignedByte;

            Width = image.Width;
            Height = image.Height;

            bool status = image.TryGetSinglePixelSpan(out Span<Rgba32> imageData);

            if (!status)
                throw new Exception("Couldnt get pixel span?");

            Handle = GL.Instance.GenTexture();

            bind(null);

            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)MinFilter);
            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)MagFilter);

            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            unsafe
            {
                fixed (Rgba32* imageDataPtr = imageData)
                {
                    //GL.Instance.CompressedTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedRgba, (uint)Width, (uint)Height, 0, (uint)(imageData.Length * sizeof(Rgba32)), imageDataPtr);
                    GL.Instance.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, (uint)Width, (uint)Height, 0, PixelFormat, PixelType, imageDataPtr);
                }
            }

            if (genMips)
            {
                GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);

                //Gen mipmap
                GL.Instance.GenerateMipmap(TextureTarget.Texture2D);
            }

            TextureCount++;
        }

        /// <summary>
        /// Get the memory usage of the texture
        /// </summary>
        /// <returns>The memory usage in megabytes rounded to two decimal points</returns>
        public double GetMemoryUsage()
        {
            if (IsInitialized)
                return Math.Round((Width * Height * 4) / 1048576d, 2);
            else
                return 0;
        }

        public void SetPixels<T>(int x, int y, int width, int height, T[,] pixels) where T : unmanaged
        {
            Bind();
            unsafe
            {
                fixed (T* p = pixels)
                {
                    GL.Instance.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, (uint)width, (uint)height, PixelFormat, PixelType, p);
                }
            }
        }

        public void Resize(int width, int height)
        {
            if (Width == width && Height == height)
                return;

            Width = width;
            Height = height;

            if (IsInitialized)
            {
                Bind();
                unsafe
                {
                    GL.Instance.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, (uint)width, (uint)height, 0, PixelFormat, PixelType, null);
                }
            }
        }

        public bool Equals(Texture x, Texture y)
        {
            return x.Handle == y.Handle;
        }

        public int GetHashCode([DisallowNull] Texture obj)
        {
            return Handle.GetHashCode();
        }

        public void SetImage(Stream stream)
        {
            this.stream = stream;
            ImageDoneUploading = false;

            if (IsInitialized)
            {
                if (UseAsyncLoading)
                {
                    GPUSched.Instance.EnqueueAsync(() => {
                        return (true, Image.Load<Rgba32>(stream));
                    }, (image) => {

                        uploadImage(image);
                    });
                }
                else
                {
                    var image = Image.Load<Rgba32>(stream);
                    uploadImage(image);
                }
            }
        }

        private void uploadImage(Image<Rgba32> image)
        {
            //Upload image on the scheduler thread (Graphics Thread)
            bind(null);
            Width = image.Width;
            Height = image.Height;

            image.TryGetSinglePixelSpan(out Span<Rgba32> imageData);

            unsafe
            {
                fixed (Rgba32* imageDataPtr = imageData)
                {
                    //GL.Instance.CompressedTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedRgba, (uint)Width, (uint)Height, 0, (uint)(imageData.Length * sizeof(Rgba32)), imageDataPtr);
                    GL.Instance.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, (uint)Width, (uint)Height, 0, PixelFormat, PixelType, imageDataPtr);
                }
            }

            if (GenerateMipmaps)
            {
                GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapNearest);

                //Gen mipmap
                GL.Instance.GenerateMipmap(TextureTarget.Texture2D);
            }

            Utils.Log($"Loaded texture [{Handle}] {Width}x{Height} {GetMemoryUsage()} mb  Mipmaps: {GenerateMipmaps} Async: {UseAsyncLoading} Stream: {stream is null}", LogLevel.Debug);
            image.Dispose();

            ImageDoneUploading = true;
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenTexture();

            bind(slot);

            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.Instance.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            //If no path was supplied just use empty texture, else load texture from file
            if (stream is null)
            {
                if (Width < 0 || Height < 0)
                    //throw new Exception($"Can't have a negative sized texture! {Width}x{Height}");
                    Utils.Log($"TEXTURE ERROR: InvalidSize: {Width}x{Height}", LogLevel.Error);

                unsafe
                {
                    GL.Instance.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, (uint)Width, (uint)Height, 0, PixelFormat, PixelType, null);
                }
                Utils.Log($"Created texture: {Handle} {Width}x{Height} {GetMemoryUsage()} mb", LogLevel.Debug);
            }
            else
            {
                stream.Position = 0;
                SetImage(stream);
            }

            TextureCount++;
        }

        protected override void bind(int? slot)
        {
            if (slot.HasValue)
                GL.Instance.ActiveTexture(TextureUnit.Texture0 + slot.Value);

            GL.Instance.BindTexture(TextureTarget.Texture2D, Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteTexture(Handle);
            Handle = uint.MaxValue;
            TextureCount--;

            ImageDoneUploading = false;
        }

        public Rectangle GetTextureRect(Rectangle rectangle)
        {
            return new Rectangle(rectangle.X / Width, rectangle.Y / Height, rectangle.Width / Width, rectangle.Height / Height);
        }
    }
}
