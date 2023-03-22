using Easy2D;
using Easy2D.Game;
using System.Numerics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class ResultScreen : Screen
    {
        public static readonly Font Font = new Font(Utils.GetResource("UI.Assets.roboto_bold.fnt"), Utils.GetResource("UI.Assets.roboto_bold.png"));

        private SmoothFloat resultAnim = new SmoothFloat();

        public override void Render(Graphics g)
        {
            g.DrawString($"Score: {OsuContainer.Score}\nCombo: {OsuContainer.MaxCombo}/{OsuContainer.Beatmap.MaxCombo}\nMisses: {OsuContainer.CountMiss}\nAccuracy: {OsuContainer.Accuracy*100:F2}%\nRank: ", Font,
                new Vector2(10) * MainGame.Scale, new Vector4(1f, 1f, 1f, resultAnim.Value.Clamp(0, 1)), MainGame.Scale);

            var letterTex = OsuContainer.CurrentRankingToTexture().Texture;
            g.DrawRectangleCentered(new Vector2(125 * resultAnim, 220) * MainGame.Scale, new Vector2(45, 45 / letterTex.Size.AspectRatio()) * MainGame.Scale, Colors.White, letterTex);

            base.Render(g);
        }

        public override void Update(float delta)
        {
            resultAnim.Update(delta);

            base.Update(delta);
        }

        public override void OnExiting()
        {
            OsuContainer.Beatmap?.Song.SlideAttribute(ManagedBass.ChannelAttribute.Volume, (float)GlobalOptions.SongVolume.Value, 500);

            Skin.Applause?.SlideAttribute(ManagedBass.ChannelAttribute.Volume, 0, 250);
            Skin.Applause?.SetSync(ManagedBass.SyncFlags.Slided, (long)ManagedBass.ChannelAttribute.Volume, new ManagedBass.SyncProcedure((s, e, x, y) => {
                Skin.Applause?.Stop();
            }));
        }

        public override void OnEntering()
        {
            //Dim the song audio.
            OsuContainer.Beatmap?.Song.SlideAttribute(ManagedBass.ChannelAttribute.Volume, (float)GlobalOptions.SongVolume.Value / 4, 500);

            if (Skin.Applause != null)
                Skin.Applause.Volume = 1;

            Skin.Applause?.Play(true);

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
