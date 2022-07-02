using Easy2D;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class DrawableContainer : Drawable
    {
        protected List<Drawable> children = new List<Drawable>();

        public IReadOnlyList<Drawable> Children => children.AsReadOnly();

        public int ChildrenCount => children.Count;

        private List<Drawable> childrenPendingAdd = new List<Drawable>();

        private HashSet<Drawable> hashedChildren = new HashSet<Drawable>();

        public void Add(params Drawable[] drawables) => childrenPendingAdd.AddRange(drawables);

        public void Add(Drawable drawable) => childrenPendingAdd.Add(drawable);

        public void Clear()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                children[i].IsDead = true;
            }
        }

        public void Clear<T>() where T : Drawable
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is T)
                    children[i].IsDead = true;
            }
        }

        public void Get<T>(Action<T> onObjectGet) where T : Drawable
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is T t)
                    onObjectGet?.Invoke(t);
            }
        }

        public IEnumerable<T> Get<T>() where T : Drawable
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is T t)
                    yield return t;
            }
        }

        //Drawable input events is front to back, whilst rendering and updating is back to front
        #region INPUT
        public virtual new void OnTextInput(char args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnTextInput(args))
                    break;
            }
        }

        public virtual new void OnKeyDown(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnKeyDown(key))
                    break;
            }
        }

        public virtual new void OnKeyUp(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnKeyUp(key))
                    break;
            }
        }

        public virtual new void OnMouseDown(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnMouseDown(args))
                    break;
            }
        }

        public virtual new void OnMouseUp(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnMouseUp(args))
                    break;
            }
        }

        public virtual new void OnMouseWheel(float delta)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead || !children[i].IsAcceptingInput)
                    continue;

                if (children[i].OnMouseWheel(delta))
                    break;
            }
        }

        #endregion

        public override void Update(float delta)
        {
            bool requireSorting = false;
            bool requireRemoval = false;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];

                if (child.IsDead)
                {
                    requireRemoval = true;
                    continue;
                }

                child.Update(delta);

                if (i > 1)
                {
                    var prevDepth = children[i - 1].Layer;
                    var nowDepth = child.Layer;

                    if (prevDepth > nowDepth)
                        requireSorting = true;
                }
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
            int childCount = children.Count;

            for (int i = 0; i < childCount; i++)
            {
                if (children[i].IsDead || !children[i].IsVisible)
                    continue;

                children[i].Render(g);
            }

            for (int i = 0; i < childCount; i++)
            {
                if (children[i].IsDead)
                    continue;

                children[i].AfterRender(g);
            }
        }
    }
}
