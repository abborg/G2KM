using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows;
using RawInput;


namespace G2KM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private RawPresentationInput _rawInput;
        private int _keyboardCount;
        private int _mouseCount;
        private int _hidCount;
        private RawInputKeyboardEventArgs _keyboardEvent;
        private RawInputMouseEventArgs _mouseEvent;
        private RawInputHidEventArgs _hidEvent;

        private HidController controller;

        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        public int KeyboardCount
        {
            get { return _keyboardCount; }
            set
            {
                _keyboardCount = value;
                OnPropertyChanged();
            }
        }

        public int MouseCount
        {
            get { return _mouseCount; }
            set
            {
                _mouseCount = value;
                OnPropertyChanged();
            }
        }
        public int HidCount
        {
            get { return _hidCount; }
            set
            {
                _hidCount = value;
                OnPropertyChanged();
            }
        }

        public RawInputKeyboardEventArgs KeyboardEvent
        {
            get { return _keyboardEvent; }
            set 
            {
                _keyboardEvent = value;
                OnPropertyChanged();
            }
        }

        public RawInputMouseEventArgs MouseEvent
        {
            get { return _mouseEvent; }
            set
            {
                _mouseEvent = value;
                OnPropertyChanged();
            }
        }


        public RawInputHidEventArgs HidEvent
        {
            get { return _hidEvent; }
            set
            {
                _hidEvent = value;
                OnPropertyChanged();
            }
        }

        private void OnKeyEvent(object sender, RawInputKeyboardEventArgs e)
        {
            KeyboardEvent = e;
            KeyboardCount = _rawInput.NumberOfKeyboards;
            e.Handled = (ShouldHandleKeyboard.IsChecked == true);
        }

        private void OnMouseEvent(object sender, RawInputMouseEventArgs e)
        {
            MouseEvent = e;
            MouseCount = _rawInput.NumberOfMice;
            e.Handled = (ShouldHandleMouse.IsChecked == true);
        }

        private void OnHidEvent(object sender, RawInputHidEventArgs e)
        {
            HidEvent = e;
            HidCount = _rawInput.NumberOfHid;
            Canvas.SetLeft(lstickdot, BitConverter.ToUInt16(e.RawData, 1) * 150 / ushort.MaxValue);
            Canvas.SetTop(lstickdot, BitConverter.ToUInt16(e.RawData, 3) * 150 / ushort.MaxValue);
            Canvas.SetLeft(rstickdot, BitConverter.ToUInt16(e.RawData, 5) * 150 / ushort.MaxValue);
            Canvas.SetTop(rstickdot, BitConverter.ToUInt16(e.RawData, 7) * 150 / ushort.MaxValue);
            if((e.RawData[9] & 0x80) == 0x0) 
                Canvas.SetLeft(ltLine, e.RawData[10] ^ 0x80);
            if ((e.RawData[9] & ~0x80) == 0x0) 
                Canvas.SetLeft(rtLine, e.RawData[10] & 0x80);
            Style active = this.FindResource("ActiveStyle") as Style;
            Style normal = this.FindResource("NormalStyle") as Style;
            aButton.Style = (e.RawData[11] & 0x01) == 0x01 ? active : normal;
            bButton.Style = (e.RawData[11] & 0x02) == 0x02 ? active : normal;
            xButton.Style = (e.RawData[11] & 0x04) == 0x04 ? active : normal;
            yButton.Style = (e.RawData[11] & 0x08) == 0x08 ? active : normal;
            lbButton.Style = (e.RawData[11] & 0x10) == 0x10 ? active : normal;
            rbButton.Style = (e.RawData[11] & 0x20) == 0x20 ? active : normal;
            backButton.Style = (e.RawData[11] & 0x40) == 0x40 ? active : normal;
            startButton.Style = (e.RawData[11] & 0x80) == 0x80 ? active : normal;
            l3Button.Style = (e.RawData[12] & 0x01) == 0x01 ? active : normal;
            r3Button.Style = (e.RawData[12] & 0x02) == 0x02 ? active : normal;
            DpadEnum t = (DpadEnum)((e.RawData[12] & ~0x80) >> 2);
            upDpad.Style = (t == DpadEnum.TOP) || (t == DpadEnum.TOPRIGHT) || (t == DpadEnum.TOPLEFT) ? active : normal;
            downDpad.Style = (t == DpadEnum.BOTTOM) || (t == DpadEnum.BOTTOMRIGHT) || (t == DpadEnum.BOTTOMLEFT) ? active : normal;
            leftDpad.Style = (t == DpadEnum.LEFT) || (t == DpadEnum.TOPLEFT) || (t == DpadEnum.BOTTOMLEFT) ? active : normal;
            rightDpad.Style = (t == DpadEnum.RIGHT) || (t == DpadEnum.TOPRIGHT) || (t == DpadEnum.BOTTOMRIGHT) ? active : normal;
            e.Handled = (ShouldHandleHid.IsChecked == true);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            StartWndProcHandler();
            base.OnSourceInitialized(e);
        }

        private void StartWndProcHandler()
        {
            _rawInput = new RawPresentationInput(this, RawInputCaptureMode.Foreground);
            _rawInput.KeyPressed += OnKeyEvent;
            _rawInput.MouseClicked += OnMouseEvent;
            _rawInput.HidUsed += OnHidEvent;
            KeyboardCount = _rawInput.NumberOfKeyboards;
            MouseCount = _rawInput.NumberOfMice;
            HidCount = _rawInput.NumberOfHid;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler propertyChanged = PropertyChanged;
            if(propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
