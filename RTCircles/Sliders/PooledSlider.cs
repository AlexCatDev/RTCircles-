using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    //public class PooledSlider : ISlider
    //{
    //    internal class SliderFrameBuffer
    //    {
    //        public FrameBuffer FrameBuffer { get; private set; }

    //        public static readonly Vector2i BufferSpace = new Vector2i(512, 384);
    //        //public static readonly Vector2i BufferSpace = new Vector2i(0, 0);

    //        public SliderFrameBuffer()
    //        {
    //            FrameBuffer = new FrameBuffer(512 + BufferSpace.X, 384 + BufferSpace.Y,
    //            FramebufferAttachment.DepthAttachment, InternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedShort);
    //        }
    //    }

    //    private const int CIRCLE_RESOLUTION = 40;

    //    private static readonly Easy2D.Shader sliderShader = new Easy2D.Shader();

    //    static PooledSlider()
    //    {
    //        sliderShader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Sliders.slider.vert"));
    //        sliderShader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Sliders.slider.frag"));
    //    }

    //    public Path path = new Path();

    //    private float radius = -1;

    //    private SliderFrameBuffer sliderFrameBuffer;

    //    private FrameBuffer frameBuffer => sliderFrameBuffer.FrameBuffer;

    //    //1mb for stupid aspire abuse sliders
    //    //Also optimize circle betweens points, so i can cut this down and in turn the amount of vertices to render
    //    private static UnsafePrimitiveBatch<Vector3> sliderBatch = new UnsafePrimitiveBatch<Vector3>(50_000, 130_000);

    //    private void drawLine(Vector2 startPosition, Vector2 endPosition, float radius)
    //    {
    //        Vector2 difference = endPosition - startPosition;
    //        Vector2 perpen = new Vector2(difference.Y, -difference.X);

    //        perpen.Normalize();

    //        //First line top
    //        Vector3 topRight = new Vector3(startPosition.X + perpen.X * radius,
    //            startPosition.Y + perpen.Y * radius, 1);

    //        Vector3 topLeft = new Vector3(endPosition.X + perpen.X * radius,
    //            endPosition.Y + perpen.Y * radius, 1);

    //        Vector3 bottomRight = new Vector3(startPosition.X, startPosition.Y, 0);

    //        Vector3 bottomLeft = new Vector3(endPosition.X, endPosition.Y, 0); ;

    //        unsafe
    //        {
    //            var quad = sliderBatch.GetQuad();

    //            quad[0] = topRight;
    //            quad[1] = bottomRight;
    //            quad[2] = bottomLeft;
    //            quad[3] = topLeft;

    //            //Second line bottom

    //            topRight = bottomRight;

    //            topLeft = bottomLeft;

    //            bottomRight = new Vector3(startPosition.X - perpen.X * radius,
    //                                             startPosition.Y - perpen.Y * radius, 1);

    //            bottomLeft = new Vector3(endPosition.X - perpen.X * radius,
    //                                             endPosition.Y - perpen.Y * radius, 1);

    //            quad = sliderBatch.GetQuad();

    //            quad[0] = topRight;
    //            quad[1] = bottomRight;
    //            quad[2] = bottomLeft;
    //            quad[3] = topLeft;
    //        }
    //    }

    //    //Circles can be pregenerated, optimize circle resolution for the radius
    //    private void drawCircle(Vector2 pos, float radius)
    //    {
    //        unsafe
    //        {
    //            var verts = sliderBatch.GetTriangleFan(CIRCLE_RESOLUTION);

    //            verts[0] = new Vector3(pos.X, pos.Y, 0f);

    //            float theta = 0;
    //            const float stepTheta = (MathF.PI * 2) / (CIRCLE_RESOLUTION - 2);

    //            Vector3 vertPos = new Vector3(0, 0, 1f);

    //            for (int i = 1; i < CIRCLE_RESOLUTION; i++)
    //            {
    //                vertPos.X = MathF.Cos(theta) * radius + pos.X;
    //                vertPos.Y = MathF.Sin(theta) * radius + pos.Y;

    //                verts[i] = vertPos;

    //                theta += stepTheta;
    //            }
    //        }
    //    }

    //    private void drawSlider()
    //    {
    //        //The path coordinates are in osu pixel space, and we need to convert them to framebuffer space
    //        Vector2 posOffset = -SliderFrameBuffer.BufferSpace / 2;

    //        float startLength = Path.Length * startProgress;
    //        float endLength = Path.Length * endProgress;

    //        //All of this is stupid, unreadable and gave me cancer looking at it and made me die writing it all for stupid optimizations
    //        if (startLength == endLength)
    //            drawCircle(Path.CalculatePositionAtProgress(endProgress) - posOffset, radius);
    //        else
    //        {
    //            int count = 0;

    //            Vector2? start = null;
    //            Vector2? end = null;

    //            for (int i = 0; i < Path.Points.Count - 1; i++)
    //            {
    //                float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

    //                if (startLength - dist <= 0 && count == 0)
    //                {
    //                    float blend = startLength / dist;
    //                    start = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
    //                    drawCircle(start.Value, radius);
    //                    count++;
    //                }

    //                if (endLength - dist <= 0 && end.HasValue == false)
    //                {
    //                    float blend = endLength / dist;
    //                    end = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
    //                    drawCircle(end.Value, radius);
    //                    count++;
    //                }
    //                startLength -= dist;
    //                endLength -= dist;

    //                if (count > 0)
    //                {
    //                    drawLine(start ?? Path.Points[i] - posOffset, end ?? Path.Points[i + 1] - posOffset, radius);
    //                    start = null;

    //                    if (end.HasValue == false)
    //                        drawCircle(Path.Points[i + 1] - posOffset, radius);

    //                    count++;

    //                    if (end.HasValue)
    //                        break;
    //                }
    //            }
    //        }
    //        sliderBatch.Draw();
    //    }

    //    public void SetRadius(float radius)
    //    {
    //        if (this.radius != radius)
    //        {
    //            this.radius = radius;
    //            hasBeenUpdated = true;
    //        }
    //    }

    //    public void SetPoints(List<Vector2> points) => Path.SetPoints(points);

    //    private float startProgress = 0f;
    //    private float endProgress = 1f;
    //    public void SetProgress(float startProgress, float endProgress)
    //    {
    //        startProgress = startProgress.Clamp(0, 1);
    //        endProgress = endProgress.Clamp(0, 1);

    //        if (this.startProgress == startProgress && this.endProgress == endProgress)
    //            return;

    //        hasBeenUpdated = true;
    //        this.startProgress = startProgress;
    //        this.endProgress = endProgress;
    //    }

    //    private bool hasBeenUpdated = true;

    //    public Path Path => path;

    //    /// <summary>
    //    /// Scaling of the final actual quad that gets drawn to screen
    //    /// </summary>
    //    public float DrawScale { get; set; } = 1f;
    //    /// <summary>
    //    /// The point to scale around, THIS WILL CHANGE THE RENDERED POSITION, Set to null to use the original position
    //    /// </summary>
    //    public Vector2? ScalingOrigin { get; set; }
    //    public float Alpha { get; set; }

    //    public void OffscreenRender()
    //    {
    //        if (sliderFrameBuffer == null)
    //            sliderFrameBuffer = ObjectPool<SliderFrameBuffer>.Take();

    //        if (radius < -1)
    //            throw new Exception("Slider radius was less than 0????");

    //        //Console.WriteLine(ObjectPool<SliderFrameBuffer>.TotalCreated);

    //        if (!hasBeenUpdated)
    //            return;

    //        hasBeenUpdated = false;

    //        //Avoid 'context' switches by rendering all the sliders in the background first

    //        frameBuffer.Bind();

    //        Viewport.SetViewport(0, 0, frameBuffer.Width, frameBuffer.Height);
    //        GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
    //        sliderShader.Bind();
    //        sliderShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, 0, frameBuffer.Height, 1, -1));
    //        drawSlider();
    //    }

    //    public void Render(Graphics g)
    //    {
    //        if (Precision.AlmostEquals(Alpha, 0))
    //            return;

    //        if (Path.Points.Count == 0)
    //        {
    //            g.DrawString($"No slider points?", Font.DefaultFont, Path.CalculatePositionAtProgress(0), Colors.Red);
    //            return;
    //        }

    //        Rectangle texCoords = new Rectangle();
    //        Vector2 renderPosition;

    //        var tx = path.Bounds.X - radius + SliderFrameBuffer.BufferSpace.X / 2;
    //        var ty = path.Bounds.Y - radius + SliderFrameBuffer.BufferSpace.Y / 2;
    //        var tsize = Path.Bounds.Size + new Vector2(radius * 2);

    //        texCoords.X = tx;
    //        texCoords.Y = ty;
    //        texCoords.Width = tsize.X;
    //        texCoords.Height = tsize.Y;

    //        //If hr is on change the way the slider is rendered so it matches
    //        if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HR))
    //        {
    //            //texCoords = new Rectangle(0, 1, 1, -1);
    //            texCoords.Y += tsize.Y;
    //            texCoords.Height = -tsize.Y;
    //            renderPosition = OsuContainer.MapToPlayfield(Path.Bounds.X - radius, Path.Bounds.Y + Path.Bounds.Height + radius);
    //        }
    //        else
    //        {

    //            renderPosition = OsuContainer.MapToPlayfield(Path.Bounds.Position - new Vector2(radius));
    //        }

    //        //Convert the size of the slider from osu pixels to playfield pixels, since the points and the radius are in osu pixels
    //        Vector2 renderSize = (Path.Bounds.Size + new Vector2(radius * 2));

    //        renderSize *= OsuContainer.Playfield.Width / 512;

    //        /*
    //        g.DrawRectangle(OsuContainer.Playfield.TopLeft, OsuContainer.Playfield.Size, new Vector4(0f, 1f, 0, 0.2f));

    //        g.DrawRectangle(Vector2.Zero, frameBuffer.Texture.Size, new Vector4(1f, 1f, 0, 0.2f), frameBuffer.Texture);

    //        g.DrawRectangleCentered(new Vector2(tx, ty), new Vector2(4), Colors.Red);

    //        g.DrawRectangle(new Vector2(tx, ty), tsize, new Vector4(1f, 0f, 0f, 0.2f));
    //        */

    //        //Console.WriteLine(renderSize);
    //        //g.DrawRectangle(renderPosition, renderSize, new Vector4(1, 1, 1, Alpha * 0.5f));

    //        if (ScalingOrigin.HasValue)
    //        {
    //            Vector2 size = renderSize * DrawScale;

    //            Vector2 diff = renderPosition - ScalingOrigin.Value;

    //            Vector2 fp = ScalingOrigin.Value + diff * DrawScale;

    //            //The color is 10000 red, to make the shader know that this is a slider, see Default.Frag
    //            //i had to hack this in since otherwise i would have to end the graphics batch, render this, and begin the graphics batch again
    //            //causing huge performance dips on mobile
    //            g.DrawRectangle(fp, size, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, false);
    //        }
    //        else
    //        {
    //            //^ and we draw this, within the main batcher
    //            g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, false);

    //            //g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, renderPosition, (float)MainGame.Instance.TotalTime);
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return $"Points: {Path.Points.Count} Vertices: {sliderBatch.VertexRenderCount}";
    //    }

    //    public void Cleanup()
    //    {
    //        ObjectPool<SliderFrameBuffer>.Return(sliderFrameBuffer);
    //        sliderFrameBuffer = null;

    //        hasBeenUpdated = true;
    //    }
    //}

    public class PooledSlider : ISlider
    {
        internal class SliderFrameBuffer
        {
            public FrameBuffer FrameBuffer { get; private set; }

            public static readonly Vector2i BufferSpace = new Vector2i(100, 100);
            //public static readonly Vector2i BufferSpace = new Vector2i(0, 0);

            public SliderFrameBuffer()
            {
                FrameBuffer = new FrameBuffer(512 + BufferSpace.X, 384 + BufferSpace.Y,
                FramebufferAttachment.DepthAttachment, InternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedShort);
            }
        }

        private const int CIRCLE_RESOLUTION = 40;

        private static readonly Easy2D.Shader sliderShader = new Easy2D.Shader();

        static PooledSlider()
        {
            sliderShader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Sliders.PooledSlider.vert"));
            sliderShader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Sliders.PooledSlider.frag"));

            circleModel = new Vector3[CIRCLE_RESOLUTION];

            circleModel[0] = new Vector3(0, 0, 0);

            float theta = 0;
            const float stepTheta = (MathF.PI * 2) / (CIRCLE_RESOLUTION - 2);

            Vector3 vertPos = new Vector3(0, 0, 1);

            for (int i = 1; i < CIRCLE_RESOLUTION; i++)
            {
                vertPos.X = MathF.Cos(theta);
                vertPos.Y = MathF.Sin(theta);

                circleModel[i] = vertPos;

                theta += stepTheta;
            }
        }

        public Path path = new Path();

        private float radius = -1;

        private SliderFrameBuffer sliderFrameBuffer;

        private FrameBuffer frameBuffer => sliderFrameBuffer.FrameBuffer;

        private static Vector3[] circleModel;

        //1mb for stupid aspire abuse sliders
        //Also optimize circle betweens points, so i can cut this down and in turn the amount of vertices to render
        private static InstanceBatch<Vector3, Vector2> sliderBatch;

        //10k points monka
        private static Vector2[] instanceDataCache = new Vector2[256_000];

        private void drawSlider()
        {
            if(sliderBatch == null)
                sliderBatch = new InstanceBatch<Vector3, Vector2>(circleModel, indices: null) { PrimitiveType = PrimitiveType.TriangleFan };

            //The path coordinates are in osu pixel space, and we need to convert them to framebuffer space
            Vector2 posOffset = -SliderFrameBuffer.BufferSpace / 2;

            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            int count = 0;

            //All of this is stupid, unreadable and gave me cancer looking at it and made me die writing it all for stupid optimizations
            if (startLength == endLength)
            {
                instanceDataCache[0] = Path.CalculatePositionAtProgress(endProgress) - posOffset;
                count++;
            }
            else
            {
                float totalDistance = 0;

                Vector2? start = null;
                Vector2? end = null;

                for (int i = 0; i < Path.Points.Count - 1; i++)
                {
                    //distance to next point
                    float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);
                    //We found the start point, put a circle there
                    if (startLength - dist <= 0 && count == 0)
                    {
                        float blend = startLength / dist;
                        start = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        instanceDataCache[count++] = start.Value;
                    }

                    //We found the end point, put a circle there
                    if (endLength - dist <= 0.01f)
                    {
                        float blend = endLength / dist;
                        end = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        instanceDataCache[count++] = end.Value;
                    }

                    //Everything inbetween
                    if (count > 0)
                    {
                        var startPoint = start ?? Path.Points[i];
                        var endPoint = end ?? Path.Points[i + 1];

                        totalDistance += dist;

                        const float stepAmount = 4;

                        //Ignore very close together points
                        if (totalDistance >= stepAmount)
                            totalDistance = 0;

                        //Gradually step to the next point filling in the blanks inbetween
                        float startDist = dist;
                        while (dist > stepAmount)
                        {
                            float blend = 1 - (dist / startDist);

                            instanceDataCache[count++] = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;


                            dist -= stepAmount;
                        }
                    }

                    endLength -= dist;

                    startLength -= dist;

                    if (end.HasValue)
                        break;
                }
            }

            Console.WriteLine(count);
            sliderBatch.UploadInstanceData(instanceDataCache.AsSpan(0, count));
            sliderBatch.Draw(count);
        }

        public void SetRadius(float radius)
        {
            if (this.radius != radius)
            {
                this.radius = radius;
                hasBeenUpdated = true;
            }
        }

        public void SetPoints(List<Vector2> points) => Path.SetPoints(points);

        private float startProgress = 0f;
        private float endProgress = 1f;
        public void SetProgress(float startProgress, float endProgress)
        {
            startProgress = startProgress.Clamp(0, 1);
            endProgress = endProgress.Clamp(0, 1);

            if (this.startProgress == startProgress && this.endProgress == endProgress)
                return;

            hasBeenUpdated = true;
            this.startProgress = startProgress;
            this.endProgress = endProgress;
        }

        private bool hasBeenUpdated = true;

        public Path Path => path;

        /// <summary>
        /// Scaling of the final actual quad that gets drawn to screen
        /// </summary>
        public float DrawScale { get; set; } = 1f;
        /// <summary>
        /// The point to scale around, THIS WILL CHANGE THE RENDERED POSITION, Set to null to use the original position
        /// </summary>
        public Vector2? ScalingOrigin { get; set; }
        public float Alpha { get; set; }

        public void OffscreenRender()
        {
            if (sliderFrameBuffer == null)
                sliderFrameBuffer = ObjectPool<SliderFrameBuffer>.Take();

            if (radius < -1)
                throw new Exception("Slider radius was less than 0????");

            //Console.WriteLine(ObjectPool<SliderFrameBuffer>.TotalCreated);

            //if (!hasBeenUpdated)
            //    return;

            hasBeenUpdated = false;

            //Avoid 'context' switches by rendering all the sliders in the background first

            frameBuffer.Bind();

            Viewport.SetViewport(0, 0, frameBuffer.Width, frameBuffer.Height);
            GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
            sliderShader.Bind();
            sliderShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, 0, frameBuffer.Height, 1, -1));
            sliderShader.SetFloat("u_OsuRadius", radius);
            drawSlider();
        }

        public void Render(Graphics g)
        {
            if (Precision.AlmostEquals(Alpha, 0))
                return;

            if (Path.Points.Count == 0)
            {
                g.DrawString($"No slider points?", Font.DefaultFont, Path.CalculatePositionAtProgress(0), Colors.Red);
                return;
            }

            Rectangle texCoords = new Rectangle();
            Vector2 renderPosition;

            var tx = path.Bounds.X - radius + SliderFrameBuffer.BufferSpace.X / 2;
            var ty = path.Bounds.Y - radius + SliderFrameBuffer.BufferSpace.Y / 2;
            var tsize = Path.Bounds.Size + new Vector2(radius * 2);

            texCoords.X = tx;
            texCoords.Y = ty;
            texCoords.Width = tsize.X;
            texCoords.Height = tsize.Y;

            //If hr is on change the way the slider is rendered so it matches
            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HR))
            {
                //texCoords = new Rectangle(0, 1, 1, -1);
                texCoords.Y += tsize.Y;
                texCoords.Height = -tsize.Y;
                renderPosition = OsuContainer.MapToPlayfield(Path.Bounds.X - radius, Path.Bounds.Y + Path.Bounds.Height + radius);
            }
            else
            {

                renderPosition = OsuContainer.MapToPlayfield(Path.Bounds.Position - new Vector2(radius));
            }

            //Convert the size of the slider from osu pixels to playfield pixels, since the points and the radius are in osu pixels
            Vector2 renderSize = (Path.Bounds.Size + new Vector2(radius * 2));

            renderSize *= OsuContainer.Playfield.Width / 512;

            /*
            g.DrawRectangle(OsuContainer.Playfield.TopLeft, OsuContainer.Playfield.Size, new Vector4(0f, 1f, 0, 0.2f));

            g.DrawRectangle(Vector2.Zero, frameBuffer.Texture.Size, new Vector4(1f, 1f, 0, 0.2f), frameBuffer.Texture);

            g.DrawRectangleCentered(new Vector2(tx, ty), new Vector2(4), Colors.Red);

            g.DrawRectangle(new Vector2(tx, ty), tsize, new Vector4(1f, 0f, 0f, 0.2f));
            */

            //Console.WriteLine(renderSize);
            //g.DrawRectangle(renderPosition, renderSize, new Vector4(1, 1, 1, Alpha * 0.5f));

            if (ScalingOrigin.HasValue)
            {
                Vector2 size = renderSize * DrawScale;

                Vector2 diff = renderPosition - ScalingOrigin.Value;

                Vector2 fp = ScalingOrigin.Value + diff * DrawScale;

                //The color is 10000 red, to make the shader know that this is a slider, see Default.Frag
                //i had to hack this in since otherwise i would have to end the graphics batch, render this, and begin the graphics batch again
                //causing huge performance dips on mobile
                g.DrawRectangle(fp, size, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, false);
            }
            else
            {
                //^ and we draw this, within the main batcher
                g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, false);

                //g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, renderPosition, (float)MainGame.Instance.TotalTime);
            }
        }

        public override string ToString()
        {
            return $"Points: {Path.Points.Count} Vertices: {sliderBatch.PrimitiveType}";
        }

        public void Cleanup()
        {
            ObjectPool<SliderFrameBuffer>.Return(sliderFrameBuffer);
            sliderFrameBuffer = null;

            hasBeenUpdated = true;
        }
    }
}
