using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.Input;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace PogGame
{
    //This is pretty good an threadsafe.
    public class Drawable : IComparable<Drawable>
    {
        public virtual Rectangle Bounds => new Rectangle(0, 0, 0, 0);

        private List<Drawable> children = new List<Drawable>();
        private HashSet<Drawable> hashedChildren = new HashSet<Drawable>();
        private List<int> childrenToRemove = new List<int>();

        public IReadOnlyList<Drawable> Children => children.AsReadOnly();

        public int ChildrenCount => children.Count;

        public int Layer;

        /// <summary>
        /// This is initially null, will be set when it's added
        /// </summary>
        public Drawable Parent { get; private set; }

        public volatile bool IsDead;

        public double Time { get; private set; }
        public double Delta { get; private set; }

        public float fDelta => (float)Delta;

        protected virtual void OnRemove() { }
        protected virtual void OnAdd() { }

        private bool requireSorting;

        public void Add(params Drawable[] drawables)
        {
            //This doesnt really need to be locked
            //Reason is, they get added to the end of the list, and even if it gets added, and then rendered right after
            //It's only a single frame, where it gets rendered ontop of everything
            //But the perf boost is fucking huge
            //Also if it was locked and called from the render thread, there would be a deadlock L + ratio
            for (int i = 0; i < drawables.Length; i++)
            {
                var currentDrawable = drawables[i];

                currentDrawable.IsDead = false;
                if (hashedChildren.Contains(currentDrawable) == false)
                {
                    children.Add(currentDrawable);
                    hashedChildren.Add(currentDrawable);
                    requireSorting = true;
                }

                currentDrawable.Parent = this;

                currentDrawable.OnAdd();
            }
        }

        /// <summary>
        /// Clear the children of <typeparamref name="T"/> in this container, use this.Parent to access neighbouring drawables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Clear<T>() where T : Drawable
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is T)
                    children[i].IsDead = true;
            }
        }

        /// <summary>
        /// Get an enumerator of drawables with type <typeparamref name="T"/> in this container, use this.Parent to access neighbouring drawables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerator<T> Get<T>() where T : Drawable
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is T t)
                    yield return t;
            }
        }

        //Drawable input events is front to back, whilst rendering and updating is back to front
        #region INPUT
        public virtual bool OnTextInput(char args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnTextInput(args))
                    return true;
            }

            return false;
        }

        public virtual bool OnKeyDown(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnKeyDown(key))
                    return true;
            }
            return false;
        }

        public virtual bool OnKeyUp(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnKeyUp(key))
                    return true;
            }

            return false;
        }

        public virtual bool OnMouseDown(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseDown(args))
                    return true;
            }

            return false;
        }

        public virtual bool OnMouseUp(MouseButton args)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseUp(args))
                    return true;
            }

            return false;
        }

        public virtual bool OnMouseMove()
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseMove())
                    return true;
            }

            return false;
        }

        public virtual bool OnMouseWheel(float delta)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].OnMouseWheel(delta))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// TODO: Make each drawable have a mouse state, and keyboard state, and children should inherit those when added
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual bool Input(object input)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].IsDead)
                    continue;

                if (children[i].Input(input))
                    return true;
            }

            return false;
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

                children[i].Render(g);
            }
        }

        private double previousTime = double.NaN;

        public virtual void OnUpdate() { }

        /// <summary>
        /// Update children
        /// </summary>
        /// <param name="time">The absolute time, not delta-time</param>
        public void Update(double time)
        {
            Time = time;

            //First update will always have a delta time of 0
            if (double.IsNaN(previousTime))
                previousTime = Time;

            Delta = Time - previousTime;

            previousTime = Time;

            OnUpdate();

            int childCount = children.Count;
            for (int i = 0; i < childCount; i++)
            {
                //if children[i].isdead
                //add the current index to the childrenToRemoveList
                if (children[i].IsDead)
                {
                    childrenToRemove.Add(i);
                    continue;
                }

                children[i].Update(Time);

                if (i > 0)
                {
                    var nowDepth = children[i].Layer;
                    var prevDepth = children[i - 1].Layer;

                    if (prevDepth > nowDepth)
                        requireSorting = true;
                }
            }

            if (childrenToRemove.Count > 0)
            {
                for (int i = childrenToRemove.Count - 1; i >= 0; i--)
                {
                    int indexRemove = childrenToRemove[i];

                    if (children[indexRemove].IsDead)
                    {
                        children[indexRemove].OnRemove();
                        //Set the parent to null, now this brings upon an issue where it's possible for two drawables, to own the same object?

                        children[indexRemove].Parent = null;
                        hashedChildren.Remove(children[indexRemove]);
                        children.RemoveAt(indexRemove);
                    }
                }

                childrenToRemove.Clear();
            }

            if (requireSorting)
            {
                children.Sort();
                requireSorting = false;
            }
        }

        /// <summary>
        /// Used for sorting
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Drawable other)
        {
            return Layer.CompareTo(other.Layer);
        }
    }
}
