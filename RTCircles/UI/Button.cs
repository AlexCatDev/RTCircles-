using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy2D;
using Easy2D.Game;
using OpenTK;
using OpenTK.Mathematics;
using Silk.NET.Input;

namespace RTCircles
{
    public class Button : Drawable
    {
        public override Rectangle Bounds
        {
            get
            {
                return new Rectangle(Position, Size);
            }
            set
            {
                Position = value.Position;
                Size = value.Size;
            }
        }

        public Vector2 Position;

        private FrameBuffer frameBuffer = new FrameBuffer(1, 1);

        public Vector2 TextOffset;

        private Vector2i _size;
        public Vector2 Size 
        { 
            get { return _size; }
            set
            {
                _size = (Vector2i)value;
            }
        }

        public float TextureRotation = 0;

        public bool Disabled;

        public Rectangle TextureRectangle = new Rectangle(0, 0, 1, 1);

        public Vector4 Color = Colors.White;
        public Vector4 TextColor = Colors.White;

        public Vector4 AnimationColor = new Vector4(0.8f, 0.8f, 0.8f, 1f);

        public event Func<bool> OnClick;

        public Texture Texture;

        public string Text;

        private Animation buttonPressAnimation = new Animation();
        private Animation buttonPressFadeAnimation = new Animation();
        private Vector2 buttonPressLocation;

        private bool hasFocus;

        public override bool OnMouseDown(MouseButton args)
        {
            if (Disabled)
                return false;

            if (new Rectangle(Position, Size).IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
            {
                hasFocus = true;
                //Offset location by the framebuffer drawing position.
                buttonPressLocation = Input.MousePosition - Position;

                buttonPressAnimation.From = 0;
                buttonPressAnimation.To = Size.X > Size.Y ? Size.X : Size.Y;
                buttonPressAnimation.To *= 1.4f;
                buttonPressAnimation.Duration = 0.7f;
                buttonPressAnimation.Easing = EasingTypes.OutQuint;

                buttonPressAnimation.Reset();

                buttonPressFadeAnimation.From = 0.8f;
                buttonPressFadeAnimation.To = 0f;
                buttonPressFadeAnimation.Duration = 0.7f;
                buttonPressFadeAnimation.Easing = EasingTypes.Out;

                buttonPressFadeAnimation.IsPaused = true;

                buttonPressFadeAnimation.Reset();

                return true;
            }

            return false;
        }

        public override bool OnMouseUp(MouseButton args)
        {
            if (hasFocus)
            {
                hasFocus = false;

                bool clickHandled = OnClick?.Invoke() ?? false;
                if (clickHandled)
                {
                    buttonPressFadeAnimation.IsPaused = false;

                    Skin.Click.Play(true);
                }

                return clickHandled;
            }

            return false;
        }

        public override void Render(Graphics g)
        {
            frameBuffer.EnsureSize(Size.X, Size.Y);

            float textScale = Size.Y / Font.DefaultFont.Size;

            g.DrawRectangle(Position, Size, Disabled ? new Vector4(Color.Xyz, 0.2f) : Color, Texture, TextureRectangle, true, TextureRotation);

            if (!buttonPressAnimation.IsCompleted || !buttonPressFadeAnimation.IsCompleted)
            {
                g.DrawInFrameBuffer(frameBuffer, () =>
                {
                    g.DrawRectangleCentered(buttonPressLocation, new Vector2(buttonPressAnimation.Output * 2), new Vector4(AnimationColor.Xyz, AnimationColor.W * buttonPressFadeAnimation.Output), Texture.WhiteCircle);
                });
            }

            if(frameBuffer.Texture is not null && frameBuffer.IsInitialized)
                g.DrawFrameBuffer(Position, Colors.White, frameBuffer);

            if (!string.IsNullOrEmpty(Text))
            {
                Vector2 textSize = Font.DefaultFont.MessureString(Text, textScale);
                Vector2 textPos = new Vector2(Position.X + (Size.X / 2f) - textSize.X / 2f, Position.Y + (Size.Y / 2f) - textSize.Y / 2f) + TextOffset;
                g.DrawString(Text, Font.DefaultFont, textPos, TextColor, textScale);
            }
        }

        public override void Update(float delta)
        {
            buttonPressAnimation.Update(delta);
            buttonPressFadeAnimation.Update(delta);

            if (!Bounds.IntersectsWith(new Rectangle(Input.MousePosition, Vector2.One)))
            {
                hasFocus = false;
                buttonPressFadeAnimation.IsPaused = false;
            }
        }
    }
}
