using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class ResultScreen : Screen
    {
        private Font font = new Font(Utils.GetResource("UI.Assets.roboto_bold.fnt"), Utils.GetResource("UI.Assets.roboto_bold.png"));

        public override void Render(Graphics g)
        {
            g.DrawRectangle(Vector2.Zero, MainGame.WindowSize, new Vector4(MathUtils.RainbowColor(OsuContainer.SongPosition / 1000), 1));
            var tex = OsuContainer.CurrentRankingToTexture();

            g.DrawRectangleCentered(MainGame.WindowCenter + new Vector2(0, 200) * MainGame.AbsoluteScale, new Vector2(100,100/tex.Texture.Size.AspectRatio())*MainGame.AbsoluteScale.Y, Colors.White, tex.Texture);
            g.DrawStringCentered("Result screen place holder", font, MainGame.WindowCenter, Colors.White);

            base.Render(g);
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();

            base.OnKeyDown(key);
        }
    }
}
