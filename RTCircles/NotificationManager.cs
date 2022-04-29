using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RTCircles
{
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
}
