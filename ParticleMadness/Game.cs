using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ParticleMadness
{
    /*
    public struct Vertex
    {
        public Vector2 Position;
        public Vector2 TextureUV;
        public Vector2 Velocity;
        public Vector4 Color;
    }

    public class FrameBuffer : GLObject
    {
        public static WeakReference<FrameBuffer> DefaultFrameBuffer { get; private set; } = new WeakReference<FrameBuffer>(null);

        public (Easy2D.Texture Texture, FramebufferAttachment Attachment)[] Textures { get; private set; }

        public GLEnum Status { get; private set; }

        /// <summary>
        /// Will not bind
        /// </summary>
        /// <param name="width">The width of the framebuffer</param>
        /// <param name="height">The height of the framebuffer</param>
        public FrameBuffer(params (Easy2D.Texture texture, FramebufferAttachment attachment)[] textures)
        {
            Textures = textures;
        }

        /// <summary>
        /// Will bind the underlying texture if initialised.
        /// Will only resize if theres a size difference 
        /// The width and height will not go under 1 even if 0 is inputed
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(float fWidth, float fHeight)
        {
            int width = (int)MathF.Max(fWidth, 1);
            int height = (int)MathF.Max(fHeight, 1);

            foreach (var item in Textures)
            {
                item.Texture.Resize(width, height);
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

            unsafe
            {
                DrawBufferMode* drawBufferModes = stackalloc DrawBufferMode[Textures.Length];
                for (int i = 0; i < Textures.Length; i++)
                {
                    drawBufferModes[i] = (DrawBufferMode)Textures[i].Attachment;
                }
                GL.Instance.DrawBuffers((uint)Textures.Length, drawBufferModes);
            }

            foreach (var item in Textures)
            {
                item.Texture.Bind(0);
                GL.Instance.FramebufferTexture2D(FramebufferTarget.Framebuffer, item.Attachment, TextureTarget.Texture2D, item.Texture.Handle, 0);

                Status = GL.Instance.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                Utils.Log($"Created framebuffer {Handle} : {Status}", Status == GLEnum.FramebufferComplete ? LogLevel.Success : LogLevel.Error);
            }
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteFramebuffer(Handle);
            foreach (var item in Textures)
            {
                item.Texture.Delete();
            }
            Handle = uint.MaxValue;
        }
    }

    public unsafe class Game : Easy2D.Game.Game
    {
        private Matrix4 projection;
        public static int Width, Height;
        public static Vector2 Center;

        private VertexArray<Vertex> vertexArray = new VertexArray<Vertex>();

        private GLBuffer<Vertex> buffer = new GLBuffer<Vertex>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StreamDraw, 6000);

        private Easy2D.Shader shader = new Easy2D.Shader();
        private Easy2D.Shader shader2 = new Easy2D.Shader();

        private ParticleMadness.FrameBuffer framebuffer;

        private ParticleMadness.FrameBuffer framebuffer2;

        private Easy2D.Texture outputImage, outputVelocity;

        private Easy2D.Texture finalImage;

        private Easy2D.Texture testTexture;

        private Graphics Graphics;

        public override void OnImportFile(string path)
        {

        }

        public override void OnLoad()
        {
            Graphics = new Graphics();

            testTexture = new Easy2D.Texture(File.OpenRead(@"C:\Users\user\Desktop\cock.jpg"));

            outputImage = new Easy2D.Texture(Width, Height);
            outputVelocity = new Easy2D.Texture(Width, Height, InternalFormat.RG16f, PixelFormat.RG, PixelType.Float);

            framebuffer = new FrameBuffer((outputImage, FramebufferAttachment.ColorAttachment0), (outputVelocity, FramebufferAttachment.ColorAttachment1));

            finalImage = new Easy2D.Texture(Width, Height, InternalFormat.Rgb, PixelFormat.Rgb);

            framebuffer2 = new FrameBuffer((finalImage, FramebufferAttachment.ColorAttachment0));

            framebuffer2.Bind();

            shader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Shader.vert"));
            shader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Shader.frag"));

            shader2.AttachShader(ShaderType.VertexShader, Utils.GetResource("Shader.vert"));
            shader2.AttachShader(ShaderType.FragmentShader, Utils.GetResource("ShaderPass.frag"));
        }

        public void DrawRectangle(Vector2 position, Vector2 size, Vector4 color, Vector2 velocity)
        {
            Vertex[] quad = new Vertex[6];

            quad[0].Position = position;
            quad[0].Color = color;
            quad[0].Velocity = velocity;
            quad[0].TextureUV = new Vector2(0, 0);

            quad[1].Position = position + size;
            quad[1].Color = color;
            quad[1].Velocity = velocity;
            quad[1].TextureUV = new Vector2(1, 1);

            quad[2].Position = new Vector2(position.X, position.Y + size.Y);
            quad[2].Color = color;
            quad[2].Velocity = velocity;
            quad[2].TextureUV = new Vector2(0, 1);

            quad[3].Position = position;
            quad[3].Color = color;
            quad[3].Velocity = velocity;
            quad[3].TextureUV = new Vector2(0, 0);

            quad[4].Position = new Vector2(position.X + size.X, position.Y);
            quad[4].Color = color;
            quad[4].Velocity = velocity;
            quad[4].TextureUV = new Vector2(1, 0);

            quad[5].Position = position + size;
            quad[5].Color = color;
            quad[5].Velocity = velocity;
            quad[5].TextureUV = new Vector2(1, 1);

            buffer.UploadData(0, 6, quad);
        }

        Vector2 lastMousePos;

        Vector2 lastDiff;

        public override void OnRender(double delta)
        {
            Vector2 diff = Input.MousePosition - lastMousePos;
            lastMousePos = Input.MousePosition;

            diff.X = MathF.Abs(diff.X);
            diff.Y = MathF.Abs(diff.Y);

            lastDiff = Vector2.Lerp(lastDiff, diff, 10f * (float)delta);

            DrawRectangle(Input.MousePosition, new Vector2(96), Colors.White, new Vector2(0, -10));

            vertexArray.Bind();

            testTexture.Bind(0);

            shader.Bind();
            shader.SetMatrix("u_Projection", projection);
            shader.SetInt("u_Texture", 0);

            framebuffer.Resize(Width, Height);
            framebuffer.Bind();

            Viewport.SetViewport(0, 0, Width, Height);
            GL.Instance.Clear(ClearBufferMask.ColorBufferBit);
            GL.Instance.DrawArrays(PrimitiveType.Triangles, 0, 6);

            framebuffer2.Resize(Width, Height);
            framebuffer2.Bind();
            Viewport.SetViewport(0, 0, Width, Height);
            GL.Instance.Clear(ClearBufferMask.ColorBufferBit);

            outputImage.Bind(0);
            outputVelocity.Bind(1);

            shader2.Bind();
            shader2.SetMatrix("u_Projection", projection);
            shader2.SetInt("u_FrameTexture", 0);
            shader2.SetInt("u_VelocityTexture", 1);
            //Horizontal Pass
            shader2.SetVector("u_Direction", new Vector2(1, 0));

            DrawRectangle(Vector2.Zero, new Vector2(Width, Height), Colors.White, Vector2.Zero);
            GL.Instance.DrawArrays(PrimitiveType.Triangles, 0, 6);

            finalImage.Bind(0);
            //Vertical Pass
            shader2.SetVector("u_Direction", new Vector2(0, 1));
            DrawRectangle(Vector2.Zero, new Vector2(Width, Height), Colors.White, Vector2.Zero);
            GL.Instance.DrawArrays(PrimitiveType.Triangles, 0, 6);
            

            framebuffer.Unbind();
            Viewport.SetViewport(0, 0, Width, Height);

            Graphics.Projection = projection;
            Graphics.DrawRectangle(Vector2.Zero, new Vector2(Width, Height), Colors.White, finalImage, new Rectangle(0, 1, 1, -1), true);
            Graphics.DrawRectangleCentered(Input.MousePosition, new Vector2(32), Colors.Red);
            Graphics.EndDraw();
        }

        public override void OnResize(int width, int height)
        {
            Viewport.SetViewport(0, 0, width, height);
            projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            Width = width;
            Height = height;
            Center = new Vector2(Width, Height) / 2;
        }

        public override void OnUpdate(double delta)
        {
            
        }
    }
    */

    public class Game : Easy2D.Game.Game
    {
        private Matrix4 projection;
        private Graphics g;

        public override void OnImportFile(string path)
        {

        }

        public override void OnLoad()
        {
            g = new Graphics();

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                points.Add(Input.MousePosition);
            };

            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                if (e == Silk.NET.Input.Key.Backspace)
                    points.Clear();
            };
        }

        private List<Vector2> points = new List<Vector2>();

        public (Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight) DrawLine(Vector2 startPosition, Vector2 endPosition, Vector4 color1, Vector4 color2, float thickness, int textureSlot)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topRight = new Vector2(startPosition.X + perpen.X * thickness,
                startPosition.Y + perpen.Y * thickness);

            Vector2 bottomRight = startPosition;

            Vector2 topLeft = new Vector2(endPosition.X + perpen.X * thickness,
                endPosition.Y + perpen.Y * thickness);

            Vector2 bottomLeft = endPosition;

            Vector2 tr = topRight;
            Vector2 tl = topLeft;

            var quad = g.VertexBatch.GetQuad();

            quad[0].Color = color1;
            quad[0].Position = topRight;
            quad[0].TexCoord = new Vector2(0, 0);
            quad[0].TextureSlot = textureSlot;

            quad[1].Color = color1;
            quad[1].Position = bottomRight;
            quad[1].TexCoord = new Vector2(1, 0);
            quad[1].TextureSlot = textureSlot;

            quad[2].Color = color1;
            quad[2].Position = bottomLeft;
            quad[2].TexCoord = new Vector2(1, 1);
            quad[2].TextureSlot = textureSlot;

            quad[3].Color = color1;
            quad[3].Position = topLeft;
            quad[3].TexCoord = new Vector2(0, 1);
            quad[3].TextureSlot = textureSlot;

            topRight = startPosition;

            bottomRight = new Vector2(startPosition.X - perpen.X * thickness,
                                             startPosition.Y - perpen.Y * thickness);

            topLeft = endPosition;

            bottomLeft = new Vector2(endPosition.X - perpen.X * thickness,
                                             endPosition.Y - perpen.Y * thickness);

            Vector2 br = bottomRight;
            Vector2 bl = bottomLeft;

            quad = g.VertexBatch.GetQuad();

            quad[0].Color = color1;
            quad[0].Position = topRight;
            quad[0].TexCoord = new Vector2(1, 1);
            quad[0].TextureSlot = textureSlot;

            quad[1].Color = color1;
            quad[1].Position = bottomRight;
            quad[1].TexCoord = new Vector2(0, 0);
            quad[1].TextureSlot = textureSlot;

            quad[2].Color = color1;
            quad[2].Position = bottomLeft;
            quad[2].TexCoord = new Vector2(0, 1);
            quad[2].TextureSlot = textureSlot;

            quad[3].Color = color1;
            quad[3].Position = topLeft;
            quad[3].TexCoord = new Vector2(1, 0);
            quad[3].TextureSlot = textureSlot;

            return (tr, tl, bl, br);
        }

        private static Easy2D.Texture sliderTex = new Easy2D.Texture(File.OpenRead(@"C:\Users\user\Desktop\slidergradient.png"));

        private List<Vector2> vertices = new List<Vector2>();

        public override void OnRender(double delta)
        {
            /*
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            Vector2 topLeft = new Vector2(startPosition.X + perpen.X * thickness / 2f,
                startPosition.Y + perpen.Y * thickness / 2f);

            Vector2 topRight = new Vector2(startPosition.X - perpen.X * thickness / 2f,
                startPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomLeft = new Vector2(endPosition.X - perpen.X * thickness / 2f,
                endPosition.Y - perpen.Y * thickness / 2f);

            Vector2 bottomRight = new Vector2(endPosition.X + perpen.X * thickness / 2f,
                endPosition.Y + perpen.Y * thickness / 2f);
            */
            Utils.Benchmark(() =>
            {
                Polyline.GetStrokeGeometry(points, Polyline.JointStyle.Round, Polyline.CapStyle.Round, 50f, vertices);

                for (int i = 0; i < vertices.Count; i++)
                {
                    g.DrawRectangleCentered(vertices[i], new Vector2(4), new Vector4(0f, 1f, 0f, 0.1f));
                }

                for (int i = 0; i < vertices.Count; i+=3)
                {
                    var triangle = g.VertexBatch.GetTriangle();

                    triangle[0].Color = new Vector4(1f, 1f, 1f, 0.2f);
                    triangle[0].TextureSlot = 0;
                    triangle[0].TexCoord = Vector2.Zero;
                    triangle[0].Position = vertices[i];

                    triangle[1].Color = new Vector4(1f, 1f, 1f, 0.2f);
                    triangle[1].TextureSlot = 0;
                    triangle[1].TexCoord = Vector2.Zero;
                    triangle[1].Position = vertices[i + 1];

                    triangle[2].Color = new Vector4(1f, 1f, 1f, 0.2f);
                    triangle[2].TextureSlot = 0;
                    triangle[2].TexCoord = Vector2.Zero;
                    triangle[2].Position = vertices[i + 2];
                }

                for (int i = 0; i < points.Count; i++)
                {
                    g.DrawRectangleCentered(points[i], new Vector2(8), Colors.Red);
                }
                
                g.Projection = projection;
                g.EndDraw();
            }, $"Draw : {vertices.Count}");
        }

        public override void OnResize(int width, int height)
        {
            Viewport.SetViewport(0, 0, width, height);
            projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        }

        public override void OnUpdate(double delta)
        {

        }
    }
}
