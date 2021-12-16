using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Easy2D
{
    /// <summary>
    /// Abstract class for GL type objects, for handling: binding, initialization, resource deallocation etc.
    /// </summary>
    public abstract class GLObject
    {
        public uint Handle { get; protected set; } = UInt32.MaxValue;

        public bool IsInitialized => Handle != UInt32.MaxValue;

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
                Utils.Log($"Deleted GLObject<{this.ToString()}> Handle -> [{Handle}]", LogLevel.Debug);
                delete();

                Debug.Assert(IsInitialized == false, "You forgot to handle GLObject deletion, expected handle to be -1");

                AllObjects.Remove(weakReference);
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
            //Just schedule a delete call lololololol
            GPUScheduler.Run(new (() =>
            {
                Delete();
            }), allowDuplicates: false);
        }

        protected abstract void initialize(int? slot);

        protected abstract void bind(int? slot);

        protected abstract void delete();
    }
}
