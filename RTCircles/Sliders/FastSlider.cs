using Easy2D;
using Easy2D.Game;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RTCircles
{
    //This ended up being slower somehow..

    /// <summary>
    /// A ugly fast slider that doesn't use a framebuffer
    /// </summary>
    ///TODO:
    ///Half circles, one sided lines
    ///Fix interpolated points cancer
    ///Make pretier somehow with lots of hacks and experimenting
    ///Only update/resubmit the contents of the slider when it has been changed, either that or global vertexbuffer
    ///disable slider snaking i guess lol man
    ///pregenerate them to disk as a texture
    ///download them from internet
    ///remove sliders :tf:
    //public class FastSlider
    //{
    //    private const int CIRCLE_RESOLUTION = 40;

    //    private static PrimitiveBatch<Vector3> batch = new PrimitiveBatch<Vector3>(20000, 40000) { Resizable = false, AutoClearOnRender = false };
    //    public Path Path = new Path();

    //    private static Easy2D.Shader SliderShader = new Easy2D.Shader();
    //    private static Easy2D.Texture SliderGradient;

    //    static FastSlider()
    //    {
    //        SliderShader.AttachShader(Silk.NET.OpenGLES.ShaderType.VertexShader, Utils.GetResource("Sliders.FastSlider.vert"));
    //        SliderShader.AttachShader(Silk.NET.OpenGLES.ShaderType.FragmentShader, Utils.GetResource("Sliders.FastSlider.frag"));

    //        SliderGradient = new Easy2D.Texture(Utils.GetResource("Sliders.slidergradient.png"));
    //    }

    //    private float startProgress = 0f;
    //    private float endProgress = 1f;
    //    public void SetProgress(float startProgress, float endProgress)
    //    {
    //        this.startProgress = startProgress;
    //        this.endProgress = endProgress;
    //    }

    //    public void SetPoints(List<Vector2> points, float pointSize)
    //    {
    //        Path.Radius = pointSize;
    //        Path.SetPoints(points);
    //    }

    //    private void drawLine(Vector2 startPosition, Vector2 endPosition, float radius)
    //    {
    //        Vector2 difference = endPosition - startPosition;
    //        Vector2 perpen = new Vector2(difference.Y, -difference.X);

    //        perpen.Normalize();

    //        //First line top
    //        Vector3 topRight = new Vector3(startPosition.X + perpen.X * radius,
    //            startPosition.Y + perpen.Y * radius, 0);

    //        Vector3 topLeft = new Vector3(endPosition.X + perpen.X * radius,
    //            endPosition.Y + perpen.Y * radius, 0);

    //        Vector3 bottomRight = new Vector3(startPosition.X, startPosition.Y, 0);

    //        Vector3 bottomLeft = new Vector3(endPosition.X, endPosition.Y, 0);

    //        var quad = batch.GetQuad();

    //        quad[0] = topRight;

    //        quad[1] = bottomRight;

    //        quad[2] = bottomLeft;

    //        quad[3] = topLeft;

    //        //Second line bottom

    //        topRight = bottomRight;

    //        topLeft = bottomLeft;

    //        bottomRight = new Vector3(startPosition.X - perpen.X * radius,
    //                                         startPosition.Y - perpen.Y * radius, 0);

    //        bottomLeft = new Vector3(endPosition.X - perpen.X * radius,
    //                                         endPosition.Y - perpen.Y * radius, 0);

    //        quad = batch.GetQuad();

    //        quad[0] = topRight;

    //        quad[1] = bottomRight;

    //        quad[2] = bottomLeft;

    //        quad[3] = topLeft;
    //    }

    //    //fix only use circle neccersary
    //    //use half circle
    //    private void drawCircle(Vector2 pos, float radius)
    //    {
    //        var verts = batch.GetTriangleFan(CIRCLE_RESOLUTION);

    //        verts[0] = new Vector3(pos.X, pos.Y, 0);

    //        float theta = 0;
    //        float stepTheta = (MathF.PI * 2f) / (verts.Length - 2);

    //        Vector3 vertPos = new Vector3(0, 0, 0);

    //        for (int i = 1; i < verts.Length; i++)
    //        {
    //            vertPos.X = MathF.Cos(theta) * radius + pos.X;
    //            vertPos.Y = MathF.Sin(theta) * radius + pos.Y;

    //            verts[i] = vertPos;

    //            theta += stepTheta;
    //        }
    //    }

    //    //dont interpolate the points like this ffs
    //    private List<Vector2> interpolatedPoints = new List<Vector2>();
    //    private void renderSlider(float scale = 1f, bool redraw = false)
    //    {
    //        if (redraw)
    //        {
    //            batch.Redraw();
    //            batch.Clear();
    //            return;
    //        }

    //        //scale *= (OsuContainer.Playfield.Width / 512);

    //        interpolatedPoints.Clear();

    //        float startLength = Path.Length * startProgress;
    //        float endLength = Path.Length * endProgress;

    //        for (int i = 0; i < Path.Points.Count - 1; i++)
    //        {
    //            float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

    //            if (startLength - dist <= 0 && interpolatedPoints.Count == 0)
    //            {
    //                float blend = startLength / dist;
    //                //put start circle
    //                interpolatedPoints.Add(MathUtils.Lerp(Path.Points[i], Path.Points[i + 1], blend));
    //            }
    //            startLength -= dist;

    //            if (endLength - dist <= 0)
    //            {
    //                float blend = endLength / dist;
    //                //put end circle
    //                interpolatedPoints.Add(MathUtils.Lerp(Path.Points[i], Path.Points[i + 1], blend));
    //                break;
    //            }
    //            endLength -= dist;

    //            //put lines
    //            if (interpolatedPoints.Count > 0)
    //                interpolatedPoints.Add(Path.Points[i + 1]);
    //        }

    //        //put end circle
    //        if (interpolatedPoints.Count == 0)
    //            interpolatedPoints.Add(Path.CalculatePositionAtProgress(endProgress));

    //        float radius = Path.Radius * scale;

    //        //TODO: make the first and last circle a half circle, makes more sense.

    //        //Draw the first circle
    //        drawCircle(interpolatedPoints[0], radius);

    //        for (int i = 0; i < interpolatedPoints.Count - 1; i++)
    //        {
    //            var now = interpolatedPoints[i];
    //            var next = interpolatedPoints[i + 1];

    //            //Draw line between points
    //            drawLine(now, next, radius);

    //            //Draw circle if it's not the first or the last point
    //            if (i > 0 && i < interpolatedPoints.Count - 1)
    //                drawCircle(now, radius);
    //        }

    //        //Draw the last circle
    //        if (interpolatedPoints.Count > 1)
    //            drawCircle(interpolatedPoints[interpolatedPoints.Count - 1], radius);

    //        //Console.WriteLine($"[FastSlider] Verts: {batch.VertexRenderCount} Inds: {batch.IndexRenderCount}");

    //        batch.Draw();
    //    }

    //    public Vector4 BorderColor;
    //    public Vector4 InnerColor;

    //    public void Render(Graphics g)
    //    {
    //        if (Path.Points.Count == 0)
    //            return;

    //        g.EndDraw();

    //        GL.Instance.Enable(EnableCap.DepthTest);
    //        GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
    //        SliderShader.Bind();

    //        var proj = Matrix4.CreateScale((OsuContainer.Playfield.Width / 512)) * Matrix4.CreateTranslation(OsuContainer.Playfield.X, OsuContainer.Playfield.Y, 0) * g.Projection;

    //        float scale = (float)Input.MousePosition.X.Map(0, MainGame.WindowWidth, 1, 1.5f);
    //        float realScale = (OsuContainer.Playfield.Width / 512) * scale;

    //        var proj2 = Matrix4.CreateTranslation(Path.Bounds.Center.X * realScale, Path.Bounds.Center.Y * realScale, 0);

    //        var projOuter = Matrix4.CreateTranslation(Path.Radius / realScale, Path.Radius / realScale, 0) * proj * Matrix4.CreateScale(scale);

    //        SliderShader.SetMatrix("u_Projection", proj);
    //        SliderShader.SetVector("u_Color", InnerColor);
    //        renderSlider(0.82f);

    //        SliderShader.SetVector("u_Color", BorderColor);
    //        // Matrix4.CreateScale(1.146f);
    //        SliderShader.SetMatrix("u_Projection", projOuter);
    //        renderSlider(0, true);

    //        GL.Instance.Disable(EnableCap.DepthTest);
    //    }

    //    /// <summary>
    //    /// Does nothing
    //    /// </summary>
    //    public void DeleteFramebuffer()
    //    {
            
    //    }
    //}
}
