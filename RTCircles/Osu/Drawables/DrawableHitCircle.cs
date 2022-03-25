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
        private float approachScale = 0;
        private bool playedSound = false;
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

        public bool IsHit = false;
        public bool IsMissed = false;

        private float hitAlpha = 0;
        private double hitTime = 0;

        private float hittableTime;

        private int colorIndex;

        public DrawableHitCircle(HitCircle circle, int colorIndex, int combo)
        {
            this.circle = circle;
            this.combo = combo;
            this.colorIndex = colorIndex;
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
            playedSound = false;
        }

        public bool Hit()
        {
            if (!playedSound && !IsHit && !IsMissed)
            {
                OsuContainer.ScoreHit(circle);

                hitTime = OsuContainer.SongPosition;
                double t = Math.Abs(hittableTime);

                if (alpha < 0.7f)
                    return true;

                if (t < OsuContainer.Beatmap.Window300)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Max, Position);
                else if (t < OsuContainer.Beatmap.Window100)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Good, Position);
                else if (t < OsuContainer.Beatmap.Window50)
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Meh, Position);
                else
                {
                    OsuContainer.HUD.AddHit(hittableTime, HitResult.Miss, Position);
                    IsMissed = true;
                    IsHit = true;
                    return true;
                }
                
                IsHit = true;
                hitAlpha = alpha;

                OsuContainer.PlayHitsound(circle.HitSound, circle.Extras.SampleSet);

                //play the extra addition if there is one
                if (circle.Extras.AdditionSet is not SampleSet.None)
                    OsuContainer.PlayHitsound(circle.HitSound, circle.Extras.AdditionSet);

                playedSound = true;
                return true;
            }
            return false;
        }

        public override void Render(Graphics g)
        {
            g.DrawRectangleCentered(Position, (Vector2)Size * explodeScale * Skin.GetScale(Skin.HitCircle), new Vector4(Color.X, Color.Y, Color.Z, alpha), Skin.HitCircle);

            g.DrawRectangleCentered(Position, (Vector2)Size * explodeScale * Skin.GetScale(Skin.HitCircleOverlay), new Vector4(1f, 1f, 1f, alpha), Skin.HitCircleOverlay);

            
            if (!IsHit && !IsMissed)
            {
                Skin.CircleNumbers.DrawCentered(g, Position, Size.X / 2.7f, new Vector4(1f, 1f, 1f, alpha), combo.ToString());
            }
        }

        public override void AfterRender(Graphics g)
        {
            if (approachScale > 1f && !IsHit && !IsMissed)
            {
                Vector2 approachCircleSize = (Vector2)Size * approachScale * Skin.GetScale(Skin.ApproachCircle);
                g.DrawRectangleCentered(Position, approachCircleSize, new Vector4(Color.X, Color.Y, Color.Z, alpha), Skin.ApproachCircle);
            }
        }

        public override void Update(float delta)
        {
            Position = OsuContainer.MapToPlayfield(circle.Position.X, circle.Position.Y);

            double timeElapsed = (OsuContainer.SongPosition - circle.StartTime + OsuContainer.Beatmap.Preempt);

            hittableTime = (float)(OsuContainer.SongPosition - circle.StartTime);

            if (!IsHit && !IsMissed)
            {
                alpha = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Fadein, 0, 1).Clamp(0, 1);

                approachScale = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Preempt, OsuContainer.ApproachCircleScale, 1)
                    .Clamp(1, OsuContainer.ApproachCircleScale);

                if (OsuContainer.CookieziMode && approachScale == 1f)
                    Hit();
                
                if (hittableTime > OsuContainer.Beatmap.Window50)
                {
                    IsMissed = true;
                    hitAlpha = 1f;
                    OsuContainer.HUD.AddHit((float)OsuContainer.Beatmap.Window50, HitResult.Miss, Position);
                    hitTime = (float)OsuContainer.SongPosition;
                }
            }
            else
            {
                var start = hitTime;
                var to = (float)(hitTime + OsuContainer.Fadeout);

                if (IsHit)
                {
                    explodeScale = (float)MathUtils.Map(OsuContainer.SongPosition, start, to, 1, OsuContainer.CircleExplodeScale);
                    alpha = (float)MathUtils.Map(OsuContainer.SongPosition, start, to, hitAlpha, 0).Clamp(0, 1);
                }
                else if (IsMissed)
                {
                    explodeScale = (float)MathUtils.Map(OsuContainer.SongPosition, start, to - 120, 1, 0).Clamp(0, 1);
                    alpha = (float)MathUtils.Map(OsuContainer.SongPosition, start, to - 120, hitAlpha, 0).Clamp(0, 1);
                }

                if (alpha == 0)
                    IsDead = true;
            }
        }

        public override bool OnKeyDown(Key key)
        {
            if (OsuContainer.CookieziMode)
                return false;

            if (key == OsuContainer.Key1 || key == OsuContainer.Key2)
            {
                if (IsHit)
                    return false;

                if (!IsHit || !IsMissed)
                {
                    if (MathUtils.IsPointInsideRadius(OsuContainer.CursorPosition, Position, OsuContainer.Beatmap.CircleRadius))
                        return Hit();
                }
            }
            
            return false;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (OsuContainer.CookieziMode)
                return false;

            if (args == MouseButton.Left && OsuContainer.EnableMouseButtons)
            {
                if (IsHit)
                    return false;

                if (!IsHit || !IsMissed)
                {
                    if (MathUtils.IsPointInsideRadius(OsuContainer.CursorPosition, Position, OsuContainer.Beatmap.CircleRadius))
                        return Hit();
                }
            }

            return false;
        }

        public override void OnRemove()
        {
            
        }
    }
}
