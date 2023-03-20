using Silk.NET.Windowing;
using System.Diagnostics;

namespace RTCircles.WinForms
{
    class WinFormsGame : RTCircles.MainGame
    {
        private Easy2D.Graphics g;
        private Stopwatch clock = new Stopwatch();

        public WinFormsGame(IView view) : base(view) { }

        public void Load()
        {
            g = new Easy2D.Graphics();

            Skin.Load(GlobalOptions.SkinFolder.Value);
            ScreenManager.SetScreen<MenuScreen>(false);

            registerEvents();
        }

        public void Render()
        {
            g.Projection = Projection;
            ScreenManager.Render(g);

            //g.DrawString("Hello?", Easy2D.Font.DefaultFont, new OpenTK.Mathematics.Vector2(0), new OpenTK.Mathematics.Vector4(1));

            g.EndDraw();
        }

        public void Update()
        {
            double delta = ((double)clock.ElapsedTicks / Stopwatch.Frequency);
            clock.Restart();
            DeltaTime = delta;
            TotalTime += delta;

            float fDelta = (float)delta;

            OsuContainer.Update(fDelta * 1000);
            ScreenManager.Update(fDelta);
        }
    }
}