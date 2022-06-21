using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public struct Rectangle
    {
        public static readonly Rectangle Empty = new Rectangle(Vector2.Zero, Vector2.Zero);

        public Vector2 Position;
        public Vector2 Size;

        public float X {
            get {
                return Position.X;
            }
            set {
                Position.X = value;
            }
        }

        public float Y {
            get {
                return Position.Y;
            }
            set {
                Position.Y = value;
            }
        }

        public float Width {
            get {
                return Size.X;
            }
            set {
                Size.X = value;
            }
        }

        public float Height {
            get {
                return Size.Y;
            }
            set {
                Size.Y = value;
            }
        }

        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        public Vector2 BottomRight => new Vector2(Right, Bottom);

        public Vector2 Center => new Vector2(X + Width / 2f, Y + Height / 2f);

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Rectangle(Vector2 position, Vector2 size) {
            Position = position;
            Size = size;
        }

        public static Rectangle FromTBLR(float top, float bottom, float left, float right)
        {
            return new Rectangle(left, top, right - left, bottom - top);
        }

        public Rectangle(float x, float y, float width, float height) : this(new Vector2(x, y), new Vector2(width, height)) { }

        public static Rectangle operator *(Rectangle a, Rectangle b) {
            Rectangle value = new Rectangle(a.Position*b.Position,a.Size*b.Size);
            return value;
        }

        public static Rectangle operator *(Rectangle a, float b) {
            Rectangle value = new Rectangle(a.Position, a.Size*b);
            return value;
        }

        public static Rectangle operator +(Rectangle a, Rectangle b) {
            Rectangle value = new Rectangle(a.Position + b.Position, a.Size + b.Size);
            return value;
        }

        public static Rectangle operator -(Rectangle a, Rectangle b) {
            Rectangle value = new Rectangle(a.Position - b.Position, a.Size - b.Size);
            return value;
        }

        public bool IntersectsWith(Vector2 point) => point.X > X && point.X < X + Width && point.Y > Y && point.Y < Y + Height;

        public bool IntersectsWith(Rectangle rect) {
            return (rect.X < this.X + this.Width) &&
            (this.X < (rect.X + rect.Width)) &&
            (rect.Y < this.Y + this.Height) &&
            (this.Y < rect.Y + rect.Height);
        }

        public override string ToString()
        {
            return $"Position: {Position} Size: {Size}";
        }
    }
}
