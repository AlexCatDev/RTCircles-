using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace RTCircles
{
    public static class GlobalOptions
    {
        public readonly static Option<bool> Bloom 
            = Option<bool>.CreateProxy("Bloom", (value) => { GPUSched.Instance.Add(() => { PostProcessing.Bloom = value; }); }, true);

        public readonly static Option<bool> MotionBlur 
            = Option<bool>.CreateProxy("MotionBlur", (value) => { GPUSched.Instance.Add(() => { PostProcessing.MotionBlur = value; }); }, true);

        public readonly static Option<bool> UseFancyCursorTrail = new Option<bool>("UseFancyCursorTrail", true);

        public readonly static Option<bool> SliderSnakeIn = new Option<bool>("SliderSnakeIn", true);

        public readonly static Option<bool> SliderSnakeOut = new Option<bool>("SliderSnakeOut", true);

        public readonly static Option<bool> SliderSnakeExplode = new Option<bool>("SliderSnakeExplode", true);

        public readonly static Option<bool> AutoCursorDance = new Option<bool>("AutoCursorDance", true);

        public readonly static Option<bool> ShowRenderGraphOverlay = new Option<bool>("ShowRenderGraphOverlay", false);

        public readonly static Option<bool> ShowLogOverlay = new Option<bool>("ShowLogOverlay", false);

        public readonly static Option<bool> ShowFPS = new Option<bool>("ShowFPS", true);

        public static void Init() 
        {
            Utils.Log($"Loaded Settings", LogLevel.Info);
            //We need to access a variable to instantiate every variable lol
            var ok = Bloom.Value;
        }
    }

    public class MainGame : Game
    {
        private Graphics g;
        public static Matrix4 Projection { get; private set; }

        private SmoothFloat shakeKiai = new SmoothFloat();
        private Matrix4 shakeMatrix => 
            Matrix4.CreateTranslation(new Vector3(RNG.Next(-10, 10) * shakeKiai.Value, RNG.Next(-10, 10) * shakeKiai.Value, 0f)) * Projection;

        public override void OnLoad()
        {
            GlobalOptions.Init();
            Skin.Load(@"C:\Users\user\Desktop\osu!\Skins\- HAPS MIT NU");

            g = new Graphics();

            OsuContainer.OnKiai += () =>
            {
                //shakeKiai.Value = 2f;
                //shakeKiai.TransformTo(0f, 0.5f, EasingTypes.Out);

                //Det her ser bedere ud tbh
                shakeKiai.Value = 2f;
                //shakeKiai.TransformTo(0f, 1f, EasingTypes.OutCirc);
                shakeKiai.TransformTo(0f, 1f, EasingTypes.OutQuart);
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                ScreenManager.OnMouseDown(e);
            };

            Input.InputContext.Mice[0].MouseUp += (s, e) =>
            {
                ScreenManager.OnMouseUp(e);
            };


            Input.InputContext.Mice[0].Scroll += (s, e) =>
            {
                ScreenManager.OnMouseWheel(e.Y);
            };

            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                ScreenManager.OnKeyDown(e);
            };

            Input.InputContext.Keyboards[0].KeyUp += (s, e, x) =>
            {
                ScreenManager.OnKeyUp(e);
            };

            Input.InputContext.Keyboards[0].KeyChar += (s, e) =>
            {
                ScreenManager.OnTextInput(e);
            };

            float introTime = 0;
            float introDuration = 0.15f;
            ScreenManager.OnIntroTransition += (delta) =>
            {
                introTime += delta;
                introTime = introTime.Clamp(0f, introDuration);

                float alpha = Interpolation.ValueAt(introTime, 0f, 1f, 0f, introDuration, EasingTypes.Out);

                g.DrawRectangle(Vector2.Zero, new Vector2(WindowWidth, WindowHeight), new Vector4(0f, 0f, 0f, alpha));

                if (introTime == introDuration)
                {
                    introTime = 0;
                    return true;
                }

                return false;
            };

            float outroTime = 0;
            float outroDuration = 0.15f;
            ScreenManager.OnOutroTransition += (delta) =>
            {
                outroTime += delta;
                outroTime = outroTime.Clamp(0f, outroDuration);

                float alpha = Interpolation.ValueAt(outroTime, 1f, 0f, 0f, outroDuration, EasingTypes.Out);

                //Fade black fullscreen quad
                g.DrawRectangle(new Vector2(0, 0), new Vector2(WindowWidth, WindowHeight), new Vector4(0f, 0f, 0f, alpha));

                if (outroTime == outroDuration)
                {
                    outroTime = 0;
                    return true;
                }

                return false;
            };

            ScreenManager.SetScreen<MenuScreen>(false);

            lastOpened = Settings.GetValue<DateTime>("LastOpened", out bool exists);
            Settings.SetValue(DateTime.Now, "LastOpened");

            Input.OnBackPressed += () =>
            {
                ScreenManager.GoBack();
            };

            IsMultiThreaded = false;

            //Utils.IgnoredLogLevels.Add(LogLevel.Debug);
        }

        private DateTime lastOpened;

        private ulong prevVertices;
        private ulong prevIndices;
        private ulong prevTriangles;

        private List<double> renderTimes = new List<double>();

        public override void OnRender(double delta)
        {
            PostProcessing.Use(new Vector2i((int)WindowWidth, (int)WindowHeight), new Vector2i((int)WindowWidth, (int)WindowHeight));
            ScreenManager.Render(g);

            drawFPSGraph(g);
            drawLog(g);

            g.Projection = PostProcessing.MotionBlur ? shakeMatrix : Projection;
            g.EndDraw();
            PostProcessing.PresentFinalResult();
        }

        private Vector2? trueHoverSize = null;
        private Vector2? trueHoverPos = null;
        private void drawFPSGraph(Graphics g)
        {
            if (GlobalOptions.ShowRenderGraphOverlay.Value)
            {
                if (renderTimes.Count == 1000)
                    renderTimes.RemoveAt(0);

                renderTimes.Add(RenderDeltaTime);

                Vector2 offset = Vector2.Zero;
                float height20MS = 250;
                for (int i = 0; i < renderTimes.Count; i++)
                {
                    float height = (float)renderTimes[i].Map(0, 0.020, 0, height20MS);
                    Vector2 size = new Vector2((float)WindowWidth / 1000, height);
                    g.DrawRectangle(offset, size, new Vector4(0f, 1f, 0f, 0.5f));
                    offset.X += size.X;
                }

                g.DrawLine(new Vector2(WindowWidth - 75, height20MS / 2), new Vector2(WindowWidth, height20MS / 2), Colors.Black, 2f);
                g.DrawString("10 MS", Font.DefaultFont, new Vector2(WindowWidth - 75, height20MS / 2), Colors.Yellow, 0.35f);

                g.DrawLine(new Vector2(WindowWidth - 75, height20MS), new Vector2(WindowWidth, height20MS), Colors.Black, 2f);
                g.DrawString("20 MS", Font.DefaultFont, new Vector2(WindowWidth - 75, height20MS), Colors.Red, 0.35f);
            }

            if (GlobalOptions.ShowFPS.Value)
            {
                ulong diffVertices = g.VerticesDrawn - prevVertices;
                prevVertices = g.VerticesDrawn;

                ulong diffIndices = g.IndicesDrawn - prevIndices;
                prevIndices = g.IndicesDrawn;

                ulong diffTriangles = g.TrianglesDrawn - prevTriangles;
                prevTriangles = g.TrianglesDrawn;

                double verticesPerSecond = (diffVertices) * (1.0 / RenderDeltaTime);

                double indicesPerSecond = (diffIndices) * (1.0 / RenderDeltaTime);

                double trianglesPerSecond = (diffTriangles) * (1.0 / RenderDeltaTime);

                const float scale = 0.35f;
                string text = $"FPS: {FPS}/{1000.0 / FPS:F2}ms UPS: {UPS}/{1000.0 / UPS:F2}ms";

                Vector2 hoverSize = Font.DefaultFont.MessureString(text, scale);
                Vector2 hoverPos = new Vector2(WindowWidth, WindowHeight) - hoverSize;

                if(trueHoverSize is null)
                    trueHoverSize = hoverSize;

                if (trueHoverPos is null)
                    trueHoverPos = hoverPos;

                if (new Rectangle(Input.MousePosition, Vector2.One).IntersectsWith(new Rectangle(trueHoverPos.Value, trueHoverSize.Value)))
                {
                    text+= $"\nVertices: {Utils.ToKMB(verticesPerSecond)}/s\nIndices: {Utils.ToKMB(indicesPerSecond)}/s\nTris: {Utils.ToKMB(trianglesPerSecond)}/s\nFramework: {RuntimeInformation.FrameworkDescription}\nOS: {RuntimeInformation.OSDescription}\nLast visit: {lastOpened}";

                    int pendingTasks = 0;
                    int asyncWorkloads = 0;
                    for (int i = 0; i < Scheduler.AllSchedulers.Count; i++)
                    {
                        if (Scheduler.AllSchedulers[i].TryGetTarget(out var scheduler))
                        {
                            asyncWorkloads += scheduler.AsyncWorkloadsRunning;
                            pendingTasks += scheduler.PendingTaskCount;
                        }
                    }

                    text += $"\nSchedulers Count: {Scheduler.AllSchedulers.Count} [TotalPending: {pendingTasks} TotalAsync: {asyncWorkloads}]";
                }
                else
                {
                    trueHoverSize = null;
                }

                Vector2 textSize = Font.DefaultFont.MessureString(text, scale);
                Vector2 drawTextPos = new Vector2(WindowWidth, WindowHeight) - textSize - new Vector2(0);

                trueHoverPos = drawTextPos;
                trueHoverSize = textSize;

                float value = ((float)FPS).Map(0, 240, 0f, 1).Clamp(0, 1);

                Vector4 color;

                if (value > 0.5f)
                    color = Interpolation.ValueAt(value, Colors.Yellow, Colors.Green, 0.5f, 1f);
                else
                    color = Interpolation.ValueAt(value, Colors.Red, Colors.Yellow, 0f, 0.5f);

                g.DrawRectangle(drawTextPos, textSize, new Vector4(0f, 0f, 0f, 0.5f));
                g.DrawString(text, Font.DefaultFont, drawTextPos, color, scale);
            }
        }

        private void drawLog(Graphics g)
        {
            if (!GlobalOptions.ShowLogOverlay.Value)
                return;

            const int MAX_VISIBLE_LOGS = 20;

            Vector2 offset = new Vector2(0, 0);
            float scale = 0.30f;

            float totalSize = (Font.DefaultFont.Size * scale) * MAX_VISIBLE_LOGS;

            for (int i = Utils.Logs.Count - MAX_VISIBLE_LOGS; i < Utils.Logs.Count; i++)
            {
                if (i < 0)
                    continue;

                Vector2 textOffset = new Vector2(0, WindowHeight - totalSize);

                var log = Utils.Logs[i];

                if(log.Tag is null)
                {
                    log.Tag = new SmoothFloat();
                    (log.Tag as SmoothFloat).Value = 1f;
                    (log.Tag as SmoothFloat).TransformTo(0f, 0.5f, EasingTypes.OutElasticHalf);
                }

                (log.Tag as SmoothFloat).Update((float)RenderDeltaTime);

                Vector2 animOffset = new Vector2((log.Tag as SmoothFloat).Value * MainGame.WindowWidth / 3, 0);

                for (int k = 0; k < log.Log.Count; k++)
                {
                    var item = log.Log[k];

                    Vector2 size = Font.DefaultFont.MessureString(item.Text, scale, includeLastCharAdvance: true);
                    g.DrawRectangle(offset + textOffset + animOffset, size, new Vector4(0f, 0f, 0f, 0.5f));
                    g.DrawString(item.Text, Font.DefaultFont, offset + textOffset + animOffset, item.Color, scale);
                    textOffset.X += size.X;
                }

                offset.Y += Font.DefaultFont.Size * scale;
            }
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
            GPUSched.Instance.Add(() =>
            {
                WindowSize = new Vector2(width, height);

                Viewport.SetViewport(0, 0, width, height);
                Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            });
        }

        public override void OnUpdate(double delta)
        {
            if (ScreenManager.ActiveScreen() is OsuScreen)
                PostProcessing.BloomThreshold = shakeKiai.Value.Map(2, 0, 0.2f, 1f);
            else
                PostProcessing.BloomThreshold = 1f;

            shakeKiai.Update((float)delta);
            OsuContainer.Update((float)delta);
            ScreenManager.Update((float)delta);
        }

        public override void OnImportFile(string path)
        {
            Utils.Log($"Somebody wants to open a file with this program and the path is: {path}", LogLevel.Info);

            using (var fileStream = File.OpenRead(path))
            {
                if (path.EndsWith(".osz"))
                {
                    //Handle osu beatmap import.
                    BeatmapMirror.ImportBeatmap(fileStream);
                }
                else if (path.EndsWith(".osk"))
                {
                    //Handle skin import.
                }
                else if (path.EndsWith(".osr"))
                {
                    //Handle replay import.
                }
            }
        }
    }
}
