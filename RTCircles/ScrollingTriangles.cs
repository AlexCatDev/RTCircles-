using Easy2D;
using Silk.NET.OpenGLES;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace RTCircles
{
    public class ScrollingTriangles : Drawable
    {
        class Triangle
        {
            public bool RemoveMe { get; private set; }

            private Vector4 color;
            private Vector2 pos;
            public Vector2 Size = new Vector2(80, 160);

            private ScrollingTriangles parent;

            public Triangle(ScrollingTriangles parent)
            {
                pos = new Vector2(RNG.Next(0, 1000), RNG.Next(-1000, 0));

                color = parent.BaseColor * RNG.Next(0.85f, 1.15f);
                color.W = 1.0f;

                Size *= RNG.Next(1f, 2f);

                this.parent = parent;
            }

            public void Update(float delta, Vector2 size)
            {
                float scale = Size.X / 100;
                pos.Y += delta * parent.Speed * scale;

                //The coordinate system Y is flipped inside, since the framebuffer is rendered flipped in the Y direction and cba fixing
                if (pos.Y > 1000 + Size.Y)
                    RemoveMe = true;
            }

            public void Render(Graphics g)
            {
                /*
                var tex = Skin.Star;
                var ar = (float)tex.Width / tex.Height;

                Vector2 s = new Vector2(scaledSize.X, scaledSize.X / ar);

                g.DrawRectangleCentered(pos, s, Colors.White, tex);

                return;
                */
                var triangle = g.VertexBatch.GetTriangleStrip(3);
                
                int slot = g.GetTextureSlot(Easy2D.Texture.WhiteSquare);

                triangle[0].Position = pos;
                triangle[0].Color = color + parent.TriangleAdditive;
                triangle[0].TextureSlot = slot;
                triangle[0].TexCoord = Vector2.Zero;

                triangle[1].Position = pos - Size;
                triangle[1].Color = color + parent.TriangleAdditive;
                triangle[1].TextureSlot = slot;
                triangle[1].TexCoord = Vector2.One;

                triangle[2].Position = pos + new Vector2(Size.X, -Size.Y);
                triangle[2].Color = color + parent.TriangleAdditive;
                triangle[2].TextureSlot = slot;
                triangle[2].TexCoord = Vector2.Zero;

                /*
                g.DrawRectangleCentered(triangle[0].Position, new Vector2(4), Colors.White);

                g.DrawRectangleCentered(triangle[1].Position, new Vector2(4), Colors.Red);

                g.DrawRectangleCentered(triangle[2].Position, new Vector2(4), Colors.Green);
                */
            }
        }

        public Vector2 Position;
        public float Radius;

        public float Speed = 55;

        public Vector4 BaseColor = Colors.Pink;
        public Vector4? BackgroundColor;
        public Vector4 TriangleAdditive;

        private int triangleCount;

        private FrameBuffer triangleFramebuffer = new FrameBuffer(1, 1, textureComponentCount: InternalFormat.Rgba, pixelFormat: PixelFormat.Rgba, pixelType: PixelType.UnsignedByte);
        private List<Triangle> triangles = new List<Triangle>();

        public ScrollingTriangles(int TriangleCount)
        {
            triangleCount = TriangleCount;
        }

        public override Rectangle Bounds => new Rectangle();

        private Graphics graphics;

        public override void Render(Graphics g)
        {
            if (graphics is null)
                graphics = new Graphics();

            //Fixed resolution for now..
            triangleFramebuffer.Resize(1000, 1000);

            graphics.DrawInFrameBuffer(triangleFramebuffer, () =>
            {
                graphics.DrawRectangle(Vector2.Zero, triangleFramebuffer.Texture.Size, BackgroundColor ?? BaseColor);
                for (int i = triangles.Count - 1; i >= 0; i--)
                {
                    triangles[i].Render(graphics);
                }
            });
            g.DrawEllipse(Position, 0, 360, Radius, 0, Colors.White, triangleFramebuffer.Texture);
        }

        public override void Update(float delta)
        {
            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                triangles[i].Update(delta, new Vector2(Radius * 2));
                if (triangles[i].RemoveMe)
                {
                    triangles.RemoveAt(i);
                    triangleCount++;
                }
            }

            for (int i = 0; i < triangleCount; i++)
            {
                triangles.Add(new Triangle(this));
            }
            triangleCount = 0;
        }
    }
}
