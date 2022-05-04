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

        private SmoothFloat resultAnim = new SmoothFloat();

        public override void Render(Graphics g)
        {
            g.DrawRectangle(Vector2.Zero, MainGame.WindowSize, new Vector4(MathUtils.RainbowColor(MainGame.Instance.TotalTime, 1f, 0.25f), 1));
            g.DrawStringCentered("Result screen place holder", font, MainGame.WindowCenter, Colors.White);

            g.DrawString($"Score: {OsuContainer.Score}\nCombo: {OsuContainer.MaxCombo}/{OsuContainer.Beatmap.MaxCombo}\nMisses: {OsuContainer.CountMiss}\nRank: ", font, new Vector2(10), new Vector4(1f, 1f, 1f, resultAnim.Value));
            var letterTex = OsuContainer.CurrentRankingToTexture().Texture;

            g.DrawRectangleCentered(new Vector2(130 * resultAnim, 173), new Vector2(75, 75 / letterTex.Size.AspectRatio()) * MainGame.AbsoluteScale.Y, Colors.White, letterTex);

            base.Render(g);
        }

        public override void Update(float delta)
        {
            resultAnim.Update(delta);
            base.Update(delta);
        }

        public override void OnEntering()
        {
            resultAnim.Value = -1;
            resultAnim.TransformTo(1f, 1f, EasingTypes.OutQuad);
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();

            base.OnKeyDown(key);
        }
    }
}
