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

        private uint sizeOfTypeInBytes;

        public GLBuffer(BufferTargetARB bufferType, BufferUsageARB bufferUsage, int capacity)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity can't be less than 0");

            this.bufferType = bufferType;
            this.bufferUsage = bufferUsage;

            sizeOfTypeInBytes = (uint)Marshal.SizeOf<T>();

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
            Capacity = capacity;

            if (IsInitialized == false)
                return;

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
            unsafe
            {
                fixed (T* data = objects)
                {
                    UploadData((uint)startIndex, (uint)objectCount, data);
                }
            }
        }

        /// <summary>
        /// Will bind and upload data to GPU
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="objectCount"></param>
        /// <param name="objects"></param>
        public unsafe void UploadData(uint startIndex, uint objectCount, T* data)
        {
            Bind();

            uint sizeInBytes = objectCount * sizeOfTypeInBytes;
            uint offsetInBytes = startIndex * sizeOfTypeInBytes;

            GL.Instance.BufferSubData(bufferType, (IntPtr)offsetInBytes, sizeInBytes, data);

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
