using System;

namespace RawInput
{
    public abstract class RawInput : IDisposable
    {
        private readonly RawKeyboard _keyboardDriver;
        private readonly RawMouse _mouseDriver;
        private readonly RawHid _hidDriver;

        public event EventHandler<RawInputKeyboardEventArgs> KeyPressed
        {
            add { KeyboardDriver.KeyPressed += value; }
            remove { KeyboardDriver.KeyPressed -= value; }
        }

        public event EventHandler<RawInputMouseEventArgs> MouseClicked
        {
            add { MouseDriver.MouseClicked += value; }
            remove { MouseDriver.MouseClicked -= value; }
        }

        public event EventHandler<RawInputHidEventArgs> HidUsed
        {
            add { HidDriver.HidUsed += value; }
            remove { HidDriver.HidUsed -= value; }
        }

        public int NumberOfKeyboards
        {
            get { return KeyboardDriver.NumberOfKeyboards; }
        }

        public int NumberOfMice
        {
            get { return MouseDriver.NumberOfMice; }
        }

        public int NumberOfHid
        {
            get { return HidDriver.NumberOfHid; }
        }

        protected RawKeyboard KeyboardDriver
        {
            get { return _keyboardDriver; }
        }

        protected RawMouse MouseDriver
        {
            get { return _mouseDriver; }
        }

        protected RawHid HidDriver
        {
            get { return _hidDriver; }
        }

        protected RawInput(IntPtr handle, RawInputCaptureMode captureMode)
        {
            _mouseDriver = new RawMouse(handle, captureMode == RawInputCaptureMode.Foreground);
            _hidDriver = new RawHid(handle, captureMode == RawInputCaptureMode.Foreground);
            _keyboardDriver = new RawKeyboard(handle, captureMode == RawInputCaptureMode.Foreground);
        }

        public abstract void AddMessageFilter();
        public abstract void RemoveMessageFilter();

        public void Dispose()
        { 
            KeyboardDriver.Dispose();
            MouseDriver.Dispose();
            HidDriver.Dispose();
        }
    }
}
