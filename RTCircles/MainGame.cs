using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace RTCircles
{
    public class MainGame : GameBase
    {
        public static MainGame Instance { get; private set; }

        public MainGame() => Instance = this;

        private Graphics g;
        public static Matrix4 Projection { get; private set; }

        private SmoothFloat kiaiAnimation = new SmoothFloat();

        public Shaker Shaker = new Shaker() { Duration = 1.5f, Radius = 125, Speed = 100, Easing = EasingTypes.OutQuint };
        private Matrix4 shakeMatrix => Matrix4.CreateTranslation(new Vector3(Shaker.OutputShake * MainGame.Scale)) * Projection;

        private bool debugCameraActive = false;

        private SmoothFloat volumeBarFade = new SmoothFloat();
        private Option<double> volumeSource = GlobalOptions.GlobalVolume;

        public override void OnLoad()
        {
#if RELEASE
            ToggleFullScreen();
#endif
            Input.CursorMode = CursorMode.Raw;

            Size = new Silk.NET.Maths.Vector2D<int>(1600, 900);

            string build = "RELEASE";
#if DEBUG
            build = "DEBUG";
#endif

            //Make some sort of build versioning idk
            Version = $"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}";

            Utils.IgnoredLogLevels.Add(LogLevel.Debug);
#if RELEASE
            Utils.IgnoredLogLevels.Add(LogLevel.Info);
            Utils.IgnoredLogLevels.Add(LogLevel.Success);
#endif

            VSync = false;
            MaxAllowedDeltaTime = 0.1;
            Utils.WriteToConsole = true;
            registerEvents();

            Skin.Load(GlobalOptions.SkinFolder.Value);

            g = new Graphics();

            GPUSched.Instance.EnqueueDelayed(() =>
            {
                NotificationManager.ShowMessage($"App is work in progress!", ((Vector4)Color4.Orange).Xyz, 5);
            }, 500);

            lastOpened = Settings.GetValue<DateTime>("LastOpened", out bool exists);
            Settings.SetValue(DateTime.Now, "LastOpened");

            ScreenManager.SetScreen<MenuScreen>(false);
        }

        private DateTime lastOpened;

        private Camera debugCamera = new Camera();
        private float debugCameraScale = 1f;

        public Cursor MenuCursor = new Cursor();

        public override void OnRender()
        {
            g.Projection = shakeMatrix;

            if (debugCameraActive)
            {
                if (Input.IsKeyDown(Key.W))
                    debugCamera.Position.Y += (float)(1000 * DeltaTime);
                else if (Input.IsKeyDown(Key.S))
                    debugCamera.Position.Y -= (float)(1000 * DeltaTime);

                if (Input.IsKeyDown(Key.A))
                    debugCamera.Position.X += (float)(1000 * DeltaTime);
                else if (Input.IsKeyDown(Key.D))
                    debugCamera.Position.X -= (float)(1000 * DeltaTime);

                debugCamera.Size = new Vector2(WindowWidth, WindowHeight);
                debugCamera.Scale = (float)Interpolation.Damp(debugCamera.Scale, debugCameraScale, 0.1, DeltaTime * 10);
                debugCamera.Update();

                g.Projection = debugCamera.Projection;
            }

            PostProcessing.Use(new Vector2i((int)WindowWidth, (int)WindowHeight), new Vector2i((int)WindowWidth, (int)WindowHeight));
            ScreenManager.Render(g);
            drawFPSGraph(g);
            drawLog(g);

            NotificationManager.Update((float)DeltaTime);
            NotificationManager.Render(g);

            drawVolumeBar(g);

            if(ScreenManager.ActiveScreen is not OsuScreen)
                MenuCursor.Render(g, (float)DeltaTime, Input.MousePosition, Colors.White);
            //c
            //g.DrawRectangleCentered(Input.MousePosition, new Vector2(16), Colors.Red);

            g.EndDraw();

            if (debugCameraActive)
            {
                g.DrawString($"Camera mode", Font.DefaultFont, WindowCenter,
                    new Vector4(1f, 1f, 0f,(float)Math.Cos(TotalTime*3).Map(-1, 1, 0.3, 1)));

                g.Projection = Projection;
                g.EndDraw();
            }

            PostProcessing.PresentFinalResult();
        }
        public override void OnUpdate()
        {
            float mult = kiaiAnimation.Value * 1;
            float finalScale = ScreenManager.ActiveScreen is OsuScreen or MenuScreen ? 1 : 0.2f;
            g.FinalColorMult.Xyz = new Vector3(1 + mult * finalScale);

            kiaiAnimation.Update((float)DeltaTime);
            Shaker.Update((float)DeltaTime);

            OsuContainer.Update(DeltaTime * 1000);
            ScreenManager.Update((float)DeltaTime);
        }

        private void drawVolumeBar(Graphics g)
        {
            volumeBarFade.Update((float)DeltaTime);
            if (volumeBarFade.Value == 0)
                return;

            Vector2 pos = new Vector2(10, 10);
            Vector2 size = new Vector2(500, 30);

            float padding = 20;

            if (new Rectangle(pos, size).IntersectsWith(Input.MousePosition))
                volumeSource = GlobalOptions.GlobalVolume;

            g.DrawRectangle(pos, size, Colors.From255RGBA(31, 31, 31, 127 * volumeBarFade.Value));
            g.DrawRectangle(pos, new Vector2(size.X * (float)GlobalOptions.GlobalVolume.Value, size.Y), Colors.From255RGBA(0, 255, 128, 255 * volumeBarFade.Value));

            g.DrawStringCentered($"Volume: {GlobalOptions.GlobalVolume.Value * 100:F0}", Font.DefaultFont, pos + size / 2f, new Vector4(1f, 1f, 1f, volumeBarFade.Value), 0.5f);

            pos.Y += size.Y + padding;

            if (new Rectangle(pos, size).IntersectsWith(Input.MousePosition))
                volumeSource = GlobalOptions.SongVolume;

            g.DrawRectangle(pos, size, Colors.From255RGBA(31, 31, 31, 127 * volumeBarFade.Value));
            g.DrawRectangle(pos, new Vector2(size.X * (float)GlobalOptions.SongVolume.Value, size.Y), Colors.From255RGBA(0, 255, 128, 255 * volumeBarFade.Value));

            g.DrawStringCentered($"Song Volume: {GlobalOptions.SongVolume.Value * 100:F0}", Font.DefaultFont, pos + size / 2f, new Vector4(1f, 1f, 1f, volumeBarFade.Value), 0.5f);

            pos.Y += size.Y + padding;

            if (new Rectangle(pos, size).IntersectsWith(Input.MousePosition))
                volumeSource = GlobalOptions.SkinVolume;

            g.DrawRectangle(pos, size, Colors.From255RGBA(31, 31, 31, 127 * volumeBarFade.Value));
            g.DrawRectangle(pos, new Vector2(size.X * (float)GlobalOptions.SkinVolume.Value, size.Y), Colors.From255RGBA(0, 255, 128, 255 * volumeBarFade.Value));

            g.DrawStringCentered($"Skin Volume: {GlobalOptions.SkinVolume.Value * 100:F0}", Font.DefaultFont, pos + size / 2f, new Vector4(1f, 1f, 1f, volumeBarFade.Value), 0.5f);
        }

        private List<double> renderTimes = new List<double>();
        private Vector2? trueHoverSize = null;
        private Vector2? trueHoverPos = null;
        private double ms = 0;
        private void drawFPSGraph(Graphics g)
        {
            if (GlobalOptions.ShowRenderGraphOverlay.Value)
            {
                if (renderTimes.Count == 1000)
                    renderTimes.RemoveAt(0);

                renderTimes.Add(DeltaTime);

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
                const float scale = 0.5f;

                ms = Interpolation.Damp(ms, DeltaTime * 1000, 0.25, DeltaTime);

                if (double.IsNaN(ms))
                    ms = 0;

                double mem = GC.GetTotalMemory(false) / 1048576d;

                StringBuilder sb = new StringBuilder();

                string text = $"Mem: {mem:F0}mb DrawCalls: {GL.DrawCalls} FPS: {FPS}/{ms:F2}ms UPS: {UPS}";

                sb.Append(text);

                Vector2 hoverSize = ResultScreen.Font.MessureString(text, scale);
                Vector2 hoverPos = new Vector2(WindowWidth, WindowHeight) - hoverSize;

                if(trueHoverSize is null)
                    trueHoverSize = hoverSize;

                if (trueHoverPos is null)
                    trueHoverPos = hoverPos;

                if (new Rectangle(trueHoverPos.Value, trueHoverSize.Value).IntersectsWith(Input.MousePosition))
                {
                    text+= $"\n{GL.GLVersion}\n" +
                        $"GPU: {GL.GLRenderer}\n" +
                        $"Vertices: {Utils.ToKMB(g.VerticesDrawn)}\n" +
                        $"Indices: {Utils.ToKMB(g.IndicesDrawn)}\n" +
                        $"Tris: {Utils.ToKMB(g.TrianglesDrawn)}\n" +
                        $"Textures: {Easy2D.Texture.TextureCount} DrawCall_Max: {GL.MaxTextureSlots} \n" +
                        $"Framework: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}\n" +
                        $"Audio Latency: {Sound.DeviceLatency}ms\n" +
                        $"OS: {RuntimeInformation.OSDescription}\n" +
                        $"GC: [{GC.CollectionCount(0)}, {GC.CollectionCount(1)}, {GC.CollectionCount(2)}]\n" +
                        $"Last visit: {lastOpened}";

                    text += $"\nSchedulers [Pending: {Scheduler.TotalPendingWorkloads} Async: {Scheduler.TotalAsyncWorkloads}]";
                }
                else
                {
                    trueHoverSize = null;
                }

                GL.ResetStatistics();
                g.ResetStatistics();

                Vector2 textSize = ResultScreen.Font.MessureString(text, scale);
                Vector2 drawTextPos = new Vector2(WindowWidth, WindowHeight) - textSize - new Vector2(25);

                trueHoverPos = drawTextPos;
                trueHoverSize = textSize;

                float value = ((float)FPS).Map(0, 240, 0f, 1).Clamp(0, 1);

                Vector4 color;

                if (value > 0.5f)
                    color = Interpolation.ValueAt(value, Colors.Yellow, Colors.Green, 0.5f, 1f);
                else
                    color = Interpolation.ValueAt(value, Colors.Red, Colors.Yellow, 0f, 0.5f);

                var boxSize = textSize + new Vector2(25f);
                g.DrawRoundedRect(drawTextPos + textSize/2f, boxSize, new Vector4(0f, 0f, 0f, 0.5f), 15f);

                g.DrawString(text, ResultScreen.Font, drawTextPos, color, scale);
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

                (log.Tag as SmoothFloat).Update((float)DeltaTime);

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

        private static Vector2 _windowSize;
        public static Vector2 WindowSize
        {
            get
            {
                return _windowSize;
            }
            set
            {
                _windowSize = value;

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

                Scale = width / TargetResolution.X;
            }
        }

        public static Vector2 WindowCenter => WindowSize / 2f;

        public static float WindowWidth => WindowSize.X;
        public static float WindowHeight => WindowSize.Y;

        public static readonly Vector2 TargetResolution = new Vector2(1920, 1080);

        public static float Scale { get; protected set; }

        public static Vector2 AbsoluteScale => new Vector2(WindowWidth / TargetResolution.X, WindowHeight / TargetResolution.Y);

        public override void OnResize(int width, int height)
        {
            WindowSize = new Vector2(width, height);

            GPUSched.Instance.Enqueue(() =>
            {
                Viewport.SetViewport(0, 0, width, height);
                Projection = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
            });
        }

        public void FakeWindowSize(Vector2 newSize, Action a)
        {
            var prev = WindowSize;
            WindowSize = newSize;
            a.Invoke();
            WindowSize = prev;
        }

        public override void OnOpenFile(string path)
        {
            Utils.Log($"Somebody wants to open a file with this program and the path is: {path}", LogLevel.Info);

            System.Threading.ThreadPool.QueueUserWorkItem((o) =>
            {
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
            }, null);
        }

        private void registerEvents()
        {
            OsuContainer.OnKiai += () =>
            {
                //Det her ser bedere ud tbh
                kiaiAnimation.Value = 1f;
                kiaiAnimation.TransformTo(0f, 1f, EasingTypes.Out);
            };

            Input.OnBackPressed += () =>
            {
                ScreenManager.GoBack();
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                if (!NotificationManager.OnMouseDown(e))
                    ScreenManager.OnMouseDown(e);
            };

            Input.InputContext.Mice[0].MouseUp += (s, e) =>
            {
                ScreenManager.OnMouseUp(e);
            };

            Input.InputContext.Mice[0].Scroll += (s, e) =>
            {
                if (Input.IsKeyDown(Key.AltLeft))
                {
                    volumeBarFade.Value = 1;
                    volumeBarFade.Wait(1f);
                    volumeBarFade.TransformTo(0, 0.5f, EasingTypes.Out);
                    volumeSource.Value = (volumeSource.Value + e.Y * 0.01f).Clamp(0, 1);
                    return;
                }

                if (debugCameraActive && !Input.IsKeyDown(Key.ControlLeft))
                {
                    debugCameraScale += e.Y * 0.03f;
                    return;
                }

                ScreenManager.OnMouseWheel(e.Y);
            };

            bool importing = false;
            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                if (Input.IsKeyDown(Key.ShiftLeft) && Input.IsKeyDown(Key.ShiftRight) && Input.IsKeyDown(Key.Space))
                {
                    ScreenManager.SetScreen<EasingExplorerScreen>();
                    return;
                }

                /*
                if (Input.IsKeyDown(Key.U))
                {
                    ScreenManager.SetScreen<ResultScreen>();
                    return;
                }
                */

                if (e == Key.F3)
                {
                    var osuSongsFolder = GlobalOptions.OsuFolder.Value + "/Songs";

                    if (!Directory.Exists(osuSongsFolder))
                    {
                        NotificationManager.ShowMessage("Can't import maps because i dont know where to look", new Vector3(1, 0, 0), 5);
                        return;
                    }

                    if (importing)
                    {
                        importing = false;
                        return;
                    }

                    importing = true;

                    new System.Threading.Thread(() => {
                        NotificationManager.ShowMessage("Started import process! press f3 to abort", new Vector3(0.5f, 0.5f, 0.5f), 2f);

                        byte[] buffer = new byte[1024];

                        int startCount = BeatmapCollection.Items.Count;
                        if (!string.IsNullOrEmpty(GlobalOptions.OsuFolder.Value))
                        {
                            foreach (var item in System.IO.Directory.EnumerateDirectories(osuSongsFolder))
                            {
                                if (!IsClosing)
                                {
                                    BeatmapMirror.ImportBeatmapFolder(item, ref buffer);

                                    if (!importing)
                                        break;
                                }
                            }
                        }
                        int endCount = BeatmapCollection.Items.Count;

                        NotificationManager.ShowMessage($"Import finished! Imported: {endCount - startCount} maps.", new Vector3(0.5f, 1, 0.5f), 10f);
                    }).Start();

                    return;
                }
                

                if (Input.IsKeyDown(Key.S) && Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyDown(Key.ShiftLeft) && Input.IsKeyDown(Key.AltLeft))
                {
                    Skin.Reload();
                    NotificationManager.ShowMessage($"Reloaded skin!", new Vector3(0.4f, 0.4f, 1f), 2f);

                    return;
                }

                if (e == Key.A)
                {
                    if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyDown(Key.AltLeft))
                    {
                        debugCameraActive = !debugCameraActive;
                        return;
                    }
                }

                if (e == Key.F10)
                {
                    GlobalOptions.EnableMouseButtons.Value = !GlobalOptions.EnableMouseButtons.Value;

                    NotificationManager.ShowMessage($"Mousebuttons: {GlobalOptions.EnableMouseButtons.Value}", ((Vector4)Color4.Violet).Xyz, 2f);
                }

                if (debugCameraActive)
                {
                    if (e == Key.W || e == Key.A || e == Key.S || e == Key.D)
                        return;
                }

                ScreenManager.OnKeyDown(e);
            };

            Input.InputContext.Keyboards[0].KeyUp += (s, e, x) =>
            {
                ScreenManager.OnKeyUp(e);
            };

            Input.InputContext.Keyboards[0].KeyChar += (s, e) =>
            {
                if (debugCameraActive)
                {
                    if (e == 'w' || e == 'a' || e == 's' || e == 'd')
                        return;
                }

                ScreenManager.OnTextInput(e);
            };
        }

        public Func<HttpClient> GetHttpClientFunc;

        public HttpClient GetPlatformHttpClient() => GetHttpClientFunc?.Invoke() ?? new HttpClient();
    }
}
