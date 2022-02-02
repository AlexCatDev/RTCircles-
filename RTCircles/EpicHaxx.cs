
using System;

namespace RTCircles
{
    public class EpicHaxx
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        public struct Hack
        {
            //a union?
            [System.Runtime.InteropServices.FieldOffset(0)] public object AsObject;
            //watafuk
            [System.Runtime.InteropServices.FieldOffset(0)] public IntPtrWrapper AsIntPtr;
        }

        public class IntPtrWrapper
        {
            public IntPtr Value;
        }
    }
}
