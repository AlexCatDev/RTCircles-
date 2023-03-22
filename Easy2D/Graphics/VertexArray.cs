using Silk.NET.OpenGLES;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public struct VertexMember
    {
        public uint Divisor { get; private set; }

        public uint SizeOfType { get; private set; }

        public uint? CustomOffset { get; private set; } = null;

        public uint Count { get; private set; }
        public VertexAttribPointerType Type { get; private set; }

        internal static Dictionary<Type, (VertexAttribPointerType Type, uint Count, uint SizeOfType)> typeCache =
        new()
            {
                { typeof(float), (VertexAttribPointerType.Float, 1, 4) },
                { typeof(Vector2), (VertexAttribPointerType.Float, 2, 4) },
                { typeof(Vector3), (VertexAttribPointerType.Float, 3, 4) },
                { typeof(Vector4), (VertexAttribPointerType.Float, 4 , 4) },
                { typeof(int), (VertexAttribPointerType.Int, 1, 4) },
                { typeof(uint), (VertexAttribPointerType.UnsignedInt, 1, 4) },
                { typeof(ushort), (VertexAttribPointerType.UnsignedShort, 1, 2) },
                { typeof(short), (VertexAttribPointerType.Short, 1, 2) },
                { typeof(byte), (VertexAttribPointerType.UnsignedByte, 1, 1) },
            };

        public static void RegisterType<T>(VertexAttribPointerType Type, uint Count, uint SizeOfType) => typeCache.Add(typeof(T), (Type, Count, SizeOfType));

        public VertexMember(VertexAttribPointerType type, uint count, uint baseSize, uint divisor = 0)
        {
            Divisor = divisor;
            SizeOfType = baseSize;
            Count = count;
            Type = type;
        }

        public static VertexMember ParseFromType<T>(uint divisor = 0)
        {
            VertexMember member = new();

            var type = typeCache[typeof(T)];

            member.Type = type.Type;
            member.SizeOfType = type.SizeOfType;
            member.Count = type.Count;
            member.Divisor = divisor;

            return member;
        }
    }

    public class VertexArray : GLObject
    {
        private uint vertexAttribIndex = 0;

        public void AddBuffer<T>(GLBuffer<T> buffer, List<VertexMember> layout, uint globalOffset = 0) where T : unmanaged
        {
            uint stride = 0;
            foreach (var member in layout)
            {
                stride += member.SizeOfType*member.Count;
            }

            uint offsetInStruct = 0;

            buffer.Bind();
            Bind();

            foreach (var member in layout)
            {
                GL.Instance.EnableVertexAttribArray(vertexAttribIndex);
                unsafe
                {
                    GL.Instance.VertexAttribPointer(vertexAttribIndex, (int)member.Count, member.Type, normalized: false, stride, (void*)(globalOffset + member.CustomOffset ?? offsetInStruct));
                    GL.Instance.VertexAttribDivisor(vertexAttribIndex, member.Divisor);
                    vertexAttribIndex++;
                }

                offsetInStruct += member.SizeOfType * member.Count;
            }
        }

        protected override void bind(int? slot)
        {
            GL.Instance.BindVertexArray(Handle);
        }

        protected override void delete()
        {
            GL.Instance.DeleteVertexArray(Handle);
            Handle = UninitializedHandle;
        }

        protected override void initialize(int? slot)
        {
            Handle = GL.Instance.GenVertexArray();

            bind(0);
        }
    }

    public class VertexArray<T> : GLObject where T : struct
    {
        private static Dictionary<Type, (int Size, int ComponentCount, VertexAttribPointerType Type)> typeCache =
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
