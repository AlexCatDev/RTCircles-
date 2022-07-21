using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums.Beatmaps;
using Silk.NET.Input;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public class DrawableHitCircle : Drawable, IDrawableHitObject
    {
        private float alpha = 0;
        private int combo = 0;

        private float explodeScale = 1f;

        private Vector2 Position;
        private Vector2 Size => new Vector2(OsuContainer.Beatmap.CircleRadius * 2);

        //This only works perfectly, if the order of the ID's Stays consistent
        //Ex: 3 players are connected
        //ID 1, ID 2, ID 3, ID 4
        //ID 2 disconnects
        //No longer works,
        //A new player would have to connect, who will be assigned to ID 2, now everything works again

        //Server should:
        //when Someone disconnects
        //See who disconnects take note of their id
        //now go through every player
        //If the ID difference between Now and Next player is greater than one or if the first player doesnt start from one
        //Theres a mishap, IE 1.. 3.. 4..
        //Or if ID 1 disconnected: ?.. 2.. 3.. 4..
        //Go through every player, from where the check failed, and decrease their ID by one, and then notify everyone of these changes, included in the disconnect packet
        /*
        private bool IsMineToHit_TAG
        {
            get
            {
                bool isMine = colorIndex % Network.Instance.Players.Count == Network.Instance.ID - 1;
                Utils.Log($"Count: {Network.Instance.Players.Count}\nIndex: {colorIndex}\nID: {Network.Instance.ID}\nStatus: {isMine}", LogLevel.Debug);
                return isMine;
            }
        }*/

        private Vector4 Color => new Vector4(Skin.Config.ColorFromIndex(colorIndex), 1f);

        private HitCircle circle;

        public override Rectangle Bounds => new Rectangle(Position - (Vector2)Size / 2f, Size);

        public HitObject BaseObject => circle;

        public Vector4 CurrentColor => Color;

        public bool IsHit { get; private set; }
        public bool IsMissed { get; private set; }

        public int ObjectIndex { get; private set; }

        private float hitAlpha = 0;
        private double hitTime = 0;

        private double hittableTime;

        private int colorIndex;

        public DrawableHitCircle(HitCircle circle, int colorIndex, int combo, int objectIndex)
        {
            this.circle = circle;
            this.combo = combo;
            this.colorIndex = colorIndex;

            ObjectIndex = objectIndex;
        }

        public override void OnAdd()
        {
            IsHit = false;
            IsMissed = false;
            hittableTime = 0;
            alpha = 0;
            explodeScale = 1f;
            hitAlpha = 0;
            hitTime = 0;
        }

        public override void Render(Graphics g)
        {
            if (alpha == 0f)
                return;

            void drawNumber()
            {
                if (!IsHit && !IsMissed)
                    Skin.CircleNumbers.DrawCentered(g, Position, Size.X / 2.8f, new Vector4(1f, 1f, 1f, alpha), combo.ToString());
            }

            g.DrawRectangleCentered(Position, (Vector2)Size * explodeScale * Skin.GetScale(Skin.HitCircle), new Vector4(Color.X, Color.Y, Color.Z, alpha), Skin.HitCircle);

            if (!Skin.Config.HitCircleOverlayAboveNumber)
                drawNumber();

            g.DrawRectangleCentered(Position, (Vector2)Size * explodeScale * Skin.GetScale(Skin.HitCircleOverlay), new Vector4(1f, 1f, 1f, alpha), Skin.HitCircleOverlay);

            if (Skin.Config.HitCircleOverlayAboveNumber)
                drawNumber();
        }

        public override void AfterRender(Graphics g)
        {
            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
                return;

            double preempt = OsuContainer.Beatmap.Preempt;
            double songPos = OsuContainer.SongPosition;
            int startTime = circle.StartTime;

            double startScale = OsuContainer.ApproachCircleScale;
            double endScale = 1;

            float approachScale = (float)Interpolation.ValueAt(songPos, startScale, endScale,
                startTime - preempt,
                startTime          ).Clamp(endScale, startScale);

            double startAlpha = 0;
            double endAlpha = 0.9;

            float approachCircleAlpha =
                (float)Interpolation.ValueAt(songPos, startAlpha, endAlpha,
                startTime - preempt,
                Math.Min(startTime, Math.Min(startTime, startTime - preempt + OsuContainer.Beatmap.FadeIn * 2))).Clamp(startAlpha, endAlpha);

            if (IsMissed)
                approachCircleAlpha *= alpha;

            if (approachScale > 1f && !IsHit)
            {
                Vector2 approachCircleSize = Size * approachScale * Skin.GetScale(Skin.ApproachCircle);
                g.DrawRectangleCentered(Position, approachCircleSize, new Vector4(Color.X, Color.Y, Color.Z, approachCircleAlpha), Skin.ApproachCircle);
            }
        }

        public override void Update(float delta)
        {
            shakeAnim.Update(delta);
            Vector2 shakeOffset = new Vector2(8, 0) * OsuContainer.OsuScale * shakeAnim.Value;

            Position = OsuContainer.MapToPlayfield(circle.Position.X, circle.Position.Y) + shakeOffset;

            double timeElapsed = (OsuContainer.SongPosition - circle.StartTime + OsuContainer.Beatmap.Preempt);

            hittableTime = OsuContainer.SongPosition - circle.StartTime;

            if (!IsHit && !IsMissed)
            {
                if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
                {
                    alpha = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Preempt * 0.4, 0, 1).Clamp(0, 1);

                    if(alpha == 1)
                        alpha = (float)MathUtils.Map(timeElapsed, OsuContainer.Beatmap.Preempt * 0.4, OsuContainer.Beatmap.Preempt * 0.4 + OsuContainer.Beatmap.Preempt * 0.3, 1, 0).Clamp(0, 1);
                }
                else
                    alpha = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.FadeIn, 0, 1).Clamp(0, 1);

                if (OsuContainer.CookieziMode && OsuContainer.SongPosition >= circle.StartTime)
                    checkHit();
                
                //Auto miss when the hit window is outside 50
                if (hittableTime > OsuContainer.Beatmap.Window50)
                    MissIfNotHit();
            }
            else
            {
                //Just instantly disappear when missed or hit when using hidden
                if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
                {
                    IsDead = true;
                    return;
                }

                var start = hitTime;
                var to = hitTime + OsuContainer.Fadeout;
                var time = OsuContainer.SongPosition.Clamp(start, to);

                if (IsHit)
                {
                    //explodeScale = (float)MathUtils.Map(OsuContainer.SongPosition, start, to, 1, OsuContainer.CircleExplodeScale);
                    //alpha = (float)MathUtils.Map(OsuContainer.SongPosition, start, to, hitAlpha, 0).Clamp(0, 1);

                    explodeScale = (float)Interpolation.ValueAt(time, 1, OsuContainer.CircleExplodeScale, start, to, EasingTypes.Out);
                    alpha = (float)Interpolation.ValueAt(time, hitAlpha, 0, start, to, EasingTypes.None);
                }
                else if (IsMissed)
                {
                    to = hitTime + (OsuContainer.Fadeout / 2);
                    time = OsuContainer.SongPosition.Clamp(start, to);

                    alpha = (float)Interpolation.ValueAt(time, hitAlpha, 0, start, to, EasingTypes.None);
                }

                if (alpha == 0)
                    IsDead = true;
            }
        }
         
        private bool checkHit()
        {
            if (!IsHit && !IsMissed && MathUtils.IsPointInsideRadius(OsuContainer.CursorPosition, Position, OsuContainer.Beatmap.CircleRadius))
            {
                //if we're pressing the note and it's further than 300ms away just auto shake it
                if (OsuContainer.SongPosition < circle.StartTime - 300) 
                {
                    Shake();
                    return true;
                }

                //Auto gets to ignore notelock :tf:
                if (!OsuContainer.CookieziMode)
                {
                    //We're hitting to far ahead
                    if (ObjectIndex > 0)
                    {
                        var previousObject = OsuContainer.Beatmap.HitObjects[ObjectIndex - 1];

                        if (!previousObject.IsHit && !previousObject.IsMissed)
                        {
                            Shake();
                            return true;
                        }
                    }
                }

                hitTime = OsuContainer.SongPosition;
                double t = Math.Abs(hittableTime);

                OsuContainer.ScoreHit(circle);

                if (t < OsuContainer.Beatmap.Window300)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Max, Position);
                else if (t < OsuContainer.Beatmap.Window100)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Good, Position);
                else if (t < OsuContainer.Beatmap.Window50)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Meh, Position);
                else
                {
                    MissIfNotHit();
                    return true;
                }

                IsHit = true;
                hitAlpha = alpha;

                OsuContainer.PlayHitsound(circle.HitSound, circle.Extras.SampleSet);

                //play the extra addition if there is one
                if (circle.Extras.AdditionSet is not SampleSet.None)
                    OsuContainer.PlayHitsound(circle.HitSound, circle.Extras.AdditionSet);

                return true;
            }
            return false;
        }

        public override bool OnKeyDown(Key key)
        {
            if (OsuContainer.CookieziMode)
                return false;

            if (key == OsuContainer.Key1 || key == OsuContainer.Key2)
                return checkHit();
            
            return false;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (OsuContainer.CookieziMode)
                return false;

            if (args == MouseButton.Left && OsuContainer.EnableMouseButtons)
                return checkHit();
            
            return false;
        }

        private SmoothFloat shakeAnim = new SmoothFloat();
        public void Shake()
        {
            shakeAnim.ClearTransforms();

            shakeAnim.TransformTo(1 , 0.020f);
            shakeAnim.TransformTo(-1, 0.020f);
            shakeAnim.TransformTo(1 , 0.020f);
            shakeAnim.TransformTo(-1, 0.020f);
            shakeAnim.TransformTo(1 , 0.020f);
            shakeAnim.TransformTo(0 , 0.020f);
        }

        public void MissIfNotHit()
        {
            if (IsHit || IsMissed)
                return;

            OsuContainer.HUD.AddHit(hittableTime, HitResult.Miss, Position);
            IsMissed = true;
            hitAlpha = alpha;
            hitTime = OsuContainer.SongPosition;
        }
    }
}
