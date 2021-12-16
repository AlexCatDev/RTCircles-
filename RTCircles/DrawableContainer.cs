using Easy2D;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    /*
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

                //Dont add dead entities, but mark them as undead, since they might be in the removal list
                //But what if the drawable is not in the removal list?
                //It would have to be added twice.
                if (currentDrawable.IsDead)
                {
                    currentDrawable.IsDead = false;
                    continue;
                }

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

        //Drawable input events is front to back, whilst rendering and updating is back to front
        #region INPUT
        private object inputLock = new object();
        public virtual void OnTextInput(char c)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnTextInput(c))
                    return;
            }
        }

        public virtual void OnKeyDown(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnKeyDown(key))
                    return;
            }
        }

        public virtual void OnKeyUp(Key key)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnKeyUp(key))
                    return;
            }
        }

        public virtual void OnMouseDown(MouseButton button)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnMouseDown(button))
                    return;
            }

        }

        public virtual void OnMouseUp(MouseButton button)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnMouseUp(button))
                    return;
            }
        }

        public virtual void OnMouseWheel(float position)
        {
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i].OnMouseWheel(position))
                    return;
            }
        }
        #endregion

        private object renderLock = new object();
        public virtual void Render(Graphics g)
        {
            for (int i = 0; i < children.Count; i++)
            {
                children[i].Render(g);
            }
        }

        public virtual void Update(float delta)
        {
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

                children[i].Update(delta);

                if (i > 0)
                {
                    var nowDepth = children[i].Layer;
                    var prevDepth = children[i - 1].Layer;

                    if (prevDepth > nowDepth)
                        requireSorting = true;
                }
            }

            for (int i = childrenToRemove.Count - 1; i >= 0; i--)
            {
                int indexRemove = childrenToRemove[i];

                children[indexRemove].OnRemove();
                //Mark as not dead anymore so it can be re added.
                children[indexRemove].IsDead = false;
                children.RemoveAt(indexRemove);

                childrenToRemove.RemoveAt(i);
            }

            if (requireSorting)
            {
                children.Sort();
                requireSorting = false;
            }
        }
    }
    */

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

                children[i].Render(g);
            }
        }

        public virtual void Update(float delta)
        {
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

                children[i].Update(delta);

                if (i > 0)
                {
                    var nowDepth = children[i].Layer;
                    var prevDepth = children[i - 1].Layer;

                    if (prevDepth > nowDepth)
                        requireSorting = true;
                }
            }

            //Wait for sorting of children and removal of them till both render and input is idle?
            //If both of them are idle
            //Lock them down from the update thread so they cant execute temporarily?
            //Then do the business
            //unlock the methods, profit?

            //But that would also delay newly added children from being sorted, so either being shown out of order, or hiden till its been sorted
            //Creating a spawn delay which is yikes

            //Then lock them just in case

            //Instead of pepega locking
            //How could i make this as smooth as possible?
            //Check if a render is in progress?
            //If it is, then propagate the sorting and removal of children to there?
            if (childrenToRemove.Count > 0)
            {

                for (int i = childrenToRemove.Count - 1; i >= 0; i--)
                {
                    int indexRemove = childrenToRemove[i];

                    if (children[indexRemove].IsDead)
                    {
                        children[indexRemove].OnRemove();
                        //Set the parent to null, now this brings upon an issue where it's possible for two drawables, to own the same object?

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
    }
}
