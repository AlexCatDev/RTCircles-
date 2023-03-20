using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class BetterSlider : ISlider
    {
        private const int CIRCLE_RESOLUTION = 40;

        private static readonly Easy2D.Shader sliderShader = new Easy2D.Shader();

        static BetterSlider()
        {
            sliderShader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Sliders.slider.vert"));
            sliderShader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Sliders.slider.frag"));
        }

        public Path path = new Path();

        private float radius = -1;

        private FrameBuffer frameBuffer = new FrameBuffer(1, 1, FramebufferAttachment.DepthAttachment,
            InternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedShort);

        //1mb for stupid aspire abuse sliders
        //Also optimize circle betweens points, so i can cut this down and in turn the amount of vertices to render
        private static readonly UnsafePrimitiveBatch<Vector3> sliderBatch = new UnsafePrimitiveBatch<Vector3>(50_000, 130_000);

        private void drawLine(Vector2 startPosition, Vector2 endPosition, float radius)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            //First line top
            Vector3 topRight = new Vector3(startPosition.X + perpen.X * radius,
                startPosition.Y + perpen.Y * radius, 1);

            Vector3 topLeft = new Vector3(endPosition.X + perpen.X * radius,
                endPosition.Y + perpen.Y * radius, 1);

            Vector3 bottomRight = new Vector3(startPosition.X, startPosition.Y, 0);

            Vector3 bottomLeft = new Vector3(endPosition.X, endPosition.Y, 0); ;

            unsafe
            {
                var quad = sliderBatch.GetQuad();

                quad[0] = topRight;
                quad[1] = bottomRight;
                quad[2] = bottomLeft;
                quad[3] = topLeft;

                //Second line bottom

                topRight = bottomRight;

                topLeft = bottomLeft;

                bottomRight = new Vector3(startPosition.X - perpen.X * radius,
                                                 startPosition.Y - perpen.Y * radius, 1);

                bottomLeft = new Vector3(endPosition.X - perpen.X * radius,
                                                 endPosition.Y - perpen.Y * radius, 1);

                quad = sliderBatch.GetQuad();

                quad[0] = topRight;
                quad[1] = bottomRight;
                quad[2] = bottomLeft;
                quad[3] = topLeft;
            }
        }

        //Circles can be pregenerated, optimize circle resolution for the radius
        private void drawCircle(Vector2 pos, float radius)
        {
            unsafe
            {
                var verts = sliderBatch.GetTriangleFan(CIRCLE_RESOLUTION);

                verts[0] = new Vector3(pos.X, pos.Y, 0f);

                float theta = 0;
                const float stepTheta = (MathF.PI * 2) / (CIRCLE_RESOLUTION - 2);

                Vector3 vertPos = new Vector3(0, 0, 1f);

                for (int i = 1; i < CIRCLE_RESOLUTION; i++)
                {
                    vertPos.X = MathF.Cos(theta) * radius + pos.X;
                    vertPos.Y = MathF.Sin(theta) * radius + pos.Y;

                    verts[i] = vertPos;

                    theta += stepTheta;
                }
            }
        }

        private void drawSlider()
        {
            //The path coordinates are in osu pixel space, and we need to convert them to framebuffer space
            Vector2 posOffset = Path.Bounds.Position - new Vector2(radius);

            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            //All of this is stupid, unreadable and gave me cancer looking at it and made me die writing it all for stupid optimizations
            if (startLength == endLength)
                drawCircle(Path.CalculatePositionAtProgress(endProgress) - posOffset, radius);
            else
            {
                int count = 0;

                Vector2? start = null;
                Vector2? end = null;

                for (int i = 0; i < Path.Points.Count - 1; i++)
                {
                    float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

                    if (startLength - dist <= 0 && count == 0)
                    {
                        float blend = startLength / dist;
                        start = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        drawCircle(start.Value, radius);
                        count++;
                    }

                    if (endLength - dist <= 0 && end.HasValue == false)
                    {
                        float blend = endLength / dist;
                        end = Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        drawCircle(end.Value, radius);
                        count++;
                    }
                    startLength -= dist;
                    endLength -= dist;

                    if (count > 0)
                    {
                        drawLine(start ?? Path.Points[i] - posOffset, end ?? Path.Points[i + 1] - posOffset, radius);
                        start = null;

                        if (end.HasValue == false)
                            drawCircle(Path.Points[i + 1] - posOffset, radius);

                        count++;

                        if (end.HasValue)
                            break;
                    }
                }
            }
            sliderBatch.Draw();
        }

        public void SetRadius(float radius)
        {
            if (this.radius != radius)
            {
                this.radius = radius;

                int beforeWidth = frameBuffer.Width;
                int beforeHeight = frameBuffer.Height;

                //Make framebuffer the size of the slider bounding box, + the circle radius (circle radius and size is in osu pixels)
                frameBuffer.EnsureSize(Path.Bounds.Width + radius * 2, Path.Bounds.Height + radius * 2);

                //Cant really rely on a float comparison
                if (beforeWidth != frameBuffer.Width || beforeHeight != frameBuffer.Height)
                {
                    projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, frameBuffer.Height, 0, 1f, -1f);
                    hasBeenUpdated = true;
                }
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

        private Matrix4 projectionMatrix;

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
            if (radius < -1)
                throw new Exception("Slider radius was less than 0????");

            if (!hasBeenUpdated)
                return;

            hasBeenUpdated = false;

            frameBuffer.Bind();
            Viewport.SetViewport(0, 0, frameBuffer.Width, frameBuffer.Height);
            GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
            sliderShader.Bind();
            sliderShader.SetMatrix("u_Projection", projectionMatrix);
            drawSlider();
        }

        public void Render(Graphics g)
        {
            if (Path.Points.Count == 0 || (frameBuffer.Status != GLEnum.FramebufferComplete && frameBuffer.IsInitialized))
            {
                g.DrawString($"Aspire slider brr\nPoints: {Path.Points.Count} {frameBuffer.Width}x{frameBuffer.Height}", Font.DefaultFont, Path.CalculatePositionAtProgress(0), Colors.Red);
                return;
            }

            if (Precision.AlmostEquals(Alpha, 0))
                return;

            Rectangle bounds = Path.Bounds;

            bounds.X -= radius;
            bounds.Y -= radius;

            Rectangle texCoords;
            Vector2 renderPosition;
            //If hr is on change the way the slider is rendered so it matches
            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HR))
            {
                texCoords = new Rectangle(0, 0, 1, 1);
                renderPosition = OsuContainer.MapToPlayfield(bounds.Position.X, bounds.Position.Y + frameBuffer.Height);
            }
            else
            {
                texCoords = new Rectangle(0, 1, 1, -1);
                renderPosition = OsuContainer.MapToPlayfield(bounds.Position.X, bounds.Position.Y);
            }

            //Convert the size of the slider from osu pixels to playfield pixels, since the points and the radius are in osu pixels
            Vector2 renderSize = new Vector2(frameBuffer.Width, frameBuffer.Height) * OsuContainer.OsuScale;

            if (ScalingOrigin.HasValue)
            {
                Vector2 size = renderSize * DrawScale;

                Vector2 diff = renderPosition - ScalingOrigin.Value;

                Vector2 fp = ScalingOrigin.Value + diff * DrawScale;

                //The color is 10000 red, to make the shader know that this is a slider, see Default.Frag
                //i had to hack this in since otherwise i would have to end the graphics batch, render this, and begin the graphics batch again
                //causing huge performance dips on mobile
                g.DrawRectangle(fp, size, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);
            }
            else
            {
                //^ and we draw this, within the main batcher
                g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);

                //g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, renderPosition, (float)MainGame.Instance.TotalTime);
            }
        }

        public override string ToString()
        {
            return $"Points: {Path.Points.Count} Vertices: {sliderBatch.VertexRenderCount}";
        }

        public void Cleanup()
        {
            GPUSched.Instance.Enqueue(() =>
            {
                //Todo: Check if delete is still pending when this is called.         
                frameBuffer.Delete();
                hasBeenUpdated = true;
            });
        }
    }

    //public class BetterSlider : ISlider
    //{
    //    public const int CIRCLE_VERTICES = 40;
    //    public const float SliderResolution = 1;

    //    private static readonly Easy2D.Shader sliderShader = new Easy2D.Shader();

    //    static BetterSlider()
    //    {
    //        sliderShader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Sliders.slider.vert"));
    //        sliderShader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Sliders.slider.frag"));
    //    }

    //    public Path path = new Path();

    //    private float radius = -1;

    //    private FrameBuffer frameBuffer = new FrameBuffer(1, 1, FramebufferAttachment.DepthAttachment,
    //        InternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedShort);

    //    //1mb for stupid aspire abuse sliders
    //    //Also optimize circle betweens points, so i can cut this down and in turn the amount of vertices to render
    //    private static readonly UnsafePrimitiveBatch<Vector3> sliderBatch = new UnsafePrimitiveBatch<Vector3>(50_000, 130_000);

    //    private void drawLine(Vector2 startPosition, Vector2 endPosition, float radius)
    //    {
    //        startPosition *= SliderResolution;
    //        endPosition *= SliderResolution;
    //        radius *= SliderResolution;

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
    //        pos *= SliderResolution;
    //        radius *= SliderResolution;

    //        unsafe
    //        {
    //            var verts = sliderBatch.GetTriangleFan(CIRCLE_VERTICES);

    //            verts[0] = new Vector3(pos.X, pos.Y, 0f);

    //            float theta = 0;
    //            const float stepTheta = (MathF.PI * 2) / (CIRCLE_VERTICES - 2);

    //            Vector3 vertPos = new Vector3(0, 0, 1f);

    //            for (int i = 1; i < CIRCLE_VERTICES; i++)
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
    //        Vector2 posOffset = Path.Bounds.Position - new Vector2(radius);

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

    //            int beforeWidth = frameBuffer.Width;
    //            int beforeHeight = frameBuffer.Height;

    //            //Make framebuffer the size of the slider bounding box, + the circle radius (circle radius and size is in osu pixels)
    //            frameBuffer.EnsureSize((Path.Bounds.Width + radius * 2) * SliderResolution, (Path.Bounds.Height + radius * 2) * DrawableSlider.SliderResolution);

    //            //Cant really rely on a float comparison
    //            if (beforeWidth != frameBuffer.Width || beforeHeight != frameBuffer.Height)
    //            {
    //                projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, frameBuffer.Width, frameBuffer.Height, 0, 1f, -1f);
    //                hasBeenUpdated = true;
    //            }
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

    //    private Matrix4 projectionMatrix;

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
    //        if (radius < -1)
    //            throw new Exception("Slider radius was less than 0????");

    //        if (!hasBeenUpdated)
    //            return;

    //        hasBeenUpdated = false;

    //        frameBuffer.Bind();
    //        Viewport.SetViewport(0, 0, frameBuffer.Width, frameBuffer.Height);
    //        GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
    //        sliderShader.Bind();
    //        sliderShader.SetMatrix("u_Projection", projectionMatrix);
    //        drawSlider();
    //    }

    //    public void Render(Graphics g)
    //    {
    //        if (Path.Points.Count == 0 || (frameBuffer.Status != GLEnum.FramebufferComplete && frameBuffer.IsInitialized))
    //        {
    //            g.DrawString($"Aspire slider brr\nPoints: {Path.Points.Count} {frameBuffer.Width}x{frameBuffer.Height}", Font.DefaultFont, Path.CalculatePositionAtProgress(0), Colors.Red);
    //            return;
    //        }

    //        if (Precision.AlmostEquals(Alpha, 0))
    //            return;

    //        var scaleReverse = 1 / SliderResolution;

    //        Rectangle bounds = Path.Bounds;

    //        bounds.X -= radius;
    //        bounds.Y -= radius;

    //        Rectangle texCoords;
    //        Vector2 renderPosition;
    //        //If hr is on change the way the slider is rendered so it matches
    //        if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HR))
    //        {
    //            texCoords = new Rectangle(0, 0, 1, 1);
    //            renderPosition = OsuContainer.MapToPlayfield(bounds.Position.X, bounds.Position.Y + frameBuffer.Height);
    //        }
    //        else
    //        {
    //            texCoords = new Rectangle(0, 1, 1, -1);
    //            renderPosition = OsuContainer.MapToPlayfield(bounds.Position.X, bounds.Position.Y);
    //        }

    //        //Convert the size of the slider from osu pixels to playfield pixels, since the points and the radius are in osu pixels
    //        Vector2 renderSize = new Vector2(frameBuffer.Width, frameBuffer.Height) * (OsuContainer.Playfield.Width / 512);

    //        if (ScalingOrigin.HasValue)
    //        {
    //            Vector2 size = renderSize * DrawScale;

    //            Vector2 diff = renderPosition - ScalingOrigin.Value;

    //            Vector2 fp = ScalingOrigin.Value + diff * DrawScale;

    //            //The color is 10000 red, to make the shader know that this is a slider, see Default.Frag
    //            //i had to hack this in since otherwise i would have to end the graphics batch, render this, and begin the graphics batch again
    //            //causing huge performance dips on mobile
    //            g.DrawRectangle(fp, size * scaleReverse, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);
    //        }
    //        else
    //        {
    //            //^ and we draw this, within the main batcher
    //            g.DrawRectangle(renderPosition, renderSize * scaleReverse, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);

    //            //g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, renderPosition, (float)MainGame.Instance.TotalTime);
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return $"Points: {Path.Points.Count} Vertices: {sliderBatch.VertexRenderCount}";
    //    }

    //    public void Cleanup()
    //    {
    //        GPUSched.Instance.Enqueue(() =>
    //        {
    //            //Todo: Check if delete is still pending when this is called.         
    //            frameBuffer.Delete();
    //            hasBeenUpdated = true;
    //        });
    //    }
    //}
}
