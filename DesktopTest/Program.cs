using Easy2D;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using System;
using OpenTK.Mathematics;
using System.Threading;
using System.Reflection;

namespace DesktopTest
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowOptions b = WindowOptions.Default;
            b.VSync = false;
            b.Title = "nabo";
            b.Size = new Vector2D<int>(1280, 720);
            b.API = new GraphicsAPI(ContextAPI.OpenGLES, new APIVersion(3, 0));
            b.WindowBorder = WindowBorder.Resizable;
            b.TransparentFramebuffer = false;

            IWindow window = Window.Create(b);

            Graphics graphix = null;

            Vector2 mousePos = Vector2.Zero;
            Easy2D.Texture tex = null;

            Matrix4 projection = Matrix4.Identity;

            Sound sound = null;

            window.Load += () =>
            {
                PostProcessing.Bloom = true;

                var input = window.CreateInput();

                input.Mice[0].Cursor.CursorMode = CursorMode.Normal;

                input.Mice[0].MouseMove += (s, e) =>
                {
                    mousePos.X = e.X;
                    mousePos.Y = e.Y;
                };

                GL.SetGL(Silk.NET.OpenGLES.GL.GetApi(window));

                GL.Instance.Enable(EnableCap.Texture2D);
                GL.Instance.Enable(EnableCap.ScissorTest);
                GL.Instance.Enable(EnableCap.Blend);

                GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                Viewport.SetViewport(0, 0, window.Size.X, window.Size.Y);
                projection = Matrix4.CreateOrthographicOffCenter(0, window.Size.X, window.Size.Y, 0, 0, 1);

                //Easy2D.GL.Instance.ClearColor(0.2f, 0f, 0f, 1f);

                tex = new Easy2D.Texture(File.OpenRead(@"C:\Users\user\Desktop\sliderbody1.png"));

                graphix = new Graphics();
                
                Sound.Init(window.Handle);
                sound = new Sound(File.OpenRead(@"C:\Users\user\Downloads\y2mate.com - 03   Shikakui Uchuu De Matteru Yo flac.mp3"));
                sound.Play(true);
            };

            window.Resize += (s) =>
            {
                Viewport.SetViewport(0, 0, s.X, s.Y);
                projection = Matrix4.CreateOrthographicOffCenter(0, s.X, s.Y, 0, 0, 1);
            };

            int fps = 0;
            double elapsed = 0;
            int fpsFinal = 0;
            //using opengl es 3.0 btw
            window.Render += (s) =>
            {
                GPUScheduler.Update((float)s);
                PostProcessing.Update((float)s);
                fps++;
                elapsed += s;
                GL.Instance.Clear(ClearBufferMask.ColorBufferBit);
                graphix.DrawLine(Vector2.Zero, mousePos, Colors.LightGray, 10f);
                graphix.DrawRectangleCentered(mousePos, new Vector2(128, 128), Colors.Blue, tex);

                graphix.DrawString($"Hello silk.net\nFPS: {fpsFinal}", Font.DefaultFont, mousePos, Colors.White * 2);

                graphix.DrawEllipse(new Vector2(500, 500), 0, 360, 55, 20, Colors.White, tex, 50, false);

                graphix.Projection = projection;
                PostProcessing.Use(new Vector2i(window.Size.X, window.Size.Y), new Vector2i(window.Size.X, window.Size.Y));
                graphix.EndDraw();
                PostProcessing.PresentFinalResult();
                if (elapsed >= 1)
                {
                    fpsFinal = fps;
                    elapsed = 0;
                    fps = 0;
                }
            };
            window.Run();
            Console.WriteLine("Hello World!");
        }
    }

}
