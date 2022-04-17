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
        private SmoothFloat fade = new SmoothFloat();

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
        private Faderino faderino = new Faderino();

        private BreakPanel breakOverlay = new BreakPanel();

        private int objectIndex = 0;

        private Cursor cursor = new Cursor();
        private float delta;
        public OsuScreen()
        {
            OsuContainer.HUD.Container = this;

            //Debug rudimentary touch support 
            Input.OnFingerDown += (finger) =>
            {
                if (ScreenManager.ActiveScreen == this)
                {
                    OnKeyDown(OsuContainer.Key1);

                    for (int i = 0; i < 5; i++)
                    {
                        Add(new HUD.Firework(new Vector2(MainGame.WindowWidth * finger.X, MainGame.WindowHeight * finger.Y)));
                    }
                }
            };

            Input.OnFingerUp += (finger) =>
            {
                if (Input.TouchFingerEvents.Count == 0)
                    OnKeyUp(OsuContainer.Key1);
            };

            OsuContainer.OnKiai += () =>
            {
                bgAlpha.Value = 0.3f;
                bgAlpha.TransformTo(0.1f, 1f, EasingTypes.Out);
            };

            bgAlpha.Value = 0.3f;

            retryButton.OnClick += (s, e) =>
            {
                faderino.FadeTo(1f, 0.25f, EasingTypes.Out);

                pauseOverlayFade.TransformTo(0f, 0.5f, EasingTypes.Out, () =>
                {
                    OnEntering();
                    faderino.FadeTo(0f, 0.25f, EasingTypes.Out);
                });
            };

            quitButton.OnClick += (s, e) =>
            {
                ScreenManager.GoBack();
                OsuContainer.Beatmap.Song.Play(false);
                pauseOverlayFade.Value = 0f;
            };
        }

        public override void OnEntering()
        {
            Clear<DrawableHitCircle>();
            Clear<DrawableSpinner>();
            Clear<DrawableSlider>();
            Clear<HitJudgement>();
            Clear<WarningArrows>();
            Clear<FollowPoints>();

            pauseStart = double.MaxValue;
            dieFlag = false;

            bgZoom = 100;
            bgAlpha.Value = 1f;
            bgAlpha.TransformTo(0.1f, 1f, EasingTypes.Out);

            objectIndex = 0;

            OsuContainer.Combo = 0;
            OsuContainer.Count300 = 0;
            OsuContainer.Count100 = 0;
            OsuContainer.Count50 = 0;
            OsuContainer.CountMiss = 0;
            OsuContainer.Score = 0;
            OsuContainer.MaxCombo = 0;

            if (OsuContainer.Beatmap is null || OsuContainer.Beatmap.HitObjects.Count == 0)
                return;

            OsuContainer.SongPosition = OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime - 3000;

            OnKeyUp(OsuContainer.Key1);
            OnKeyUp(OsuContainer.Key2);
            OnMouseUp(MouseButton.Left);

            if(OsuContainer.Beatmap.Song.PlaybackPosition >= 0)
                OsuContainer.Beatmap.Song.Play();

            OsuContainer.Beatmap.AutoGenerator.Reset();

            breakOverlay.Reset();
        }

        public void SyncObjectIndexToTime()
        {
            Clear<DrawableHitCircle>();
            Clear<DrawableSpinner>();
            Clear<DrawableSlider>();
            Clear<HitJudgement>();
            Clear<WarningArrows>();
            Clear<FollowPoints>();

            if (OsuContainer.Beatmap is null || OsuContainer.Beatmap.HitObjects.Count == 0)
                return;

            objectIndex = -1;

            if (OsuContainer.SongPosition < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                objectIndex = 0;
            else
                objectIndex = OsuContainer.Beatmap.HitObjects.FindIndex((o) => o.BaseObject.EndTime > OsuContainer.SongPosition);

            if (objectIndex == -1)
                objectIndex = OsuContainer.Beatmap.HitObjects.Count;

            OsuContainer.Beatmap.AutoGenerator.SyncToTime(OsuContainer.SongPosition);
        }

        public override void OnExiting()
        {
            Clear<DrawableHitCircle>();
            Clear<DrawableSpinner>();
            Clear<DrawableSlider>();
            Clear<HitJudgement>();
            Clear<WarningArrows>();
            Clear<FollowPoints>();
        }

        public override void OnEnter()
        {
            Input.InputContext.Mice[0].Cursor.CursorMode = CursorMode.Hidden;
        }

        public override void OnExit()
        {
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

            dieAnim.Update(delta);

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

            if (OsuContainer.Combo % 50 == 0 && ComboBurst.CanSpawn && OsuContainer.Combo > 0 && Skin.ComboBurst is not null)
                Add(new ComboBurst());
        }

        private void spawnFollowPointsCheck(IDrawableHitObject current, IDrawableHitObject next)
        {
            if (next.BaseObject.IsNewCombo == false)
                Add(new FollowPoints(current.BaseObject, next.BaseObject));
        }

        public bool IsCurrentlyBreakTime
        {
            get
            {
                if (OsuContainer.Beatmap.HitObjects.Count == 0)
                    return true;

                if (OsuContainer.SongPosition < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                    return true;

                if (objectIndex - 1 < 0)
                    return true;

                if (objectIndex >= OsuContainer.Beatmap.HitObjects.Count)
                    return OsuContainer.SongPosition > OsuContainer.Beatmap.HitObjects[^1].BaseObject.EndTime;

                var prevObject = OsuContainer.Beatmap.HitObjects[objectIndex -1];
                var currObject = OsuContainer.Beatmap.HitObjects[objectIndex];

                if (currObject.BaseObject.StartTime - prevObject.BaseObject.EndTime >= 3000)
                    return OsuContainer.SongPosition > prevObject.BaseObject.EndTime && OsuContainer.SongPosition < currObject.BaseObject.StartTime;

                return false;
            }
        }
        private void spawnWarningArrowsCheck(IDrawableHitObject current, IDrawableHitObject next)
        {
            if (next.BaseObject.StartTime - current.BaseObject.EndTime >= 3000)
            {
                breakOverlay.Show(current, next);
                //breakOverlay.Show(next.BaseObject.StartTime - 1000);
                //Add(new WarningArrows(next.BaseObject.StartTime - 3000));
            }
        }

        private float bgZoom = 0f;
        private SmoothFloat bgAlpha = new SmoothFloat();
        private void drawBackground(Graphics g)
        {
            var tex = OsuContainer.Beatmap.Background;

            if (tex is null || !GlobalOptions.RenderBackground.Value)
                return;

            bgAlpha.Update(delta);

            if (OsuContainer.IsKiaiTimeActive)
                bgZoom = 100f;
            else
                bgZoom = MathHelper.Lerp(bgZoom, 0f, delta * 10f);

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

            drawBackground(g);

            drawSmoke(g);

            base.Render(g);

            OsuContainer.HUD.Render(g);

            breakOverlay.Render(g);

            drawPauseOverlay(g);

            faderino.Render(g);

            drawCursor(g);
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
                OsuContainer.Beatmap.AutoGenerator.Update(OsuContainer.SongPosition);
                Vector2 autoPos = OsuContainer.MapToPlayfield(OsuContainer.Beatmap.AutoGenerator.CurrentPosition);

                OsuContainer.CustomCursorPosition = autoPos;
                cursor.Render(g, delta, autoPos, Colors.White);
                //Else if not playing with auto, just render the cursor normally.
            }
            else
            {
                OsuContainer.CustomCursorPosition = null;
                cursor.Render(g, delta, Input.MousePosition, Colors.White);
            }
        }

        private double pauseStart;

        private bool isPaused => pauseOverlayFade.Value > 0;

        private SmoothFloat pauseOverlayFade = new SmoothFloat();
        private Button retryButton = new Button() { Color = Colors.From255RGBA(255, 255, 0, 200), Text = "Retry" };
        private Button quitButton = new Button() { Color = Colors.From255RGBA(0,100,255, 255), Text = "Quit"};

        private void drawPauseOverlay(Graphics g)
        {
            pauseOverlayFade.Update((float)MainGame.Instance.DeltaTime);

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

        private bool dieFlag;
        public void dieLol()
        {
            return;

            if (!dieFlag && ScreenManager.ActiveScreen == this)
            {
                dieFlag = true;
                dieAnim.Value = 1;
                dieAnim.TransformTo(0f, 0.5f, EasingTypes.Out, () =>
                {
                    ScreenManager.GoBack();
                });
            }
        }

        private bool showedPauseMessage;

        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                //If the current beatmap is null or the map is finished just return for now
                if (OsuContainer.SongPosition > OsuContainer.Beatmap?.HitObjects[^1].BaseObject.EndTime + 400 || (OsuContainer.Beatmap?.Song.IsStopped ?? true))
                {
                    ScreenManager.GoBack();
                    return;
                }

                if (Math.Abs(pauseStart - MainGame.Instance.TotalTime) < 3 && !isPaused)
                {
                    if (!showedPauseMessage)
                    {
                        NotificationManager.ShowMessage("Please wait atleast 3 seconds between pauses", ((Vector4)Color4.SteelBlue).Xyz, 5);
                        showedPauseMessage = true;
                    }
                }
                else
                {
                    if(pauseOverlayFade.Value == 0)
                    {
                        OsuContainer.Beatmap?.Song.Pause();
                        pauseStart = MainGame.Instance.TotalTime;

                        pauseOverlayFade.TransformTo(1f, 0.25f, EasingTypes.Out);
                        OnExit();
                        showedPauseMessage = false;
                    }
                    else if(pauseOverlayFade.Value == 1)
                    {
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
