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
        private Button mtThreadButton = new Button() { Color = Colors.Blue };
        private Button showGraphButton = new Button() { Color = Colors.Green };
        private Button showLogButton = new Button() { Color = Colors.Yellow };

        public OptionsScreen()
        {
            showLogButton.OnClick += (s, e) =>
            {
                MainGame.ShowLogOverlay = !MainGame.ShowLogOverlay;
            };
            Add(showLogButton);

            mtThreadButton.OnClick += (s, e) =>
            {
                MainGame.Instance.IsMultiThreaded = !MainGame.Instance.IsMultiThreaded;
            };
            Add(mtThreadButton);

            showGraphButton.OnClick += (s, e) =>
            {
                MainGame.ShowRenderGraph = !MainGame.ShowRenderGraph;
            };
            Add(showGraphButton);
        }

        public override void Update(float delta)
        {
            mtThreadButton.Text = $"Multithreading: {(MainGame.Instance.IsMultiThreaded ? "On" : "Off")}";
            showGraphButton.Text = $"Render Graph: {(MainGame.ShowRenderGraph ? "On" : "Off")}";
            showLogButton.Text = $"Show Log: {(MainGame.ShowLogOverlay ? "On" : "Off")}";

            base.Update(delta);
        }

        public override void Render(Graphics g)
        {
            layoutDrawableStack(g, MainGame.WindowCenter, new Vector2(1200, 300) * MainGame.Scale, 10 * MainGame.Scale, origin,
                mtThreadButton, showGraphButton, showLogButton);

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
