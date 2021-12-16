using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    public class MusicFreckle : Drawable
    {
        private Vector2 pos;
        private Vector2 size;
        private Vector4 color;

        private Vector2 velocity;
        private SoundVisualizer visualizer;

        private SmoothFloat alpha = new SmoothFloat();

        public override Rectangle Bounds => new Rectangle(pos - size / 2f + visualizer.FreckleOffset, size);

        private float scale = 1;

        public MusicFreckle(Vector2 spawnPos, SoundVisualizer visualizer)
        {
            this.visualizer = visualizer;

            float theta = RNG.Next(0, MathF.PI * 2);
            pos = spawnPos + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * visualizer.Radius;

            Layer = visualizer.Layer - 1;
            color = Colors.White * 3;
            color.W = 0;
            size.X = RNG.Next(2, 16);
            size.Y = size.X;

            scale = size.X.Map(2, 16, 0.2f, 1.0f);

            velocity.X = MathF.Cos(theta) * 2500f * scale;
            velocity.Y = MathF.Sin(theta) * 2500f * scale;

            alpha.Value = 0f;
            alpha.TransformTo(1f, 0.25f, EasingTypes.In);
        }

        public override void Render(Graphics g)
        {
            g.DrawRectangleCentered(pos + visualizer.FreckleOffset, size, color, Texture.WhiteCircle);
        }

        public override void Update(float delta)
        {
            pos += velocity * delta * (visualizer.BeatValue + 0.05f);

            alpha.Update(delta * visualizer.BeatValue * scale);
            color.W = alpha.Value;

            if (new Rectangle(new Vector2(-100), new Vector2(MainGame.WindowWidth + 100, MainGame.WindowHeight + 100)).IntersectsWith(Bounds) == false)
                IsDead = true;
        }
    }

    //This is ok at best, i need to make it fully smooth and thick
    //idk how to though
    //I want a trap nation style one ngl, that would be cool AF

    //TODO: Make it truly smooth, where "bars" data points effect each other!
    public class SoundVisualizer : Drawable
    {
        public enum VisualizerStyle
        {
            Smooth, Bars
        }

        public Vector2 FreckleOffset = Vector2.Zero;

        public Vector4 StartColor;
        public Vector4 EndColor;

        public Vector2 Position;
        public float Radius;
        public float Thickness = 10f;
        public float BarLength = 200f;

        public float StartRotation = -MathF.PI / 2f;

        public int MirrorCount = 1;

        public int Smoothness = 10;
        public bool PatchEnd = true;

        public VisualizerStyle Style;

        public float BeatValue { get; private set; }

        public override Rectangle Bounds => new Rectangle(Position - new Vector2(Radius), new Vector2(Radius * 2f));

        public event Func<Vector2, Vector4> ColorAt;

        public Sound Sound;

        public float[] RawBuffer { get; private set; } = new float[4096];

        public float[] SmoothBuffer { get; private set; } = new float[33];

        public float LerpSpeed = 20;

        public Texture BarTexture;

        public override void Render(Graphics g)
        {
            if (Sound is null)
                return;
            if (Style == VisualizerStyle.Bars)
                drawCircle(g);
            else
                drawTrapnation(g);
        }

        private List<Vector2> controlPoints = new List<Vector2>();
        private void drawTrapnation(Graphics g)
        {
            //This is fine, and way better than before, but it can be even better
            //stupid floating horn at the top since, the beginning and end arent connected in order so the bspline fails
            //no matter where will there be a beginning and an end so some where will always be stupid thing
            //search for better alg

            controlPoints.Clear();

            for (int k = 0; k < MirrorCount; k++)
            {
                if (k % 2 == 0)
                {
                    controlPoints.Add(new Vector2(0, 0));
                    controlPoints.Add(new Vector2(0, 0));
                    for (int i = 0; i < SmoothBuffer.Length; i++)
                    {
                        var now = SmoothBuffer[i];

                        controlPoints.Add(new Vector2(0, now));
                    }
                }
                else
                {
                    for (int i = SmoothBuffer.Length - 1; i >= 0; i--)
                    {
                        var now = SmoothBuffer[i];

                        controlPoints.Add(new Vector2(0, now));
                    }

                    controlPoints.Add(new Vector2(0, 0));
                    controlPoints.Add(new Vector2(0, 0));
                }
            }

            var points = PathApproximator.ApproximateBSpline(controlPoints, 6);
            float theta = -MathF.PI / 2;
            float stepTheta = (MathF.PI * 2) / (points.Count);

            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);

            int verticesIndex = 0;

            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y;

                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;

                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? StartColor;
                vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? EndColor;
                vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }

            if (Input.IsKeyDown(Key.Backspace))
            {
                theta = -MathF.PI / 2;
                stepTheta = (MathF.PI * 2) / (controlPoints.Count);

                Vector2? prevPos = null;
                //Debug control points
                for (int i = 0; i < controlPoints.Count; i++)
                {
                    float volume = controlPoints[i].Y;
                    Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (volume * BarLength));
                    pos += Position;

                    if (prevPos.HasValue)
                    {
                        g.DrawLine(prevPos.Value, pos, Colors.Black, 10f);
                    }

                    g.DrawRectangleCentered(pos, new Vector2(20), (Vector4)Color4.Red, Texture.WhiteCircle);

                    theta += stepTheta;

                    prevPos = pos;
                }
            }
            
            /*
            if (PatchEnd)
            {
                vertices[^1].Position = vertices[1].Position;
                vertices[^2].Position = vertices[0].Position;
            }
            */
        }

        private void drawCircle2(Graphics g)
        {
            float theta = StartRotation;
            float stepTheta = (MathF.PI * 2) / (SmoothBuffer.Length) / 4;

            var vertices = g.VertexBatch.GetTriangleStrip(SmoothBuffer.Length * 2 * 4);

            int verticesIndex = 0;

            for (int k = 0; k < 4; k++)
            {
                for (int i = 0; i < SmoothBuffer.Length; i++)
                {
                    float now = SmoothBuffer[i];

                    Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
                    pos += Position;

                    Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));
                    pos2 += Position;

                    vertices[verticesIndex].Position = pos;
                    vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? StartColor;
                    vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                    vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                    verticesIndex++;

                    vertices[verticesIndex].Position = pos2;
                    vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? EndColor;
                    vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                    vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                    verticesIndex++;

                    theta += stepTheta;
                }
            }

            if (PatchEnd)
            {
                vertices[^1].Position = vertices[1].Position;
                vertices[^2].Position = vertices[0].Position;
            }
        }

        private void drawCircle(Graphics g)
        {
            float stepTheta = ((MathF.PI * 2) / SmoothBuffer.Length) / MirrorCount;
            float theta = StartRotation;

            for (int i = 0; i < MirrorCount; i++)
            {
                for (int j = 0; j < SmoothBuffer.Length; j++)
                {
                    float volume = SmoothBuffer[j];

                    Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
                    pos += Position;

                    Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (volume * BarLength));
                    pos2 += Position;

                    g.DrawLine(pos2, pos, EndColor, StartColor, Thickness, BarTexture);
                    theta += stepTheta;
                }
            }
        }

        //TODO: Make it truly smooth, where bars effect each other!
        private void drawSmoothCircle(Graphics g, float scale = 1f)
        {
            if (MirrorCount < 1 || Smoothness < 1)
                return;

            //HVAD FANDEN I HELE HULE HELVEDET
            float stepTheta = (MathF.PI * 2) / (SmoothBuffer.Length * Smoothness) / MirrorCount;

            float theta = StartRotation;

            var vertices = g.VertexBatch.GetTriangleStrip(SmoothBuffer.Length * Smoothness * MirrorCount * 2);

            int verticesIndex = 0;

            for (int j = 0; j < MirrorCount; j++)
            {
                for (int i = 0; i < SmoothBuffer.Length; i++)
                {
                    for (int k = 0; k < Smoothness; k++)
                    {
                        var now = SmoothBuffer[i];
                        var next = i + 1 < SmoothBuffer.Length ? SmoothBuffer[i + 1] : SmoothBuffer[0];

                        float interpVolume = 0;

                        if (next > now)
                            interpVolume = Interpolation.ValueAt(k, now, next, 0, Smoothness - 1, EasingTypes.OutSine);
                        else
                            interpVolume = Interpolation.ValueAt(k, now, next, 0, Smoothness - 1, EasingTypes.InSine);

                        Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
                        pos += Position;

                        Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (interpVolume * BarLength) * scale);
                        pos2 += Position;

                        vertices[verticesIndex].Position = pos;
                        vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? StartColor;
                        vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                        vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                        verticesIndex++;

                        vertices[verticesIndex].Position = pos2;
                        vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? EndColor;
                        vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
                        vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                        verticesIndex++;

                        theta += stepTheta;
                    }
                }
            }

            if (PatchEnd)
            {
                vertices[^1].Position = vertices[1].Position;
                vertices[^2].Position = vertices[0].Position;
            }
        }

        private float freckleSpawnTimer = 0;

        public float FreckleSpawnRate = 0.01f;
        public float FreckleBeatThreshold = 0.1f;

        public override void Update(float delta)
        {
            if (Sound is null)
                return;

            if (Sound.IsPlaying)
                Sound.GetFFTData(RawBuffer, ManagedBass.DataFlags.FFT8192);

            for (int i = 0; i < SmoothBuffer.Length; i++)
            {
                //Maybe sqrt buffer value
                float raw = RawBuffer[i + 1];

                raw = raw.Clamp(0, 1);

#if DEBUG
                float buffValue = Sound.IsPlaying ? raw : raw;
#else
                float buffValue = Sound.IsPlaying ? raw : 0;
#endif
                SmoothBuffer[i] = MathHelper.Lerp(SmoothBuffer[i], buffValue, delta * LerpSpeed);
            }

            //BeatValue = (SmoothBuffer[1] + SmoothBuffer[2] + SmoothBuffer[3]) / 3f;

            float bass = (SmoothBuffer[3] + SmoothBuffer[4] + SmoothBuffer[5] + SmoothBuffer[6]);
            float kicks = (SmoothBuffer[12] + SmoothBuffer[13] + SmoothBuffer[14] + SmoothBuffer[15] + SmoothBuffer[16]);

            //30% bass, 70% kicks
            //BeatValue = bass * .3f + kicks * .7f;

            BeatValue = 0;
            for (int i = 0; i < SmoothBuffer.Length; i++)
            {
                BeatValue += SmoothBuffer[i];
            }
            BeatValue = (BeatValue / 6);

            freckleSpawnTimer += delta;

            if (freckleSpawnTimer >= FreckleSpawnRate && Sound.IsPlaying && BeatValue > FreckleBeatThreshold)
            {
                Container.Add(new MusicFreckle(Position, this));
                freckleSpawnTimer = 0;
            }
        }
    }
}
