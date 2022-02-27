using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class GoodSliderBar : Drawable
    {
        private static Texture sliderTexture = new Texture(Utils.GetResource("UI.Slider.png"));

        public Vector2 Position;
        public Vector2 Size;

        public override Rectangle Bounds => new Rectangle(Position, Size);

        public Vector4 BackgroundColor;
        public Vector4 ForegroundColor;

        public double MinimumValue = 0;
        public double MaximumValue = 1;

        private double internalValue;

        public double Value { 
            get 
            { 
                return internalValue; 
            } 
            set {
                internalValue = value.Clamp(MinimumValue, MaximumValue);

                internalValue = Math.Round(internalValue, 6);
            } 
        }

        public event Action <double> ValueChanged;

        public override void Render(Graphics g)
        {
            g.DrawRectangle(Position, Size, BackgroundColor, sliderTexture);

            double zeroOne = internalValue.Map(MinimumValue, MaximumValue, 0, 1);

            Vector2 foregroundSize = new Vector2((float)(Size.X * zeroOne), Size.Y);
            g.DrawRectangle(Position, foregroundSize, ForegroundColor, sliderTexture, new Rectangle(0, 0, (float)zeroOne, 1), true);
            
            g.DrawString($"Value: {Value}", Font.DefaultFont, Input.MousePosition, Colors.Red);
        }

        private Vector2? mouseDownPos;

        private bool mouseHover => new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(Bounds);

        public double KeyboardStep = 0.01;

        public override bool OnKeyDown(Key key)
        {
            if(mouseHover)
            {
                if (key == Key.Left)
                {
                    Value -= KeyboardStep;
                    return true;
                }
                else if(key == Key.Right)
                {
                    Value += KeyboardStep;
                    return true;
                }
            }

            return false;
        }

        public override bool OnMouseDown(MouseButton button)
        {
            if(button == MouseButton.Left && mouseHover)
            {
                mouseDownPos = Input.MousePosition;
                return true;
            }

            mouseDownPos = null;

            return false;
        }

        public override bool OnMouseUp(MouseButton button)
        {
            if(button == MouseButton.Left)
                mouseDownPos = null;

            return base.OnMouseUp(button);
        }

        public int StepDecimals = 1;

        private float playSound = 0;

        Vector2 lastMousePos = Vector2.Zero;
        public override void Update(float delta)
        {
            var mousePos = Input.MousePosition;

            playSound += delta;

            if (mouseDownPos.HasValue)
            {
                double x = mousePos.X;
                double val = x.Map(Position.X, Position.X + Size.X, MinimumValue, MaximumValue).Clamp(MinimumValue, MaximumValue);

                val = Math.Round(val, StepDecimals);
                Console.WriteLine(val);
                if(val != internalValue)
                {
                    internalValue = val;

                    ValueChanged?.Invoke(internalValue);

                    if (playSound >= 0.06f)
                    {
                        Skin.Click.Play(true);
                        playSound = 0;
                    }
                }
            }
        }
    }
}
