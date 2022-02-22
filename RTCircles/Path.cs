using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCircles
{
    /// <summary>
    /// Path class with progressionAt, distance calculation, and bounding box
    /// </summary>
    public class Path
    {
        private List<Vector2> points = new List<Vector2>();

        public IReadOnlyList<Vector2> Points => points.AsReadOnly();

        public float Length { get; private set; }
        public Rectangle Bounds { get; private set; }

        public void SetPoints(List<Vector2> points)
        {
            this.points.Clear();
            this.points.AddRange(points);

            Length = calculateLength();
            Bounds = calculateBoundingBox();
        }

        public Vector2 CalculatePositionAtProgress(float progress) => CalculatePositionAtLength(Length * progress);

        //Thank you so much Raresica1234
        public Vector2 CalculatePositionAtLength(float length)
        {
            if (length <= 0)
                return points[0];

            if (length >= Length)
                return points[^1];

            for (int i = 0; i < points.Count - 1; i++)
            {
                float dist = Vector2.Distance(points[i], points[i + 1]);

                if (length - dist <= 0)
                {
                    float blend = length / dist;
                    return Vector2.Lerp(points[i], points[i + 1], blend);
                }
                length -= dist;
            }
            //just return the last point, if we're over the length
            return points[^1];
        }

        //Thanks rares
        private float calculateLength()
        {
            float length = 0f;
            for (int i = 0; i < points.Count - 1; i++)
                length += Vector2.Distance(points[i], points[i + 1]);

            return length;
        }

        private Rectangle calculateBoundingBox()
        {
            if (points.Count == 0)
                return new Rectangle(0, 0, 0, 0);

            float xmin = float.MaxValue;
            float xmax = 0;
            float ymin = float.MaxValue;
            float ymax = 0;

            for (int i = 0; i < points.Count; i++)
            {
                var current = points[i];

                if (xmin > current.X)
                    xmin = current.X;

                if (ymin > current.Y)
                    ymin = current.Y;

                if (xmax < current.X)
                    xmax = current.X;

                if (ymax < current.Y)
                    ymax = current.Y;
            }

            Rectangle rect = new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin);

            return rect;
        }
    }
}
