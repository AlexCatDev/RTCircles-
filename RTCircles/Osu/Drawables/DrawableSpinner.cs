﻿using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public class DrawableSpinner : Drawable, IDrawableHitObject
    {
        public HitObject BaseObject => spinner;

        public Vector2 FollowPointAttachment => position;

        public Vector4 CurrentColor => color;

        public override Rectangle Bounds => new Rectangle(position - size/2f, size);

        private Vector2 size { 
            get {
                return new Vector2(OsuContainer.Playfield.Width * 0.9f);
            } 
        }
        private Vector2 position;

        private Spinner spinner;
        private Vector4 color;
        private int combo;

        public DrawableSpinner(Spinner spinner, int colorIndex, int combo)
        {
            this.spinner = spinner;
            this.color = Colors.White;
            this.combo = combo;
        }

        public override void OnAdd()
        {
            rotationCounter = 0;
            lastAngle = 0;
            rotation = 0;
            lastRotation = 0;
            currentRotation = 0;
            score = 0;
            spinRPM = 0;
            base.OnAdd();
        }

        private float currentRotation = 0;

        private float lastAngle;

        private void addRotation(float angle)
        {
            if (angle > 180)
            {
                lastAngle += 360;
                angle -= 360;
            }
            else if (-angle > 180)
            {
                lastAngle -= 360;
                angle += 360;
            }

            currentRotation += angle;
        }

        private float rotation;
        private float lastRotation;
        private int rotationCounter;

        public override void Render(Graphics g)
        {
            float colorBoost = rotationCounter > 0 ? 1.5f : 1;

            float approachScale = (float)MathUtils.Map(OsuContainer.SongPosition, spinner.StartTime, spinner.EndTime, 1, 0).Clamp(0, 1);

            float scale = Skin.GetScale(Skin.SpinnerApproachCircle, 384, 768);

            g.DrawRectangleCentered(position, size * scale * approachScale, color * colorBoost, Skin.SpinnerApproachCircle);

            g.DrawRectangleCentered(position, size, color * colorBoost, Skin.SpinnerCircle, rotDegrees: rotation);

            if (score > 0)
                Skin.CircleNumbers.DrawCentered(g, OsuContainer.MapToPlayfield(512 / 2, 280), (size.Y / 9) * scoreBonusScale, new Vector4(1f, 1f, 1f, scoreBonusAlpha), score.ToString());

            if (OsuContainer.SongPosition >= spinner.EndTime + OsuContainer.Fadeout)
                IsDead = true;
        }

        private int score;
        private SmoothFloat scoreBonusAlpha = new SmoothFloat();
        private SmoothFloat scoreBonusScale = new SmoothFloat();

        private float spinRPM;

        public override void Update(float delta)
        {
            if ((this as IDrawableHitObject).TimeElapsed < 0)
            {
                IsDead = true;
                return;
            }

            position = OsuContainer.MapToPlayfield(spinner.Position.X, spinner.Position.Y);

            //Just complete all spinners with a duration of less than some value thats impossible to complete

            if (spinner.EndTime - spinner.StartTime < 250)
                rotationCounter = 1;


            float timeElapsed = (float)(OsuContainer.SongPosition - spinner.StartTime + OsuContainer.Beatmap.Preempt);

            if (OsuContainer.SongPosition >= spinner.EndTime && color.W == 1f)
            {
                if (rotationCounter > 0)
                {
                    OsuContainer.HUD.AddHit(0f, HitResult.Max, position);
                    OsuContainer.PlayHitsound(spinner.HitSound, spinner.Extras.SampleSet);
                }
                else
                {
                    OsuContainer.HUD.AddHit(0f, HitResult.Miss, position);
                }
            }

            if (timeElapsed < OsuContainer.Beatmap.Fadein)
                color.W = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Fadein, 0, 1).Clamp(0, 1);
            else
                color.W = (float)MathUtils.Map(OsuContainer.SongPosition, spinner.EndTime, spinner.EndTime + OsuContainer.Fadeout, 1, 0).Clamp(0, 1);

            var thisAngle = -MathHelper.RadiansToDegrees(MathF.Atan2(OsuContainer.CursorPosition.X - position.X, OsuContainer.CursorPosition.Y - position.Y));

            var deltaRot = thisAngle - lastAngle;

            //Only allow for rotations if it's fully faded in
            if ((OsuContainer.Key1Down || OsuContainer.Key2Down) && color.W == 1f && OsuContainer.DeltaSongPosition > 0)
                addRotation(deltaRot);

            lastAngle = thisAngle;

            rotation = (float)Interpolation.Damp(rotation, currentRotation, 0.99, (float)OsuContainer.DeltaSongPosition);

            scoreBonusAlpha.Update((float)OsuContainer.DeltaSongPosition);
            scoreBonusScale.Update((float)OsuContainer.DeltaSongPosition);

            spinRPM = (float)((MathF.Abs(rotation) / 360f) / (OsuContainer.SongPosition - spinner.StartTime));
            spinRPM *= 60000;

            if (Math.Abs(rotation - lastRotation) >= 360)
            {
                lastRotation = rotation;
                rotationCounter++;

                if (rotationCounter > 1)
                {
                    Skin.SpinnerBonus.Play(true);
                    scoreBonusAlpha.Value = 1f;
                    scoreBonusAlpha.TransformTo(0f, 1000f, EasingTypes.None);

                    scoreBonusScale.Value = 1f;
                    scoreBonusScale.TransformTo(0.6f, 1000, EasingTypes.OutQuad);

                    score += 1000;
                    OsuContainer.Score += 1000;
                }
            }
        }
    }
}