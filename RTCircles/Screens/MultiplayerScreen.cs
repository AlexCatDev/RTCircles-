using Easy2D;
using System.Numerics;
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
        }

        public override void OnEnter()
        {
           
        }

        public override void OnKeyDown(Key key)
        {
            if(key == Key.Escape)
                ScreenManager.GoBack();
        }
    }
}
