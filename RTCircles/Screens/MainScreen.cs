using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RTCircles
{
    public class MainScreen : Screen
    {
        private SoundVisualizer soundVisualizer;
        private Sound sound;
        private Texture logo;
        private ScrollingTriangles triangles;

        public MainScreen()
        {
            logo = new Texture(Utils.GetResource("logo.png"));

            sound = new Sound(Utils.GetResource("song.mp3"));
            sound.PlaybackPosition = 85000;

            soundVisualizer = new SoundVisualizer();
            soundVisualizer.MirrorCount = 2;
            // Rainbow color wow
            soundVisualizer.ColorAt += (pos) =>
            {
                Vector4 col = Vector4.Zero;

                col.X = pos.X.Map(0, MainGame.WindowWidth, 0f, 1f);
                col.Y = 1f - col.X;
                col.Z = pos.Y.Map(0, MainGame.WindowHeight, 0f, 1f);

                col = Colors.Tint(col, 1.2f);

                //col += soundVisualizer.;

                col.W = 1.0f;

                return col;
            };

            triangles = new ScrollingTriangles(40);
            triangles.Layer = 69;
            triangles.BaseColor = Colors.From255RGBA(37, 37, 37, 255);

            soundVisualizer.Sound = sound;

            Add(triangles);
            Add(soundVisualizer);
        }

        public override void OnEnter()
        {
            sound.Play();
        }

        public override void OnExiting()
        {
            sound.Pause();
        }

        public override void Render(Graphics g)
        {
            base.Render(g);

            Rectangle goOsuScreenButton = new Rectangle(new Vector2(1600, 100) * MainGame.AbsoluteScale, new Vector2(250, 80) * MainGame.Scale);

            g.DrawRectangle(goOsuScreenButton.Position, goOsuScreenButton.Size, Colors.Red);
            g.DrawString("Go to osu screen", Font.DefaultFont, goOsuScreenButton.Position, Colors.White, 0.5f * MainGame.Scale);

            if (new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(goOsuScreenButton))
                ScreenManager.SetScreen<OsuScreen>();

            g.DrawRectangleCentered(soundVisualizer.Position, new Vector2(soundVisualizer.Radius * 2 + 14f * MainGame.Scale), Colors.White, logo);

            g.DrawRectangleCentered(Input.MousePosition, new Vector2(64) * (Input.IsKeyDown(Key.W) ? 2 : 1), Colors.Red, rotDegrees: Input.MousePosition.X.Map(0, MainGame.WindowWidth, 0, 360));
            for (int i = 0; i < Input.TouchFingerEvents.Count; i++)
            {
                var finger = Input.TouchFingerEvents[i];

                long fingerID = finger.FingerId;
                Vector2 fingerPos = new Vector2(finger.X * MainGame.WindowWidth, finger.Y * MainGame.WindowHeight);
                float r = MathUtils.Map((float)fingerID, 0, 9, 0, 1);

                float green = 1f - r;

                Vector4 fingerColor = new Vector4(r, green, 1f, 1f);

                g.DrawRectangleCentered(fingerPos, new Vector2(64), fingerColor);
                g.DrawString($"ID: {fingerID}", Font.DefaultFont, fingerPos, Colors.White);
            }
            //slider.Render(g);
        }

        List<Vector2> clicks = new List<Vector2>();
        FastSlider slider = new FastSlider();
        public override void OnMouseDown(MouseButton button)
        {
            clicks.Add(Input.MousePosition);
            //slider.SetPoints(clicks, 40f);
            base.OnMouseDown(button);
        }

        public override void Update(float delta)
        {
            soundVisualizer.Radius = (350f * MainGame.Scale) + (200f * soundVisualizer.BeatValue * MainGame.Scale);
            soundVisualizer.BarLength = 1200 * MainGame.Scale;
            soundVisualizer.Position = MainGame.WindowCenter;

            triangles.Position = soundVisualizer.Position;
            triangles.Radius = soundVisualizer.Radius - 5;
            triangles.Speed = 50 + 500f * soundVisualizer.BeatValue;

            base.Update(delta);
        }
    }
}
