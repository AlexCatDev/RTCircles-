using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadTest
{

    public class BatchRenderer
    {
        private static int s_MaxTextureSlots;
        private static int[] s_TextureSlots; 

        static BatchRenderer()
        {
            if (GL.Instance == null)
                throw new Exception("GL isn't initialized");

            s_MaxTextureSlots = GL.MaxTextureSlots;
            s_TextureSlots = new int[s_MaxTextureSlots];

            for (byte i = 0; i < s_MaxTextureSlots; i++)
            {
                s_TextureSlots[i] = i;
            }
        }

        public struct Vertex3D
        {
            public Vector3 Position;
            public Vector2 TextureCoord;
            public Vector4 Color;
            public byte TextureSlot;
        }

        private readonly Easy2D.Shader shader = new();
        private readonly VertexArray<Vertex3D> vao = new();
        public readonly UnsafePrimitiveBatch<Vertex3D> VertexBatch;

        public Matrix4 Projection;

        public BatchRenderer(int vertexCapacity = 400_000, int indexCapacity = 600_000)
        {
            VertexBatch = new UnsafePrimitiveBatch<Vertex3D>(vertexCapacity, indexCapacity);

            shader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Shaders.test.vert"));
            shader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Shaders.test.frag"));
        }

        private byte textureBindIndex = 0;
        private Dictionary<Easy2D.Texture, int> texturesToBind = new Dictionary<Easy2D.Texture, int>();
        public int GetTextureSlot(Easy2D.Texture? texture)
        {
            if (texture == null)
                texture = Easy2D.Texture.WhiteSquare;

            if (texturesToBind.TryGetValue(texture, out int slot))
            {
                return slot;
            }
            else
            {
                if (texturesToBind.Count == GL.MaxTextureSlots)
                {
                    Draw();
                    Utils.Log($"Renderer flushed because of texture limit: {GL.MaxTextureSlots}", LogLevel.Debug);
                }

                int slotToAdd = textureBindIndex;
                texturesToBind.Add(texture, textureBindIndex);
                textureBindIndex++;
                return slotToAdd;
            }
        }

        public void Draw()
        {
            shader.Bind();
            shader.SetIntArray("u_Projection", s_TextureSlots);

            VertexBatch.Draw();
        }
    }

    public static class BatchRendererExentsions
    {
        public static void PutQuad(this BatchRenderer renderer)
        {

        }
    }


    public class Game : MultiThreadedGameBase
    {
        private FastGraphics graphics;
        private Vector2 quadSize = new Vector2(32);
        private Vector2 quadPosition;
        
        public override void OnLoad()
        {
            VSync = false;
            Utils.WriteToConsole = true;

            graphics = new FastGraphics();
            //graphics.VertexBatch.Resizable = false;
        }

        public override void OnOpenFile(string fullpath)
        {
           
        }

        public override void OnRender()
        {
            RenderScheduler.Enqueue(() =>
            {
                graphics.DrawString($"FPS: {FPS} UPS: {UPS}\nDrawCalls: {GL.DrawCalls} Quads: {graphics.IndicesDrawn / 4}", Font.DefaultFont, new Vector2(10), Colors.White, 0.5f);
                GL.ResetStatistics();
                graphics.ResetStatistics();

                graphics.DrawRectangleCentered(quadPosition, quadSize, Colors.White);
            });

            RenderScheduler.Enqueue(() =>
            {
                graphics.EndDraw();
            });
        }

        public override void OnResize(int width, int height)
        {
            Viewport.SetViewport(0, 0, width, height);
            graphics.Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
        }

        private int direction = 1;
        public override void OnUpdate()
        {
            quadPosition = Input.MousePosition;
            quadSize += new Vector2(250) * (float)DeltaTime * direction;

            if (quadSize.X > 256)
                direction = -1;
            else if (quadSize.X < 64)
                direction = 1;
        }
    }
}
