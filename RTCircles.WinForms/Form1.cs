using Easy2D;
using Silk.NET.Input;
using Silk.NET.OpenGLES;

namespace RTCircles.WinForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            glControl1.Context.Clear();
            new Thread(() =>
            {
                glControl1.Context.MakeCurrent();

                Utils.WriteToConsole = true;

                Easy2D.Game.Input.SetContext(glControl1.EnableNativeInput());

                Sound.Init();
                GL.SetGL(glControl1.OpenGLES);

                GL.Instance.Enable(EnableCap.Texture2D);
                GL.Instance.Enable(EnableCap.ScissorTest);
                GL.Instance.Enable(EnableCap.Blend);

                GL.Instance.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                Size s = new Size();

                WinFormsGame game = new WinFormsGame(glControl1.View);
                game.Load();

                while (true)
                {
                    if(s != glControl1.Size)
                    {
                        s = glControl1.Size;
                        game.OnResize(s.Width, s.Height);
                    }

                    GPUSched.Instance.RunPendingTasks();

                    GL.Instance.Clear(ClearBufferMask.ColorBufferBit);

                    game.Update();
                    game.Render();

                    glControl1.SwapBuffers();
                }

            }).Start();
        }
    }
}