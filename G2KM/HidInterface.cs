using System;
using System.Text;
using System.Runtime.InteropServices;

namespace G2KM
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct HidController
    {
        [FieldOffset(0)]
        public Xbox360Controller xbox;
        [FieldOffset(0)]
        public PS3Controller ps3;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct Xbox360Controller
    {
        [FieldOffset(0)]
        public byte dummy1;
        [FieldOffset(1)]
        public ushort lx;
        [FieldOffset(3)]
        public ushort ly;
        [FieldOffset(5)]
        public ushort rx;
        [FieldOffset(7)]
        public ushort ry;
        [FieldOffset(9)]
        public short trigger;
        [FieldOffset(11)]
        public byte facebuttons;
        [FieldOffset(12)]
        public byte otherbuttons;
        [FieldOffset(13)]
        public byte dummy2;
        [FieldOffset(14)]
        public byte dummy3;
    }
    
    [StructLayout(LayoutKind.Explicit)]
    internal struct PS3Controller
    {
        [FieldOffset(0)]
        public byte pls;
    }
}
