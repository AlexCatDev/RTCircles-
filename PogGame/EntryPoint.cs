using System;
using System.Runtime.InteropServices;
using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;

namespace PogGame
{

    public class EntryPoint : GameBase
    {
        public static Camera camera = new Camera();
        private Graphics graphics;
        private Drawable container = new Drawable();

        public static DrawableMonster Monster = new DrawableMonster();
        public static DrawablePlayer Player = new DrawablePlayer();

        public override void OnLoad()
        {
            graphics = new Graphics();
            container.Add(Player);
            container.Add(Monster);

            //VSync = false;

            //PostProcessing.Bloom = true;
            //PostProcessing.MotionBlur = true;

            //VSync = false;
            //PostProcessing.MotionBlur = true;
            //PostProcessing.MotionBlurScale = 20;

            Input.InputContext.Mice[0].Scroll += (s, e) =>
            {
                container.OnMouseWheel(e.Y);
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                container.OnMouseDown(e);
            };

            Input.InputContext.Mice[0].MouseUp += (s, e) =>
            {
                container.OnMouseUp(e);
            };

            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                if(e == Silk.NET.Input.Key.M)
                {
                    var cursor = Input.InputContext.Mice[0].Cursor;

                    if (cursor.CursorMode == Silk.NET.Input.CursorMode.Normal)
                        cursor.CursorMode = Silk.NET.Input.CursorMode.Raw;
                    else
                        cursor.CursorMode = Silk.NET.Input.CursorMode.Normal;

                    Console.WriteLine($"CursorMode = {cursor.CursorMode}");
                }

                container.OnKeyDown(e);
            };
        }

        public override void OnRender()
        {
            graphics.Projection = camera.Projection;

            PostProcessing.Use(((Vector2i)WindowSize), ((Vector2i)WindowSize));

            container.Render(graphics);

            renderStatistics(graphics);

            var cursorBeat = 2 * (float)Interpolation.ValueAt(AudioMain.BeatProgress, 0, 1, 1, 0, EasingTypes.Out).Clamp(0, 1);
            graphics.DrawEllipse(Input.MousePosition, 0, 360, 10 + cursorBeat, 0, Colors.White);

            graphics.EndDraw();
            PostProcessing.PresentFinalResult();
        }

        private ulong prevVertices, prevIndices, prevTriangles;
        private void renderStatistics(Graphics g)
        {
            ulong diffVertices = g.VerticesDrawn - prevVertices;
            prevVertices = g.VerticesDrawn;

            ulong diffIndices = g.IndicesDrawn - prevIndices;
            prevIndices = g.IndicesDrawn;

            ulong diffTriangles = g.TrianglesDrawn - prevTriangles;
            prevTriangles = g.TrianglesDrawn;

            double verticesPerSecond = (diffVertices) * (1.0 / DeltaTime);

            double indicesPerSecond = (diffIndices) * (1.0 / DeltaTime);

            double trianglesPerSecond = (diffTriangles) * (1.0 / DeltaTime);

            float scale = 0.35f;
            string text = $"FPS: {FPS}/{1000.0 / FPS:F2}ms\nVertices: {Utils.ToKMB(verticesPerSecond)}/s\nIndices: {Utils.ToKMB(indicesPerSecond)}/s\nTris: {Utils.ToKMB(trianglesPerSecond)}/s\nFramework: {RuntimeInformation.FrameworkDescription}\nOS: {RuntimeInformation.OSDescription}";

            g.DrawString(text, Font.DefaultFont, new Vector2(20), Colors.Yellow, scale);
        }

        public static Vector2 WindowSize { get; private set; }

        public static Vector2 WindowCenter => WindowSize / 2f;

        public static float WindowWidth => WindowSize.X;
        public static float WindowHeight => WindowSize.Y;

        public static readonly Vector2 TargetResolution = new Vector2(1920, 1080);

        public static float Scale
        {
            get
            {
                //Calculate the aspectratio of our virtual resolution
                var aspectRatio = TargetResolution.X / TargetResolution.Y;

                //Store width of window width
                var width = WindowWidth;
                //calculate viewportheight by dividing window width with the virtual resolution aspect ratio
                var height = (int)(width / aspectRatio);
                //If the calculated viewport height is bigger than the window height then height is equals window height
                if (height > WindowHeight)
                {
                    height = (int)WindowHeight;
                    //Set viewport width to height times aspect ratio
                    width = (int)(height * aspectRatio);
                }

                return width / TargetResolution.X;
            }
        }

        public static Vector2 AbsoluteScale => new Vector2(WindowWidth / TargetResolution.X, WindowHeight / TargetResolution.Y);

        public override void OnResize(int width, int height)
        {
            GPUSched.Instance.Enqueue(() =>
            {
                WindowSize = new Vector2(width, height);

                Viewport.SetViewport(0, 0, width, height);
                camera.Size = new Vector2(width, height);
            });
        }

        public override void OnUpdate()
        {
            camera.Update();
            container.Update(TotalTime);
        }

        public override void OnOpenFile(string fullpath)
        {
            throw new NotImplementedException();
        }
    }
}
