using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public class FancyCursorTrail : Drawable
    {
        private class TrailPiece
        {
            public float FadeOutSpeed = 12;

            public Vector2 Position;
            public Vector4 Color;
            public float Width;

            public bool RemoveMe;

            private float startWidth;

            public TrailPiece(Vector2 Position, float startWidth)
            {
                this.Position = Position;
                this.startWidth = startWidth;

                this.Color = Vector4.One * 1.5f;
                this.Color.W = 1;
            }

            public void Update(float delta)
            {
                //Use with FadeOutSpeed 2
                //Color.Xyz = MathUtils.RainbowColor(Color.W * 10 + MainGame.Instance.TotalTime * 25, 0.5f, 2.0f);
                Color.W -= delta * FadeOutSpeed;

                Color.W.ClampRef(0, 1);

                Width = Interpolation.ValueAt(Color.W, startWidth, 0, 1, 0, EasingTypes.None);

                if (Color.W == 0)
                    RemoveMe = true;
            }
        }

        public Vector2? PositionOverride;
        public float Width = 8;

        private List<TrailPiece> trailPieces = new List<TrailPiece>();

        public override void Render(Graphics g)
        {
            unsafe
            {
                if (trailPieces.Count > 2)
                {
                    var verts = g.VertexBatch.GetTriangleStrip((trailPieces.Count * 2) - 2);

                    //Some weird ass artificating happening here lol!
                    if (verts != null)
                    {
                        int slot = g.GetTextureSlot(null);
                        int vertIndex = 0;
                        for (int i = 1; i < trailPieces.Count; i++)
                        {
                            Vector2 difference = trailPieces[i].Position - trailPieces[i - 1].Position;
                            Vector2 perpen = new Vector2(difference.Y, -difference.X);

                            perpen.Normalize();

                            verts[vertIndex].Position = trailPieces[i - 1].Position - perpen * trailPieces[i - 1].Width;
                            verts[vertIndex].Color = trailPieces[i - 1].Color;
                            verts[vertIndex].Rotation = 0;
                            verts[vertIndex].TextureSlot = slot;
                            verts[vertIndex].TexCoord = Vector2.Zero;
                            ++vertIndex;

                            verts[vertIndex].Position = trailPieces[i].Position + perpen * trailPieces[i].Width;
                            verts[vertIndex].Color = trailPieces[i].Color;
                            verts[vertIndex].Rotation = 0;
                            verts[vertIndex].TextureSlot = slot;
                            verts[vertIndex].TexCoord = Vector2.One;
                            ++vertIndex;
                        }
                        if (verts.Length != vertIndex)
                            Console.WriteLine("wtf");
                    }
                }
            }

            //g.DrawRectangleCentered(Easy2D.Game.Input.MousePosition, new Vector2(25), Vector4.One, Texture.WhiteCircle);
        }

        private Vector2 lastMousePos;

        public override void Update(float delta)
        {
            for (int i = trailPieces.Count - 1; i >= 0; i--)
            {
                trailPieces[i].Update(delta);
            }

            trailPieces.RemoveAll((o) => o.RemoveMe);

            Vector2 mousePos = PositionOverride ?? Input.MousePosition;

            if (lastMousePos == Vector2.Zero)
                lastMousePos = mousePos;

            var length = (mousePos - lastMousePos).Length;

            if (length >= 5)
            {
                lastMousePos = mousePos;
                TrailPiece p = new TrailPiece(mousePos, Width);

                trailPieces.Add(p);
            }
        }
    }

    public class Cursor
    {
        public float CursorRadius = 148;

        public Vector2 TrailSize => getScaledSize(CursorSize, Skin.CursorTrail);
        public Vector2 CursorSize => new Vector2(CursorRadius) * MainGame.Scale;

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

        private float rotation;

        private FancyCursorTrail fancyTrail = new FancyCursorTrail();

        void renderFancy(Graphics g, float delta, Vector2 position, Vector4 color)
        {
            rotation += 45 * delta;

            rotation.ClampRef(0, 360);
            if (rotation == 360)
                rotation = 0;

            fancyTrail.PositionOverride = position;
            fancyTrail.Width = CursorSize.X/9;

            fancyTrail.Update(delta);
            fancyTrail.Render(g);

            renderCursor(g, position, color);
        }

        private void renderCursor(Graphics g, Vector2 position, Vector4 color)
        {
            if (Skin.CursorMiddle is not null)
                g.DrawRectangleCentered(position, getScaledSize(CursorSize, Skin.CursorMiddle), color, Skin.CursorMiddle);

            if (Skin.Cursor is not null)
                g.DrawRectangleCentered(position, getScaledSize(CursorSize, Skin.Cursor), color, Skin.Cursor);
        }

        public void Render(Graphics g, float delta, Vector2 position, Vector4 color)
        {
            if (GlobalOptions.UseFancyCursorTrail.Value)
            {
                renderFancy(g, delta, position, color);
                return;
            }

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

            renderCursor(g, position, color);
        }
    }
}
