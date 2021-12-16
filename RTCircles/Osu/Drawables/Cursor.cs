using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public class Cursor
    {
        public Vector2 TrailSize => getScaledSize(CursorSize, Skin.CursorTrail);
        public Vector2 CursorSize { get; set; } = new Vector2(96 * 2);

        private const float TrailEmitRate = 1f / 60f;

        private const float TrailFadeRate = 6.5f;

        private Vector2 getScaledSize(Vector2 size, OsuTexture texture)
        {
            return new Vector2(size.X, size.Y / texture.Texture.Size.AspectRatio()) * Skin.GetScale(texture);
        }

        internal class FadingTrail
        {
            public bool DestroyMeDaddy;

            private Vector2 spawnPos;
            private Vector4 color = new Vector4(1f, 1f, 1f, 1f);

            private Cursor cursor;

            public FadingTrail(Vector2 spawnPos, Cursor cursor, Vector4 color)
            {
                this.spawnPos = spawnPos;
                this.cursor = cursor;
                this.color = color;
            }

            public void DrawUpdate(Graphics g, float delta)
            {
                if(Skin.CursorMiddle is null)
                color.W -= TrailFadeRate * delta;
                else
                    color.W -= 3 * delta;

                color.W = color.W.Clamp(0f, 1f);

                if (color.W == 0f)
                    DestroyMeDaddy = true;

                g.DrawRectangleCentered(spawnPos, cursor.TrailSize, color, Skin.CursorTrail);
            }
        }

        private List<FadingTrail> trailPieces = new List<FadingTrail>();

        private float emitTimer;

        private Vector2 previousPosition;

        public void Render(Graphics g, float delta, Vector2 position, Vector4 color)
        {
            if (float.IsFinite(position.X) == false || float.IsFinite(position.Y) == false)
                return;

            if (Skin.CursorTrail is not null)
            {
                //Draw trail
                for (int i = 0; i < trailPieces.Count; i++)
                {
                    trailPieces[i].DrawUpdate(g, delta);
                    if (trailPieces[i].DestroyMeDaddy)
                        trailPieces.RemoveAt(i);
                }

                if (Skin.CursorMiddle is null)
                {
                    emitTimer -= delta;

                    if (emitTimer <= 0f)
                    {
                        emitTimer = TrailEmitRate;
                        trailPieces.Add(new FadingTrail(position, this, color));
                    }
                }
                else
                {
                    if (previousPosition == Vector2.Zero)
                    {
                        previousPosition = position;
                        return;
                    }

                    Vector2 diff = position - previousPosition;

                    float angle = MathF.Atan2(diff.Y, diff.X);

                    float cos = MathF.Cos(angle);
                    float sin = MathF.Sin(angle);
                    Vector2 step = new Vector2(cos, sin) * (TrailSize.Y / 2);

                    while (previousPosition != position && diff.LengthSquared >= step.LengthSquared)
                    {
                        if(trailPieces.Count > 4000)
                        {
                            break;
                        }

                        trailPieces.Add(new FadingTrail(previousPosition, this, color));

                        if (step.X < 0)
                            previousPosition.X = (previousPosition.X + step.X).Clamp(position.X, previousPosition.X);
                        else
                            previousPosition.X = (previousPosition.X + step.X).Clamp(previousPosition.X, position.X);

                        if (step.Y < 0)
                            previousPosition.Y = (previousPosition.Y + step.Y).Clamp(position.Y, previousPosition.Y);
                        else
                            previousPosition.Y = (previousPosition.Y + step.Y).Clamp(previousPosition.Y, position.Y);

                        diff = position - previousPosition;
                    }
                }
            }

            if (Skin.Cursor is not null)
                g.DrawRectangleCentered(position, getScaledSize(CursorSize, Skin.Cursor), color, Skin.Cursor);
        }
    }
}
