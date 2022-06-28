using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RTCircles.Sliders
{
    public struct LineSegment
    {
        /// <summary>
        /// The starting point (a)
        /// </summary>
        public Vector2 start;

        /// <summary>
        /// The ending point (b)
        /// </summary>
        public Vector2 end;

        public LineSegment(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }

        public static LineSegment operator +(LineSegment segment, Vector2 toAdd)
        {
            segment.start += toAdd;
            segment.end += toAdd;

            return segment;
        }

        public static LineSegment operator -(LineSegment segment, Vector2 toRemove)
        {
            segment.start -= toRemove;
            segment.end -= toRemove;

            return segment;
        }

        public Vector2 Direction(bool normalized = true)
        {
            var vec = end - start;

            if (normalized)
                vec.Normalize();

            return vec;
        }

        public Vector2 Normal()
        {
            var dir = Direction();
            return new Vector2(-dir.Y, dir.X);
        }

        public static Vector2? Intersection(LineSegment a, LineSegment b, bool infiniteLines)
        {
            // calculate un-normalized direction vectors
            var r = a.Direction(false);
            var s = b.Direction(false);

            var originDist = b.start - a.start;

            var uNumerator = Vector2.PerpDot(originDist, r);
            var denominator = Vector2.PerpDot(r, s);

            // The lines are parallel
            if (MathF.Abs(denominator) < 0.0001f)
                return null;

            // solve the intersection positions
            var u = uNumerator / denominator;
            var t = Vector2.PerpDot(originDist, s) / denominator;

            // the intersection lies outside of the line segments
            if (!infiniteLines && (t < 0 || t > 1 || u < 0 || u > 1))
                return null;

            // calculate the intersection point
            // a.a + r * t;

            return a.start + (r * t);
        }
    }
    public class Polyline2D
    {
        public struct PolySegment
        {
            public PolySegment(LineSegment center, float thickness)
            {
                this.center = center;

                edge1 = center + center.Normal() * thickness;
                edge2 = center - center.Normal() * thickness;
            }

            public LineSegment center, edge1, edge2;
        }

        public enum JointStyle
        {
            /**
             * Corners are drawn with sharp joints.
             * If the joint's outer angle is too large,
             * the joint is drawn as beveled instead,
             * to avoid the miter extending too far out.
             */
            MITER,
            /**
             * Corners are flattened.
             */
            BEVEL,
            /**
             * Corners are rounded off.
             */
            ROUND
        };

        public enum EndCapStyle
        {
            /**
             * Path ends are drawn flat,
             * and don't exceed the actual end point.
             */
            BUTT, // lol
            /**
             * Path ends are drawn flat,
             * but extended beyond the end point
             * by half the line thickness.
             */
            SQUARE,
            /**
             * Path ends are rounded off.
             */
            ROUND,
            /**
             * Path ends are connected according to the JointStyle.
             * When using this EndCapStyle, don't specify the common start/end point twice,
             * as Polyline2D connects the first and last input point itself.
             */
            JOINT
        };

        /**
        * The threshold for mitered joints.
        * If the joint's angle is smaller than this angle,
        * the joint will be drawn beveled instead.
        */
        private const float miterMinAngle = 0.349066f; // ~20 degrees

        /**
         * The minimum angle of a round joint's triangles.
         */
        private const float roundMinAngle = 0.174533f; // ~10 degrees

        private static void createTriangleFan(Graphics g, Vector4 color, int texSlot, Vector2 connectTo, Vector2 origin, Vector2 start, Vector2 end, bool clockwise, bool isEndCap)
        {
            var point1 = start - origin;
            var point2 = end - origin;

            var angle1 = MathF.Atan2(point1.Y, point1.X);
            var angle2 = MathF.Atan2(point2.Y, point2.X);

            if (clockwise)
            {
                if (angle2 > angle1)
                    angle2 = angle2 - 2 * MathF.PI;
            }
            else
            {
                if (angle1 > angle2)
                    angle1 = angle1 - 2 * MathF.PI;
            }

            var jointAngle = angle2 - angle1;

            var numTriangles = Math.Max(1, (int)Math.Floor(Math.Abs(jointAngle) / roundMinAngle));
            var triAngle = jointAngle / numTriangles;

            Vector2 startPoint = start;
            Vector2 endPoint;

            for (int t = 0; t < numTriangles; t++)
            {
                if (t + 1 == numTriangles)
                {
                    // it's the last triangle - ensure it perfectly
                    // connects to the next line
                    endPoint = end;
                }
                else
                {
                    var rot = (t + 1) * triAngle;

                    // rotate the original point around the origin
                    endPoint.X = MathF.Cos(rot) * point1.X - MathF.Sin(rot) * point1.Y;
                    endPoint.Y = MathF.Sin(rot) * point1.X + MathF.Cos(rot) * point1.Y;

                    // re-add the rotation origin to the target point
                    endPoint = endPoint + origin;
                }

                
                var triangle = g.VertexBatch.GetTriangle();

                triangle[0].Position = startPoint;
                triangle[0].Color = color;
                triangle[0].TextureSlot = texSlot;
                triangle[0].Rotation = 0;
                triangle[0].TexCoord = Vector2.Zero;

                triangle[1].Position = endPoint;
                triangle[1].Color = color;
                triangle[1].TextureSlot = texSlot;
                triangle[1].Rotation = 0;
                triangle[1].TexCoord = Vector2.Zero;

                triangle[2].Position = connectTo;
                triangle[2].Color = color;
                triangle[2].TextureSlot = texSlot;
                triangle[2].Rotation = 0;
                triangle[2].TexCoord = isEndCap ? new Vector2(0.5f) : Vector2.One;

                //g.DrawTriangle(startPoint, endPoint, connectTo, new Vector4(1f,1f,1f,color.W*0.5f));

                // emit the triangle
                //*vertices++ = startPoint;
                //*vertices++ = endPoint;
                //*vertices++ = connectTo;

                startPoint = endPoint;
            }

            //return vertices;
        }

        public static void Render(Graphics g, List<PolySegment> segments, Texture texture, Vector4 color, float thickness,
            JointStyle jointStyle = JointStyle.ROUND, EndCapStyle endCapStyle = EndCapStyle.ROUND,
            bool allowOverlap = false)
        {
            /*
            // create poly segments from the points
            List<PolySegment> segments = new List<PolySegment>();
            for (int i = 0; i + 1 < points.Count; i++)
            {
                var point1 = points[i];
                var point2 = points[i + 1];

                // to avoid division-by-zero errors,
                // only create a line segment for non-identical points

                if (point1 != point2)
                    segments.Add(new PolySegment(new LineSegment(point1, point2), thickness));
            }

            if (endCapStyle == EndCapStyle.JOINT)
            {
                // create a connecting segment from the last to the first point

                var point1 = points[points.Count - 1];
                var point2 = points[0];

                // to avoid division-by-zero errors,
                // only create a line segment for non-identical points
                if (point1 != point2)
                {
                    segments.Add(new PolySegment(new LineSegment(point1, point2), thickness));
                }
            }
            */

            if (segments.Count == 0)
            {
                return;
                // handle the case of insufficient input points
                //return vertices;
            }

            int slot = g.GetTextureSlot(texture);

            if (endCapStyle == EndCapStyle.JOINT)
            {
                // create a connecting segment from the last to the first point
                //last point
                var point1 = segments[^1].center.end;
                //first point
                var point2 = segments[0].center.start;

                // to avoid division-by-zero errors,
                // only create a line segment for non-identical points
                if (point1 != point2)
                    segments.Add(new PolySegment(new LineSegment(point1, point2), thickness));
            }

            Vector2 nextStart1 = Vector2.Zero;
            Vector2 nextStart2 = Vector2.Zero;
            Vector2 start1 = Vector2.Zero;
            Vector2 start2 = Vector2.Zero;
            Vector2 end1 = Vector2.Zero;
            Vector2 end2 = Vector2.Zero;

            // calculate the path's global start and end points
            var firstSegment = segments[0];
            var lastSegment = segments[segments.Count - 1];

            var pathStart1 = firstSegment.edge1.start;
            var pathStart2 = firstSegment.edge2.start;
            var pathEnd1 = lastSegment.edge1.end;
            var pathEnd2 = lastSegment.edge2.end;

            // handle different end cap styles
            if (endCapStyle == EndCapStyle.SQUARE)
            {
                // extend the start/end points by half the thickness
                pathStart1 = pathStart1 - firstSegment.edge1.Direction() * thickness;
                pathStart2 = pathStart2 - firstSegment.edge2.Direction() * thickness;
                pathEnd1 = pathEnd1 + lastSegment.edge1.Direction() * thickness;
                pathEnd2 = pathEnd2 + lastSegment.edge2.Direction() * thickness;

            }
            else if (endCapStyle == EndCapStyle.ROUND)
            {
                // draw half circle end caps
                createTriangleFan(g, color, slot, firstSegment.center.start, firstSegment.center.start,
                                  firstSegment.edge1.start, firstSegment.edge2.start, false, true);
                createTriangleFan(g, color, slot, lastSegment.center.end, lastSegment.center.end,
                                  lastSegment.edge1.end, lastSegment.edge2.end, true, true);

            }
            else if (endCapStyle == EndCapStyle.JOINT)
            {
                // join the last (connecting) segment and the first segment
                createJoint(g, color, slot, lastSegment, firstSegment, jointStyle,
                            ref pathEnd1, ref pathEnd2, ref pathStart1, ref pathStart2, allowOverlap);
            }

            // generate mesh data for path segments
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                // calculate start
                if (i == 0)
                {
                    // this is the first segment
                    start1 = pathStart1;
                    start2 = pathStart2;
                }

                if (i + 1 == segments.Count)
                {
                    // this is the last segment
                    end1 = pathEnd1;
                    end2 = pathEnd2;

                }
                else
                {
                    createJoint(g, color, slot, segment, segments[i + 1], jointStyle,
                                ref end1, ref end2, ref nextStart1, ref nextStart2, allowOverlap);
                }

                //g.DrawTriangle(start1, start2, end1, color);
                //g.DrawTriangle(end1, start2, end2, color);

                var triangle = g.VertexBatch.GetTriangle();

                triangle[0].Position = start1;
                triangle[0].Color = color;
                triangle[0].TextureSlot = slot;
                triangle[0].Rotation = 0;
                triangle[0].TexCoord = Vector2.One;

                triangle[1].Position = start2;
                triangle[1].Color = color;
                triangle[1].TextureSlot = slot;
                triangle[1].Rotation = 0;
                triangle[1].TexCoord = Vector2.Zero;

                triangle[2].Position = end1;
                triangle[2].Color = color;
                triangle[2].TextureSlot = slot;
                triangle[2].Rotation = 0;
                triangle[2].TexCoord = Vector2.One;

                triangle = g.VertexBatch.GetTriangle();

                triangle[0].Position = end1;
                triangle[0].Color = color;
                triangle[0].TextureSlot = slot;
                triangle[0].Rotation = 0;
                triangle[0].TexCoord = Vector2.One;

                triangle[1].Position = start2;
                triangle[1].Color = color;
                triangle[1].TextureSlot = slot;
                triangle[1].Rotation = 0;
                triangle[1].TexCoord = Vector2.Zero;

                triangle[2].Position = end2;
                triangle[2].Color = color;
                triangle[2].TextureSlot = slot;
                triangle[2].Rotation = 0;
                triangle[2].TexCoord = Vector2.Zero;

                // emit vertices
                //*vertices++ = start1;
                //*vertices++ = start2;
                //*vertices++ = end1;

                //*vertices++ = end1;
                //*vertices++ = start2;
                //*vertices++ = end2;

                start1 = nextStart1;
                start2 = nextStart2;
            }

            //return vertices;
        }

        private static float angleFunc(in Vector2 a, in Vector2 b)
        {
            return MathF.Acos(Vector2.Dot(a, b) / a.Length * b.Length);
        }

        private static void createJoint(Graphics g, Vector4 color, int texSlot, in PolySegment segment1, in PolySegment segment2,
            JointStyle jointStyle, ref Vector2 end1, ref Vector2 end2,
            ref Vector2 nextStart1, ref Vector2 nextStart2, bool allowOverlap)
        {
            // calculate the angle between the two line segments
            var dir1 = segment1.center.Direction();
            var dir2 = segment2.center.Direction();

            var angle = angleFunc(in dir1, in dir2);

            // wrap the angle around the 180° mark if it exceeds 90°
            // for minimum angle detection
            var wrappedAngle = angle;
            if (wrappedAngle > MathF.PI / 2)
            {
                wrappedAngle = MathF.PI - wrappedAngle;
            }

            if (jointStyle == JointStyle.MITER && wrappedAngle < miterMinAngle)
            {
                // the minimum angle for mitered joints wasn't exceeded.
                // to avoid the intersection point being extremely far out,
                // thus producing an enormous joint like a rasta on 4/20,
                // we render the joint beveled instead.
                jointStyle = JointStyle.BEVEL;
            }

            if (jointStyle == JointStyle.MITER)
            {
                // calculate each edge's intersection point
                // with the next segment's central line
                var sec1 = LineSegment.Intersection(segment1.edge1, segment2.edge1, true);
                var sec2 = LineSegment.Intersection(segment1.edge2, segment2.edge2, true);

                end1 = sec1 ?? segment1.edge1.end;
                end2 = sec2 ?? segment2.edge2.end;

                nextStart1 = end1;
                nextStart2 = end2;

            }
            else
            {
                // joint style is either BEVEL or ROUND

                // find out which are the inner edges for this joint
                var x1 = dir1.X;
                var x2 = dir2.X;
                var y1 = dir1.Y;
                var y2 = dir2.Y;

                var clockwise = x1 * y2 - x2 * y1 < 0;

                //const LineSegment<Vec2>* inner1, *inner2, *outer1, *outer2;

                LineSegment inner1, inner2, outer1, outer2;

                // as the normal vector is rotated counter-clockwise,
                // the first edge lies to the left
                // from the central line's perspective,
                // and the second one to the right.
                if (clockwise)
                {
                    outer1 = segment1.edge1;
                    outer2 = segment2.edge1;
                    inner1 = segment1.edge2;
                    inner2 = segment2.edge2;
                }
                else
                {
                    outer1 = segment1.edge2;
                    outer2 = segment2.edge2;
                    inner1 = segment1.edge1;
                    inner2 = segment2.edge1;
                }

                // calculate the intersection point of the inner edges
                var innerSecOpt = LineSegment.Intersection(inner1, inner2, allowOverlap);

                // for parallel lines, simply connect them directly
                var innerSec = innerSecOpt ?? inner1.end;

                // if there's no inner intersection, flip
                // the next start position for near-180° turns
                Vector2 innerStart;
                if (innerSecOpt.HasValue)
                {
                    innerStart = innerSec;
                }
                else if (angle > MathF.PI / 2)
                {
                    innerStart = outer1.end;
                }
                else
                {
                    innerStart = inner1.end;
                }

                if (clockwise)
                {
                    end1 = outer1.end;
                    end2 = innerSec;

                    nextStart1 = outer2.start;
                    nextStart2 = innerStart;

                }
                else
                {
                    end1 = innerSec;
                    end2 = outer1.end;

                    nextStart1 = innerStart;
                    nextStart2 = outer2.start;
                }

                // connect the intersection points according to the joint style

                if (jointStyle == JointStyle.BEVEL)
                {
                    //g.DrawTriangle(outer1.end, outer2.start, innerSec, color);
                    // simply connect the intersection points
                    //*vertices++ = outer1->b;
                    //*vertices++ = outer2->a;
                    //*vertices++ = innerSec;

                }
                else if (jointStyle == JointStyle.ROUND)
                {
                    // draw a circle between the ends of the outer edges,
                    // centered at the actual point
                    // with half the line thickness as the radius
                    createTriangleFan(g, color, texSlot, innerSec, segment1.center.end, outer1.end, outer2.start, clockwise, false);
                }
                else
                {
                    //assert(false);
                }
            }

            //return vertices;
        }
    }
}
