using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ParticleMadness
{
    public struct Vertex
    {
        //public float X, Y;
        //public float R, G, B, A;
        public Vector2 Position;
        public Vector4 Color;
    }

    public struct Particle
    {
        public Vector2 Position;
        public static Vector2 Size;
        public Vector4 Color;

        private Vector2 velocity;

        public Particle()
        {
            velocity.X = RNG.Next(-500, 500);
            velocity.Y = RNG.Next(-500, 500);

            Color.W = 1.0f;

            Color.X = RNG.Next(0f, 1f);
            Color.Y = RNG.Next(0f, 1f);
            Color.Z = RNG.Next(0f, 1f);

            Position = Vector2.Zero;
        }

        public void Update(float delta)
        {
            Position += velocity*delta;

            if (Position.X >= Game.Width - Size.X || Position.X <= 0)
                velocity.X *= -1;


            if (Position.Y >= Game.Height - Size.Y || Position.Y <= 0)
                velocity.Y *= -1;
            
        }
    }

    public unsafe class Game : Easy2D.Game.Game
    {
        private Matrix4 projection;
        public static int Width, Height;
        public static Vector2 Center;

        private VertexArray<Vertex> vertexArray = new VertexArray<Vertex>();

        private Easy2D.Shader shader = new Easy2D.Shader();

        private Vertex* vertexVBO;
        private Vertex* VBOPtr;

        private volatile uint vertexRenderCount;

        private uint vbo;

        private Particle[] particles = new Particle[960_000];

        private StreamingPrimitiveBuffer<Vertex> penis;

        public override void OnImportFile(string path)
        {
           
        }

        public override void OnLoad()
        {
            Particle.Size = new Vector2(2);

            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new Particle();
            }

            PrintFPS = true;

            penis = new StreamingPrimitiveBuffer<Vertex>((uint)particles.Length * 4, (uint)particles.Length * 6);

            /*
            uint capacity = (uint)(sizeof(Vertex) * 6 * particles.Length);

            vbo = GL.Instance.GenBuffer();
            GL.Instance.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            MapBufferAccessMask accessMask = MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit;
            GL.Instance.TryGetExtension<Silk.NET.OpenGLES.Extensions.EXT.ExtBufferStorage>(out var extBufferStorage);


            extBufferStorage.BufferStorage(BufferStorageTarget.ArrayBuffer, capacity, null, BufferStorageMask.DynamicStorageBit | (BufferStorageMask)accessMask);

            vertexVBO = (Vertex*)GL.Instance.MapBufferRange(BufferTargetARB.ArrayBuffer, 0, capacity, accessMask);
            VBOPtr = vertexVBO;
            */

            shader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Shader.vert"));
            shader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Shader.frag"));
        }

        public void DrawRectangle(Vector2 position, Vector2 size, Vector4 color)
        {
            unsafe
            {
                VBOPtr->Position = position;
                VBOPtr->Color = color;

                VBOPtr++;

                VBOPtr->Position = position + size;
                VBOPtr->Color = color;

                VBOPtr++;

                VBOPtr->Position = new Vector2(position.X, position.Y + size.Y);
                VBOPtr->Color = color;

                VBOPtr++;

                VBOPtr->Position = position;
                VBOPtr->Color = color;

                VBOPtr++;

                VBOPtr->Position = new Vector2(position.X + size.X, position.Y);
                VBOPtr->Color = color;

                VBOPtr++;

                VBOPtr->Position = position + size;
                VBOPtr->Color = color;

                VBOPtr++;
            }

            vertexRenderCount += 6;

            // first triangle
            /*
            VBOPtr[0] = x;
            VBOPtr[1] = y;

            VBOPtr[2] = x + width;
            VBOPtr[3] = y + height;

            VBOPtr[4] = x;
            VBOPtr[5] = y + height;

            // second triangle
            VBOPtr[6] = x;
            VBOPtr[7] = y;

            VBOPtr[8] = x + width;
            VBOPtr[9] = y;

            VBOPtr[10] = x + width;
            VBOPtr[11] = y + height;
            */

            /*
            quad[0].Position = position;
            quad[0].Color = color;

            quad[1].Position = new Vector2(position.X + size.X, position.Y);
            quad[1].Color = color;

            quad[2].Position = position + size;
            quad[2].Color = color;

            quad[3].Position = new Vector2(position.X, position.Y + size.Y);
            quad[3].Color = color;
            */
        }

        public void DrawRectangle2(Vector2 position, Vector2 size, Vector4 color, uint offset)
        {
            var start = vertexVBO + offset;
            start->Position = position;
            start->Color = color;

            start++;

            start->Position = position + size;
            start->Color = color;

            start++;

            start->Position = new Vector2(position.X, position.Y + size.Y);
            start->Color = color;

            start++;

            start->Position = position;
            start->Color = color;

            start++;

            start->Position = new Vector2(position.X + size.X, position.Y);
            start->Color = color;

            start++;

            start->Position = position + size;
            start->Color = color;

            vertexRenderCount += 6;
        }

        private void drawSingleThreaded()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Update((float)RenderDeltaTime);
                DrawRectangle(particles[i].Position, Particle.Size, particles[i].Color);
            }
        }

        /*
        public override void OnRender(double delta)
        {
            drawSingleThreaded();

            vertexArray.Bind();

            shader.Bind();
            shader.SetMatrix("u_Projection", projection);
            GL.Instance.DrawArrays(PrimitiveType.Triangles, 0, (uint)(VBOPtr - vertexVBO));
            //GL.Instance.DrawArrays(PrimitiveType.Triangles, 0, vertexRenderCount);
            //Console.WriteLine(vertexRenderCount);

            vertexRenderCount = 0;
            VBOPtr = vertexVBO;
        }
        */

        public override void OnRender(double delta)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Update((float)delta);

                var quad = penis.GetQuad();

                quad[0].Position = particles[i].Position;
                quad[0].Color = particles[i].Color;

                quad[1].Position = particles[i].Position + new Vector2(0, Particle.Size.X);
                quad[1].Color = particles[i].Color;

                quad[2].Position = particles[i].Position + Particle.Size;
                quad[2].Color = particles[i].Color;

                quad[3].Position = particles[i].Position + new Vector2(Particle.Size.Y, 0);
                quad[3].Color = particles[i].Color;
            }

            shader.Bind();
            shader.SetMatrix("u_Projection", projection);
            penis.Draw();
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
}
