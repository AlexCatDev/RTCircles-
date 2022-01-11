using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy2D
{
    /// <summary>
    /// Fast buffer streaming primitive batch
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe class StreamingPrimitiveBuffer<T> where T : unmanaged
    {
        public static bool IsSupported => StreamingBuffer<T>.IsSupported;

        /// <summary>
        /// How many buffers to cycle through
        /// </summary>
        public int BUFFER_COUNT = 1;

        private VertexArray<T> vao;
        private StreamingBuffer<T> vbo;
        private StreamingBuffer<uint> ibo;

        private int bufferIndex = 0;

        private StreamingBuffer<T>[] vbos;
        private StreamingBuffer<uint>[] ibos;
        private VertexArray<T>[] vaos;

        public uint VertexRenderCount { get; private set; }
        public uint IndexRenderCount { get; private set; }

        public uint VertexCapacity { get; private set; }
        public uint IndexCapacity { get; private set; }

        public StreamingPrimitiveBuffer(uint vertexCount, uint indexCount)
        {
            if (IsSupported == false)
            {
                Utils.Log($"This device does not support streaming buffers!", LogLevel.Error);
                throw new NotSupportedException();
            }

            VertexCapacity = vertexCount;
            IndexCapacity = indexCount;

            //Buffering double buffering buffers? XD
            //Buffer boofa båffa buffer these nuts

            /*
            ibo1 = new StreamingBuffer<uint>(BufferTargetARB.ElementArrayBuffer, indexCount);
            ibo2 = new StreamingBuffer<uint>(BufferTargetARB.ElementArrayBuffer, indexCount);

            vao1 = new VertexArray<T>();
            vao2 = new VertexArray<T>();

            vbo1 = new StreamingBuffer<T>(BufferTargetARB.ArrayBuffer, vertexCount);
            vbo2 = new StreamingBuffer<T>(BufferTargetARB.ArrayBuffer, vertexCount);

            vbo1.Bind();
            vao1.Bind();

            vbo2.Bind();
            vao2.Bind();

            ibo1.Bind();
            ibo2.Bind();

            vbo = vbo1;
            ibo = ibo1;
            vao = vao1;
            */

            vbos = new StreamingBuffer<T>[BUFFER_COUNT];
            ibos = new StreamingBuffer<uint>[BUFFER_COUNT];
            vaos = new VertexArray<T>[BUFFER_COUNT];

            for (int i = 0; i < BUFFER_COUNT; i++)
            {
                vbos[i] = new StreamingBuffer<T>(BufferTargetARB.ArrayBuffer, vertexCount);
                ibos[i] = new StreamingBuffer<uint>(BufferTargetARB.ElementArrayBuffer, indexCount);
                vaos[i] = new VertexArray<T>();

                vbos[i].Bind();
                vaos[i].Bind();
                ibos[i].Bind();
            }

            vbo = vbos[0];
            ibo = ibos[0];
            vao = vaos[0];

        }

        /// <summary>
        /// Get 3 vertices connected to each other
        /// </summary>
        /// <returns></returns>
        public Span<T> GetTriangle()
        {
            ensureCapacity(3, 3);

            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 1;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 2;

            var data = new Span<T>(vbo.Pointer + VertexRenderCount, 3);
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
            
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 1;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 2;

            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 2;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 3;

            var data = new Span<T>(vbo.Pointer + VertexRenderCount, 4);
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
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 1;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 2;

            for (uint i = 3; i < pointCount; i++)
            {
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount - 2 + i;
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount - 1 + i;
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0 + i;
            }

            var data = new Span<T>(vbo.Pointer + VertexRenderCount, pointCount);
            VertexRenderCount += (uint)pointCount;

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
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 1;
            ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 2;

            for (uint i = 3; i < pointCount; i++)
            {
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0;
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount - 1 + i;
                ibo.Pointer[IndexRenderCount++] = VertexRenderCount + 0 + i;
            }

            var data = new Span<T>(vbo.Pointer + VertexRenderCount, pointCount);
            VertexRenderCount += (uint)pointCount;

            return data;
        }

        private void ensureCapacity(int vertexCount, int indexCount)
        {
            if (IndexRenderCount + indexCount > IndexCapacity)
            {
                Draw();
                Utils.Log($"IndexBuffer ran out of space, so the whole batch ha been FLUSHED", LogLevel.Warning);
            }

            if (VertexRenderCount + vertexCount > VertexCapacity)
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
            vao.Bind();

            ibo.Bind();

            //????? Finish because async transfer?
            GL.Instance.DrawElements(PrimitiveType.Triangles, IndexRenderCount, DrawElementsType.UnsignedInt, null);
            //GL.Instance.Finish();
            IndexRenderCount = 0;
            VertexRenderCount = 0;

            bufferIndex++;

            if (bufferIndex >= BUFFER_COUNT)
                bufferIndex = 0;

            vbo = vbos[bufferIndex];
            ibo = ibos[bufferIndex];
            vao = vaos[bufferIndex];

        }
    }
}
