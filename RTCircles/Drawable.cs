using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;

namespace RTCircles
{
    public abstract class Drawable : IComparable<Drawable>
    {
        public DrawableContainer Container { get; set; }
        public virtual Rectangle Bounds { get; set; }

        public long Layer = 0;

        public bool IsDead;

        public virtual void OnRemove() { }

        public virtual void OnAdd() { }

        public abstract void Update(float delta);
        public abstract void Render(Graphics g);

        public virtual void BeforeRender(Graphics g) { }
        public virtual void AfterRender(Graphics g) { }

        public virtual bool OnTextInput(char c) { return false; }

        public virtual bool OnKeyDown(Key key) { return false; }
        public virtual bool OnKeyUp(Key key) { return false; }

        public virtual bool OnMouseDown(MouseButton button) { return false; }
        public virtual bool OnMouseUp(MouseButton button) { return false; }

        public virtual bool OnMouseWheel(float delta) { return false; }

        public int CompareTo(Drawable other)
        {
            return Layer.CompareTo(other.Layer);
        }
    }
}
