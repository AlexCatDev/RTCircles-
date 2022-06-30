using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using System;

namespace RTCircles
{
    public class MapBackground : Drawable
    {
        public override Rectangle Bounds => throw new NotImplementedException();

        private SmoothVector4 fadeColor = new SmoothVector4();

        public float Opacity = 0.5f;
        public float KiaiFlash = 2.0f;

        public static readonly Texture FlashTexture = new Texture(Utils.GetResource("UI.Assets.menu-flash.png")) { GenerateMipmaps = false };

        public bool ShowMenuFlash = true;

        public Texture TextureOverride;

        public MapBackground()
        {
            Layer = -72727;
            OsuContainer.OnKiai += () =>
            {
                fadeColor.Value = new Vector4(KiaiFlash, KiaiFlash, KiaiFlash, 1f);
                fadeColor.TransformTo(new Vector4(Opacity, Opacity, Opacity, 1f), 1.5f, EasingTypes.OutSine);
            };
        }

        public void TriggerFadeIn()
        {
            fadeColor.TransformTo(new Vector4(Opacity, Opacity, Opacity, 1f), 0.25f, EasingTypes.Out);
        }

        private Vector2 position = Vector2.Zero;

        public Vector2 ParallaxPosition { get; private set; }

        public float ParallaxAmount = 10;
        public float ParallaxSmooth = 15f;

        public float BEAT_SIZE = 0f;
        public float Zoom = 50f;

        public float Rotation;

        private SmoothFloat beatFlash = new SmoothFloat();
        private SmoothFloat beatFlash2 = new SmoothFloat();
        private int previousBeat;

        private float beatScale = 0;

        public bool UseBluredGameplayAsBackgroundSource;

        private FrameBuffer fb = new FrameBuffer(1,1);
        private FrameBuffer bluredFB = new FrameBuffer(1, 1);
        private void drawBluredGameplay(Graphics g)
        {
            var osuScreen = ScreenManager.GetScreen<OsuScreen>();

            fb.EnsureSize(MainGame.WindowWidth/10, MainGame.WindowHeight/10);

            g.DrawInFrameBuffer(fb, () => {
                MainGame.Instance.FakeWindowSize(fb.Texture.Size, () =>
                {
                    osuScreen.EnsureObjectIndexSynchronization();
                    osuScreen.Update((float)MainGame.Instance.DeltaTime);
                    osuScreen.Render(g);
                });
            });
            bluredFB.EnsureSize(fb.Width, fb.Height);

            Blur.BlurTexture(fb.Texture, bluredFB, 1, 4);
        }

        public override void Render(Graphics g)
        {
            if (OsuContainer.Beatmap is null)
                return;

            var color = fadeColor.Value;
            var textureRectangle = new Rectangle(0, 0, 1, 1);

            var tex = TextureOverride ?? OsuContainer.Beatmap.Background;

            if (UseBluredGameplayAsBackgroundSource)
            {
                drawBluredGameplay(g);
                textureRectangle = new Rectangle(0, 1, 1, -1);
                color.X += 1;
                color.Y += 1;
                color.Z += 1;

                tex = bluredFB.Texture;
            }

            float aspectRatio = tex.Size.AspectRatio();

            beatScale = (float)Interpolation.Damp(beatScale, OsuContainer.BeatProgressKiai, 0.95, MainGame.Instance.DeltaTime * 1000);

            float width = MainGame.WindowWidth + (BEAT_SIZE * beatScale) + Zoom;
            float height = MainGame.WindowHeight + (BEAT_SIZE * beatScale) + Zoom;

            Vector2 bgSize;
            bgSize = new Vector2(width, width / aspectRatio) + new Vector2(ParallaxAmount * 2);

            if(bgSize.Y < MainGame.WindowHeight)
                bgSize = new Vector2(height * aspectRatio, height) + new Vector2(ParallaxAmount * 2);

            g.DrawRectangleCentered(MainGame.WindowCenter + ParallaxPosition, bgSize, color, tex, textureRectangle, true, Rotation);

            if (OsuContainer.IsKiaiTimeActive && ShowMenuFlash)
            {
                int beat = (int)Math.Floor(OsuContainer.CurrentBeat);

                if (previousBeat > beat)
                    previousBeat = beat;

                if (beat - previousBeat > 0)
                {
                    EasingTypes fadeOutEasing = EasingTypes.InQuad;
                    float fadeOutTime = 400f;

                    EasingTypes fadeInEasing = EasingTypes.OutQuad;
                    float fadeInTime = 50f;

                    if (OsuContainer.CurrentBeatTimingPoint != null)
                        fadeOutTime = (float)OsuContainer.CurrentBeatTimingPoint?.BeatLength;

                    fadeOutTime /= 1000;
                    fadeInTime /= 1000;

                    float fadeOutOpacity = 0.4f;

                    if (beat % 2 == 0)
                    {
                        beatFlash2.Value = 0;
                        beatFlash2.TransformTo(fadeOutOpacity, fadeInTime, fadeInEasing);
                        beatFlash2.TransformTo(0f, fadeOutTime, fadeOutEasing);
                    }
                    else
                    {
                        beatFlash.Value = 0;
                        beatFlash.TransformTo(fadeOutOpacity, fadeInTime, fadeInEasing);
                        beatFlash.TransformTo(0f, fadeOutTime, fadeOutEasing);
                    }

                    previousBeat = beat;
                }
            }

            if (beatFlash.HasCompleted == false || beatFlash2.HasCompleted == false)
            {
                Vector2 flashSize = new Vector2(MainGame.WindowWidth / 4, MainGame.WindowHeight);

                g.DrawRectangle(Vector2.Zero, flashSize, new Vector4(1f, 1f, 1f, beatFlash.Value), FlashTexture, new Rectangle(1, 0, -1, 1), true);

                g.DrawRectangle(new Vector2(MainGame.WindowWidth - flashSize.X, 0), flashSize, new Vector4(1f, 1f, 1f, beatFlash2.Value), FlashTexture);
            }
        }

        public override void Update(float delta)
        {
            beatFlash.Update(delta);
            beatFlash2.Update(delta);

            fadeColor.Update(delta);

            Vector2 mousePosition = Input.MousePosition;

            position = Vector2.Lerp(position, mousePosition, delta * ParallaxSmooth);

            ParallaxPosition = new Vector2(
                position.X.Map(0, MainGame.WindowWidth, ParallaxAmount, -ParallaxAmount),
                position.Y.Map(0, MainGame.WindowHeight, ParallaxAmount, -ParallaxAmount));
        }
    }
}
