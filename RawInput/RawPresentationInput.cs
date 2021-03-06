﻿using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace RawInput
{
    public class RawPresentationInput : RawInput
    {
        private bool _hasFilter;

        public RawPresentationInput(HwndSource hwndSource, RawInputCaptureMode captureMode) : base(hwndSource.Handle, captureMode)
        {
            hwndSource.AddHook(Hook);
        }

        public RawPresentationInput(Visual visual, RawInputCaptureMode captureMode) : this(GetHwndSource(visual), captureMode)
        {
        }

        private static HwndSource GetHwndSource(Visual visual)
        {
            var source = PresentationSource.FromVisual(visual) as HwndSource;
            if(source == null)
            {
                throw new InvalidOperationException("Cannot find a valid HwndSource");
            }
            return source;
        }

        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            HandleMessage(msg, wparam, lparam);
            return IntPtr.Zero;
        }

        public override void AddMessageFilter()
        {
            if (_hasFilter)
            {
                return;
            }
            ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
            _hasFilter = true;
        }

        public override void RemoveMessageFilter()
        {
            ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
            _hasFilter = false;
        }

        private void OnThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            handled = HandleMessage(msg.message, msg.wParam, msg.lParam);
        }
    }
}
