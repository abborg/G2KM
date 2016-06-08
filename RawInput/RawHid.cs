using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using RawInput.Win32;

namespace RawInput
{
    public sealed class RawHid : IDisposable
    {
        private static readonly Guid DeviceInterfaceHid = new Guid("53eafdce-0c40-4829-8e73-d474b62a644a");

        private readonly Dictionary<IntPtr, RawHidDevice> _deviceList = new Dictionary<IntPtr, RawHidDevice>();
        private readonly object _lock = new object();

        private IntPtr _devNotifyHandle;

        public int NumberOfHid { get; private set; }

        public event EventHandler<RawInputHidEventArgs> HidUsed;

        public RawHid(IntPtr hwnd, bool captureOnlyInForeground)
        {
            RawInputDevice[] array = {
                                        new RawInputDevice
                                        {
                                            UsagePage = HidUsagePage.GENERIC,
                                            Usage = HidUsage.Gamepad,
                                            Flags = (captureOnlyInForeground ? RawInputDeviceFlags.NONE : RawInputDeviceFlags.INPUTSINK) | RawInputDeviceFlags.DEVNOTIFY,
                                            Target = hwnd
                                        }
                                    };
            if(!Win32Methods.RegisterRawInputDevices(array, (uint)array.Length, (uint)Marshal.SizeOf(array[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).", new Win32Exception());
            }
            EnumerateDevices();
            _devNotifyHandle = RegisterForDeviceNotifications(hwnd);
        }

        ~RawHid()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if(_devNotifyHandle != IntPtr.Zero)
            {
                Win32Methods.UnregisterDeviceNotification(_devNotifyHandle);
                _devNotifyHandle = IntPtr.Zero;
            }
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
            } catch(Exception ex)
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

        public void EnumerateDevices()
        {
            lock(_lock)
            {
                _deviceList.Clear();
                var rawHidDevice = new RawHidDevice("Global Hid", RawDeviceType.Hid, IntPtr.Zero, "Fake hid.");
                _deviceList.Add(rawHidDevice.Handle, rawHidDevice);
                uint devices = 0u;
                int size = Marshal.SizeOf(typeof(RawInputDeviceList));
                if(Win32Methods.GetRawInputDeviceList(IntPtr.Zero, ref devices, (uint)size) != 0u)
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
                        RawHidDevice device = GetDevice(pRawInputDeviceList, size, index);
                        if (device != null && !_deviceList.ContainsKey(device.Handle))
                        {
                            _deviceList.Add(device.Handle, device);
                        }
                        index++;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
                NumberOfHid = _deviceList.Count;
            }
        }

        private static RawHidDevice GetDevice(IntPtr pRawInputDeviceList, int dwSize, int index)
        {
            uint size = 0u;
            // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
            var rawInputDeviceList = (RawInputDeviceList)Marshal.PtrToStructure(new IntPtr(pRawInputDeviceList.ToInt64() + dwSize * index), typeof(RawInputDeviceList));
            Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref size);
            if(size <= 0u)
            {
                return null;
            }
            IntPtr intPtr = Marshal.AllocHGlobal((int)size);
            try
            {
                Win32Methods.GetRawInputDeviceInfo(rawInputDeviceList.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, intPtr, ref size);
                string device = Marshal.PtrToStringAnsi(intPtr);
                if(rawInputDeviceList.dwType == DeviceType.RimTypeHid)
                {
                    string deviceDescription = Win32Methods.GetDeviceDescription(device);
                    return new RawHidDevice(Marshal.PtrToStringAnsi(intPtr), (RawDeviceType)rawInputDeviceList.dwType, rawInputDeviceList.hDevice, deviceDescription);
                }
            }
            finally
            {
                if(intPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(intPtr);
                }
            }
            return null;
        }

        private bool ProcessRawInput(IntPtr hdevice)
        {
            if(_deviceList.Count == 0)
            {
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
            uint sizeHid = rawBuffer.data.hid.dwSizHid;
            uint count = rawBuffer.data.hid.dwCount;
            byte rawData = rawBuffer.data.hid.bRawData;
            RawHidDevice device;
            lock(_lock)
            {
                if(!_deviceList.TryGetValue(rawBuffer.header.hDevice, out device))
                {
                    Debug.WriteLine("Handle: {0} was not in the device list.", rawBuffer.header.hDevice);
                    return false;
                }
            }

            EventHandler<RawInputHidEventArgs> hidUsed = HidUsed;
            if(hidUsed != null)
            {
                var rawInputEventArgs = new RawInputHidEventArgs(device, sizeHid, count, rawData);
                hidUsed(this, rawInputEventArgs);
                if(rawInputEventArgs.Handled)
                {
                    MSG msg;
                    Win32Methods.PeekMessage(out msg, IntPtr.Zero, 0u, 0u, Win32Consts.PM_REMOVE);
                }
                return rawInputEventArgs.Handled;
            }
            return false;
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
