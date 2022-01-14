using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class OptionsScreen : Screen
    {
        public OptionsScreen()
        {
           
        }

        public override void Update(float delta)
        {
            
            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            /*
            layoutDrawableStack(g, MainGame.WindowCenter, new Vector2(1200, 300) * MainGame.Scale, 10 * MainGame.Scale, origin,
                mtThreadButton, showGraphButton, showLogButton, toggleBloomButton, toggleMotionBlurButton);
            */

            Vector2 offset = new Vector2(MainGame.WindowWidth/2, 0);
            Vector2 panelSize = new Vector2(1200, 60) * MainGame.Scale;
            float padding = 4f * MainGame.Scale;
            float fontSize = 0.75f * MainGame.Scale;

            for (int i = 0; i < Option<bool>.AllOptions.Count; i++)
            {
                if (Option<bool>.AllOptions[i].TryGetTarget(out var option)) {
                    Vector2 truePos = new Vector2(offset.X - panelSize.X / 2, offset.Y);

                    Vector4 color = option.Value ? Colors.Green : Colors.Red;

                    g.DrawRectangle(truePos, panelSize, color);

                    string text = $"{option.Name}: {(option.Value ? "On" : "Off")}";

                    Vector2 textSize = Font.DefaultFont.MessureString(text, fontSize);

                    g.DrawString(text, Font.DefaultFont, truePos + new Vector2(panelSize.X / 2 - textSize.X / 2, panelSize.Y / 2f - textSize.Y/2), Colors.White, fontSize);

                    if (clickedSomewhere)
                    {
                        if (new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(new Rectangle(truePos, panelSize)))
                        {
                            option.Value = !option.Value;
                            Skin.Click.Play(true);
                            clickedSomewhere = false;
                        }
                    }
                    offset.Y += panelSize.Y + padding;
                }
            }

            clickedSomewhere = false;
            base.Render(g);
        }

        enum PositionOrigin
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            Center
        }

        enum Origin
        {
            Top,
            Bottom,
            Left,
            Right,
            Center
        }

        private PositionOrigin origin = PositionOrigin.Center;
        private void layoutDrawableStack(Graphics g, Vector2 pos, Vector2 size, float padding, PositionOrigin positionOrigin, params Drawable[] drawables)
        {
            Vector2 drawSize = new Vector2(size.X, size.Y / drawables.Length - padding / 2);
            switch (positionOrigin)
            {
                case PositionOrigin.TopLeft:
                    break;
                case PositionOrigin.TopRight:
                    pos.X -= size.X;
                    break;
                case PositionOrigin.BottomLeft:
                    pos.Y -= size.Y;
                    break;
                case PositionOrigin.BottomRight:
                    pos -= size;
                    break;
                case PositionOrigin.Center:
                    pos -= size / 2f;
                    break;
                default:
                    break;
            }

            //g.DrawRectangle(pos, size, Colors.White);

            foreach (var item in drawables)
            {
                item.Bounds = new Rectangle(pos, drawSize);
                pos.Y += drawSize.Y;
                pos.Y += padding;
                //g.DrawRectangle(pos, drawSize, Colors.Red);
            }
        }

        private bool clickedSomewhere;
        public override void OnMouseDown(MouseButton args)
        {
            if (args == MouseButton.Left)
                clickedSomewhere = true;

            base.OnMouseDown(args);
        }

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();

            base.OnKeyDown(key);
        }

        public override void OnMouseWheel(float delta)
        {
            if(delta > 0)
            {
                origin++;
            }
            else
            {
                origin--;
            }

            origin = (PositionOrigin)((int)origin).Clamp(0, 4);

            base.OnMouseWheel(delta);
        }
    }
}
