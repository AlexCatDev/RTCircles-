using Silk.NET.OpenGLES;
using System;

namespace Easy2D
{
    /// <summary>
    /// Batch different primitive types together into one drawcall and alot of wasted memory
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    public class PrimitiveBatch<T> where T : unmanaged
    {
        private GLBuffer<T> vertexBuffer;
        private GLBuffer<int> indexBuffer;
        private VertexArray<T> vao;

        private int[] indexPool;
        private T[] vertexPool;

        public int IndexRenderCount { get; private set; }
        public int VertexRenderCount { get; private set; } 

        public int TriangleRenderCount => IndexRenderCount / 3;

        public bool AutoClearOnRender { get; set; } = true;

        /// <summary>
        /// If true, this batch will use automatic resizing, otherwise, if data can't fit, it's flushed(rendered) to screen immediately.
        /// (Default: True)
        /// </summary>
        public bool Resizable { get; set; } = true;

        public PrimitiveBatch(int vertexCount, int indexCount)
        {
            vao = new VertexArray<T>();

            vertexPool = new T[vertexCount];
            vertexBuffer = new GLBuffer<T>(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw, vertexPool.Length);

            indexPool = new int[indexCount];
            indexBuffer = new GLBuffer<int>(BufferTargetARB.ElementArrayBuffer, BufferUsageARB.DynamicDraw, indexPool.Length);
        }

        /// <summary>
        /// Get 3 vertices connected to each other
        /// </summary>
        /// <returns></returns>
        public Span<T> GetTriangle()
        {
            ensureCapacity(3, 3);

            indexPool[IndexRenderCount++] = VertexRenderCount + 0;
            indexPool[IndexRenderCount++] = VertexRenderCount + 1;
            indexPool[IndexRenderCount++] = VertexRenderCount + 2;

            var data = vertexPool.AsSpan(VertexRenderCount, 3);
            VertexRenderCount += 3;

            return data;
        }

        /// <summary>
        /// Get 4 vertices connected to each other
        /// </summary>
        /// <returns></returns>
        public Span<T> GetQuad()
        {
            ensureCapacity(4, 6);
            indexPool[IndexRenderCount++] = VertexRenderCount + 0;
            indexPool[IndexRenderCount++] = VertexRenderCount + 1;
            indexPool[IndexRenderCount++] = VertexRenderCount + 2;

            indexPool[IndexRenderCount++] = VertexRenderCount + 0;
            indexPool[IndexRenderCount++] = VertexRenderCount + 2;
            indexPool[IndexRenderCount++] = VertexRenderCount + 3;

            var data = vertexPool.AsSpan(VertexRenderCount, 4);
            VertexRenderCount += 4;

            return data;
        }

        /// <summary>
        /// Get a span of vertices connected to each other which count is equal to the inputed point count.
        /// </summary>
        /// <param name="pointCount"></param>
        /// <returns>A empty span if point count is less than 3</returns>
        public Span<T> GetTriangleStrip(int pointCount)
        {
            if (pointCount < 3)
                return Span<T>.Empty;

            ensureCapacity(pointCount, (pointCount - 2) * 3);

            //Generate the first triangle
            indexPool[IndexRenderCount++] = VertexRenderCount + 0;
            indexPool[IndexRenderCount++] = VertexRenderCount + 1;
            indexPool[IndexRenderCount++] = VertexRenderCount + 2;

            for (int i = 3; i < pointCount; i++)
            {
                indexPool[IndexRenderCount++] = VertexRenderCount - 2 + i;
                indexPool[IndexRenderCount++] = VertexRenderCount - 1 + i;
                indexPool[IndexRenderCount++] = VertexRenderCount + 0 + i;
            }

            var data = vertexPool.AsSpan(VertexRenderCount, pointCount);
            VertexRenderCount += pointCount;

            return data;
        }

        /// <summary>
        /// Get a span of vertices connected to each other which count is equal to the inputed point count.
        /// </summary>
        /// <param name="pointCount"></param>
        /// <returns>A empty span if point count is less than 3</returns>
        public Span<T> GetTriangleFan(int pointCount)
        {
            if (pointCount < 3)
                return Span<T>.Empty;

            ensureCapacity(pointCount, (pointCount - 2) * 3);

            //Generate the first triangle
            indexPool[IndexRenderCount++] = VertexRenderCount + 0;
            indexPool[IndexRenderCount++] = VertexRenderCount + 1;
            indexPool[IndexRenderCount++] = VertexRenderCount + 2;

            for (int i = 3; i < pointCount; i++)
            {
                indexPool[IndexRenderCount++] = VertexRenderCount + 0;
                indexPool[IndexRenderCount++] = VertexRenderCount - 1 + i;
                indexPool[IndexRenderCount++] = VertexRenderCount + 0 + i;
            }

            var data = vertexPool.AsSpan(VertexRenderCount, pointCount);
            VertexRenderCount += pointCount;

            return data;
        }

        private void ensureCapacity(int vertexCount, int indexCount)
        {
            if (IndexRenderCount + indexCount > indexPool.Length)
            {
                if (!Resizable)
                {
                    Draw();
                    //Utils.Log($"IndexBuffer ran out of space, so the whole batch ha been FLUSHED", LogLevel.Warning);
                    return;
                }

                //Resize internal buffer, and ibo
                int newSize = indexCount > indexPool.Length * 2 ? indexCount : indexPool.Length * 2;

                int[] newIndexPool = new int[newSize];
                Array.Copy(indexPool, newIndexPool, indexPool.Length);
                indexPool = newIndexPool;

                indexBuffer.Resize(newSize);

                Utils.Log($"IndexBuffer ran out of space and has been RESIZED", LogLevel.Warning);
            }

            if (VertexRenderCount + vertexCount > vertexPool.Length)
            {
                if (!Resizable)
                {
                    Draw();
                    //Utils.Log($"VertexBuffer ran out of space, so the whole batch ha been FLUSHED", LogLevel.Warning);
                    return;
                }

                //Resize internal buffer, and vbo
                int newSize = vertexCount > vertexPool.Length * 2 ? vertexCount : vertexPool.Length * 2;

                T[] newVertexPool = new T[newSize];
                Array.Copy(vertexPool, newVertexPool, vertexPool.Length);
                vertexPool = newVertexPool;

                vertexBuffer.Resize(newSize);

                Utils.Log($"VertexBuffer ran out of space and has been RESIZED", LogLevel.Warning);
            }
        }

        public Span<T> GetRaw(int vertexCount, int[] indices)
        {
            ensureCapacity(vertexCount, indices.Length);

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0)
                    throw new ArgumentOutOfRangeException("Can't have negative indices");

                if (indices[i] > vertexCount - 1)
                    throw new ArgumentOutOfRangeException("Indices may not overflow input vertices");
                indexPool[IndexRenderCount++] = indices[i] + VertexRenderCount;
            }

            var vertices = vertexPool.AsSpan(VertexRenderCount, vertexCount);

            VertexRenderCount += vertexCount;

            return vertices;
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

            unsafe
            {
                GL.DrawElements(PrimitiveType.Triangles, (uint)IndexRenderCount, DrawElementsType.UnsignedInt, null);
            }

            if(AutoClearOnRender)
                Clear();
        }

        //Redraw the current contents
        /// <summary>
        /// Redraw the current contents, if it hasnt been cleared before
        /// </summary>
        public void Redraw()
        {
            unsafe
            {
                GL.DrawElements(PrimitiveType.Triangles, (uint)IndexRenderCount, DrawElementsType.UnsignedInt, null);
            }
        }

        public void Reset()
        {
            VertexRenderCount = 0;
            IndexRenderCount = 0;
        }

        public void Clear()
        {
            //Either clear the array to reset everything to 0
            //Or make sure that every vertex gotten from the pool, every property gets overriden
            //So the old values wont make it

            //Performance diff:

            //Clear 100k Quads = 65-68 fps
            //No clear 100k Quads = 70-73 fps
            //Clear time: Roughly 800 microseconds

            //Clear 1 Million Quads = 6-7 fps
            //No clear 1 Million Quads = 7-8 fps
            //Clear time: Roughly 8 ms

            Array.Clear(vertexPool, 0, VertexRenderCount);
            VertexRenderCount = 0;
            IndexRenderCount = 0;
        }
    }
}
