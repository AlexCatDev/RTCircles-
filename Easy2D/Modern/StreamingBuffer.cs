using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy2D
{
    /// <summary>
    /// A faster glbuffer which is unsynchronized with the gpu, requires EXT.BufferStorage GLES 3.1+
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe class StreamingBuffer<T> : GLObject where T : unmanaged
    {
        private static Silk.NET.OpenGLES.Extensions.EXT.ExtBufferStorage extBufferStorage;

        public static bool IsSupported => extBufferStorage is not null;

        static StreamingBuffer()
        {
            if (extBufferStorage is null)
                GL.Instance.TryGetExtension<Silk.NET.OpenGLES.Extensions.EXT.ExtBufferStorage>(out extBufferStorage);
        }

        private static readonly MapBufferAccessMask mapBufferAccessMask = MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit;
        private static readonly BufferStorageMask bufferStorageMask = BufferStorageMask.DynamicStorageBit | (BufferStorageMask)mapBufferAccessMask;

        private BufferTargetARB bufferTarget;
        public uint Capacity { get; private set; }
        public uint SizeInBytes => Capacity * (uint)sizeof(T);

        public T* Pointer { get; private set; }

        public StreamingBuffer(BufferTargetARB bufferTarget, uint itemCapacity)
        {
            this.bufferTarget = bufferTarget;
            Capacity = itemCapacity;
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindBuffer(bufferTarget, Handle);
        }

        protected override void delete()
        {
            Pointer = null;

            GL.Instance.DeleteBuffer(Handle);
            Handle = uint.MaxValue;
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenBuffer();

            bind(null);

            extBufferStorage.BufferStorage((BufferStorageTarget)bufferTarget, SizeInBytes, null, bufferStorageMask);

            Pointer = (T*)GL.Instance.MapBufferRange(bufferTarget, 0, SizeInBytes, mapBufferAccessMask);
        }
    }
}
