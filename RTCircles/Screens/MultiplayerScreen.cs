using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class MultiplayerScreen : Screen
    {
        public override void Render(Graphics g)
        {
            base.Render(g);

            g.DrawStringCentered("Nothing here yet", Font.DefaultFont, MainGame.WindowCenter, new Vector4(1f, 1f, 0f, 1f));
        }

        public override void OnKeyDown(Key key)
        {
            if(key == Key.Escape)
                ScreenManager.GoBack();
        }
    }
}
