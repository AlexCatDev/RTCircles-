using Easy2D;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;

namespace ParticleMadness.Desktop
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowOptions b = WindowOptions.Default;
            b.VSync = false;
            b.Title = "ParticleMadness";
            b.Size = new Vector2D<int>(1280, 720);
            b.API = new GraphicsAPI(ContextAPI.OpenGLES, new APIVersion(3, 0));
            b.WindowBorder = WindowBorder.Resizable;
            b.TransparentFramebuffer = false;
            b.ShouldSwapAutomatically = false;
            b.IsContextControlDisabled = true;

            IWindow window = Window.Create(b);

            var game = new Game();

            game.View = window;

            bool shiftDown = false;

            window.Load += () =>
            {
                var input = window.CreateInput();

                input.Keyboards[0].KeyDown += (s, e, x) =>
                {
                    if (e == Key.ShiftRight)
                        shiftDown = true;

                    if (e == Key.Backspace && shiftDown)
                    {
                        window.WindowState = window.WindowState == WindowState.Fullscreen ? WindowState.Normal : WindowState.Fullscreen;
                    }
                };

                input.Keyboards[0].KeyUp += (s, e, x) =>
                {
                    if (e == Key.ShiftRight)
                        shiftDown = false;
                };

                GL.SetGL(Silk.NET.OpenGLES.GL.GetApi(window));

                GL.Instance.Enable(EnableCap.Texture2D);
                GL.Instance.Enable(EnableCap.ScissorTest);
                GL.Instance.Enable(EnableCap.Blend);

                GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                Sound.Init(window.Handle);

                game.Load(input);
                game.OnResize(window.Size.X, window.Size.Y);
            };

            window.FileDrop += (files) =>
            {
                foreach (var file in files)
                {
                    game.OnImportFile(file);
                }
            };

            window.Resize += (s) =>
            {
                game.OnResize(s.X, s.Y);
            };

            window.Render += (s) =>
            {
                game.Render(s);
            };

            window.Update += (s) =>
            {
                game.Update(s);
            };

            window.Run();
        }
    }

}
