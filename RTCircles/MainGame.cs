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
    public class MainGame : Game
    {
        private Graphics g;
        public static Matrix4 Projection { get; private set; }

        private SmoothFloat shakeKiai = new SmoothFloat();
        private Matrix4 shakeMatrix
        {
            get
            {
                var scale = Matrix4.CreateScale(
                (OsuContainer.IsKiaiTimeActive ? 1f : 0).Map(0f, 1f, 1f, 0.95f),
                (OsuContainer.IsKiaiTimeActive ? 1f : 0).Map(0f, 1f, 1f, 0.95f),
                1f);

                scale = Matrix4.Identity;

                float beatScaled = PostProcessing.MotionBlur ? shakeKiai.Value : 0;

                return Matrix4.CreateTranslation(new Vector3((float)RNG.Next(-10, 10) * beatScaled, (float)RNG.Next(-10, 10) * beatScaled, 0f)) * Projection * scale;
            }
        }

        public override void OnLoad()
        {
            OsuContainer.OnKiai += () =>
            {
                shakeKiai.Value = 2f;
                shakeKiai.TransformTo(0f, 0.5f, EasingTypes.Out);
            };

            g = new Graphics();

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

            if (RuntimeInformation.ProcessArchitecture == Architecture.X86 || RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                PostProcessing.Bloom = true;
                PostProcessing.MotionBlur = true;
            }

            IsMultiThreaded = false;

            Utils.IgnoredLogLevels.Add(LogLevel.Debug);
        }

        private DateTime lastOpened;


        public static bool ShowRenderGraph = false;
        public static bool ShowLogOverlay = false;
        public static bool ShowFPS = true;

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

            g.Projection = shakeMatrix;
            g.EndDraw();
            PostProcessing.PresentFinalResult();
        }

        private void drawFPSGraph(Graphics g)
        {
            if (ShowRenderGraph)
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

            if (ShowFPS)
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
                string text = $"FPS: {FPS}/{1000.0 / FPS:F2}ms UPS: {UPS}/{1000.0 / UPS:F2}ms\nVertices: {Utils.ToKMB(verticesPerSecond)}/s\nIndices: {Utils.ToKMB(indicesPerSecond)}/s\nTris: {Utils.ToKMB(trianglesPerSecond)}/s\nFramework: {RuntimeInformation.FrameworkDescription}\nOS: {RuntimeInformation.OSDescription}\nLast visit: {lastOpened}";

                int pendingTasks = 0;
                int asyncWorkloads = 0;
                for (int i = 0; i < Scheduler.AllSchedulers.Count; i++)
                {
                    if(Scheduler.AllSchedulers[i].TryGetTarget(out var scheduler))
                    {
                        asyncWorkloads += scheduler.AsyncWorkloadsRunning;
                        pendingTasks += scheduler.PendingTaskCount;
                    }
                }

                text += $"\nSchedulers Count: {Scheduler.AllSchedulers.Count} [TotalPending: {pendingTasks} TotalAsync: {asyncWorkloads}]";

                g.DrawString(text, Font.DefaultFont, new Vector2(20), Colors.Yellow, scale);
            }
        }

        private void drawLog(Graphics g)
        {
            if (ShowLogOverlay == false)
                return;

            const int MAX_VISIBLE_LOGS = 15;

            Vector2 offset = new Vector2(0, 0);
            float scale = 0.25f;

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
            GPUScheduler.Run(new (() =>
            {
                WindowSize = new Vector2(width, height);

                Viewport.SetViewport(0, 0, width, height);
                Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            }));
        }

        public override void OnUpdate(double delta)
        {
            //System.Threading.Thread.Sleep(1);

            if (ScreenManager.ActiveScreen() is OsuScreen)
                PostProcessing.BloomThreshold = MathHelper.Lerp(PostProcessing.BloomThreshold, OsuContainer.IsKiaiTimeActive ? 0.75f : 1.0f, (float)delta * 20f);
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
