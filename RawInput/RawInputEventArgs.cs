using System;
using System.Windows.Input;

namespace RawInput
{
    public sealed class RawInputKeyboardEventArgs : EventArgs
    {
        public RawKeyboardDevice Device { get; private set; }
        public KeyPressState KeyPressState { get; private set; }
        public uint Message { get; private set; }
        public Key Key { get; private set; }
        public int VirtualKey { get; private set; }
        public bool Handled { get; set; }

        internal RawInputKeyboardEventArgs(RawKeyboardDevice device, KeyPressState keyPressState, uint message, Key key, int virtualKey)
        {
            Device = device;
            KeyPressState = keyPressState;
            Message = message;
            Key = key;
            VirtualKey = virtualKey;
        }

    }

    public sealed class RawInputMouseEventArgs : EventArgs
    {
        public RawMouseDevice Device { get; private set; }
        public ushort Flags { get; private set; }
        public ushort ButtonFlags { get; private set; }
        public short ButtonData { get; private set; }
        public long LastX { get; private set; }
        public long LastY { get; private set; }
        public bool Handled { get; set; }
        
        internal RawInputMouseEventArgs(RawMouseDevice device, ushort flags, ushort buttonFlags, short buttonData, int lastX, int lastY)
        {
            Device = device;
            Flags = flags;
            ButtonFlags = buttonFlags;
            ButtonData = buttonData;
            LastX = lastX;
            LastY = lastY;
        }
    }

    public sealed class RawInputHidEventArgs : EventArgs
    {
        public RawHidDevice Device { get; private set; }
        public uint SizeHid { get; private set; }
        public uint Count { get; private set; }
        public byte[] RawData { get; private set; }
        public bool Handled { get; set; }

        internal RawInputHidEventArgs(RawHidDevice device, uint sizeHid, uint count, byte[] rawData)
        {
            Device = device;
            SizeHid = sizeHid;
            Count = count;
            RawData = new byte[sizeHid*count];
            for (int i = 0; i < sizeHid * count; i++)
            {
                RawData[i] = rawData[i];
            }
        }
    }
}
