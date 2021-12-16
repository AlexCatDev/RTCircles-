
using Android.App;
using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Sdl.Android;

namespace AndroidTest
{
    [Activity(Label = "@string/app_name", MainLauncher = true, ScreenOrientation = Android.Content.PM.ScreenOrientation.ReverseLandscape)]
    public class MainActivity : SilkActivity
    {
        private Graphics graphics;
        private IView view;
        private Matrix4 projection;

        protected override void OnRun()
        {
            //PostProcessing.Bloom = true;
            //PostProcessing.MotionBlur = true;

            ViewOptions options = ViewOptions.Default;
            options.Samples = 0;
            options.VSync = false;
            options.API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default, new APIVersion(3, 0));

            view = Silk.NET.Windowing.Window.GetView(options);

            view.Load += View_Load;
            view.Render += View_Render;
            view.Update += View_Update;
            
            view.Resize += (s) =>
            {
                Viewport.SetViewport(0, 0, s.X, s.Y);
                projection = Matrix4.CreateOrthographicOffCenter(0, s.X, s.Y, 0, -1, 1);
                System.Console.WriteLine($"Resize: Width: {s.X} Height: {s.Y}");
            };

            view.Run();
        }

        private Vector2 mousePos;
        private Sound sound;
        private SoundVisualizer soundVisualizer = new SoundVisualizer();
        private Easy2D.Texture logoTexture;
        private void View_Load()
        {
            Sound.Init(view.Handle);
            sound = new Sound(Utils.GetResource("sound.mp3"));
            sound.Play(true);

            logoTexture = new Easy2D.Texture(Utils.GetResource("logo_dark.png"));

            soundVisualizer.Sound = sound;
            soundVisualizer.MirrorCount = 2;
            // Rainbow color wow
            soundVisualizer.ColorAt += (pos) =>
            {
                Vector4 col = Vector4.Zero;

                col.X = pos.X.Map(0, view.Size.X, 0f, 1f);
                col.Y = 1f - col.X;
                col.Z = pos.Y.Map(0, view.Size.Y, 0f, 1f);

                col = Colors.Tint(col, 1.2f);

                //col += soundVisualizer.;

                col.W = 1.0f;

                return col;
            };

            var input = view.CreateInput();

            input.Mice[0].MouseMove += (sender, pos) =>
             {
                 mousePos.X = pos.X;
                 mousePos.Y = pos.Y;
             };

            GL.SetGL(Silk.NET.OpenGLES.GL.GetApi(view));

            GL.Instance.Enable(EnableCap.Texture2D);
            GL.Instance.Enable(EnableCap.ScissorTest);
            GL.Instance.Enable(EnableCap.Blend);
            GL.Instance.Disable(EnableCap.DepthTest);

            GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            graphics = new Graphics();
            Viewport.SetViewport(0, 0, view.Size.X, view.Size.Y);
            graphics.Projection = Matrix4.CreateOrthographicOffCenter(0, view.Size.X, view.Size.Y, 0, 0, 1);
        }

        private Vector2 position = new Vector2(500, 540);
        private Vector2 velocity = new Vector2(900, -900);
        private Vector2 size = new Vector2(16);

        private double elapsedTime;
        private int fps;
        private int ups;
        private int fpsFinal;
        private int upsFinal;
        private void View_Update(double obj)
        {
            soundVisualizer.Update((float)obj);
            PostProcessing.Update((float)obj);
            ups++;
            position += velocity * (float)obj;

            if (position.X > view.Size.X + size.X / 2f)
                velocity.X = -900;
            else if (position.X < -size.X / 2f)
                velocity.X = 900;

            if (position.Y > view.Size.Y + size.Y / 2f)
                velocity.Y = -900;
            else if (position.Y < -size.Y / 2f)
                velocity.Y = 900;

            elapsedTime += obj;

            if(elapsedTime >= 1)
            {
                fpsFinal = fps;
                upsFinal = ups;
                elapsedTime -= 1;
                fps = 0;
                ups = 0;
            }
        }

        private void View_Render(double obj)
        {
            GPUScheduler.Update((float)obj);
            GL.Instance.Clear(Silk.NET.OpenGLES.ClearBufferMask.ColorBufferBit);

            graphics.Projection = projection;

            graphics.DrawLine(Vector2.Zero, position, Colors.Green, 10f);

            graphics.DrawRectangleCentered(position, size, Colors.Red);

            graphics.DrawRectangleCentered(mousePos, new Vector2(32), Colors.Pink);

            soundVisualizer.Render(graphics);
            soundVisualizer.Radius = (385f) + (250f * soundVisualizer.BeatValue);
            soundVisualizer.BarLength = 1000;

            soundVisualizer.Position = new Vector2(view.Size.X, view.Size.Y) / 2f;
            graphics.DrawRectangleCentered(soundVisualizer.Position, new Vector2(soundVisualizer.Radius * 2 + 15), Colors.White, logoTexture);

            graphics.DrawString($"FPS: {fpsFinal} UPS: {upsFinal}", Font.DefaultFont, mousePos, Colors.White);



            PostProcessing.Use(new Vector2i(view.Size.X, view.Size.Y), new Vector2i(view.Size.X, view.Size.Y));
            graphics.EndDraw();
            PostProcessing.PresentFinalResult();
            fps++;
        }
    }
}