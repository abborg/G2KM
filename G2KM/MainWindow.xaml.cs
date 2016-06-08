using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        private void OnKeyPressed(object sender, RawInputKeyboardEventArgs e)
        {
            KeyboardEvent = e;
            KeyboardCount = _rawInput.NumberOfKeyboards;
            e.Handled = (ShouldHandle.IsChecked == true);
        }

        private void OnMouseEvent(object sender, RawInputMouseEventArgs e)
        {
            MouseEvent = e;
            MouseCount = _rawInput.NumberOfMice;
            e.Handled = (ShouldHandle1.IsChecked == true);
        }

        private void OnHidEvent(object sender, RawInputHidEventArgs e)
        {
            _hidEvent = e;
            HidCount = _rawInput.NumberOfHid;
            e.Handled = (ShouldHandle2.IsChecked == true);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            StartWndProcHandler();
            base.OnSourceInitialized(e);
        }

        private void StartWndProcHandler()
        {
            _rawInput = new RawPresentationInput(this, RawInputCaptureMode.Foreground);
            _rawInput.KeyPressed += OnKeyPressed;
            KeyboardCount = _rawInput.NumberOfKeyboards;
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
