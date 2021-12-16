using Silk.NET.OpenGLES;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public class VertexArray<T> : GLObject where T : struct
    {
        private Dictionary<Type, (int Size, int ComponentCount, VertexAttribPointerType Type)> typeCache =
        new Dictionary<Type, (int Size, int ComponentCount, VertexAttribPointerType Type)>()
            {
                { typeof(float), (4, 1, VertexAttribPointerType.Float) },
                { typeof(Vector2), (8, 2, VertexAttribPointerType.Float) },
                { typeof(Vector3), (12, 3, VertexAttribPointerType.Float) },
                { typeof(Vector4), (16, 4, VertexAttribPointerType.Float) },
                { typeof(Color4), (16, 4, VertexAttribPointerType.Float) },
                { typeof(int), (4, 1, VertexAttribPointerType.Float) },
                { typeof(uint), (4, 1, VertexAttribPointerType.UnsignedInt) },
                { typeof(ushort), (2, 1, VertexAttribPointerType.UnsignedShort) },
                { typeof(short), (2, 1, VertexAttribPointerType.Short) },
                { typeof(byte), (1, 1, VertexAttribPointerType.UnsignedByte) },
            };

        private int vertexAttribIndex;

        private int sizeOfBaseType;

        public VertexArray()
        {
            sizeOfBaseType = Marshal.SizeOf<T>();
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindVertexArray(Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteVertexArray(Handle);
            Handle = uint.MaxValue;
        }

        private void enableAttribute(int size, int componentCount, VertexAttribPointerType type,
           int offset, int stride, bool normalized = false, int divisor = 0)
        {
            Utils.Log($"VertexAttribute[{vertexAttribIndex}]\n\tTYPE: {type} Count: {componentCount} SIZE: {size} OFFSET: {offset} DIVISOR: {divisor} STRIDE: {stride}", LogLevel.Info);

            GL.Instance.EnableVertexAttribArray((uint)vertexAttribIndex);
            unsafe
            {
                GL.Instance.VertexAttribPointer((uint)vertexAttribIndex, componentCount, type, normalized, (uint)stride, (void*)offset);
                GL.Instance.VertexAttribDivisor((uint)vertexAttribIndex, (uint)divisor);
            }
            vertexAttribIndex++;
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenVertexArray();
            bind(null);

            Utils.Log($"Initialising VertexArray[{Handle}]<{typeof(T)}> {sizeOfBaseType} bytes", LogLevel.Info);

            if (typeCache.TryGetValue(typeof(T), out (int Size, int ComponentCount, VertexAttribPointerType Type) value))
            {
                enableAttribute(value.Size, value.ComponentCount, value.Type, 0, sizeOfBaseType, false, 0);
            }
            else
            {
                int offset = 0;

                //Search through every field in the struct
                foreach (FieldInfo field in typeof(T).GetFields())
                {
                    var attrib = typeCache[field.FieldType];
                    enableAttribute(attrib.Size, attrib.ComponentCount, attrib.Type, offset, sizeOfBaseType, false, 0);

                    offset += attrib.Size;
                }

                if (offset != sizeOfBaseType)
                    Utils.Log($"Every field in {typeof(T).ToString()} could not be parsed", LogLevel.Error);
            }
        }
    }
}
