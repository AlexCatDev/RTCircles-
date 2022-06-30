using Easy2D;
using OpenTK.Mathematics;
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

        public MenuScreen()
        {
            BeatmapMirror.DatabaseAction((realm) =>
            {
                ScreenManager.GetScreen<SongSelectScreen>().LoadCarouselItems(realm);
                var carouselItems = BeatmapCollection.Items;

                PlayableBeatmap playableBeatmap;

                if (BeatmapCollection.Items.Count > 0)
                {
                    var item = carouselItems[RNG.Next(0, carouselItems.Count - 1)];
                    //var item = carouselItems.Find((o) => o.Text.ToLower().Contains("corona (net0) [oto"));
                    //item = carouselItems.Find((o) => o.Text.Contains("(pishifat) [insane"));
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
                        logo.IntroSizeAnimation.TransformTo(Vector2.Zero, 200f, EasingTypes.InOutSine);
                    });
                }
            });

            mapBG = new MapBackground() { BEAT_SIZE = 10 };

            logo = new MenuLogo(mapBG);
            logo.IntroSizeAnimation.Value = new Vector2(1800);
            logo.ToggleInput(false);

            Add(mapBG);
            Add(logo);
        }

        public override void OnMouseDown(MouseButton args)
        {
            base.OnMouseDown(args);
        }

        public override void Update(float delta)
        {
            mapBG.UseBluredGameplayAsBackgroundSource = GlobalOptions.UseGameplayAsBackgroundSrc.Value;
            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            base.Render(g);
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }
    }
}
