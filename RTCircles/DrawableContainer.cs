using Easy2D;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RTCircles
{
    public class DrawableContainer : Drawable
    {
        protected List<Drawable> children = new List<Drawable>();

        public int ChildrenCount => children.Count;

        private List<Drawable> childrenPendingAdd = new List<Drawable>();

        private HashSet<Drawable> hashedChildren = new HashSet<Drawable>();

        public void Add(params Drawable[] drawables) => childrenPendingAdd.AddRange(drawables);

        public void Add(Drawable drawable) => childrenPendingAdd.Add(drawable);

        public void Clear()
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                span[i].IsDead = true;
            }
        }

        public void Clear<T>() where T : Drawable
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i] is T)
                    span[i].IsDead = true;
            }
        }

        public void Get<T>(Action<T> onObjectGet) where T : Drawable
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] is T t)
                    onObjectGet?.Invoke(t);
            }
        }

        //Drawable input events is front to back, whilst rendering and updating is back to front
        #region INPUT
        public virtual new void OnTextInput(char args)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnTextInput(args))
                    break;
            }
        }

        public virtual new void OnKeyDown(Key key)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnKeyDown(key))
                    break;
            }
        }

        public virtual new void OnKeyUp(Key key)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnKeyUp(key))
                    break;
            }
        }

        public virtual new void OnMouseDown(MouseButton args)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnMouseDown(args))
                    break;
            }
        }

        public virtual new void OnMouseUp(MouseButton args)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnMouseUp(args))
                    break;
            }
        }

        public virtual new void OnMouseWheel(float delta)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (span[i].IsDead || !span[i].IsAcceptingInput)
                    continue;

                if (span[i].OnMouseWheel(delta))
                    break;
            }
        }

        #endregion

        public override void Update(float delta)
        {
            bool requireSorting = false;
            bool requireRemoval = false;

            long previousDepth = long.MinValue;

            var span = CollectionsMarshal.AsSpan(children);

            for (int i = 0; i < span.Length; i++)
            {
                var child = span[i];

                if (child.IsDead)
                {
                    requireRemoval = true;
                    continue;
                }

                child.Update(delta);

                if (child.Layer < previousDepth)
                    requireSorting = true;

                previousDepth = child.Layer;
            }

            if (requireRemoval)
                children.RemoveAll(x => {
                    if (x.IsDead)
                    {
                        hashedChildren.Remove(x);
                        x.OnRemove();
                        return true;
                    }

                    return false;
                });

            if (childrenPendingAdd.Count > 0)
            {
                for (int i = 0; i < childrenPendingAdd.Count; i++)
                {
                    var newChild = childrenPendingAdd[i];

                    newChild.IsDead = false;
                    if (hashedChildren.Add(newChild))
                        children.Add(newChild);

                    newChild.Container = this;
                    newChild.OnAdd();

                    newChild.Update(delta);
                }

                childrenPendingAdd.Clear();

                requireSorting = true;
            }

            if (requireSorting)
                children.Sort();
        }

        public override void Render(Graphics g)
        {
            var span = CollectionsMarshal.AsSpan(children);

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].IsDead || !span[i].IsVisible)
                    continue;

                span[i].Render(g);
            }

            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].IsDead || !span[i].IsVisible)
                    continue;

                span[i].AfterRender(g);
            }
        }
    }
}
