using Silk.NET.OpenGLES;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Easy2D
{
    /// <summary>
    /// Name is slightly misleading, we're not batching instances together, just standard instanced rendering
    /// </summary>
    /// <typeparam name="TBaseVertex">The vertex type the model is made out of</typeparam>
    /// <typeparam name="TInstanceVertex">The vertex type that is used for each instance</typeparam>
    public class InstanceBatch<TBaseVertex, TInstanceVertex> where TBaseVertex : unmanaged where TInstanceVertex : unmanaged
    {
        private Dictionary<Type, (int Size, int ComponentCount, VertexAttribPointerType Type)> typeCache =
        new Dictionary<Type, (int Size, int ComponentCount, VertexAttribPointerType Type)>()
            {
                { typeof(float), (4, 1, VertexAttribPointerType.Float) },
                { typeof(Vector2), (8, 2, VertexAttribPointerType.Float) },
                { typeof(Vector3), (12, 3, VertexAttribPointerType.Float) },
                { typeof(Vector4), (16, 4, VertexAttribPointerType.Float) },
                { typeof(int), (4, 1, VertexAttribPointerType.Float) },
                { typeof(uint), (4, 1, VertexAttribPointerType.UnsignedInt) },
                { typeof(ushort), (2, 1, VertexAttribPointerType.UnsignedShort) },
                { typeof(short), (2, 1, VertexAttribPointerType.Short) },
                { typeof(byte), (1, 1, VertexAttribPointerType.UnsignedByte) },
            };

        private GLBuffer<TBaseVertex> modelVbo;
        private GLBuffer<int> modelIbo;
        private GLBuffer<TInstanceVertex> instanceVbo;

        private uint vao;
        private int vertexAttribIndex;
        private int elementCount;

        public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

        public InstanceBatch(TBaseVertex[] model, int[] indices)
        {
            modelVbo = new GLBuffer<TBaseVertex>(BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw, model.Length);
            modelVbo.UploadData(0, model.Length, model);

            if (indices is not null)
            {
                modelIbo = new GLBuffer<int>(BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw, indices.Length);
                modelIbo.UploadData(0, indices.Length, indices);
                elementCount = indices.Length;
            }
            else
            {
                elementCount = model.Length;
            }

            instanceVbo = new GLBuffer<TInstanceVertex>(BufferTargetARB.ArrayBuffer, BufferUsageARB.DynamicDraw, 1);

            setupVAO();
        }

        private void enableAttribute(int size, int componentCount, VertexAttribPointerType type,
           int offset, int stride, bool normalized = false, int divisor = 0)
        {
            Utils.Log($"VertexAttribute[{vertexAttribIndex}]  Count: {componentCount} type: {type} offset: {offset} stride: {stride} divisor: {divisor}", LogLevel.Info);

            GL.Instance.EnableVertexAttribArray((uint)vertexAttribIndex);
            unsafe
            {
                GL.Instance.VertexAttribPointer((uint)vertexAttribIndex, componentCount, type, normalized, (uint)stride, (void*)offset);
                GL.Instance.VertexAttribDivisor((uint)vertexAttribIndex, (uint)divisor);
            }
            vertexAttribIndex++;
        }

        private void scanType<T>()
        {
            int sizeOfType = Marshal.SizeOf<T>();
            int divisor = typeof(T) == typeof(TInstanceVertex) ? 1 : 0;

            if (typeCache.TryGetValue(typeof(T), out (int Size, int ComponentCount, VertexAttribPointerType Type) value))
            {
                enableAttribute(value.Size, value.ComponentCount, value.Type, 0, sizeOfType, false, divisor);
            }
            else
            {
                int offset = 0;

                //Search through every field in the struct
                foreach (FieldInfo field in typeof(T).GetFields())
                {
                    var attrib = typeCache[field.FieldType];
                    enableAttribute(attrib.Size, attrib.ComponentCount, attrib.Type, offset, sizeOfType, false, divisor);

                    offset += attrib.Size;
                }
            }
        }

        private void setupVAO()
        {
            vao = GL.Instance.GenVertexArray();
            GL.Instance.BindVertexArray(vao);

            modelVbo.Bind();
            scanType<TBaseVertex>();

            instanceVbo.Bind();
            scanType<TInstanceVertex>();
        }

        public void UploadInstanceData(Span<TInstanceVertex> data)
        {
            if(instanceVbo.Capacity < data.Length)
                instanceVbo.Resize(data.Length);

            unsafe
            {
                fixed(TInstanceVertex* ptr = data)
                    instanceVbo.UploadData(0, (uint)data.Length, ptr);
            }
        }

        public void Draw(int instanceCount)
        {
            GL.Instance.BindVertexArray(vao);
            if (modelIbo is null)
                GL.Instance.DrawArraysInstanced(PrimitiveType, 0, (uint)elementCount, (uint)instanceCount);
            else 
                unsafe
                {
                    GL.Instance.DrawElementsInstanced(PrimitiveType, (uint)elementCount, DrawElementsType.UnsignedInt, null, (uint)instanceCount);
                }
        }
    }
}
