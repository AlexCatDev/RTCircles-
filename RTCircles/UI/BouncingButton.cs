using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class BouncingButton : Drawable
    {
        public Vector2 Position;
        public Vector2 Size;

        public override Rectangle Bounds { get => new Rectangle(Position - Size / 2, Size); }

        public event Action OnClick;

        private bool IsMouseHovering => new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(Bounds);

        private SmoothVector4 colorAnim = new SmoothVector4();
        private Texture texture;

        public float Alpha = 1;

        public BouncingButton(Texture texture)
        {
            colorAnim.Value = Vector4.One;
            this.texture = texture;
        }

        public override void Render(Graphics g)
        {
            g.DrawRectangleCentered(Position + animPos, animSize, colorAnim, texture, null, false, animRotation);
        }

        public override bool OnMouseDown(MouseButton button)
        {
            if (IsMouseHovering && button == MouseButton.Left)
            {
                Skin.Click.Play(true);
                OnClick?.Invoke();
                return true;
            }

            return false;
        }

        private double lastBeat;
        private double totalBeats;

        private SmoothVector2 animPos = new SmoothVector2();
        private SmoothVector2 animSize = new SmoothVector2();
        private SmoothFloat animRotation = new SmoothFloat();
        public override void Update(float delta)
        {
            colorAnim.Update(delta);
            animPos.Update((float)OsuContainer.DeltaSongPosition);
            animSize.Update((float)OsuContainer.DeltaSongPosition);
            animRotation.Update((float)OsuContainer.DeltaSongPosition);

            if (OsuContainer.Beatmap == null || OsuContainer.Beatmap.Song.IsPlaying == false)
            {
                animSize.Value = Size;
                animPos.Value = Vector2.Zero;
            }

            var diff = OsuContainer.CurrentBeat - lastBeat;

            if(diff >= 2 || diff < 0)
            {
                if(diff < 0)
                {
                    animSize.ClearTransforms();
                    animPos.ClearTransforms();
                    animRotation.ClearTransforms();
                }

                float beatDuration = (float)(OsuContainer.CurrentBeatTimingPoint?.BeatLength*2 ?? 1000);

                lastBeat = OsuContainer.CurrentBeat;
                totalBeats++;

                if (IsMouseHovering && OsuContainer.Beatmap?.Song.IsPlaying == true)
                {
                    //Skin.Click.Play(true);

                    float destRot;

                    if (totalBeats % 2 == 0)
                        destRot = 15;
                    else
                        destRot = -15;

                    animRotation.ClearTransforms();
                    animRotation.TransformTo(destRot, beatDuration, EasingTypes.InOutSine);

                    //animSize.ClearTransforms();

                    //Button flies up in the air
                    animPos.TransformTo(new Vector2(0, -15), beatDuration / 2, EasingTypes.OutSine);

                    //Button lands
                    animPos.TransformTo(Vector2.Zero, beatDuration / 2, EasingTypes.InSine, () => {
                        //Squish
                        animSize.TransformTo(new Vector2(Size.X * 1.1f, Size.Y * 0.9f), 50f, EasingTypes.OutQuad);
                        //Popup
                        animSize.TransformTo(Size, 50f, EasingTypes.InQuad);
                    });
                }
            }

            if (!IsMouseHovering)
            {
                animPos.ClearTransforms();
                animPos.TransformTo(Vector2.Zero, 100f);

                animRotation.ClearTransforms();
                animRotation.TransformTo(0f, 100f);

                animSize.ClearTransforms();
                animSize.TransformTo(Size, 100f);

                colorAnim.ClearTransforms();
                colorAnim.TransformTo(new Vector4(1f, 1f, 1f, Alpha), 0.1f, EasingTypes.Out);
            }
            else
            {
                colorAnim.ClearTransforms();
                colorAnim.TransformTo(new Vector4(2f, 2f, 2f, Alpha), 0.1f, EasingTypes.Out);
            }
        }
    }
}
