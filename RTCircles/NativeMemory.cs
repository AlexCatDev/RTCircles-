using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RTCircles
{
    public unsafe class NativeMemory<T> : IDisposable where T : unmanaged
    {
        public readonly IntPtr RawPointer;

        public readonly T* Pointer;

        public readonly int Length;

        public T this[int index]
        {
            get
            {
                if(index < 0)
                    throw new ArgumentOutOfRangeException("index");

                if(index >= Length)
                    throw new ArgumentOutOfRangeException("index");

                return *(Pointer + index);
            }
            set
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index");

                if (index >= Length)
                    throw new ArgumentOutOfRangeException("index");

                *(Pointer + index) = value;
            }
        }

        public ref T GetRef(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (index >= Length)
                throw new ArgumentOutOfRangeException("index");

            return ref *(Pointer + index);
        }

        public ref T GetRefUnchecked(int index) => ref *(Pointer + index);

        public NativeMemory(int length)
        {
            Length = length;
            RawPointer = Marshal.AllocHGlobal(length * sizeof(T));
            Pointer = (T*)RawPointer;
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }


                Marshal.FreeHGlobal(RawPointer);

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~NativeMemory()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
