using Easy2D;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class DrawableContainer
    {
        protected List<Drawable> children = new List<Drawable>();
        private List<int> childrenToRemove = new List<int>();

        public IReadOnlyList<Drawable> Children => children.AsReadOnly();

        public int ChildrenCount => children.Count;

        private bool requireSorting = false;

        public void Add(params Drawable[] drawables)
        {
            for (int i = 0; i < drawables.Length; i++)
            {
                var currentDrawable = drawables[i];

                currentDrawable.IsDead = false;
                currentDrawable.Container = this;

                //These gets added to the top of the drawable list, so it wont interfere with the current update
                children.Add(currentDrawable);

                currentDrawable.OnAdd();
                requireSorting = true;
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
        public virtual void OnTextInput(char args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnTextInput(args))
                    break;
            }
        }

        public virtual void OnKeyDown(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnKeyDown(key))
                    break;
            }
        }

        public virtual void OnKeyUp(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnKeyUp(key))
                    break;
            }
        }

        public virtual void OnMouseDown(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseDown(args))
                    break;
            }
        }

        public virtual void OnMouseUp(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseUp(args))
                    break;
            }
        }

        public virtual void OnMouseWheel(float delta)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseWheel(delta))
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Render children
        /// </summary>
        /// <param name="g"></param>
        public virtual void Render(Graphics g)
        {
            int childCount = children.Count;

            for (int i = 0; i < childCount; i++)
            {
                if (children[i].IsDead)
                    continue;

                children[i].BeforeRender(g);
            }

            for (int i = 0; i < childCount; i++)
            {
                if (children[i].IsDead)
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

        public virtual void Update(float delta)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];

                if (child.IsDead)
                {
                    children.RemoveAt(i);
                    child.OnRemove();
                    continue;
                }

                child.Update(delta);

                if (i > 0)
                {
                    var nowDepth = child.Layer;
                    var prevDepth = children[i - 1].Layer;

                    if (prevDepth > nowDepth)
                        requireSorting = true;
                }
            }

            if (requireSorting)
            {
                children.Sort();
                requireSorting = false;
            }
        }
    }
}
