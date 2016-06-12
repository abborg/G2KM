using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using RawInput.Win32;

namespace RawInput
{
    public class RawInput : IDisposable
    {
        private static readonly Guid DeviceInterfaceHid = new Guid("b7c91732-51e5-4909-9265-99256fb322ee");

        private readonly Dictionary<IntPtr, RawKeyboardDevice> _keyboardList = new Dictionary<IntPtr, RawKeyboardDevice>();
        private readonly Dictionary<IntPtr, RawMouseDevice> _mouseList = new Dictionary<IntPtr, RawMouseDevice>();
        private readonly Dictionary<IntPtr, RawHidDevice> _hidList = new Dictionary<IntPtr, RawHidDevice>();
        private readonly object _lock = new object();

        private IntPtr _devNotifyHandle;

        public int NumberOfKeyboards { get; private set; }
        public int NumberOfMice { get; private set; }
        public int NumberOfHid { get; private set; }

        private EventHandler<RawInputKeyboardEventArgs> KeyPressedDelegate;
        private EventHandler<RawInputMouseEventArgs> MouseClickedDelegate;
        private EventHandler<RawInputHidEventArgs> HidUsedDelegate;

        public event EventHandler<RawInputKeyboardEventArgs> KeyPressed
        {
            add { KeyPressedDelegate += value; }
            remove { KeyPressedDelegate -= value; }
        }

        public event EventHandler<RawInputMouseEventArgs> MouseClicked
        {
            add { MouseClickedDelegate += value; }
            remove { MouseClickedDelegate -= value; }
        }

        public event EventHandler<RawInputHidEventArgs> HidUsed
        {
            add { HidUsedDelegate += value; }
            remove { HidUsedDelegate -= value; }
        }

        protected RawInput(IntPtr handle, RawInputCaptureMode captureMode)
        {
            RawInputDevice[] array = {
                                         new RawInputDevice
                                         {
                                             UsagePage = HidUsagePage.GENERIC,
                                             Usage = HidUsage.Keyboard,
                                             Flags = (captureMode == RawInputCaptureMode.Foreground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY,
                                             Target = IntPtr.Zero
                                         },
                                         new RawInputDevice
                                         {
                                             UsagePage = HidUsagePage.GENERIC,
                                             Usage = HidUsage.Mouse,
                                             Flags = (captureMode == RawInputCaptureMode.Foreground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY,
                                             Target = IntPtr.Zero
                                         },
                                         new RawInputDevice
                                         {
                                             UsagePage = HidUsagePage.GENERIC,
                                             Usage = HidUsage.Gamepad,
                                             Flags = (captureMode == RawInputCaptureMode.Foreground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY,
                                             Target = IntPtr.Zero
                                         }
                                     };
            if (!Win32Methods.RegisterRawInputDevices(array, (uint)array.Length, (uint)Marshal.SizeOf(array[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).", new Win32Exception());
            }
            EnumerateDevices();
            _devNotifyHandle = RegisterForDeviceNotifications(handle);
        }

        public virtual void AddMessageFilter()
        { }
        public virtual void RemoveMessageFilter()
        { }

        public void Dispose()
        { 
            GC.SuppressFinalize(this);
            if(_devNotifyHandle != IntPtr.Zero)
            {
                Win32Methods.UnregisterDeviceNotification(_devNotifyHandle);
                _devNotifyHandle = IntPtr.Zero;
            }
        }

        public void EnumerateDevices()
        {
            EnumerateKeyboards();
            EnumerateMice();
            EnumerateHid();
        }

        public void EnumerateKeyboards()
        {
            lock (_lock)
            {
                _keyboardList.Clear();
                var rawKeyboardDevice = new RawKeyboardDevice("Global Keyboard", RawDeviceType.Keyboard, IntPtr.Zero, "Fake Keyboard. Some keys (ZOOM, MUTE, VOLUMEUP, VOLUMEDOWN) are sent to rawinput with a handle of zero.");
                _keyboardList.Add(rawKeyboardDevice.Handle, rawKeyboardDevice);
                uint devices = 0u;
                int size = Marshal.SizeOf(typeof(RawInputDeviceList));
                if (Win32Methods.GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)size) != 0u)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(size * devices));
                try
                {
                    Win32Methods.GetRawInputDeviceList(pRawInputDeviceList, ref devices, (uint)size);
                    int index = 0;
                    while (index < devices)
                    {
                        RawKeyboardDevice device = GetKeyboard(pRawInputDeviceList, size, index);
                        if (device != null && !_keyboardList.ContainsKey(device.Handle))
                        {
                            _keyboardList.Add(device.Handle, device);
                        }
                        index++;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
                NumberOfKeyboards = _keyboardList.Count;
            }
        }

        public void EnumerateMice()
        {
            lock (_lock)
            {
                _mouseList.Clear();
                var rawMouseDevice = new RawMouseDevice("Global Mouse", RawDeviceType.Mouse, IntPtr.Zero, "Fake mouse.");
                _mouseList.Add(rawMouseDevice.Handle, rawMouseDevice);
                uint devices = 0u;
                int size = Marshal.SizeOf(typeof(RawInputDeviceList));
                if (Win32Methods.GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)size) != 0u)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(size * devices));
                try
                {
                    Win32Methods.GetRawInputDeviceList(pRawInputDeviceList, ref devices, (uint)size);
                    int index = 0;
                    while (index < devices)
                    {
                        RawMouseDevice device = GetMouse(pRawInputDeviceList, size, index);
                        if (device != null && !_mouseList.ContainsKey(device.Handle))
                        {
                            _mouseList.Add(device.Handle, device);
                        }
                        index++;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
                NumberOfMice = _mouseList.Count;
            }
        }

        public void EnumerateHid()
        {
            lock (_lock)
            {
                _hidList.Clear();
                uint devices = 0u;
                int size = Marshal.SizeOf(typeof(RawInputDeviceList));
                if (Win32Methods.GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)size) != 0u)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(size * devices));
                try
                {
                    Win32Methods.GetRawInputDeviceList(pRawInputDeviceList, ref devices, (uint)size);
                    int index = 0;
                    while (index < devices)
                    {
                        RawHidDevice device = GetHid(pRawInputDeviceList, size, index);
                        if (device != null && !_hidList.ContainsKey(device.Handle))
                        {
                            Debug.WriteLine("Added Hid; Handle: {0}, Name: {1}, Type: {2}, Description: {3}", (uint)device.Handle, device.Name, device.Type, device.Description);
                            _hidList.Add(device.Handle, device);
                        }
                        index++;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
                NumberOfHid = _hidList.Count;
            }
        }

        private static RawKeyboardDevice GetKeyboard(IntPtr pRawInputDeviceList, int dwSize, int index)
        {
            uint size = 0u;
            // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
            var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * index), typeof(RawInputDeviceList));
            Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size <= 0u)
            {
                return null;
            }
            IntPtr intPtr = Marshal.AllocHGlobal((int)size);
            try
            {
                Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, intPtr, ref size);
                string device = Marshal.PtrToStringAnsi(intPtr);
                if (rawInputDeviceList.dwType == DeviceType.RimTypekeyboard)
                {
                    string deviceDescription = Win32Methods.GetDeviceDescription(device);
                    return new RawKeyboardDevice(Marshal.PtrToStringAnsi(intPtr), (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            return null;
        }

        private static RawMouseDevice GetMouse(IntPtr pRawInputDeviceList, int dwSize, int index)
        {
            uint size = 0u;
            // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
            var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * index), typeof(RawInputDeviceList));
            Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size <= 0u)
            {
                return null;
            }
            IntPtr intPtr = Marshal.AllocHGlobal((int)size);
            try
            {
                Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, intPtr, ref size);
                string device = Marshal.PtrToStringAnsi(intPtr);
                if (rawInputDeviceList.dwType == DeviceType.RimTypemouse)
                {
                    string deviceDescription = Win32Methods.GetDeviceDescription(device);
                    return new RawMouseDevice(Marshal.PtrToStringAnsi(intPtr), (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            return null;
        }

        private static RawHidDevice GetHid(IntPtr pRawInputDeviceList, int dwSize, int index)
        {
            uint size = 0u;
            // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
            var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * index), typeof(RawInputDeviceList));
            Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if (size <= 0u)
            {
                return null;
            }
            IntPtr intPtr = Marshal.AllocHGlobal((int)size);
            try
            {
                Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, intPtr, ref size);
                string device = Marshal.PtrToStringAnsi(intPtr);
                if (rawInputDeviceList.dwType == DeviceType.RimTypeHid)
                {
                    string deviceDescription = Win32Methods.GetDeviceDescription(device);
                    return new RawHidDevice(Marshal.PtrToStringAnsi(intPtr), (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            return null;
        }

        private bool ProcessRawInput(IntPtr hdevice)
        {
            if (_keyboardList.Count == 0 && _mouseList.Count == 0 && _hidList.Count == 0)
            {
                Debug.WriteLine("KeyboardCount: {0}, MouseCount: {1}, HidCount: {2}", _keyboardList.Count, _mouseList.Count, _hidList.Count);
                return false;
            }
            int size = 0;
                        Win32Methods.GetRawInputData(hdevice, DataCommand.RID_INPUT, IntPtr.Zero, ref size, Marshal.SizeOf(typeof(RawInputHeader)));
            InputData rawBuffer;
            if (Win32Methods.GetRawInputData(hdevice, DataCommand.RID_INPUT, out rawBuffer, ref size, Marshal.SizeOf(typeof(RawInputHeader))) != size)
            {
                Debug.WriteLine("Error getting the rawinput buffer");
                return false;
            }

            switch ((RawDeviceType)rawBuffer.header.dwType)
            {
                case RawDeviceType.Mouse:
                    return ProcessMouseInput(rawBuffer);
                case RawDeviceType.Hid:
                    return ProcessHidInput(rawBuffer);
                case RawDeviceType.Keyboard:
                    return ProcessKeyboardInput(rawBuffer);
            }
            return false;
        }

        private bool ProcessKeyboardInput(InputData rawBuffer)
        {
            int vKey = rawBuffer.data.keyboard.VKey;
            int makecode = rawBuffer.data.keyboard.Makecode;
            int flags = rawBuffer.data.keyboard.Flags;
            if (vKey == Win32Consts.KEYBOARD_OVERRUN_MAKE_CODE)
            {
                return false;
            }
            RawKeyboardDevice device;
            lock (_lock)
            {
                if (!_keyboardList.TryGetValue(rawBuffer.header.hDevice, out device))
                {
                    Debug.WriteLine("Handle: {0} wasf not in the device list.", rawBuffer.header.hDevice);
                    return false;
                }
            }

            var isE0BitSet = ((flags & Win32Consts.RI_KEY_E0) != 0);
            bool isBreakBitSet = ((flags & Win32Consts.RI_KEY_BREAK) != 0);

            uint message = rawBuffer.data.keyboard.Message;
            Key key = KeyInterop.KeyFromVirtualKey(AdjustVirtualKey(rawBuffer, vKey, isE0BitSet, makecode));
            EventHandler<RawInputKeyboardEventArgs> keyPressed = KeyPressedDelegate;
            if (keyPressed != null)
            {
                var rawInputEventArgs = new RawInputKeyboardEventArgs(device, isBreakBitSet ? KeyPressState.Up : KeyPressState.Down, message, key, vKey);
                keyPressed(this, rawInputEventArgs);
                if (rawInputEventArgs.Handled)
                {
                    MSG msg;
                    Win32Methods.PeekMessage(out msg, IntPtr.Zero, Win32Consts.WM_KEYFIRST, Win32Consts.WM_KEYLAST, Win32Consts.PM_REMOVE);
                }
                return rawInputEventArgs.Handled;
            }
            return false;
        }

        private bool ProcessMouseInput(InputData rawBuffer)
        {
            ushort usFlags = rawBuffer.data.mouse.usFlags;
            ushort usButtonFlags = rawBuffer.data.mouse.usButtonFlags;
            ushort usButtonData = rawBuffer.data.mouse.usButtonData;
            int lLastX = rawBuffer.data.mouse.lLastX;
            int lLastY = rawBuffer.data.mouse.lLastY;

            RawMouseDevice device;
            lock (_lock)
            {
                if (!_mouseList.TryGetValue(rawBuffer.header.hDevice, out device))
                {
                    Debug.WriteLine("Handle: {0} was not in the device list.", rawBuffer.header.hDevice);
                    return false;
                }
            }

            EventHandler<RawInputMouseEventArgs> mouseClicked = MouseClickedDelegate;
            if (mouseClicked != null)
            {
                var rawInputEventArgs = new RawInputMouseEventArgs(device, usFlags, usButtonFlags, usButtonData, lLastX, lLastY);
                mouseClicked(this, rawInputEventArgs);
                if (rawInputEventArgs.Handled)
                {
                    MSG msg;
                    Win32Methods.PeekMessage(out msg, IntPtr.Zero, Win32Consts.WM_MOUSEFIRST, Win32Consts.WM_MOUSELAST, Win32Consts.PM_REMOVE);
                }
                return rawInputEventArgs.Handled;
            }
            return false;
        }

        private unsafe bool ProcessHidInput(InputData rawBuffer)
        {
            uint sizeHid = rawBuffer.data.hid.dwSizeHid;
            uint count = rawBuffer.data.hid.dwCount;
            byte[] rawData = new byte[15];
            for (int i = 0; i < 15; i++)
            {
                rawData[i] = 0;
                rawData[i] = rawBuffer.data.hid.bRawData[i];
                //Debug.Write(Convert.ToString(rawData[i], 2).PadLeft(8, '0'));
            }
            //Debug.WriteLine("");
            Debug.WriteLine(BitConverter.ToString(rawData));
            RawHidDevice device;
            lock (_lock)
            {
                if (!_hidList.TryGetValue(rawBuffer.header.hDevice, out device))
                {
                    Debug.WriteLine("Handle: {0} was not in the device list.", rawBuffer.header.hDevice);
                    return false;
                }
            }

            EventHandler<RawInputHidEventArgs> hidUsed = HidUsedDelegate;
            if (hidUsed != null)
            {
                var rawInputEventArgs = new RawInputHidEventArgs(device, sizeHid, count, rawData);
                hidUsed(this, rawInputEventArgs);
                if (rawInputEventArgs.Handled)
                {
                    MSG msg;
                    Win32Methods.PeekMessage(out msg, IntPtr.Zero, 0u, 0u, Win32Consts.PM_REMOVE);
                }
                return rawInputEventArgs.Handled;
            }
            return false;
        }

        private static IntPtr RegisterForDeviceNotifications(IntPtr parent)
        {
            IntPtr notifyHandle = IntPtr.Zero;
            BroadcastDeviceInterface broadcastDeviceInterface = default(BroadcastDeviceInterface);
            broadcastDeviceInterface.dbcc_size = Marshal.SizeOf(broadcastDeviceInterface);
            broadcastDeviceInterface.BroadcastDeviceType = BroadcastDeviceType.DBT_DEVTYP_DEVICEINTERFACE;
            broadcastDeviceInterface.dbcc_classguid = DeviceInterfaceHid;
            IntPtr interfacePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BroadcastDeviceInterface)));
            try
            {
                Marshal.StructureToPtr(broadcastDeviceInterface, interfacePtr, false);
                notifyHandle = Win32Methods.RegisterDeviceNotification(parent, interfacePtr, DeviceNotification.DEVICE_NOTIFY_WINDOW_HANDLE);
            }
            catch (Exception ex)
            {
                Debug.Print("Registration for device notifications Failed. Error: {0}", Marshal.GetLastWin32Error());
            }
            finally
            {
                Marshal.FreeHGlobal(interfacePtr);
            }
            if (notifyHandle == IntPtr.Zero)
            {
                Debug.Print("Registration for device notifications Failed: Error: {0}", Marshal.GetLastWin32Error());
            }
            return notifyHandle;
        }

        private static int AdjustVirtualKey(InputData rawBuffer, int virtualKey, bool isE0BitSet, int makeCode)
        {
            var adjustedKey = virtualKey;

            if (rawBuffer.header.hDevice == IntPtr.Zero)
            {
                // When hDevice is 0 and the vkey is VK_CONTROL indicates the ZOOM key
                if (rawBuffer.data.keyboard.VKey == Win32Consts.VK_CONTROL)
                {
                    adjustedKey = Win32Consts.VK_ZOOM;
                }
            }
            else
            {
                switch (virtualKey)
                {
                    // Right-hand CTRL and ALT have their e0 bit set
                    case Win32Consts.VK_CONTROL:
                        adjustedKey = isE0BitSet ? Win32Consts.VK_RCONTROL : Win32Consts.VK_LCONTROL;
                        break;
                    case Win32Consts.VK_MENU:
                        adjustedKey = isE0BitSet ? Win32Consts.VK_RMENU : Win32Consts.VK_LMENU;
                        break;
                    case Win32Consts.VK_SHIFT:
                        adjustedKey = makeCode == Win32Consts.SC_SHIFT_R ? Win32Consts.VK_RSHIFT : Win32Consts.VK_LSHIFT;
                        break;
                    default:
                        adjustedKey = virtualKey;
                        break;
                }
            }
            return adjustedKey;
        }

        public bool HandleMessage(int msg, IntPtr wparam, IntPtr lparam)
        {
            switch (msg)
            {
                case Win32Consts.WM_INPUT_DEVICE_CHANGE:
                    EnumerateDevices();
                    break;
                case Win32Consts.WM_INPUT:
                    return ProcessRawInput(lparam);
            }
            return false;
        }

        public static string GetDeviceDiagnostics()
        {
            return Win32Methods.GetDeviceDiagnostics();
        }
    }
}
