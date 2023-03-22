using Silk.NET.OpenGLES;
using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public unsafe class QuadBatch<T> where T : unmanaged
    {
        private GLBuffer<uint> ibo;
        private GLBuffer<T> vbo;

        private VertexArray<T> vao = new VertexArray<T>();

        private uint quadIndex;

        public int QuadCapacity { get; private set; }

        private T* localBuffer;

        private bool updateIndices = true;
        private Action updateAction;

        public QuadBatch(int quadCapacity = 10_000)
        {
            QuadCapacity = quadCapacity;

            int vertexCount = 4 * quadCapacity;
            int indicesCount = 6 * quadCapacity;

            vbo = new GLBuffer<T>(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw, Marshal.SizeOf<T>() * vertexCount);
            ibo = new GLBuffer<uint>(BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw, sizeof(uint) * indicesCount);

            localBuffer = (T*)Marshal.AllocHGlobal(Marshal.SizeOf<T>() * vertexCount);

            //kekw
            updateAction = () =>
            {
                //6 indices per quad
                uint[] indices = new uint[indicesCount];
                uint offset = 0;

                for (int i = 0; i < indices.Length; i += 6)
                {
                    indices[i + 0] = offset + 0;
                    indices[i + 1] = offset + 1;
                    indices[i + 2] = offset + 2;

                    indices[i + 3] = offset + 2;
                    indices[i + 4] = offset + 3;
                    indices[i + 5] = offset + 0;

                    offset += 4;
                }

                ibo.UploadData(0, indicesCount, indices);

                updateIndices = false;
            };
        }

        public Span<T> GetQuadSpan()
        {
            checkCapacity();

            quadIndex += 4;

            return new Span<T>(localBuffer + quadIndex - 4, 4);
        }

        public T* GetQuadPointer()
        {
            checkCapacity();

            quadIndex += 4;

            return localBuffer + quadIndex - 4;
        }

        private void checkCapacity()
        {
            if (quadIndex + 4 > QuadCapacity)
            {
                Utils.Log($"Flushed quadbatch, index exceeded capacity! {QuadCapacity}", LogLevel.Performance);
                Draw();
            }
        }

        public void Draw()
        {
            unsafe
            {
                vbo.Bind();
                vbo.UploadData(0, quadIndex, localBuffer);

                vao.Bind();

                //lol
                if (updateIndices)
                    updateAction();
                else
                    ibo.Bind();

                GL.DrawElements(PrimitiveType.Triangles, quadIndex, DrawElementsType.UnsignedInt);

                quadIndex = 0;
            }
        }

        ~QuadBatch() => Marshal.FreeHGlobal(new IntPtr(localBuffer));
    }
}
