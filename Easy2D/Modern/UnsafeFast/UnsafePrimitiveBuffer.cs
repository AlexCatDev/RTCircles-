using Silk.NET.OpenGLES;
using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    /// <summary>
    /// Batch different primitive types together into one drawcall and alot of wasted memory.
    /// Get a raw pointer to memory, it can be written to further than agreed.
    /// Memory is not zeroed, so everything from the previous draw will still be there, remember to overwrite the vertices!
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    public unsafe class UnsafePrimitiveBuffer<T> where T : unmanaged
    {
        private GLBuffer<T> vertexBuffer;
        private GLBuffer<uint> indexBuffer;
        private VertexArray<T> vao;

        private uint* indexPool;
        private T* vertexPool;

        public uint IndexRenderCount { get; private set; }
        public uint VertexRenderCount { get; private set; }

        public uint TriangleRenderCount => IndexRenderCount / 3;

        public UnsafePrimitiveBuffer(int vertexCount, int indexCount)
        {
            vao = new VertexArray<T>();

            vertexPool = (T*)Marshal.AllocHGlobal(vertexCount * sizeof(T));
            vertexBuffer = new GLBuffer<T>(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw, vertexCount);

            indexPool = (uint*)Marshal.AllocHGlobal(indexCount * sizeof(uint));
            indexBuffer = new GLBuffer<uint>(BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw, indexCount);
        }

        /// <summary>
        /// Get a pointer to pre-allocated vertices
        /// </summary>
        /// <returns>A pointer pointing to the vertices, at the current writing location: WARNING: You could totally write more vertices than 3 if you wanted to pls dont</returns>
        public T* GetTriangle()
        {
            ensureCapacity(3, 3);

            *(indexPool + IndexRenderCount + 0) = VertexRenderCount + 0;
            *(indexPool + IndexRenderCount + 1) = VertexRenderCount + 1;
            *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 2;

            IndexRenderCount += 3;
            VertexRenderCount += 3;
            return vertexPool + VertexRenderCount - 3;
        }

        /// <summary>
        /// Get a pointer to pre-allocated vertices
        /// </summary>
        /// <returns>A pointer pointing to the vertices, at the current writing location: WARNING: You could totally write more vertices than 4 if you wanted to pls dont</returns>
        public T* GetQuad()
        {
            ensureCapacity(4, 6);

            *(indexPool + IndexRenderCount + 0) = VertexRenderCount + 0;
            *(indexPool + IndexRenderCount + 1) = VertexRenderCount + 1;
            *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 2;

            *(indexPool + IndexRenderCount + 3) = VertexRenderCount + 0;
            *(indexPool + IndexRenderCount + 4) = VertexRenderCount + 2;
            *(indexPool + IndexRenderCount + 5) = VertexRenderCount + 3;

            IndexRenderCount += 6;
            VertexRenderCount += 4;

            return vertexPool + VertexRenderCount - 4;
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
            *(indexPool + IndexRenderCount + 0) = VertexRenderCount + 0;
            *(indexPool + IndexRenderCount + 1) = VertexRenderCount + 1;
            *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 2;

            IndexRenderCount += 3;

            for (uint i = 3; i < pointCount; i++)
            {
                *(indexPool + IndexRenderCount + 0) = VertexRenderCount - 2 + i;
                *(indexPool + IndexRenderCount + 1) = VertexRenderCount - 1 + i;
                *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 0 + i;

                //offset by 3
                IndexRenderCount += 3;
            }

            VertexRenderCount += pointCount;

            return vertexPool + VertexRenderCount - pointCount;
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
            *(indexPool + IndexRenderCount + 0) = VertexRenderCount + 0;
            *(indexPool + IndexRenderCount + 1) = VertexRenderCount + 1;
            *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 2;

            IndexRenderCount += 3;

            for (uint i = 3; i < pointCount; i++)
            {
                *(indexPool + IndexRenderCount + 0) = VertexRenderCount + 0;
                *(indexPool + IndexRenderCount + 1) = VertexRenderCount - 1 + i;
                *(indexPool + IndexRenderCount + 2) = VertexRenderCount + 0 + i;

                //offset by 3
                IndexRenderCount += 3;
            }

            VertexRenderCount += pointCount;

            return vertexPool + VertexRenderCount - pointCount;
        }

        private void ensureCapacity(uint vertexCount, uint indexCount)
        {
            if (IndexRenderCount + indexCount > indexBuffer.Capacity)
            {
                Draw();
                Utils.Log($"IndexBuffer ran out of space, so the whole batch ha been FLUSHED", LogLevel.Warning);
            }else if (VertexRenderCount + vertexCount > vertexBuffer.Capacity)
            {
                Draw();
                Utils.Log($"VertexBuffer ran out of space, so the whole batch ha been FLUSHED", LogLevel.Warning);
            }
        }

        /// <summary>
        /// Bind shaders and textures before calling this
        /// </summary>
        public void Draw()
        {
            //Copy cpu vertices to gpu
            vertexBuffer.UploadData(0, VertexRenderCount, vertexPool);

            vao.Bind();

            //Copy cpu indices to gpu
            indexBuffer.UploadData(0, IndexRenderCount, indexPool);

            GL.Instance.DrawElements(PrimitiveType.Triangles, IndexRenderCount, DrawElementsType.UnsignedInt, null);

            VertexRenderCount = 0;
            IndexRenderCount = 0;
        }

        ~UnsafePrimitiveBuffer()
        {
            Marshal.FreeHGlobal(new IntPtr(vertexPool));
            Marshal.FreeHGlobal(new IntPtr(indexPool));
        }
    }
}
