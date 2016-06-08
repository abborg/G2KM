using System;

namespace RawInput
{
    public sealed class RawHidDevice
    {
        public string Name { get; private set; }
        public RawDeviceType Type { get; private set; }
        public IntPtr Handle { get; private set; }
        public string Description { get; private set; }

        internal RawHidDevice(string name, RawDeviceType type, IntPtr handle, string description)
        {
            Handle = handle;
            Type = type;
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return string.Format("Device\n Name: {0}\n Type: {1}\n Handle: {2}\n Name: {3}\n",
                Name,
                Type,
                Handle.ToInt64().ToString("X"),
                Description);
        }
    }
}
