using Easy2D;
using OpenTK.Mathematics;
using Realms;
using Silk.NET.Input;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTCircles
{
    public class MenuScreen : Screen
    {
        private MapBackground mapBG;
        private MenuLogo logo;

        private SmoothFloat introAnimation = new();

        private bool doneLoading = false;

        public MenuScreen()
        {
            mapBG = new MapBackground() { BEAT_SIZE = 10 };

            logo = new MenuLogo(mapBG);
            logo.IntroSizeAnimation.Value = new Vector2(1800);
            logo.ToggleInput(false);

            introAnimation.TransformTo(1, 0.5f, EasingTypes.Out, () =>
            {
                BeatmapMirror.DatabaseAction((realm) =>
                {
                    ScreenManager.GetScreen<SongSelectScreen>().LoadCarouselItems(realm);

                    introAnimation.TransformTo(0, 1, EasingTypes.InOutQuad, () =>
                     {
                         Add(mapBG);
                         Add(logo);

                         var carouselItems = BeatmapCollection.Items;

                         PlayableBeatmap playableBeatmap;

                         if (BeatmapCollection.Items.Count > 0)
                         {
                             CarouselItem item = null;//carouselItems.Find((o) => o.Text.ToLower().Contains("4 aliens"));

                             if(item == null)
                                 item = carouselItems[RNG.Next(0, carouselItems.Count - 1)];

                             playableBeatmap = PlayableBeatmap.FromCarouselItem(item);

                         }
                         else
                         {
                             playableBeatmap = new PlayableBeatmap(
                                 BeatmapMirror.DecodeBeatmap(Utils.GetResource("Maps.BuildIn.map.osu")),
                                 new Sound(Utils.GetResource("Maps.BuildIn.audio.mp3"), true, false, ManagedBass.BassFlags.Prescan),
                                 new Texture(Utils.GetResource("Maps.BuildIn.bg.jpg")));

                             NotificationManager.ShowMessage("You don't to have have any maps. Click here to get some", ((Vector4)Color4.Violet).Xyz, 10, () =>
                             {
                                 ScreenManager.SetScreen<DownloadScreen>();
                             });
                         }

                         int firstKiaiTimePoint = 0;
                         if (playableBeatmap != null)
                         {
                             playableBeatmap.GenerateHitObjects();
                             firstKiaiTimePoint = playableBeatmap.InternalBeatmap.TimingPoints.Find((o) => o.Effects == OsuParsers.Enums.Beatmaps.Effects.Kiai)?.Offset - 600 ?? 0;
                             //We have to schedule to the main thread, so we dont just randomly set maps while the game is updating and checking the current map.
                             GPUSched.Instance.Enqueue(() =>
                             {
                                 OsuContainer.SetMap(playableBeatmap);
                                 OsuContainer.SongPosition = firstKiaiTimePoint;
                                 OsuContainer.Beatmap.Song.Play(false);

                                 logo.IntroSizeAnimation.TransformTo(new Vector2(-200), 600, EasingTypes.InQuint, () =>
                                 {
                                     MainGame.Instance.Shaker.Shake();
                                     logo.ToggleInput(true);
                                     //Fade background in
                                     mapBG.TriggerFadeIn();
                                     Add(new ExpandingCircle(logo.Bounds.Center, logo.Bounds.Size.X / 2) { Layer = -1337 });
                                 });
                                 logo.IntroSizeAnimation.TransformTo(Vector2.Zero, 200f, EasingTypes.InOutQuad);
                             });
                         }

                         doneLoading = true;
                     });
                });
            });
        }

        public override void OnMouseDown(MouseButton args)
        {
            base.OnMouseDown(args);
        }

        public override void Update(float delta)
        {
            mapBG.UseBluredGameplayAsBackgroundSource = GlobalOptions.UseGameplayAsBackgroundSrc.Value;
            introAnimation.Update(delta);
            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            base.Render(g);

            if (!doneLoading)
            {
                g.DrawString($"{BeatmapCollection.Items.Count}...", ResultScreen.Font, new Vector2(10), new Vector4(new(1), introAnimation.Value), 0.5f);

                Vector2 pos = new Vector2(MainGame.WindowCenter.X, MainGame.WindowCenter.Y * introAnimation.Value);

                g.DrawRectangleCentered(MainGame.WindowCenter + new Vector2(0, 100) * MainGame.Scale, new Vector2(100) * MainGame.Scale, new Vector4(new(1), introAnimation.Value), Skin.LoadingSpinner, rotDegrees: (float)(MainGame.Instance.TotalTime * 360));
                g.DrawStringCentered($"Loading...", Font.DefaultFont, pos, new Vector4(new(1), introAnimation.Value), MainGame.Scale);
            }

        }

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }
}
