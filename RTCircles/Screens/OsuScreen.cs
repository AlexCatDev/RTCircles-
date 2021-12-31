using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RTCircles
{

    public class OsuScreen : Screen
    {
        private int objectIndex = 0;

        private Cursor cursor = new Cursor();
        private float delta;
        public OsuScreen()
        {
            OsuContainer.HUD.Container = this;
            Skin.Load("");

            //Debug rudimentary touch support 
            Input.OnFingerDown += (finger) =>
            {
                if (ScreenManager.ActiveScreen() == this)
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
        }

        public override void OnEntering()
        {
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

            float startDelay = ScreenManager.ActiveScreen() is MapSelectScreen ? (float)OsuContainer.Beatmap.Preempt : 3000;

            OsuContainer.SongPosition = OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime - startDelay;

            OnKeyUp(OsuContainer.Key1);
            OnKeyUp(OsuContainer.Key2);
            OnMouseUp(MouseButton.Left);

            if(OsuContainer.Beatmap.Song.PlaybackPosition >= 0)
                OsuContainer.Beatmap.Song.Play();
        }

        public void SyncObjectIndexToTime()
        {
            Clear<DrawableHitCircle>();
            Clear<DrawableSpinner>();
            Clear<DrawableSlider>();
            Clear<HitJudgement>();
            Clear<WarningArrows>();
            Clear<FollowPoints>();

            if (OsuContainer.Beatmap is null)
                return;

            objectIndex = -1;

            if (OsuContainer.SongPosition < OsuContainer.Beatmap.HitObjects[0].BaseObject.StartTime)
                objectIndex = 0;
            else
            {
                for (int i = 1; i < OsuContainer.Beatmap.HitObjects.Count; i++)
                {
                    var prevObj = OsuContainer.Beatmap.HitObjects[i - 1];
                    var obj = OsuContainer.Beatmap.HitObjects[i];

                    if (obj.BaseObject.StartTime > OsuContainer.SongPosition && prevObj.BaseObject.StartTime < OsuContainer.SongPosition)
                    {
                        objectIndex = i;
                        break;
                    }
                }
            }

            if (objectIndex == -1)
                objectIndex = OsuContainer.Beatmap.HitObjects.Count;
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

            if (objectIndex < OsuContainer.Beatmap.HitObjects.Count)
            {
                while (OsuContainer.SongPosition >= (OsuContainer.Beatmap.HitObjects[objectIndex].BaseObject.StartTime - OsuContainer.Beatmap.Preempt))
                {
                    var obj = OsuContainer.Beatmap.HitObjects[objectIndex];

                    Add(obj as Drawable);

                    objectIndex++;

                    if (objectIndex == OsuContainer.Beatmap.HitObjects.Count)
                        break;

                    var nextObj = OsuContainer.Beatmap.HitObjects[objectIndex];

                    if (nextObj.BaseObject.IsNewCombo == false)
                        Add(new FollowPoints(obj.BaseObject, nextObj.BaseObject));

                    if (nextObj.BaseObject.StartTime - obj.BaseObject.EndTime >= 3000)
                        Add(new WarningArrows(nextObj.BaseObject.StartTime - 3000));
                }
            }

            if (OsuContainer.Combo % 50 == 0 && ComboBurst.CanSpawn && OsuContainer.Combo > 0 && Skin.ComboBurst is not null)
                Add(new ComboBurst());

            OsuContainer.HUD.Update(delta);
            base.Update(delta);
        }

        private float bgZoom = 0f;
        private SmoothFloat bgAlpha = new SmoothFloat();
        private void drawBackground(Graphics g)
        {
            return;

            bgAlpha.Update(delta);

            var tex = OsuContainer.Beatmap?.Background;

            if (tex is null)
                return;

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

            g.DrawRectangleCentered(MainGame.WindowCenter, bgSize, new Vector4(1f, 1f, 1f, bgAlpha.Value), OsuContainer.Beatmap.Background);

            g.DrawRectangle(OsuContainer.FullPlayfield.Position, OsuContainer.FullPlayfield.Size, new Vector4(1f, 1f, 1f, 0.1f));
        }

        public override void Render(Graphics g)
        {
            if (OsuContainer.Beatmap is null)
                return;

            drawBackground(g);

            base.Render(g);
            OsuContainer.HUD.Render(g);

            if(ScreenManager.ActiveScreen() == this)
                cursor.Render(g, delta, Input.MousePosition, Colors.White);
        }
        public override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
                ScreenManager.GoBack();

            OsuContainer.KeyDown(key);
            base.OnKeyDown(key);
        }

        public override void OnKeyUp(Key key)
        {
            OsuContainer.KeyUp(key);
            base.OnKeyUp(key);
        }

        public override void OnMouseDown(MouseButton button)
        {
            OsuContainer.MouseDown(button);
            base.OnMouseDown(button);
        }

        public override void OnMouseUp(MouseButton button)
        {
            OsuContainer.MouseUp(button);
            base.OnMouseUp(button);
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
