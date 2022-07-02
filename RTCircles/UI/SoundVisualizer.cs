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
    //    public class MusicParticle : Drawable
    //    {
    //        private Vector2 pos;
    //        private Vector2 size;
    //        private Vector4 color;

    //        private Vector2 velocity;
    //        private SoundVisualizer visualizer;

    //        private SmoothFloat alpha = new SmoothFloat();

    //        public override Rectangle Bounds => new Rectangle(pos - size / 2f + visualizer.FreckleOffset, size);

    //        private float scale = 1;
    //        private float angle = 0;
    //        private float angleDir = 1;

    //        public override void OnRemove()
    //        {
    //            ObjectPool<MusicParticle>.Return(this);
    //        }

    //        public void SetTarget(SoundVisualizer visualizer)
    //        {
    //            this.visualizer = visualizer;

    //            float theta = RNG.Next(0, MathF.PI * 2);
    //            pos = visualizer.Position + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * visualizer.Radius;

    //            Layer = visualizer.Layer - 1;
    //            color = Colors.White;
    //            color.W = 0;
    //            size.X = RNG.Next(16, 32);
    //            size.Y = size.X;

    //            scale = size.X.Map(16, 32, 0.2f, 1.0f);

    //            velocity.X = MathF.Cos(theta) * 3500f * scale;
    //            velocity.Y = MathF.Sin(theta) * 3500f * scale;

    //            alpha.Value = 0f;
    //            alpha.TransformTo(1f, 0.25f, EasingTypes.In);

    //            angle = RNG.Next(0, 360);
    //            angleDir = RNG.TryChance() ? -1 : 1;
    //        }

    //        public override void Render(Graphics g)
    //        {
    //            g.DrawRectangleCentered(pos + visualizer.FreckleOffset, size, color, Skin.Star, rotDegrees: angle);
    //        }

    //        public override void Update(float delta)
    //        {
    //            pos += velocity * delta * (visualizer.BeatValue + 0.05f);

    //            angle += 2500 * scale * delta * (visualizer.BeatValue + 0.05f) * angleDir;
    //            alpha.Update(delta * visualizer.BeatValue * scale);
    //            color.W = Interpolation.ValueAt((pos - MainGame.WindowCenter).Length, 0, 1, 0, 1000, EasingTypes.InExpo);

    //            if (new Rectangle(new Vector2(-100), new Vector2(MainGame.WindowWidth + 100, MainGame.WindowHeight + 100)).IntersectsWith(Bounds) == false)
    //                IsDead = true;
    //        }
    //    }

    //    //This is ok at best, i need to make it fully smooth and thick
    //    //idk how to though
    //    //I want a trap nation style one ngl, that would be cool AF

    //    //TODO: Make it truly smooth, where "bars" data points effect each other!
    //    public class SoundVisualizer : Drawable
    //    {
    //        public enum VisualizerStyle
    //        {
    //            Smooth, Bars
    //        }

    //        public Vector2 FreckleOffset = Vector2.Zero;

    //        public Vector4 BarStartColor;
    //        public Vector4 BarEndColor;

    //        public Vector3 BarHighlight;

    //        public Vector2 Position;
    //        public float Radius;
    //        public float Thickness = 10f;
    //        public float BarLength = 200f;

    //        public float StartRotation = -MathF.PI / 2f;

    //        public int MirrorCount = 1;

    //        public int Smoothness = 10;
    //        public bool PatchEnd = true;

    //        public VisualizerStyle Style;

    //        public float BeatValue { get; private set; }

    //        public override Rectangle Bounds => new Rectangle(Position - new Vector2(Radius), new Vector2(Radius * 2f));

    //        public event Func<float, float, Vector4> ColorAt;

    //        public Sound Sound;

    //        public float[] RawBuffer { get; private set; } = new float[4096];

    //        public float[] SmoothBuffer { get; private set; } = new float[33];

    //        public float LerpSpeed = 20;

    //        public Texture BarTexture;

    //        public override void Render(Graphics g)
    //        {
    //            if (Sound is null)
    //                return;
    //            if (Style == VisualizerStyle.Bars)
    //                drawCircle(g);
    //            else
    //                drawTrapnation(g);
    //        }

    //        private List<Vector2> controlPoints = new List<Vector2>();
    //        private void drawTrapnation(Graphics g)
    //        {
    //            //This is fine, and way better than before, but it can be even better
    //            //stupid floating horn at the top since, the beginning and end arent connected in order so the bspline fails
    //            //no matter where will there be a beginning and an end so some where will always be stupid thing
    //            //search for better alg

    //            controlPoints.Clear();

    //            for (int k = 0; k < MirrorCount; k++)
    //            {
    //                if (k % 2 == 0)
    //                {
    //                    controlPoints.Add(new Vector2(0, 0));
    //                    controlPoints.Add(new Vector2(0, 0));
    //                    for (int i = 0; i < SmoothBuffer.Length; i++)
    //                    {
    //                        var now = SmoothBuffer[i];

    //                        controlPoints.Add(new Vector2(0, now));
    //                    }
    //                }
    //                else
    //                {
    //                    for (int i = SmoothBuffer.Length - 1; i >= 0; i--)
    //                    {
    //                        var now = SmoothBuffer[i];

    //                        controlPoints.Add(new Vector2(0, now));
    //                    }

    //                    controlPoints.Add(new Vector2(0, 0));
    //                    controlPoints.Add(new Vector2(0, 0));
    //                }
    //            }

    //            int slot = g.GetTextureSlot(null);

    //            var points = PathApproximator.ApproximateBezier(controlPoints);
    //            float theta = -MathF.PI / 2;
    //            float stepTheta = (MathF.PI * 2) / (points.Count);

    //            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
    //            int verticesIndex = 0;
    //            for (int i = 0; i < points.Count; i++)
    //            {
    //                float now = points[i].Y;

    //                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;

    //                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));

    //                float progress = ((float)i / points.Count);

    //                vertices[verticesIndex].Position = pos + Position;
    //                vertices[verticesIndex].Color = ColorAt?.Invoke(progress, now) ?? BarStartColor;
    //                vertices[verticesIndex].TextureSlot = slot;
    //                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

    //                verticesIndex++;

    //                vertices[verticesIndex].Position = pos2 + Position;
    //                vertices[verticesIndex].Color = ColorAt?.Invoke(progress, now) ?? BarEndColor;
    //                vertices[verticesIndex].TextureSlot = slot;
    //                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

    //                verticesIndex++;

    //                theta += stepTheta;
    //            }

    //            Vector4 innerColor = Colors.From255RGBA(37, 37, 37, 255);
    //            vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
    //            verticesIndex = 0;
    //            for (int i = 0; i < points.Count; i++)
    //            {
    //                float now = points[i].Y - 0.005f;

    //                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;

    //                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));

    //                vertices[verticesIndex].Position = pos + Position;
    //                vertices[verticesIndex].Color = innerColor;
    //                vertices[verticesIndex].TextureSlot = slot;
    //                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

    //                verticesIndex++;

    //                vertices[verticesIndex].Position = pos2 + Position;
    //                vertices[verticesIndex].Color = innerColor;
    //                vertices[verticesIndex].TextureSlot = slot;
    //                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

    //                verticesIndex++;

    //                theta += stepTheta;
    //            }

    //            if (Input.IsKeyDown(Key.Backspace))
    //            {
    //                theta = -MathF.PI / 2;
    //                stepTheta = (MathF.PI * 2) / (controlPoints.Count);

    //                Vector2? prevPos = null;
    //                //Debug control points
    //                for (int i = 0; i < controlPoints.Count; i++)
    //                {
    //                    float volume = controlPoints[i].Y;
    //                    Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (volume * BarLength));
    //                    pos += Position;

    //                    if (prevPos.HasValue)
    //                    {
    //                        g.DrawLine(prevPos.Value, pos, Colors.Black, 10f);
    //                    }

    //                    g.DrawRectangleCentered(pos, new Vector2(20), (Vector4)Color4.Red, Texture.WhiteCircle);

    //                    theta += stepTheta;

    //                    prevPos = pos;
    //                }
    //            }

    //            /*
    //            if (PatchEnd)
    //            {
    //                vertices[^1].Position = vertices[1].Position;
    //                vertices[^2].Position = vertices[0].Position;
    //            }
    //            */
    //        }

    //        //private void drawCircle2(Graphics g)
    //        //{
    //        //    float theta = StartRotation;
    //        //    float stepTheta = (MathF.PI * 2) / (SmoothBuffer.Length) / 4;

    //        //    var vertices = g.VertexBatch.GetTriangleStrip(SmoothBuffer.Length * 2 * 4);

    //        //    int verticesIndex = 0;

    //        //    for (int k = 0; k < 4; k++)
    //        //    {
    //        //        for (int i = 0; i < SmoothBuffer.Length; i++)
    //        //        {
    //        //            float now = SmoothBuffer[i];

    //        //            Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
    //        //            pos += Position;

    //        //            Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));
    //        //            pos2 += Position;

    //        //            vertices[verticesIndex].Position = pos;
    //        //            vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? StartColor;
    //        //            vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
    //        //            vertices[verticesIndex].TexCoord = new Vector2(0, 0);

    //        //            verticesIndex++;

    //        //            vertices[verticesIndex].Position = pos2;
    //        //            vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? EndColor;
    //        //            vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
    //        //            vertices[verticesIndex].TexCoord = new Vector2(1, 1);

    //        //            verticesIndex++;

    //        //            theta += stepTheta;
    //        //        }
    //        //    }

    //        //    if (PatchEnd)
    //        //    {
    //        //        vertices[^1].Position = vertices[1].Position;
    //        //        vertices[^2].Position = vertices[0].Position;
    //        //    }
    //        //}

    //        private void drawCircle(Graphics g)
    //        {
    //            MirrorCount = 2;
    //            float stepTheta = ((MathF.PI * 2) / SmoothBuffer.Length) / MirrorCount;
    //            float theta = StartRotation;

    //            for (int i = 0; i < MirrorCount; i++)
    //            {
    //                for (int j = 0; j < SmoothBuffer.Length; j++)
    //                {
    //                    float volume = SmoothBuffer[j];

    //                    Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
    //                    pos += Position;

    //                    Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (volume * BarLength));
    //                    pos2 += Position;

    //                    var highlight = BarHighlight * volume;

    //                    //my proudest piece of code :tf:
    //                    //all this to add a half circle at the end of the bar line :tf:
    //                    g.DrawRectangleCentered(pos2 + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Thickness / 4,
    //                        new Vector2(Thickness / 2, Thickness), BarStartColor + new Vector4(highlight, 0), Texture.WhiteFlatCircle2,
    //                        new Rectangle(0, 0, 0.5f, 1), true, 180 + (float)MathHelper.RadiansToDegrees(theta));

    //                    g.DrawLine(pos2, pos, BarEndColor + new Vector4(highlight, 0), BarStartColor + new Vector4(highlight, 0), Thickness, BarTexture);
    //                    theta += stepTheta;
    //                }
    //            }
    //        }

    //        //TODO: Make it truly smooth, where bars effect each other!
    //        //private void drawSmoothCircle(Graphics g, float scale = 1f)
    //        //{
    //        //    if (MirrorCount < 1 || Smoothness < 1)
    //        //        return;

    //        //    //HVAD FANDEN I HELE HULE HELVEDET
    //        //    float stepTheta = (MathF.PI * 2) / (SmoothBuffer.Length * Smoothness) / MirrorCount;

    //        //    float theta = StartRotation;

    //        //    var vertices = g.VertexBatch.GetTriangleStrip(SmoothBuffer.Length * Smoothness * MirrorCount * 2);

    //        //    int verticesIndex = 0;

    //        //    for (int j = 0; j < MirrorCount; j++)
    //        //    {
    //        //        for (int i = 0; i < SmoothBuffer.Length; i++)
    //        //        {
    //        //            for (int k = 0; k < Smoothness; k++)
    //        //            {
    //        //                var now = SmoothBuffer[i];
    //        //                var next = i + 1 < SmoothBuffer.Length ? SmoothBuffer[i + 1] : SmoothBuffer[0];

    //        //                float interpVolume = 0;

    //        //                if (next > now)
    //        //                    interpVolume = Interpolation.ValueAt(k, now, next, 0, Smoothness - 1, EasingTypes.OutSine);
    //        //                else
    //        //                    interpVolume = Interpolation.ValueAt(k, now, next, 0, Smoothness - 1, EasingTypes.InSine);

    //        //                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;
    //        //                pos += Position;

    //        //                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (interpVolume * BarLength) * scale);
    //        //                pos2 += Position;

    //        //                vertices[verticesIndex].Position = pos;
    //        //                vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? StartColor;
    //        //                vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
    //        //                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

    //        //                verticesIndex++;

    //        //                vertices[verticesIndex].Position = pos2;
    //        //                vertices[verticesIndex].Color = ColorAt?.Invoke(pos) ?? EndColor;
    //        //                vertices[verticesIndex].TextureSlot = g.GetTextureSlot(null);
    //        //                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

    //        //                verticesIndex++;

    //        //                theta += stepTheta;
    //        //            }
    //        //        }
    //        //    }

    //        //    if (PatchEnd)
    //        //    {
    //        //        vertices[^1].Position = vertices[1].Position;
    //        //        vertices[^2].Position = vertices[0].Position;
    //        //    }
    //        //}

    //        private float freckleSpawnTimer = 0;

    //        public float FreckleSpawnRate = 0.01f;
    //        public float FreckleBeatThreshold = 0.1f;

    //        public override void Update(float delta)
    //        {
    //            if (Sound is null)
    //                return;

    //            if (Sound.IsPlaying)
    //                Sound.GetFFTData(RawBuffer, ManagedBass.DataFlags.FFT8192 | ManagedBass.DataFlags.FFTRemoveDC);

    //            for (int i = 0; i < SmoothBuffer.Length; i++)
    //            {
    //                //Maybe sqrt buffer value
    //                float raw = RawBuffer[i + 1];

    //                raw = raw.Clamp(0, 1);

    //#if DEBUG
    //                float buffValue = Sound.IsPlaying ? raw : raw;
    //#else
    //                float buffValue = Sound.IsPlaying ? raw : 0;
    //#endif
    //                SmoothBuffer[i] = MathHelper.Lerp(SmoothBuffer[i], buffValue, delta * LerpSpeed);
    //            }

    //            //BeatValue = (SmoothBuffer[1] + SmoothBuffer[2] + SmoothBuffer[3]) / 3f;
    //            /*
    //            float bass = (SmoothBuffer[0] + SmoothBuffer[1] + SmoothBuffer[2] + SmoothBuffer[3] + SmoothBuffer[4] + SmoothBuffer[5] + SmoothBuffer[6]);
    //            float kicks = (SmoothBuffer[12] + SmoothBuffer[13] + SmoothBuffer[14] + SmoothBuffer[15] + SmoothBuffer[16]);
    //            */
    //            //30% bass, 70% kicks
    //            //BeatValue = bass * .3f + kicks * .7f;

    //            BeatValue = 0;
    //            for (int i = 0; i < SmoothBuffer.Length; i++)
    //            {
    //                BeatValue += SmoothBuffer[i];
    //            }
    //            BeatValue = (BeatValue / 8);

    //            freckleSpawnTimer += delta;

    //            if (freckleSpawnTimer >= FreckleSpawnRate && Sound.IsPlaying && BeatValue > FreckleBeatThreshold)
    //            {
    //                var particle = ObjectPool<MusicParticle>.Take();

    //                particle.SetTarget(this);

    //                Container.Add(particle);
    //                freckleSpawnTimer = 0;
    //            }
    //        }
    //    }

    public class MusicParticle : Drawable
    {
        private Vector2 pos;
        private Vector2 size;
        private Vector4 color;

        private Vector2 velocity;
        private SoundVisualizer visualizer;

        private SmoothFloat alpha = new SmoothFloat();

        public override Rectangle Bounds => new Rectangle(pos - size / 2f + visualizer.FreckleOffset, size);

        private float scale = 1;
        private float angle = 0;
        private float angleDir = 1;

        public override void OnRemove()
        {
            ObjectPool<MusicParticle>.Return(this);
        }

        public void SetTarget(SoundVisualizer visualizer)
        {
            this.visualizer = visualizer;

            float theta = RNG.Next(0, MathF.PI * 2);
            pos = visualizer.Position + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * visualizer.Radius;

            Layer = visualizer.Layer - 1;
            color = Colors.White;
            color.W = 0;
            size.X = RNG.Next(16, 32);
            size.Y = size.X;

            scale = size.X.Map(16, 32, 0.2f, 1.0f);

            velocity.X = MathF.Cos(theta) * 3500f * scale;
            velocity.Y = MathF.Sin(theta) * 3500f * scale;

            alpha.Value = 0f;
            alpha.TransformTo(1f, 0.25f, EasingTypes.In);

            angle = RNG.Next(0, 360);
            angleDir = RNG.TryChance() ? -1 : 1;
        }

        public override void Render(Graphics g)
        {
            g.DrawRectangleCentered(pos + visualizer.FreckleOffset, size, color, Skin.Star, rotDegrees: angle);
        }

        public override void Update(float delta)
        {
            pos += velocity * delta * (visualizer.BeatValue + 0.05f);

            angle += 2500 * scale * delta * (visualizer.BeatValue + 0.05f) * angleDir;
            alpha.Update(delta * visualizer.BeatValue * scale);
            color.W = Interpolation.ValueAt((pos - MainGame.WindowCenter).Length, 0, 1, 0, 1000, EasingTypes.InExpo);

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

        public Vector4 BarStartColor;
        public Vector4 BarEndColor;

        public Vector3 BarHighlight;

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

        public event Func<float, float, Vector4> ColorAt;

        public Sound Sound;

        public float[] RawBuffer { get; private set; } = new float[4096];

        public float[] SmoothBuffer { get; private set; } = new float[33];

        public float LerpSpeed = 20;

        public Texture BarTexture;

        public override void Render(Graphics g)
        {
            if (Sound is null)
                return;
            /*
            if (Style == VisualizerStyle.Bars)
                drawCircle(g);
            else
                drawTrapnation(g);
            */

            const float fullCircle = MathF.PI * 2;
            const float step = fullCircle / 7;

            /*
            test2(SmoothBuffer[5..15], g, 0, new Vector4(1, 0, 0, 1), BarLength, 0.1);

            test2(SmoothBuffer[10..20], g, step * 2, new Vector4(0, 1, 0, 1), BarLength, 0.1);
            test2(SmoothBuffer[15..25], g, step * 3, new Vector4(0, 1, 1, 1), BarLength, 0.1);

            test2(SmoothBuffer[20..30], g, step * 4, new Vector4(1, 1, 0, 1), BarLength, 0.1);
            test2(SmoothBuffer[23..33], g, step * 5, new Vector4(0, 0, 1, 1), BarLength, 0.1);

            test2(SmoothBuffer[24..33], g, step * 6, new Vector4(1, 0, 1, 1), BarLength, 0.1);
            
            test2(SmoothBuffer[0..10], g, step, new Vector4(1, 1, 1, 1), BarLength, 0.1);
            */
            //Kicks/drums/bass?
            test2(GetFrequencyRangeFFT(40, 150), g, 0, new Vector4(1, 1, 1, 1), BarLength / 2f);

            //Symbals idk just the more high pitched stuff
            test2(GetFrequencyRangeFFT(200, 300), g, MathF.PI/2, new Vector4(1, 0, 0, 1), BarLength / 1.5f);

            //Male-Female vocal range
            test2(GetFrequencyRangeFFT(100, 220), g, MathF.PI, new Vector4(1, 1, 0, 1), BarLength / 2);

            //very Deep bass
            test2(GetFrequencyRangeFFT(0, 40), g, MathF.PI + MathF.PI/2, new Vector4(0, 0, 1, 1), BarLength);
        }

        private void test3(Span<float> fft, Graphics g, Vector4 color, float height)
        {
            if (fft.Length == 0)
                return;

            controlPoints.Clear();

            controlPoints.Add(new Vector2(0, 0));
            for (int i = 0; i < fft.Length; i++)
            {
                var now = fft[i];

                controlPoints.Add(new Vector2(0, now));
            }
            controlPoints.Add(new Vector2(0, 0));

            int slot = g.GetTextureSlot(null);

            var points = PathApproximator.ApproximateCatmull(controlPoints);
            float theta = -MathF.PI*1.5f;
            float stepTheta = (MathF.PI) / (points.Count);

            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            int verticesIndex = 0;
            //color += new Vector4(new Vector3(10f), 0);
            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y;
                //now = 0.2f;
                Vector2 direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));

                Vector2 pos = direction * Radius;

                Vector2 pos2 = direction * (Radius + (now * height));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }

            vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            verticesIndex = 0;
            //color += new Vector4(new Vector3(10f), 0);
            for (int i = points.Count - 1; i >= 0; i--)
            {
                float now = points[i].Y;
                //now = 0.2f;
                Vector2 direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));

                Vector2 pos = direction * Radius;

                Vector2 pos2 = direction * (Radius + (now * height));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }
        }

        private void test2(Span<float> fft, Graphics g, float startRotation, Vector4 color, float height, double rotationSpeed = 0.1)
        {
            if (fft.Length == 0)
                return;

            controlPoints.Clear();

            controlPoints.Add(new Vector2(0, 0));
            for (int i = 0; i < fft.Length; i++)
            {
                var now = fft[i];

                controlPoints.Add(new Vector2(0, now));
            }
            controlPoints.Add(new Vector2(0, 0));

            int slot = g.GetTextureSlot(null);

            PathApproximator.ApproximateCatmullNoAlloc(controlPoints, output);

            var points = output;

            float theta = startRotation + (float)((MainGame.Instance.TotalTime * rotationSpeed) % Math.PI * 2);
            float stepTheta = (MathF.PI * 2) / (points.Count);

            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            int verticesIndex = 0;

            color += new Vector4(new Vector3(0.5f), 0);

            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y;

                Vector2 direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));

                Vector2 pos = direction * Radius;

                Vector2 pos2 = direction * (Radius + (now * height));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }
            
            vertices[^1].Position = vertices[1].Position;
            vertices[^2].Position = vertices[0].Position;
            
        }

        private void test(Graphics g, float startRotation, Vector4 color, double rotationSpeed = 0.1)
        {
            controlPoints.Clear();

                    for (int i = 0; i < SmoothBuffer.Length; i++)
                    {
                        var now = SmoothBuffer[i];

                        controlPoints.Add(new Vector2(0, now));
                    }
            
            int slot = g.GetTextureSlot(null);

            var points = controlPoints;
            float theta = startRotation + (float)((MainGame.Instance.TotalTime * rotationSpeed) % Math.PI*2);
            float stepTheta = (MathF.PI * 2) / (points.Count);

            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            int verticesIndex = 0;
            color = new Vector4(new Vector3(0.5f), 1);
            //color += new Vector4(new Vector3(10f), 0);
            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y;

                Vector2 direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));

                //now = 0.2f;
                Vector2 pos = direction * (Radius + (now * BarLength - 3));

                Vector2 pos2 = direction * (Radius + (now * BarLength));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = color;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }

            vertices[^1].Position = vertices[1].Position;
            vertices[^2].Position = vertices[0].Position;
        }

        private List<Vector2> controlPoints = new List<Vector2>();
        private List<Vector2> output = new List<Vector2>();
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

            int slot = g.GetTextureSlot(null);

            var points = PathApproximator.ApproximateBezier(controlPoints);
            float theta = -MathF.PI / 2;
            float stepTheta = (MathF.PI * 2) / (points.Count);

            var vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            int verticesIndex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y;

                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;

                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));

                float progress = ((float)i / points.Count);

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = ColorAt?.Invoke(progress, now) ?? BarStartColor;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = ColorAt?.Invoke(progress, now) ?? BarEndColor;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(1, 1);

                verticesIndex++;

                theta += stepTheta;
            }

            Vector4 innerColor = Colors.From255RGBA(37, 37, 37, 255);
            vertices = g.VertexBatch.GetTriangleStrip(points.Count * 2);
            verticesIndex = 0;
            for (int i = 0; i < points.Count; i++)
            {
                float now = points[i].Y - 0.005f;

                Vector2 pos = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * Radius;

                Vector2 pos2 = new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * (Radius + (now * BarLength));

                vertices[verticesIndex].Position = pos + Position;
                vertices[verticesIndex].Color = innerColor;
                vertices[verticesIndex].TextureSlot = slot;
                vertices[verticesIndex].TexCoord = new Vector2(0, 0);

                verticesIndex++;

                vertices[verticesIndex].Position = pos2 + Position;
                vertices[verticesIndex].Color = innerColor;
                vertices[verticesIndex].TextureSlot = slot;
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

        private void drawCircle(Graphics g)
        {
            MirrorCount = 2;
            float stepTheta = ((MathF.PI * 2) / SmoothBuffer.Length) / MirrorCount;
            float theta = StartRotation;

            for (int i = 0; i < MirrorCount; i++)
            {
                for (int j = 0; j < SmoothBuffer.Length; j++)
                {
                    float volume = SmoothBuffer[j];

                    Vector2 direction = new Vector2(MathF.Cos(theta), MathF.Sin(theta));

                    Vector2 pos = direction * Radius;
                    pos += Position;

                    Vector2 pos2 = direction * (Radius + (volume * BarLength));
                    pos2 += Position;

                    double t = (double)j / (SmoothBuffer.Length - 1);
                    t += 0.25f;
                    Vector4 color = new Vector4(MathUtils.RainbowColor(t * 10, 1, 1), 1);

                    if (PostProcessing.Bloom)
                        color += new Vector4(1, 1, 1, 0);

                    //my proudest piece of code :tf:
                    //all this to add a half circle at the end of the bar line :tf:
                    g.DrawRectangleCentered(pos2 + direction * Thickness / 4,
                        new Vector2(Thickness / 2, Thickness), color, Texture.WhiteFlatCircle2,
                        new Rectangle(0, 0, 0.5f, 1), true, 180 + (float)MathHelper.RadiansToDegrees(theta));

                    g.DrawLine(pos2, pos, color, color, Thickness, BarTexture);
                    theta += stepTheta;
                }
            }
        }

        private void drawRainbowCircle(Graphics g)
        {
            //Red, Pink, Yellow, Turquoise
            //Vector3[] colorWheel = new Vector3[] { (1, 1, 1), (1, 0, 1), (1, 1, 0), (0, 0.5f, 1) };

            var length = SmoothBuffer.Length * 2;

            float theta = (MathF.PI * 2) / length;

            float currentAngle = StartRotation;

            for (int k = 0; k < MirrorCount; k++)
            {

            }

            for (int i = 0; i < length; i++)
            {
                double t = (double)i / (length - 1);
                t *= 10;
                Vector3 color = MathUtils.RainbowColor(t, 1, 1.5f);
                Console.WriteLine(t);
                float volume = RawBuffer[i];
                //volume = 0.2f;

                Vector2 direction = new Vector2(MathF.Cos(currentAngle), MathF.Sin(currentAngle));

                Vector2 pos = direction * Radius;
                pos += Position;

                Vector2 pos2 = direction * (Radius + (volume * BarLength));
                pos2 += Position;

                g.DrawLine(pos, pos2, new Vector4(color, 1), Thickness);

                currentAngle += theta;
            }

        }

        private float freckleSpawnTimer = 0;

        public float FreckleSpawnRate = 0.01f;
        public float FreckleBeatThreshold = 0.1f;

        public Span<float> GetFrequencyRangeFFT(int startFreq, int endFreq)
        {
            var indexFrequency = Sound.DefaultFrequency / lol.Length;

            var startIndex = (int)(startFreq / indexFrequency);
            var count = (int)(endFreq / indexFrequency) - startIndex;

            if (count < 1)
                return Span<float>.Empty;

            return lol.AsSpan((int)(startFreq / indexFrequency), count);
        }

        private float[] lol = new float[4096];
        public override void Update(float delta)
        {
            if (Sound is null)
                return;

            if (Sound.IsPlaying)
                Sound.GetFFTData(RawBuffer, ManagedBass.DataFlags.FFT8192 | ManagedBass.DataFlags.FFTRemoveDC);

            for (int i = 0; i < SmoothBuffer.Length; i++)
            {
                //Maybe sqrt buffer value
                float raw = RawBuffer[i + 1];

                raw = raw.Clamp(0, 1);

#if DEBUG
                float buffValue = raw;
#else
                float buffValue = Sound.IsPlaying ? raw : 0;
#endif
                SmoothBuffer[i] = MathHelper.Lerp(SmoothBuffer[i], buffValue, delta * LerpSpeed);
            }

            for (int i = 0; i < 512; i++)
            {
                lol[i] = MathHelper.Lerp(lol[i], RawBuffer[i], delta * LerpSpeed);
            }
            //BeatValue = (SmoothBuffer[1] + SmoothBuffer[2] + SmoothBuffer[3]) / 3f;
            /*
            float bass = (SmoothBuffer[0] + SmoothBuffer[1] + SmoothBuffer[2] + SmoothBuffer[3] + SmoothBuffer[4] + SmoothBuffer[5] + SmoothBuffer[6]);
            float kicks = (SmoothBuffer[12] + SmoothBuffer[13] + SmoothBuffer[14] + SmoothBuffer[15] + SmoothBuffer[16]);
            */
            //30% bass, 70% kicks
            //BeatValue = bass * .3f + kicks * .7f;

            BeatValue = 0;
            for (int i = 0; i < SmoothBuffer.Length; i++)
            {
                BeatValue += SmoothBuffer[i];
            }
            BeatValue = (BeatValue / 8);

            freckleSpawnTimer += delta;

            if (freckleSpawnTimer >= FreckleSpawnRate && Sound.IsPlaying && BeatValue > FreckleBeatThreshold)
            {
                var particle = ObjectPool<MusicParticle>.Take();

                particle.SetTarget(this);

                Container.Add(particle);
                freckleSpawnTimer = 0;
            }
        }
    }
}
