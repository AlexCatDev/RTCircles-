using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadTest
{
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
