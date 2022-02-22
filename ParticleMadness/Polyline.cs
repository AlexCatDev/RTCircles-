using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ParticleMadness
{
    public static class Polyline
    {
        public enum JointStyle
        {
            Bevel,
            Miter,
            Round
        }

        public enum CapStyle
        {
            Butt,
            Square,
            Round
        }
        private const float EPSILON = 0.0001f;

        private static List<Vector2> middlePoints = new List<Vector2>();
        public static void GetStrokeGeometry(List<Vector2> points, JointStyle jointStyle, CapStyle capStyle, float thickness, List<Vector2> vertices)
        {
            vertices.Clear();
            middlePoints.Clear();

            if (points.Count < 2)
                return;

            bool closed = true;
            var miterLimit = 10;

            if (points.Count == 2)
            {
                jointStyle = JointStyle.Bevel;
                createTriangles(points[0], Middle(points[0], points[1]), points[1], vertices, thickness, jointStyle, miterLimit);
            }
            else
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    if (i == 0)
                    {
                        middlePoints.Add(points[0]);
                    }
                    else if (i == points.Count - 2)
                    {
                        middlePoints.Add(points[points.Count - 1]);
                    }
                    else
                    {
                        middlePoints.Add(Middle(points[i], points[i + 1]));
                    }
                }

                for (int i = 1; i < middlePoints.Count; i++)
                {
                    createTriangles(middlePoints[i - 1], points[i], middlePoints[i], vertices, thickness, jointStyle, miterLimit);
                }
            }

            if (!closed)
            {
                if(capStyle == CapStyle.Round)
                {
                    var p00 = vertices[0];
                    var p01 = vertices[1];
                    var p02 = points[1];
                    var p10 = vertices[vertices.Count - 1];
                    var p11 = vertices[vertices.Count - 3];
                    var p12 = points[points.Count - 2];

                    createRoundCap(points[0], p00, p01, p02, vertices);
                    createRoundCap(points[points.Count - 1], p10, p11, p12, vertices);
                }
                else if(capStyle == CapStyle.Square)
                {
                    var p00 = vertices[vertices.Count - 1];
                    var p01 = vertices[vertices.Count - 3];

                    createSquareCap(
                            vertices[0],
                            vertices[1],
                            (points[0] - points[1]).Normalized() * (points[0] - vertices[0]).Length,
                            vertices);
                    createSquareCap(
                            p00,
                            p01,
                            (points[^1] - points[^2]).Normalized() * (p01 - points[^1]).Length,
                            vertices);
                }
            }
        }

        private static void createTriangles(Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> verts, float thickness, JointStyle jointStyle, float miterLimit)
        {
            var t0 = p1 - p0;
            var t2 = p2 - p1;

            t0 = t0.PerpendicularLeft.Normalized() * thickness;
            t2 = t2.PerpendicularLeft.Normalized() * thickness;

            if(SignedArea(p0, p1, p2) > 0)
            {
                t0 = -t0;
                t2 = -t2;
            }

            var pintersect = LineIntersection(p0 + t0, p1 + t0, p2 + t2, p1 + t2);

            Vector2 anchor = Vector2.Zero;

            var anchorLength = float.MaxValue;

            if (pintersect.HasValue)
            {
                anchor = pintersect.Value - p1;
                anchorLength = Length(anchor);
            }

            var dd = anchorLength / thickness;

            var p0p1 = p0 - p1;
            var p0p1Length = Length(p0p1);

            var p1p2 = p1 - p2;
            var p1p2Length = Length(p1p2);

            if(anchorLength > p0p1Length || anchorLength > p1p2Length)
            {
                verts.Add(p0 + t0);
                    verts.Add(p0 - t0);
                    verts.Add(p1 + t0);

                    verts.Add(p0 - t0);
                    verts.Add(p1 + t0);
                    verts.Add(p1 - t0);

                    if ( jointStyle == JointStyle.Round) {
                        createRoundCap(p1, p1 + t0, p1 + t2, p2, verts);
                    } else if (jointStyle == JointStyle.Bevel || (jointStyle == JointStyle.Miter && dd>=miterLimit) ) {
                        verts.Add( p1 );
                        verts.Add( p1 + t0 );
                        verts.Add( p1 + t2 );
                    } else if (jointStyle == JointStyle.Miter && dd<miterLimit && pintersect.HasValue) {

                        verts.Add( p1 + t0 );
                        verts.Add( p1 );
                        verts.Add( pintersect.Value );

                        verts.Add( p1 + t2 );
                        verts.Add( p1 );
                        verts.Add( pintersect.Value );
                    }

                    verts.Add(p2 + t2);
                    verts.Add(p1 - t2);
                    verts.Add(p1 + t2);

                    verts.Add(p2 + t2);
                    verts.Add(p1 - t2);
                    verts.Add(p2 - t2); 
                
            }
            else
            {
                 verts.Add(p0 + t0);
                    verts.Add(p0 - t0);
                    verts.Add(p1 - anchor);

                    verts.Add(p0 + t0);
                    verts.Add(p1 - anchor);
                    verts.Add(p1 + t0);

                    if (jointStyle == JointStyle.Round) {

                        var _p0 = p1 + t0;
                        var _p1 = p1 + t2;
                        var _p2 = p1 - anchor;

                        var center = p1;

                        verts.Add(_p0);
                        verts.Add(center);
                        verts.Add(_p2);

                        createRoundCap(center, _p0, _p1, _p2, verts);

                        verts.Add(center);
                        verts.Add(_p1);
                        verts.Add(_p2);

                    } else {

                        if (jointStyle == JointStyle.Bevel || (jointStyle == JointStyle.Miter && dd >= miterLimit)) {
                            verts.Add(p1 + t0);
                            verts.Add(p1 + t2);
                            verts.Add(p1 - anchor);
                        }

                        if (jointStyle == JointStyle.Miter && dd < miterLimit) {
                            verts.Add(pintersect.Value);
                            verts.Add(p1 + t0);
                            verts.Add(p1 + t2);
                        }
                    }

                    verts.Add(p2 + t2);
                    verts.Add(p1 - anchor);
                    verts.Add(p1 + t2);
                    verts.Add(p2 + t2);
                    verts.Add(p1 - anchor);
                    verts.Add(p2 + t2);
            }
        }

        private static void createSquareCap(Vector2 p0, Vector2 p1, Vector2 dir, List<Vector2> verts)
        {
            verts.Add(p0);
            verts.Add(p0 + dir);
            verts.Add(p1 + dir);

            verts.Add(p1);
            verts.Add(p1 + dir);
            verts.Add(p0);
        }

        private static void createRoundCap(Vector2 center, Vector2 _p0, Vector2 _p1,
            Vector2 nextPointInLine, List<Vector2> verts)
        {
            var radius = Length(center - _p0);

            var angle0 = MathF.Atan2((_p1.Y - center.Y), (_p1.X - center.X));
            var angle1 = MathF.Atan2((_p0.Y - center.Y), (_p0.X - center.X));

            var orgAngle0 = angle0;

            if(angle1 > angle0)
            {
                while (angle1 - angle0 >= MathF.PI - EPSILON)
                {
                    angle1 = angle1 - 2 * MathF.PI;
                }
            }
            else
            {
                while (angle0 - angle1 >= MathF.PI - EPSILON)
                {
                    angle0 = angle0 - 2 * MathF.PI;
                }
            }

            var angleDiff = angle1 - angle0;

            if (MathF.Abs(angleDiff) >= MathF.PI - EPSILON && MathF.Abs(angleDiff) <= MathF.PI + EPSILON)
            {
                var r1 = center - nextPointInLine;

                if(r1.X == 0)
                {
                    if(r1.Y > 0)
                    {
                        angleDiff = -angleDiff;
                    }
                } 
                else if(r1.X >= -EPSILON)
                {
                    angleDiff = -angleDiff;
                }
            }

            var nsegments = (MathF.Abs(angleDiff * radius) / 7);
            nsegments++;

            var angleInc = angleDiff / nsegments;

            for (int i = 0; i < nsegments; i++)
            {
                verts.Add(new Vector2(center.X, center.Y));
                verts.Add(new Vector2(
                        center.X + radius * MathF.Cos(orgAngle0 + angleInc * i),
                        center.Y + radius * MathF.Sin(orgAngle0 + angleInc * i)
                ));
                verts.Add(new Vector2(
                        center.X + radius * MathF.Cos(orgAngle0 + angleInc * (1 + i)),
                        center.Y + radius * MathF.Sin(orgAngle0 + angleInc * (1 + i))
                ));
            }
        }

        /// <summary>
        /// Get where two lines intersect each other
        /// </summary>
        /// <param name="p0">Line1 Start</param>
        /// <param name="p1">Line1 End</param>
        /// <param name="p2">Line2 Start</param>
        /// <param name="p3">Line2 End</param>
        /// <returns>Null if no intersection</returns>
        public static Vector2? LineIntersection(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var a0 = p1.Y - p0.Y;
            var b0 = p0.X - p1.X;

            var a1 = p3.Y - p2.Y;
            var b1 = p2.X - p3.X;

            var det = a0 * b1 - a1 * b0;

            if (MathF.Abs(det) < EPSILON)
                return null;

            /*
            if (det > -EPSILON && det < EPSILON)
                return null;
            */

            var c0 = a0 * p0.X + b0 * p0.Y;
            var c1 = a1 * p2.X + b1 * p2.Y;

            var x = (b1 * c0 - b0 * c1) / det;
            var y = (a0 * c1 - a1 * c0) / det;
            return new Vector2(x, y);
        }

        private static float Length(Vector2 p0)
        {
            return MathF.Sqrt(p0.X * p0.X + p0.Y * p0.Y);
        }

        private static Vector2 Middle(Vector2 a, Vector2 b) 
        {
            return (a + b) * 0.5f;
        }

        private static float SignedArea(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            return (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
        }
    }
}
