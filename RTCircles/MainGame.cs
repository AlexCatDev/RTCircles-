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
using System.Text;

namespace RTCircles
{
    public static class GlobalOptions
    {
        #region booleans
        public readonly static Option<bool> Bloom
            = Option<bool>.CreateProxy("Bloom", (value) => { GPUSched.Instance.Enqueue(() => { PostProcessing.Bloom = value; }); }, false, "Looks like shit lmao");

        public readonly static Option<bool> MotionBlur
            = Option<bool>.CreateProxy("MotionBlur", (value) => { GPUSched.Instance.Enqueue(() => { PostProcessing.MotionBlur = value; }); }, false, "This only looks good if you get over 800 fps");

        public readonly static Option<bool> UseFancyCursorTrail = new Option<bool>("UseFancyCursorTrail", false);

        public readonly static Option<bool> SliderSnakeIn = new Option<bool>("SliderSnakeIn", true) { Description = "Negligible performance hit" };

        public readonly static Option<bool> SliderSnakeOut = new Option<bool>("SliderSnakeOut", true) { Description = "Significant performance hit" };

        public readonly static Option<bool> SliderSnakeExplode = new Option<bool>("SliderSnakeExplode", true) { Description = "No performance hit" };

        public readonly static Option<bool> AutoCursorDance = new Option<bool>("AutoCursorDance", false);

        public readonly static Option<bool> ShowRenderGraphOverlay = new Option<bool>("ShowRenderGraphOverlay", false);

        public readonly static Option<bool> ShowLogOverlay = new Option<bool>("ShowLogOverlay", false);

        public readonly static Option<bool> ShowFPS = new Option<bool>("ShowFPS", true);

        public readonly static Option<bool> KiaiCatJam = new Option<bool>("KiaiCatJam", false);

        public readonly static Option<bool> AllowMapHitSounds = new Option<bool>("AllowMapHitSounds", true);

        public readonly static Option<bool> RenderBackground = new Option<bool>("RenderBackground", false);

        public readonly static Option<bool> RGBCircles = new Option<bool>("RGBCircles", false) { Description = "RGB ;) (Might not look good with all skins)" };

        public readonly static Option<bool> UseFastSliders = new Option<bool>("UseFastSliders", false) { Description = "Low quality sliders (Requires map reload, Recommended to use slider snaking)"};

        public readonly static Option<bool> EnableMouseButtons = new Option<bool>("MouseButtons", false) { Description = "Enable Mouse Buttons?" };
        #endregion
        #region doubles
        public readonly static Option<double> GlobalVolume = Option<double>.CreateProxy("GlobalVolume", (volume) => Sound.GlobalVolume = volume, 0.3);

        public readonly static Option<double> SkinVolume = Option<double>.CreateProxy("SkinVolume", (volume) => {
            Skin.Hitsounds?.SetVolume(volume);
            OsuContainer.Beatmap?.Hitsounds?.SetVolume(volume);
        }, 1);

        public readonly static Option<double> SongVolume = Option<double>.CreateProxy("SongVolume", (volume) => {
            if(OsuContainer.Beatmap != null)
                OsuContainer.Beatmap.Song.Volume = volume;
        }, 1);
        #endregion

        public static void Init() 
        {
            Utils.Log($"Loaded Settings", LogLevel.Info);
            //We need to access a variable to instantiate every variable lol
            var ok = Bloom.Value;
        }
    }

    public static class NotificationManager
    {
        public static bool DoNotDisturb;

        public static int MaxVisibleNotifications = 16;

        class Notification
        {
            public string Text;
            public Vector3 Color;
            public float Duration;

            internal bool DeleteMe;

            private SmoothFloat popupAnimation = new SmoothFloat();

            private SmoothFloat alphaAnimation = new SmoothFloat();

            public float Progress => popupAnimation.Value;

            public float Alpha => alphaAnimation.Value;

            public bool IsFinished;

            internal Action ClickAction;

            public Notification(string text, Vector3 color, float duration, Action clickAction)
            {
                this.Text = text;
                this.Color = color;
                this.Duration = duration;
                this.ClickAction = clickAction;

                popupAnimation.TransformTo(1f, 0.70f, EasingTypes.OutElasticHalf);
                alphaAnimation.Value = 1;
                alphaAnimation.Wait(Duration, () =>
                {
                    if (alphaAnimation.PendingTransformCount == 0)
                        Fadeout(1f);
                });
            }

            public void Fadeout(float duration)
            {
                alphaAnimation.ClearTransforms();
                alphaAnimation.TransformTo(0f, duration, EasingTypes.OutQuint, () => { IsFinished = true; });
            }

            public void Update(float delta)
            {
                popupAnimation.Update(delta);
                alphaAnimation.Update(delta);
            }
        }

        static List<Notification> notifications = new List<Notification>();

        static Queue<Notification> queue = new Queue<Notification>();

        public static void Render(Graphics g)
        {
            Vector2 box = new Vector2(25) * MainGame.Scale;

            float scale = 0.5f * MainGame.Scale;

            float spacingY = Font.DefaultFont.Size * scale + box.Y / 2;

            Vector2 position = Vector2.Zero;
            position.X = MainGame.WindowWidth;
            position.Y = MainGame.WindowHeight - spacingY;

            float spacingX = 25 * MainGame.Scale;

            for (int i = 0; i < notifications.Count; i++)
            {
                var notif = notifications[i];

                Vector2 textSize = Font.DefaultFont.MessureString(notif.Text, scale);

                Vector2 textOffset = Vector2.Zero;

                textOffset.X -= notif.Progress.Map(0f, 1f, 0, textSize.X + spacingX);

                Rectangle clickBox = new Rectangle(position + textOffset - box / 2, textSize + box);

                //Bg rectangle
                //g.DrawRectangle(clickBox.Position, clickBox.Size, new Vector4(notf.Color, notf.Alpha));

                float cornerRadius = 15f * MainGame.Scale;

                g.DrawRoundedRect((Vector2i)clickBox.Center, clickBox.Size, new Vector4(notif.Color, notif.Alpha), cornerRadius);

                float border = 0.88f;
                clickBox = new Rectangle(position + textOffset - ((box * border) / 2), textSize + box * border);
                Vector3 bgColor = (clickBox.IntersectsWith(Input.MousePosition) ? new Vector3(0.1f) : new Vector3(0));

                g.DrawRoundedRect((Vector2i)clickBox.Center, clickBox.Size, new Vector4(bgColor, notif.Alpha), cornerRadius);

                g.DrawString(notif.Text, Font.DefaultFont, position + textOffset, new Vector4(notif.Color, notif.Alpha), scale);
                position.Y -= spacingY;
            }
        }

        public static bool OnMouseDown(MouseButton mouseButton)
        {
            Vector2 box = new Vector2(25) * MainGame.Scale;

            float scale = 0.5f * MainGame.Scale;

            float spacingY = Font.DefaultFont.Size * scale + box.Y / 2;

            Vector2 position = Vector2.Zero;
            position.X = MainGame.WindowWidth;
            position.Y = MainGame.WindowHeight - spacingY;

            float spacingX = 25 * MainGame.Scale;

            for (int i = 0; i < notifications.Count; i++)
            {
                var notf = notifications[i];

                Vector2 textSize = Font.DefaultFont.MessureString(notf.Text, scale);

                Vector2 textOffset = Vector2.Zero;

                textOffset.X -= notf.Progress.Map(0f, 1f, 0, textSize.X + spacingX);

                Rectangle clickBox = new Rectangle(position + textOffset - box / 2, textSize + box);

                if (clickBox.IntersectsWith(Input.MousePosition) && notf.Alpha > 0.5f && !notf.IsFinished)
                {
                    notf.ClickAction?.Invoke();
                    notf.Fadeout(0.25f);
                    notf.IsFinished = true;
                    return true;
                }

                position.Y -= spacingY;
            }

            return false;
        }

        public static void Update(float delta)
        {
            if (!DoNotDisturb && queue.Count > 0 && notifications.Count((o) => !o.IsFinished) <= MaxVisibleNotifications)
                addWhereAvailable(queue.Dequeue());

            bool cleanDead = false;
            for (int i = notifications.Count - 1; i >= 0; i--)
            {
                notifications[i].Update(delta);

                if(notifications[i].DeleteMe)
                    cleanDead = true;
            }

            if(cleanDead)
            notifications.RemoveAll((o) => o.DeleteMe);
        }

        private static void addWhereAvailable(Notification notif)
        {
            for (int i = 0; i < notifications.Count; i++)
            {
                if (notifications[i].IsFinished)
                {
                    notifications[i].DeleteMe = true;

                    notifications.Insert(i, notif);

                    return;
                }
            }

            notifications.Add(notif);
        }

        public static void ShowMessage(string text, Vector3 color, float duration, Action clickAction = null) =>
            queue.Enqueue(new Notification(text, color, duration, clickAction));
    }

    public class MainGame : GameBase
    {
        public static MainGame Instance { get; private set; }

        public MainGame()
        {
            Instance = this;
        }

        private Graphics g;
        public static Matrix4 Projection { get; private set; }

        private SmoothFloat shakeKiai = new SmoothFloat();
        private Matrix4 shakeMatrix => 
            Matrix4.CreateTranslation(new Vector3(RNG.Next(-10, 10) * shakeKiai.Value, RNG.Next(-10, 10) * shakeKiai.Value, 0f)) * Projection;

        private bool debugCameraActive = false;

        private SmoothFloat volumeBarFade = new SmoothFloat();

        public override void OnLoad()
        {
            string build = "RELEASE";
#if DEBUG
            build = "DEBUG";
#endif

            //Make some sort of build versioning idk
            Version = $"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}";

            Utils.WriteToConsole = true;

            Utils.IgnoredLogLevels.Add(LogLevel.Debug);
#if RELEASE
            Utils.IgnoredLogLevels.Add(LogLevel.Info);
            Utils.IgnoredLogLevels.Add(LogLevel.Success);
#endif
            //IsMultiThreaded = true;

            VSync = false;

            //Rens det her lort

            MaxAllowedDeltaTime = 0.1;
            Skin.Load("");
            //GlobalOptions.Init();
            //Skin.Load(@"C:\Users\user\Desktop\osu!\Skins\-  idke 1.2 without sliderendcircle");
            //Skin.Load(@"C:\Users\user\Desktop\osu!\Skins\-  AlexSkin 1.0");
            //Skin.Load(@"C:\Users\user\Desktop\whitecat skin");

            g = new Graphics();

                NotificationManager.ShowMessage($"Under Construction !",
                    ((Vector4)Color4.Orange).Xyz, 10);

            GPUSched.Instance.EnqueueDelayed(() =>
            {
                NotificationManager.ShowMessage(
                    $"Could not connect to server", ((Vector4)Color4.Crimson).Xyz, 5);
            }, delay: 10000);

            GPUSched.Instance.EnqueueDelayed(() =>
            {
                NotificationManager.ShowMessage($"test clickable notification",
                    ((Vector4)Color4.CornflowerBlue).Xyz, 5, () =>
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            NotificationManager.ShowMessage($"{i}", ((Vector4)Color4.Peru).Xyz, 1);
                        }
                    });
            }, delay: 20000);

            OsuContainer.OnKiai += () =>
            {
                //Det her ser bedere ud tbh
                shakeKiai.Value = 2f;
                shakeKiai.TransformTo(0f, 1f, EasingTypes.OutQuart);
            };

            Input.InputContext.Mice[0].MouseDown += (s, e) =>
            {
                if(!NotificationManager.OnMouseDown(e))
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
                    GlobalOptions.GlobalVolume.Value = (GlobalOptions.GlobalVolume.Value + e.Y * 0.01f).Clamp(0, 1);
                    return;
                }

                if (debugCameraActive && !Input.IsKeyDown(Key.ControlLeft))
                {
                    debugCameraScale += e.Y*0.03f;
                    return;
                }

                ScreenManager.OnMouseWheel(e.Y);
            };

            Input.InputContext.Keyboards[0].KeyDown += (s, e, x) =>
            {
                if(e == Key.A)
                {
                    if (Input.IsKeyDown(Key.ControlLeft) && Input.IsKeyDown(Key.AltLeft))
                    {
                        debugCameraActive = !debugCameraActive;
                        return;
                    }
                }

                if(e == Key.F10)
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

            float outroTime = 0;
            float outroDuration = 0.125f;
            ScreenManager.OnOutroTransition += (delta) =>
            {
                outroTime += delta;
                outroTime = outroTime.Clamp(0f, outroDuration);
                float progress = Interpolation.ValueAt(outroTime, 1f, 0f, 0f, outroDuration, EasingTypes.None);
                g.DrawRectangle(Vector2.Zero, WindowSize, new Vector4(0f, 0f, 0f, progress));
                if (outroTime == outroDuration)
                {
                    outroTime = 0;
                    return true;
                }

                return false;
            };

            float introDuration = 0.125f;
            float introTime = 0;
            ScreenManager.OnIntroTransition += (delta) =>
            {
                introTime += delta;
                introTime = introTime.Clamp(0f, introDuration);
                float progress = Interpolation.ValueAt(introTime, 0f, 1f, 0f, introDuration, EasingTypes.None);
                g.DrawRectangle(Vector2.Zero, WindowSize, new Vector4(0f, 0f, 0f, progress));

                if (introTime == introDuration)
                {
                    introTime = 0;
                    return true;
                }

                return false;
            };

            lastOpened = Settings.GetValue<DateTime>("LastOpened", out bool exists);
            Settings.SetValue(DateTime.Now, "LastOpened");

            Input.OnBackPressed += () =>
            {
                ScreenManager.GoBack();
            };

            ScreenManager.SetScreen<MenuScreen>(false);
        }

        private DateTime lastOpened;

        private ulong prevVertices;
        private ulong prevIndices;
        private ulong prevTriangles;

        private List<double> renderTimes = new List<double>();

        private Camera debugCamera = new Camera();
        private float debugCameraScale = 1f;

        private double totalDeltaTimes = 0;
        private int deltaTimesCount = 0;
        private double averageDeltaTime = 1000;

        public override void OnRender()
        {
            totalDeltaTimes += DeltaTime;
            deltaTimesCount++;

            if(totalDeltaTimes >= 1)
            {
                averageDeltaTime = totalDeltaTimes / deltaTimesCount;

                totalDeltaTimes -= 1;
                deltaTimesCount = 0;
            }

            if (DeltaTime > 0.033)
                NotificationManager.ShowMessage($"<30fps Lag spike ! {DeltaTime *1000:F2}ms", ((Vector4)Color4.Yellow).Xyz, 3f);

            g.Projection = PostProcessing.MotionBlur && (ScreenManager.ActiveScreen is OsuScreen) || ScreenManager.ActiveScreen is MenuScreen ? shakeMatrix : Projection;

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

        public void FakeWindowSize(Vector2 newSize, Action a)
        {
            var prev = WindowSize;
            WindowSize = newSize;
            a.Invoke();
            WindowSize = prev;
        }

        private void drawVolumeBar(Graphics g)
        {
            volumeBarFade.Update((float)DeltaTime);
            if (volumeBarFade.Value == 0)
                return;

            Vector2 pos = new Vector2(10, 10);
            Vector2 size = new Vector2(500, 30);

            g.DrawRectangle(pos, size, Colors.From255RGBA(31, 31, 31, 127 *volumeBarFade.Value));
            g.DrawRectangle(pos, new Vector2(size.X * (float)GlobalOptions.GlobalVolume.Value, size.Y), Colors.From255RGBA(0, 255, 128, 255 * volumeBarFade.Value));

            g.DrawStringCentered($"Volume: {GlobalOptions.GlobalVolume.Value * 100:F0}", Font.DefaultFont, pos + size / 2f, new Vector4(1f, 1f, 1f, volumeBarFade.Value), 0.5f);
        }

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
                ulong diffVertices = g.VerticesDrawn - prevVertices;
                prevVertices = g.VerticesDrawn;

                ulong diffIndices = g.IndicesDrawn - prevIndices;
                prevIndices = g.IndicesDrawn;

                ulong diffTriangles = g.TrianglesDrawn - prevTriangles;
                prevTriangles = g.TrianglesDrawn;

                double verticesPerSecond = (diffVertices) * (1.0 / DeltaTime);

                double indicesPerSecond = (diffIndices) * (1.0 / DeltaTime);

                double trianglesPerSecond = (diffTriangles) * (1.0 / DeltaTime);

                const float scale = 0.35f;

                ms = Interpolation.Damp(ms, DeltaTime * 1000, 0.25, DeltaTime);

                if (double.IsNaN(ms))
                    ms = 0;

                double mem = GC.GetTotalMemory(false) / 1048576d;

                string text = $"Mem: {mem:F0}mb DrawCalls: {GL.DrawCalls} FPS: {FPS}/{ms:F2}ms";

                Vector2 hoverSize = Font.DefaultFont.MessureString(text, scale);
                Vector2 hoverPos = new Vector2(WindowWidth, WindowHeight) - hoverSize;

                if(trueHoverSize is null)
                    trueHoverSize = hoverSize;

                if (trueHoverPos is null)
                    trueHoverPos = hoverPos;

                if (new Rectangle(trueHoverPos.Value, trueHoverSize.Value).IntersectsWith(Input.MousePosition))
                {
                    text+= $"\nGPU: {GL.GLRenderer}\n" +
                        $"OpenGL: {GL.GLVersion}\n" +
                        $"Vertices: {Utils.ToKMB(verticesPerSecond)}/s\n" +
                        $"Indices: {Utils.ToKMB(indicesPerSecond)}/s\n" +
                        $"Tris: {Utils.ToKMB(trianglesPerSecond)}/s\n" +
                        $"Textures: {Easy2D.Texture.TextureCount}\n" +
                        $"Framework: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture}\n" +
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

                Vector2 textSize = Font.DefaultFont.MessureString(text, scale);
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

        public static float Scale { get; private set; }

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

        public override void OnUpdate()
        {
            if (ScreenManager.ActiveScreen is OsuScreen)
                PostProcessing.BloomThreshold = shakeKiai.Value.Map(2, 0, 0.2f, 0.8f);
            else
                PostProcessing.BloomThreshold = 0.8f;

            shakeKiai.Update((float)DeltaTime);
            OsuContainer.Update((float)DeltaTime);
            ScreenManager.Update((float)DeltaTime);
        }

        public override void OnOpenFile(string path)
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
