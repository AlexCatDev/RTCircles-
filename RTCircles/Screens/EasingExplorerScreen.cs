using Easy2D;
using Easy2D.Game;
using System.Numerics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class EasingExplorerScreen : Screen
    {
        private Array easingTypes;
        public EasingExplorerScreen()
        {
            easingTypes = Enum.GetValues(typeof(EasingTypes));
        }

        private Shaker shaker = new Shaker() { Duration = 1, Radius = 250, Speed = 40 };

        private Vector2 scrollOffset = new Vector2(400, 20);
        private Vector2 rawScrollOffset = new Vector2(400, 20);
        public override void Render(Graphics g)
        {
            double t = (MainGame.Instance.TotalTime / 2).OscillateValue(0, 1);

            Vector2 startPos = scrollOffset;

            Vector2 size = new Vector2(64);

            float spacing = 50;

            foreach (EasingTypes easing in easingTypes)
            {
                g.DrawRectangle(startPos, new Vector2(MainGame.WindowWidth / 2 + size.X, size.Y), Colors.SlateGray);
                double val = Interpolation.ValueAt(t, 0, 1, 0, 1, easing);
                Vector2 pos = new Vector2((float)val.Map(0, 1, 0, MainGame.WindowWidth / 2), 0);
                g.DrawRectangle(pos + startPos, size, Colors.White);
                g.DrawRectangleCentered(startPos + new Vector2(MainGame.WindowWidth / 2 + size.X*2, size.Y / 2), 
                    size * (float)val.Map(0,1,1f,2f), Colors.White, Skin.SliderFollowCircle);

                startPos.Y += size.Y;

                g.DrawString(easing.ToString(), Font.DefaultFont, startPos, Colors.Pink, 0.5f);

                startPos.Y += spacing;
            }

            g.Projection = Matrix4x4.CreateTranslation(new Vector3(shaker.OutputShake, 1)) * Matrix4x4.CreateOrthographicOffCenter(0, MainGame.WindowWidth, MainGame.WindowHeight, 0, -1, 1);
            //g.DrawRectangleCentered(Input.MousePosition + shaker.OutputShake, new Vector2(256), Colors.Red);

            base.Render(g);
        }

        public override void Update(float delta)
        {
            shaker.Update(delta);

            scrollOffset.Y = MathHelper.Lerp(scrollOffset.Y, rawScrollOffset.Y, delta * 10);

            base.Update(delta);
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();

            if(key == Key.S)
            {
                shaker.Shake();
            }
            base.OnKeyDown(key);
        }

        public override void OnMouseWheel(float delta)
        {
            rawScrollOffset.Y += delta * 200;
            base.OnMouseWheel(delta);
        }
    }
}
