using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    /// <summary>
    /// Build geometry out of primitives
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    public unsafe class PrimitiveBuilder<T> where T : unmanaged
    {
        private uint* indexBuffer;
        private T* vertexBuffer;

        public bool Resizeable { get; set; } = true;

        public uint IndicesPending { get; private set; }
        public uint VerticesPending { get; private set; }

        public uint VertexCapacity { get; private set; }
        public uint IndexCapacity { get; private set; }

        public event Action RanOutOfMemory;

        public PrimitiveBuilder(uint vertexCapacity, uint indexCapacity)
        {
            VertexCapacity = vertexCapacity;
            IndexCapacity = indexCapacity;

            vertexBuffer = (T*)Marshal.AllocHGlobal((int)VertexCapacity * sizeof(T));
            indexBuffer = (uint*)Marshal.AllocHGlobal((int)IndexCapacity * sizeof(uint));
        }

        /// <summary>
        /// Get a pointer to pre-allocated vertices
        /// </summary>
        /// <returns>A pointer pointing to the vertices, at the current writing location: WARNING: You could totally write more vertices than 3 if you wanted to pls dont</returns>
        public T* GetTriangle()
        {
            ensureCapacity(3, 3);

            *(indexBuffer + IndicesPending + 0) = VerticesPending + 0;
            *(indexBuffer + IndicesPending + 1) = VerticesPending + 1;
            *(indexBuffer + IndicesPending + 2) = VerticesPending + 2;

            IndicesPending += 3;
            VerticesPending += 3;
            return vertexBuffer + VerticesPending - 3;
        }

        /// <summary>
        /// Get a pointer to pre-allocated vertices
        /// </summary>
        /// <returns>A pointer pointing to the vertices, at the current writing location: WARNING: You could totally write more vertices than 4 if you wanted to pls dont</returns>
        public T* GetQuad()
        {
            ensureCapacity(4, 6);

            *(indexBuffer + IndicesPending + 0) = VerticesPending + 0;
            *(indexBuffer + IndicesPending + 1) = VerticesPending + 1;
            *(indexBuffer + IndicesPending + 2) = VerticesPending + 2;

            *(indexBuffer + IndicesPending + 3) = VerticesPending + 0;
            *(indexBuffer + IndicesPending + 4) = VerticesPending + 2;
            *(indexBuffer + IndicesPending + 5) = VerticesPending + 3;

            IndicesPending += 6;
            VerticesPending += 4;

            return vertexBuffer + VerticesPending - 4;
        }

        /// <summary>
        /// Get a span of vertices connected to each other which count is equal to the inputed point count.
        /// </summary>
        /// <param name="pointCount"></param>
        /// <returns>A null pointer if point count is less than 3</returns>
        public T* GetTriangleStrip(uint pointCount)
        {
            if (pointCount < 3)
                return null;

            ensureCapacity(pointCount, (pointCount - 2) * 3);

            //Generate the first triangle
            *(indexBuffer + IndicesPending + 0) = VerticesPending + 0;
            *(indexBuffer + IndicesPending + 1) = VerticesPending + 1;
            *(indexBuffer + IndicesPending + 2) = VerticesPending + 2;

            IndicesPending += 3;

            for (uint i = 3; i < pointCount; i++)
            {
                *(indexBuffer + IndicesPending + 0) = VerticesPending - 2 + i;
                *(indexBuffer + IndicesPending + 1) = VerticesPending - 1 + i;
                *(indexBuffer + IndicesPending + 2) = VerticesPending + 0 + i;

                //offset by 3
                IndicesPending += 3;
            }

            VerticesPending += pointCount;

            return vertexBuffer + VerticesPending - pointCount;
        }

        /// <summary>
        /// Get a span of vertices connected to each other which count is equal to the inputed point count.
        /// </summary>
        /// <param name="pointCount"></param>
        /// <returns>A null pointer if point count is less than 3</returns>
        public T* GetTriangleFan(uint pointCount)
        {
            if (pointCount < 3)
                return null;

            ensureCapacity(pointCount, (pointCount - 2) * 3);

            //Generate the first triangle
            *(indexBuffer + IndicesPending + 0) = VerticesPending + 0;
            *(indexBuffer + IndicesPending + 1) = VerticesPending + 1;
            *(indexBuffer + IndicesPending + 2) = VerticesPending + 2;

            IndicesPending += 3;

            for (uint i = 3; i < pointCount; i++)
            {
                *(indexBuffer + IndicesPending + 0) = VerticesPending + 0;
                *(indexBuffer + IndicesPending + 1) = VerticesPending - 1 + i;
                *(indexBuffer + IndicesPending + 2) = VerticesPending + 0 + i;

                //offset by 3
                IndicesPending += 3;
            }

            VerticesPending += pointCount;

            return vertexBuffer + VerticesPending - pointCount;
        }

        public void GetRaw(uint vertexCount, uint indexCount, out T* vertexPtr, out uint* indexPtr)
        {
            ensureCapacity(vertexCount, indexCount);

            vertexPtr = vertexBuffer + VerticesPending;
            indexPtr = indexBuffer + IndicesPending;

            VerticesPending += vertexCount;
            IndicesPending += indexCount;
        }

        public Span<T> GetQuadSpan() => new Span<T>(GetQuad(), 4);
        public Span<T> GetTriangleSpan() => new Span<T>(GetTriangle(), 3);
        public Span<T> GetTriangleStripSpan(uint pointCount) => new Span<T>(GetTriangleStrip(pointCount), (int)pointCount);
        public Span<T> GetTriangleFanSpan(uint pointCount) => new Span<T>(GetTriangleFan(pointCount), (int)pointCount);
        public void GetRawSpan(uint vertexCount, uint indexCount, out Span<T> vertexSpan, out Span<uint> indexspan)
        {
            GetRaw(vertexCount, indexCount, out T* vertexPtr, out uint* indexPtr);

            vertexSpan = new Span<T>(vertexPtr, (int)vertexCount);
            indexspan = new Span<uint>(indexPtr, (int)indexCount);
        }

        private void ensureCapacity(uint vertexCount, uint indexCount)
        {
            if (IndicesPending + indexCount > IndexCapacity)
            {
                RanOutOfMemory?.Invoke();

                if (IndicesPending + indexCount > IndexCapacity)
                    throw new OutOfMemoryException($"Not enought index space. Set {nameof(Resizeable)} to true if you want dynamic space");
            }

            if (VerticesPending + vertexCount > VertexCapacity)
            {
                RanOutOfMemory?.Invoke();

                if (VerticesPending + vertexCount > VertexCapacity)
                    throw new OutOfMemoryException($"Not enough vertex space. Set {nameof(Resizeable)} to true if you want dynamic space");
            }

            /*
            if (IndicesPending + indexCount > IndexCapacity)
            {
                if (!Resizeable)
                {
                    RanOutOfMemory?.Invoke();

                    if(IndicesPending + indexCount > IndexCapacity)
                        throw new OutOfMemoryException($"Not enought index space. Set {nameof(Resizeable)} to true if you want dynamic space");
                }

                //Resize internal buffer, and ibo
                uint newSize = indexCount > IndexCapacity * 2 ? indexCount : IndexCapacity * 2;

                uint* newIndexPool = (uint*)Marshal.AllocHGlobal((int)newSize * sizeof(uint));

                new Span<uint>(indexBuffer, (int)IndexCapacity).CopyTo(new Span<uint>(newIndexPool, (int)IndexCapacity));

                Marshal.FreeHGlobal(new IntPtr(indexBuffer));

                indexBuffer = newIndexPool;

                Utils.Log($"Index Buffer ran out of space and has been resized ({IndexCapacity}) => ({newSize})", LogLevel.Performance);

                IndexCapacity = newSize;
            }

            if (VerticesPending + vertexCount > VertexCapacity)
            {
                if (!Resizeable)
                    throw new OutOfMemoryException($"Not enough vertex space. Set {nameof(Resizeable)} to true if you want dynamic space");

                //Resize internal buffer, and ibo
                uint newSize = vertexCount > VertexCapacity * 2 ? vertexCount : VertexCapacity * 2;

                T* newVertexPool = (T*)Marshal.AllocHGlobal((int)newSize * sizeof(T));

                new Span<T>(vertexBuffer, (int)VertexCapacity).CopyTo(new Span<T>(newVertexPool, (int)VertexCapacity));

                Marshal.FreeHGlobal(new IntPtr(vertexBuffer));

                vertexBuffer = newVertexPool;

                Utils.Log($"Vertex Buffer ran out of space and has been resized ({VertexCapacity}) => ({newSize})", LogLevel.Performance);

                VertexCapacity = newSize;
            }
            */
        }

        public ReadOnlySpan<T> GetWrittenVerticesSpan() => new ReadOnlySpan<T>(vertexBuffer, (int)VerticesPending);
        public ReadOnlySpan<uint> GetWrittenIndicesSpan() => new ReadOnlySpan<uint>(indexBuffer, (int)IndicesPending);

        public void Reset()
        {
            VerticesPending = 0;
            IndicesPending = 0;
        }

        ~PrimitiveBuilder()
        {
            Marshal.FreeHGlobal(new IntPtr(vertexBuffer));
            Marshal.FreeHGlobal(new IntPtr(indexBuffer));
        }
    }
}
