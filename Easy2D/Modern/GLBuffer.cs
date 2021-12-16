using Silk.NET.OpenGLES;
using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public class GLBuffer<T> : GLObject where T : unmanaged
    {
        public int Capacity { get; private set; }

        private BufferTargetARB bufferType;
        private BufferUsageARB bufferUsage;

        private int sizeOfTypeInBytes;

        public GLBuffer(BufferTargetARB bufferType, BufferUsageARB bufferUsage, int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity can't be less than 0");

            this.bufferType = bufferType;
            this.bufferUsage = bufferUsage;

            sizeOfTypeInBytes = Marshal.SizeOf<T>();

            Capacity = capacity;
        }

        public double GetMemoryUsage()
        {
            if (IsInitialized)
                return Math.Round((sizeOfTypeInBytes * Capacity) / 1048576d, 2);
            else
                return 0;
        }

        /// <summary>
        /// Throws away all data in the buffer and resizes it
        /// </summary>
        /// <param name="objectCount"></param>
        public void Resize(int capacity)
        {
            if (IsInitialized == false)
            {
                Capacity = capacity;
                return;
            }

            bind(null);

            nuint sizeInBytes = (nuint)(capacity * sizeOfTypeInBytes);

            unsafe
            {
                GL.Instance.BufferData(bufferType, sizeInBytes, null, bufferUsage);
            }

            Utils.Log($"Initialised {bufferType}<{typeof(T)}> with a capacity of {capacity} elements which is {GetMemoryUsage():F2} MB / {sizeOfTypeInBytes} bytes per element", LogLevel.Info);
        }

        /// <summary>
        /// Will bind and upload data to GPU
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="objectCount"></param>
        /// <param name="objects"></param>
        public void UploadData(int startIndex, int objectCount, T[] objects)
        {
            Bind();

            int sizeInBytes = objectCount * sizeOfTypeInBytes;
            int offsetInBytes = startIndex * sizeOfTypeInBytes;
            unsafe
            {
                fixed (T* data = objects)
                {
                    GL.Instance.BufferSubData(bufferType, (IntPtr)offsetInBytes, (nuint)sizeInBytes, data);
                }
            }
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenBuffer();

            Resize(Capacity);
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindBuffer(bufferType, Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteBuffer(Handle);
            Handle = uint.MaxValue;
        }
    }
}
