using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Decoders;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RTCircles
{
    public class Faderino : Drawable
    {
        public readonly SmoothFloat fade = new SmoothFloat();

        public void FadeTo(float opacity, float duration, EasingTypes easing) => fade.TransformTo(opacity, duration, easing);

        public override void Render(Graphics g)
        {
            if(fade.Value > 0f)
                g.DrawRectangle(Vector2.Zero, MainGame.WindowSize, new Vector4(0f, 0f, 0f, fade.Value));
        }

        public override void Update(float delta)
        {
            fade.Update(delta);
        }
    }

    public class OsuScreen : Screen
    {
        public bool RenderHUD = true;

        private Faderino faderino = new Faderino();

        private BreakPanel breakOverlay = new BreakPanel();

        private int objectIndex = 0;

        public bool IsCurrentlyBreakTime
        {
            get
            {
                if (OsuContainer.Beatmap.HitObjects.Count == 0)
                    return true;

                if (OsuContainer.SongPosition < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                    return true;

                if (objectIndex >= OsuContainer.Beatmap.HitObjects.Count)
                    return OsuContainer.SongPosition > OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime;

                if (objectIndex < 1)
                    return true;

                var prevObject = OsuContainer.Beatmap.HitObjects[objectIndex - 1];
                var currObject = OsuContainer.Beatmap.HitObjects[objectIndex];

                if (currObject.BaseObject.StartTime - prevObject.BaseObject.EndTime >= 3000)
                    return OsuContainer.SongPosition > prevObject.BaseObject.EndTime && OsuContainer.SongPosition < currObject.BaseObject.StartTime;

                return false;
            }
        }

        private readonly Cursor cursor = new Cursor();
        private float delta;

        public OsuScreen()
        {
            OsuContainer.HUD.Container = this;

            //Debug rudimentary touch support 
            Input.OnFingerDown += (finger) =>
            {
                if (ScreenManager.ActiveScreen == this)
                {
                    //Touch inputs seems to come from another thread, so it has to be scheduled unfortunately :/
                    GPUSched.Instance.Enqueue(() =>
                    {
                        OnKeyDown(OsuContainer.Key1);
                        for (int i = 0; i < 5; i++)
                        {
                            Add(new HUD.Firework(new Vector2(MainGame.WindowWidth * finger.X, MainGame.WindowHeight * finger.Y)));
                        }
                    });
                }
            };

            Input.OnFingerUp += (finger) =>
            {
                if (Input.TouchFingerEvents.Count == 0)
                {
                    GPUSched.Instance.Enqueue(() =>
                    {
                        OnKeyUp(OsuContainer.Key1);
                    });
                }
            };

            retryButton.OnClick += () =>
            {
                if (!pauseOverlayFade.HasCompleted)
                    return false;

                OnEnter();

                faderino.FadeTo(1f, 0.25f, EasingTypes.Out);

                pauseOverlayFade.TransformTo(0f, 0.5f, EasingTypes.Out, () =>
                {
                    OnEntering();
                    faderino.FadeTo(0f, 0.25f, EasingTypes.Out);

                    retryButton.IsAcceptingInput = true;
                });

                return true;
            };

            quitButton.OnClick += () =>
            {
                if (!pauseOverlayFade.HasCompleted)
                    return false;

                ScreenManager.GoBack();
                OsuContainer.Beatmap.Song.Play(false);
                pauseOverlayFade.Value = 0f;

                return true;
            };
        }

        public void ResetState()
        {
            Clear<DrawableHitCircle>();
            Clear<DrawableSpinner>();
            Clear<DrawableSlider>();
            Clear<HitJudgement>();
            Clear<WarningArrows>();
            Clear<FollowPoints>();

            pauseStart = double.MaxValue;
            canDie = true;

            OsuContainer.HUD.AddHP(1000);

            bgAlpha.Value = 1f;
            bgAlpha.Wait(0.2f);
            bgAlpha.TransformTo(0.1f, 1f, EasingTypes.Out);

            objectIndex = 0;

            OsuContainer.Combo = 0;
            OsuContainer.Count300 = 0;
            OsuContainer.Count100 = 0;
            OsuContainer.Count50 = 0;
            OsuContainer.CountMiss = 0;
            OsuContainer.Score = 0;
            OsuContainer.MaxCombo = 0;

            OsuContainer.Beatmap?.AutoGenerator.Reset();

            breakOverlay.Reset();

            Utils.Log($"Reset Osu State!!!", LogLevel.Important);
        }

        public override void OnEntering()
        {
            ResetState();

            OnKeyUp(OsuContainer.Key1);
            OnKeyUp(OsuContainer.Key2);
            OnMouseUp(MouseButton.Left);


            if (OsuContainer.Beatmap is null || OsuContainer.Beatmap.HitObjects.Count == 0)
                return;

            OsuContainer.SongPosition = OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime - 3000;

            if(OsuContainer.Beatmap.Song.PlaybackPosition >= 0)
                OsuContainer.Beatmap.Song.Play();
        }

        //en dårlig ide >.<
        public void EnsureObjectIndexSynchronization()
        {
            //Dont do shit if the beatmap is null lol 
            if (OsuContainer.Beatmap is null || OsuContainer.Beatmap.HitObjects.Count == 0)
                return;

            var hitObjects = OsuContainer.Beatmap.HitObjects;
            var songPos = OsuContainer.SongPosition;
            var preempt = OsuContainer.Beatmap.Preempt;

            var maxCount = OsuContainer.Beatmap.HitObjects.Count - 1;

            //If the song position is behind the first object, the object index is always 0
            if (songPos < hitObjects[0].BaseObject.StartTime - preempt)
            {
                if (objectIndex == 0)
                    return;

                Clear<DrawableHitCircle>();
                Clear<DrawableSpinner>();
                Clear<DrawableSlider>();
                Clear<HitJudgement>();
                Clear<WarningArrows>();
                Clear<FollowPoints>();

                OsuContainer.Beatmap.AutoGenerator.Reset();
                objectIndex = 0;
                Utils.Log($"Return to start!", LogLevel.Important);
                return;
            }

            //If the song position is infront of the last object, the object index is always bigger than the last object
            if(songPos > OsuContainer.Beatmap.HitObjects[^1].BaseObject.StartTime)
            {
                if (objectIndex >= OsuContainer.Beatmap.HitObjects.Count)
                    return;

                Clear<DrawableHitCircle>();
                Clear<DrawableSpinner>();
                Clear<DrawableSlider>();
                Clear<HitJudgement>();
                Clear<WarningArrows>();
                Clear<FollowPoints>();

                objectIndex = OsuContainer.Beatmap.HitObjects.Count;
                OsuContainer.Beatmap.AutoGenerator.End();
                return;
            }

            //Warning: None of this made sense in my head, and i'm just guessing, and it's likely to crash when i least expect it

            var previous = OsuContainer.Beatmap.HitObjects[(objectIndex - 1).Clamp(0, maxCount)].BaseObject;
            var now = OsuContainer.Beatmap.HitObjects[objectIndex.Clamp(0, maxCount)].BaseObject;

            //If the song position is greater than the object's start time at the current object index, the index is behind.
            bool isIndexBehind = songPos > now.StartTime;

            //If the song position is smaller than the previously spawned object's time, the index is too far ahead
            bool isIndexInfront = songPos < (previous.StartTime - preempt);

            //If both of these are true, im doing something wrong
            System.Diagnostics.Debug.Assert(!(isIndexBehind && isIndexInfront));

            if(isIndexBehind || isIndexInfront)
            {
                if(isIndexBehind)
                    Utils.Log($"The index was behind! {objectIndex}", LogLevel.Important);
                else
                    Utils.Log($"The index was infront! {objectIndex}", LogLevel.Important);

                OsuContainer.Beatmap.AutoGenerator.SyncToTime(songPos);

                Clear<DrawableHitCircle>();
                Clear<DrawableSpinner>();
                Clear<DrawableSlider>();
                Clear<HitJudgement>();
                Clear<WarningArrows>();
                Clear<FollowPoints>();

                while (true)
                {
                    if (isIndexBehind)
                    {
                        objectIndex++;
                        if (songPos < OsuContainer.Beatmap.HitObjects[objectIndex].BaseObject.StartTime)
                            break;
                    }
                    else
                    {
                        var newIndex = OsuContainer.Beatmap.HitObjects.FindIndex((o) => o.BaseObject.StartTime < songPos);

                        if (newIndex == -1)
                            newIndex = 0;

                        objectIndex = newIndex;

                        break;
                    }
                }
                Utils.Log($"The new index: {objectIndex}", LogLevel.Important);
            }
        }

        public override void OnExiting()
        {
            //We're dying or we're dead
            if (!canDie)
            {
                pauseOverlayFade.Value = 0;
                canDie = true;

                if (OsuContainer.Beatmap != null)
                {
                    OsuContainer.Beatmap.Song.Play(false);
                    dieAnim.ClearTransforms();
                    dieAnim.TransformTo((float)OsuContainer.Beatmap.Song.DefaultFrequency, 0.5f, EasingTypes.In);
                }
            }
        }

        public override void OnEnter()
        {
            if (!OsuContainer.CookieziMode)
            {
                NotificationManager.DoNotDisturb = true;
                Input.InputContext.Mice[0].Cursor.CursorMode = CursorMode.Hidden;
            }
        }

        public override void OnExit()
        {
            NotificationManager.DoNotDisturb = false;
            Input.InputContext.Mice[0].Cursor.CursorMode = CursorMode.Normal;
        }

        public override void Update(float delta)
        {
            this.delta = delta;
            if (OsuContainer.Beatmap is null)
                return;

            faderino.Update(delta);

            updateSpawnHitObjects();

            OsuContainer.HUD.Update(delta);

            breakOverlay.Update(delta);

            dieAnim.Update(delta);
            if (!dieAnim.HasCompleted && OsuContainer.Beatmap != null)
                OsuContainer.Beatmap.Song.Frequency = dieAnim.Value;

            if (OsuContainer.Beatmap.HitObjects.Count > 0 && OsuContainer.SongPosition > OsuContainer.Beatmap?.HitObjects[^1].BaseObject.EndTime + 1000 && ScreenManager.ActiveScreen == this)
                ScreenManager.SetScreen<ResultScreen>(false);

            base.Update(delta);
        }

        private void updateSpawnHitObjects()
        {
            if (objectIndex < OsuContainer.Beatmap.HitObjects.Count)
            {
                while (OsuContainer.SongPosition >= (OsuContainer.Beatmap.HitObjects[objectIndex].BaseObject.StartTime - OsuContainer.Beatmap.Preempt))
                {
                    var obj = OsuContainer.Beatmap.HitObjects[objectIndex];

                    //Spawn the incomming hitobject
                    Add(obj as Drawable);

                    objectIndex++;

                    if (objectIndex == OsuContainer.Beatmap.HitObjects.Count)
                        break;

                    var nextObj = OsuContainer.Beatmap.HitObjects[objectIndex];

                    spawnFollowPointsCheck(obj, nextObj);
                    spawnWarningArrowsCheck(obj, nextObj);
                }
            }
        }

        private void spawnFollowPointsCheck(IDrawableHitObject current, IDrawableHitObject next)
        {
            if (next.BaseObject.IsNewCombo == false)
            {
                var followPoint = ObjectPool<FollowPoints>.Take();

                followPoint.SetTarget(current.BaseObject, next.BaseObject);

                Add(followPoint);
            }
        }

        private void spawnWarningArrowsCheck(IDrawableHitObject current, IDrawableHitObject next)
        {
            if (next.BaseObject.StartTime - current.BaseObject.EndTime >= 3000)
            {
                breakOverlay.Show(current, next);

                //Warning arrows are supported but i like the lazer style countdown more

                //Add(new WarningArrows(next.BaseObject.StartTime - 3000));
            }
        }

        private float bgZoom = 0f;
        private SmoothFloat bgAlpha = new SmoothFloat() { Value = 0.1f };
        private void drawBackground(Graphics g)
        {
            var tex = OsuContainer.Beatmap.Background;

            if (tex is null || !GlobalOptions.RenderBackground.Value)
                return;

            bgAlpha.Update(delta);

            float width = MainGame.WindowWidth + bgZoom;
            float height = MainGame.WindowHeight + bgZoom;

            float aspectRatio = tex.Size.AspectRatio();

            Vector2 bgSize;
            bgSize = new Vector2(width, width / aspectRatio);

            if (bgSize.Y < MainGame.WindowHeight)
                bgSize = new Vector2(height * aspectRatio, height);

            g.DrawRectangleCentered(MainGame.WindowCenter, bgSize, new Vector4(1f, 1f, 1f, bgAlpha.Value), tex);

            //g.DrawRectangle(OsuContainer.FullPlayfield.Position, OsuContainer.FullPlayfield.Size, new Vector4(1f, 1f, 1f, 0.1f));
        }

        public override void Render(Graphics g)
        {
            if (OsuContainer.Beatmap is null)
                return;

            //Check if a slider is on screen, This is to avoid state changes per slider, and just render them all with minimal state changes
            if (DrawableSlider.SliderOnScreen && !GlobalOptions.UseFastSliders.Value)
            {
                var prevViewport = Viewport.CurrentViewport;
                GL.Instance.Enable(Silk.NET.OpenGLES.EnableCap.DepthTest);
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i] is DrawableSlider slider)
                        slider.SliderPath.OffscreenRender();
                }
                GL.Instance.Disable(Silk.NET.OpenGLES.EnableCap.DepthTest);
                FrameBuffer.BindDefault();
                Viewport.SetViewport(prevViewport);
            }

            drawBackground(g);
            //drawSmoke(g);

            //Here we render the hitobjects, they are apart of the screen
            base.Render(g);

            drawFlashlightOverlay(g);

            if(ScreenManager.ActiveScreen is not MenuScreen && RenderHUD)
                OsuContainer.HUD.Render(g);

            breakOverlay.Render(g);

            drawPauseOverlay(g);

            faderino.Render(g);

            drawCursor(g);
        }

        private Vector2 flashlightPosition;

        private void drawFlashlightOverlay(Graphics g)
        {
            double flDelta = MainGame.Instance.DeltaTime * 1000;

            flashlightPosition.X = (float)Interpolation.Damp(flashlightPosition.X, OsuContainer.CursorPosition.X, 0.95, flDelta);
            flashlightPosition.Y = (float)Interpolation.Damp(flashlightPosition.Y, OsuContainer.CursorPosition.Y, 0.95, flDelta);

            if (OsuContainer.Beatmap?.Mods.HasFlag(Mods.FL) ?? false)
                g.DrawRectangleCentered(flashlightPosition, Skin.FlashlightOverlay.Size * MainGame.Scale * 2.5f, Colors.White, Skin.FlashlightOverlay);
        }

    private List<(Vector2, double)> smokePoints = new List<(Vector2, double)>();
        private Vector2 lastPos;

        private void drawSmoke(Graphics g)
        {
            Vector2 smokeParticleSize = new Vector2(32f) * MainGame.Scale;

            if ((lastPos - Input.MousePosition).Length > smokeParticleSize.X / 2)
            {
                lastPos = Input.MousePosition;
                if (Input.IsKeyDown(OsuContainer.SmokeKey))
                    smokePoints.Add((lastPos, MainGame.Instance.TotalTime));

            }

            for (int i = smokePoints.Count - 2; i >= 0; i--)
            {
                var cur = smokePoints[i];
                var next = smokePoints[i + 1];

                double time = MainGame.Instance.TotalTime - cur.Item2;

                Vector4 color = new Vector4(1f);
                color.W = (float)MainGame.Instance.TotalTime.Map(cur.Item2, cur.Item2 + 10, 10, 0).Clamp(0, 1);

                if (color.W == 0)
                    smokePoints.RemoveAt(i);

                g.DrawDottedLine(cur.Item1, next.Item1, Skin.Smoke, color, smokeParticleSize, smokeParticleSize.X / 2);
            }
        }

        private void drawCursor(Graphics g)
        {
            //Check if our current mods is auto, to see if we need to render the auto cursor
            if (OsuContainer.CookieziMode)
            {
                OsuContainer.Beatmap.AutoGenerator.Update();
                Vector2 autoPos = OsuContainer.MapToPlayfield(OsuContainer.Beatmap.AutoGenerator.CurrentPosition);

                OsuContainer.CustomCursorPosition = autoPos;
                //Else if not playing with auto, just render the cursor normally.
            }
            else
                OsuContainer.CustomCursorPosition = null;

            bool isPaused = pauseOverlayFade.Value == 1;

            cursor.Render(g, delta, OsuContainer.CustomCursorPosition ?? Input.MousePosition, isPaused ? Vector4.Zero : Colors.White);
        }

        private double pauseStart;

        private bool isPaused => pauseOverlayFade.Value > 0;

        private SmoothFloat pauseOverlayFade = new SmoothFloat();
        private Button retryButton = new Button() { Color = Colors.From255RGBA(255, 255, 0, 200), Text = "Retry" };
        private Button quitButton = new Button() { Color = Colors.From255RGBA(0,100,255, 255), Text = "Quit"};

        private void drawPauseOverlay(Graphics g)
        {
            pauseOverlayFade.Update((float)MainGame.Instance.DeltaTime);

            if (pauseOverlayFade.Value == 0)
                return;

            retryButton.Color.W = pauseOverlayFade.Value;
            retryButton.TextColor.W = pauseOverlayFade.Value;

            quitButton.TextColor.W = pauseOverlayFade.Value;
            quitButton.Color.W = pauseOverlayFade.Value;

            retryButton.Size = new Vector2(300, 50);
            retryButton.Position = MainGame.WindowCenter - retryButton.Size / 2f;

            quitButton.Position = retryButton.Position + new Vector2(0, retryButton.Size.Y + 20);
            quitButton.Size = new Vector2(300, 50);

            quitButton.Update((float)MainGame.Instance.DeltaTime);
            quitButton.Render(g);

            retryButton.Update((float)MainGame.Instance.DeltaTime);
            retryButton.Render(g);
        }

        private SmoothFloat dieAnim = new SmoothFloat() { Value = 1f };

        //buggy af, everything spaghetti code
        private bool canDie = true;
        public void reportDeath()
        {
            if (canDie && ScreenManager.ActiveScreen == this && !OsuContainer.CookieziMode)
            {
                canDie = false;
                var start = OsuContainer.Beatmap.Song.Frequency;
                dieAnim.Value = (float)start;
                dieAnim.TransformTo(0f, 3f, EasingTypes.Out, () =>
                {
                    OsuContainer.Beatmap.Song.Pause();
                    OsuContainer.Beatmap.Song.Frequency = start;
                    pauseOverlayFade.TransformTo(1f, 0.25f, EasingTypes.Out);
                    OnExit();
                });
            }
        }

        private bool showedPauseMessage;

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                if (!dieAnim.HasCompleted)
                {
                    dieAnim.EndCurrentTransform();
                    return;
                }

                if(OsuContainer.Beatmap == null || OsuContainer.SongPosition <= 0)
                {
                    ScreenManager.GoBack();
                    return;
                }

                //Hvis mappet er slut, og man trykker esc så bliver man sendt til result screen
                if (OsuContainer.Beatmap.HitObjects.Count > 0 && OsuContainer.SongPosition > OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime + 400)
                {
                    ScreenManager.SetScreen<ResultScreen>(false);
                    return;
                }

                if (Math.Abs(pauseStart - MainGame.Instance.TotalTime) < 2 && !isPaused)
                {
                    if (!showedPauseMessage)
                    {
                        NotificationManager.ShowMessage("Please wait atleast 2 seconds between pauses", ((Vector4)Color4.SteelBlue).Xyz, 5);
                        showedPauseMessage = true;
                    }
                }
                else
                {
                    if(pauseOverlayFade.Value == 0)
                    {
                        OsuContainer.Beatmap?.Song.Pause();

                        pauseOverlayFade.TransformTo(1f, 0.25f, EasingTypes.Out);
                        OnExit();
                        showedPauseMessage = false;
                    }
                    else if(pauseOverlayFade.Value == 1)
                    {
                        pauseStart = MainGame.Instance.TotalTime;
                        OnEnter();
                        pauseOverlayFade.TransformTo(0f, 0.5f, EasingTypes.In, () =>
                        {
                            OsuContainer.Beatmap?.Song.Play(false);
                        });
                    }
                }
            }

            if (!isPaused)
            {
                OsuContainer.KeyDown(key);
                base.OnKeyDown(key);
            }
        }

        public override void OnKeyUp(Key key)
        {
            OsuContainer.KeyUp(key);
            base.OnKeyUp(key);
        }

        public override void OnMouseDown(MouseButton button)
        {
            if (!isPaused)
            {
                OsuContainer.MouseDown(button);
                base.OnMouseDown(button);
            }
            else
            {
                retryButton.OnMouseDown(button);
                quitButton.OnMouseDown(button);
            }
        }

        public override void OnMouseUp(MouseButton button)
        {
            OsuContainer.MouseUp(button);
            base.OnMouseUp(button);

            retryButton.OnMouseUp(button);
            quitButton.OnMouseUp(button);
        }

        public override void OnMouseWheel(float position)
        {
#if DEBUG
            OsuContainer.Beatmap.Song.Frequency = Math.Max(OsuContainer.Beatmap.Song.Frequency + (2000 * position), 100);
#endif
            base.OnMouseWheel(position);
        }
    }
}
