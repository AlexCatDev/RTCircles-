using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Easy2D
{
    /// <summary>
    /// Abstract class for GL type objects, for handling: binding, initialization, resource deallocation etc.
    /// </summary>
    public abstract class GLObject
    {
        public const UInt32 UninitializedHandle = UInt32.MaxValue;

        public uint Handle { get; protected set; } = UninitializedHandle;

        public bool IsInitialized => Handle != UninitializedHandle;

        protected WeakReference<GLObject> weakReference;

        public static List<WeakReference<GLObject>> AllObjects { get; private set; } = new List<WeakReference<GLObject>>();

        public GLObject()
        {
            weakReference = new WeakReference<GLObject>(this);
        }

        /// <summary>
        /// Free the GPU memory used by this object
        /// </summary>
        public void Delete()
        {
            if (IsInitialized)
            {
                delete();
                Debug.Assert(IsInitialized == false, "You forgot to handle GLObject deletion, expected handle to be -1");
                AllObjects.Remove(weakReference);

                Utils.Log($"Deleted GLObject<{this.ToString()}> Handle -> [{Handle}]", LogLevel.Debug);
            };
        }

        /// <summary>
        /// Bind the object.
        /// If the object has been deleted or has not been initialized yet,
        /// it gets initialized/reinitialized when this is called.
        /// </summary>
        /// <param name="slot">A slot to bind the object to if applicable.</param>
        public void Bind(int? slot = null)
        {
            if (IsInitialized)
                bind(slot);
            else
            {
                initialize(slot);

                Debug.Assert(IsInitialized, "You forgot to handle GLObject creation, expected to have a valid handle");

                AllObjects.Add(weakReference);
            }
        }

        ~GLObject()
        {
            //If the object is initialized
            if (IsInitialized)
            {
                //Schedule the delete call to the gpu thread scheduler, since the deconstructor is run on another thread in 60% of cases
                GPUSched.Instance.Enqueue(() =>
                {
                    Delete();
                });
            }
        }

        protected abstract void initialize(int? slot);

        protected abstract void bind(int? slot);

        protected abstract void delete();
    }
}
