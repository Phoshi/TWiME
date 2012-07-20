using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TWiME {
    public class Window {
        private IntPtr _handle;
        private string _title;
        private bool _visible;
        private string _process;
        private bool _maximized;
        private string _className;
        private Rectangle _location;
        private long lastTitleUpdate = DateTime.Now.Ticks;
        private long _style;

        public IntPtr handle {
            get { return _handle; }
        }

        public bool AsyncResizing = true;

        private bool _allowResize = true;
        public bool AllowResize {
            get { return _allowResize; }
            set { _allowResize = value; }
        }

        public WindowTilingType TilingType { get; set; }

        private bool _topMost;
        public bool TopMost {
            get { return _topMost; }
            set {
                if (value) {
                    Win32API.SetWindowPos(this.handle, (IntPtr) Win32API.HWND_TOPMOST, 0, 0, 0, 0, Win32API.SWP_NOMOVE | Win32API.SWP_NOSIZE);
                }
                else {
                    Win32API.SetWindowPos(this.handle, (IntPtr) Win32API.HWND_NOTOPMOST, 0, 0, 0, 0, Win32API.SWP_NOMOVE | Win32API.SWP_NOSIZE);
                }
                _topMost = value;
            }
        }

        private bool _showCaption = true;
        public bool ShowCaption { get { return _showCaption; } 
            set {
                if (value) {
                    Style = Style | (Win32API.WS_CAPTION | Win32API.WS_THICKFRAME);
                }
                else {
                    Style = Style & ~(Win32API.WS_CAPTION | Win32API.WS_THICKFRAME);
                }
                _showCaption = value;

            }
        }

        public long Style {
            get { return _style; }
            set {
                Win32API.SetWindowLong(handle, Win32API.GWL_STYLE, value);
                _style = value;
            }
        }

        public string Title {
            get {
                if (lastTitleUpdate < (DateTime.Now.Ticks - new TimeSpan(0, 0, 0, 1).Ticks)) {
                    //If the window title is more than 1 second old
                    UpdateTitle();
                }
                return _title;
            }
        }

        public void UpdateTitle() {
            StringBuilder windowTitle = new StringBuilder(256);
            Win32API.GetWindowText(_handle, windowTitle, 256);
            _title = windowTitle.ToString();
            lastTitleUpdate = DateTime.Now.Ticks;
        }

        public string Process {
            get { return _process; }
        }

        public string ClassName {
            get { return _className; }
        }

        public Screen Screen {
            get { return Screen.FromHandle(handle); }
        }

        public void ForceVisible() {
            if (!_visible) {
                Win32API.ShowWindowAsync(_handle, _maximized ? Win32API.SW_SHOWMAXIMIZED : Win32API.SW_SHOWNORMAL);
                _visible = true;
            }
        }

        public bool Visible {
            get { return _visible; }

            set {
                Action visibleUpdateAction = (() => {
                                                  if (value) {
                                                      if (!_visible) {
                                                          Win32API.ShowWindow(_handle, Maximised ? Win32API.SW_SHOWMAXIMIZED : Win32API.SW_SHOWNORMAL);
                                                          _visible = true;
                                                      }
                                                      if (Manager.hiddenWindows.Contains(this)) {
                                                          Manager.hiddenWindows.Remove(this);
                                                      }
                                                  }
                                                  else {
                                                      _maximized = Win32API.IsZoomed(_handle);
                                                      Manager.hiddenWindows.Add(this);
                                                      Win32API.ShowWindow(_handle, Win32API.SW_HIDE);
                                                      _visible = false;
                                                  }
                                              }
                                             );
                Thread visibleUpdateThread = new Thread((()=>visibleUpdateAction()));
                visibleUpdateThread.Start();

            }
        }

        public Window(string title, IntPtr handle, string process, string className, bool isWindowVisible) {
            _title = title;
            _handle = handle;
            _process = process;
            _className = className;
            _visible = isWindowVisible;
            _style = (long) Win32API.GetWindowLongPtr(handle, Win32API.GWL_STYLE); //-16 is GWL_STYLE
        }

        public override string ToString() {
            return string.IsNullOrEmpty(Title) ? _process : _title;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != typeof (Window)) {
                return false;
            }
            return Equals((Window) obj);
        }

        /// <summary>
        /// Sets window focus. Repeatedly. 
        /// </summary>
        public void Activate() {
            if (_handle == Win32API.GetForegroundWindow()) {
                return;
            }

            if (Win32API.IsIconic(_handle)) {
                Win32API.ShowWindowAsync(_handle, Win32API.SW_RESTORE);
            }

            attemptSetForeground(_handle, Win32API.GetForegroundWindow());
            bool meAttachedToFore, foreAttachedToTarget;

            IntPtr foregroundThread = Win32API.GetWindowThreadProcessId(Win32API.GetForegroundWindow(), IntPtr.Zero);
            IntPtr thisThread = (IntPtr) Thread.CurrentThread.ManagedThreadId;
            IntPtr targetThread = Win32API.GetWindowThreadProcessId(_handle, IntPtr.Zero);

            meAttachedToFore = Win32API.AttachThreadInput(thisThread, foregroundThread, true);
            foreAttachedToTarget = Win32API.AttachThreadInput(foregroundThread, targetThread, true);
            IntPtr foreground = Win32API.GetForegroundWindow();
            Win32API.BringWindowToTop(_handle);
            for (int i = 0; i < 5; i++) {
                attemptSetForeground(_handle, foreground);
                if (Win32API.GetForegroundWindow() == _handle) {
                    break;
                }
            }

            //SetForegroundWindow(_handle);
            Win32API.AttachThreadInput(foregroundThread, thisThread, false);
            Win32API.AttachThreadInput(foregroundThread, targetThread, false);

            if (Win32API.GetForegroundWindow() != _handle) {
                // Code by Daniel P. Stasinski
                // Converted to C# by Kevin Gale
                IntPtr Timeout = IntPtr.Zero;
                Win32API.SystemParametersInfo(Win32API.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, Timeout, 0);
                Win32API.SystemParametersInfo(Win32API.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0x1);
                Win32API.BringWindowToTop(_handle); // IE 5.5 related hack
                Win32API.SetForegroundWindow(_handle);
                Win32API.SystemParametersInfo(Win32API.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, Timeout, 0x1);
            }

            if (meAttachedToFore) {
                Win32API.AttachThreadInput(thisThread, foregroundThread, false);
            }
            if (foreAttachedToTarget) {
                Win32API.AttachThreadInput(foregroundThread, targetThread, false);
            }
            Visible = true;
        }

        private void attemptSetForeground(IntPtr target, IntPtr foreground) {
            Win32API.SetForegroundWindow(target);
            Thread.Sleep(10);
            IntPtr newFore = Win32API.GetForegroundWindow();
            if (newFore == target) {
                return;
            }
            if (newFore != foreground && target == Win32API.GetWindow(newFore, 4)) {
                //4 is GW_OWNER - the window parent
                return;
            }
            return;
        }

        private void updatePosition() {
            Win32API.RECT newRect;
            Win32API.GetWindowRect(handle, out newRect);
            _location.X = newRect._Left;
            _location.Y = newRect._Top;
            _location.Width = newRect._Right - newRect._Left;
            _location.Height = newRect._Bottom - newRect._Top;
        }

        public Rectangle Location {
            get {
                updatePosition();
                return _location;
            }
            set {
                Win32API.ShowWindowAsync(handle, Win32API.SW_SHOWMAXIMIZED);
                ThreadPool.QueueUserWorkItem((delegate { assertLocation(value); }));
                _location = value;
            }
        }

        private void assertLocation(Rectangle whereTo) {
            updatePosition();
            if (!_allowResize) {
                whereTo.Width = _location.Width;
                whereTo.Height = _location.Height;
            }
            if (whereTo != _location) {
                Win32API.SetWindowPos(handle, (IntPtr) Win32API.HWND_TOP, whereTo.X, whereTo.Y, whereTo.Width, whereTo.Height,
                             Win32API.SWP_NOACTIVATE | Win32API.SWP_ASYNCWINDOWPOS | Win32API.SWP_NOZORDER);
                Win32API.ShowWindow(handle, Win32API.SW_SHOWMAXIMIZED);
            }
        }

        public bool Maximised {
            get { return _maximized; }
            set {
                if (value == _maximized) {
                    return;
                }
                if (value) {
                    Win32API.ShowWindowAsync(handle, Win32API.SW_SHOWMAXIMIZED);
                    _maximized = true;
                }
                else {
                    Win32API.ShowWindowAsync(handle, Win32API.SW_RESTORE);
                    _maximized = false;
                }
            }
        }

        public void CatchMessage(HotkeyMessage message) {
            if (message.Message == Message.Close) {
                this.Close();
            }
            else if (message.Message == Message.TilingType) {
                if (message.data > 0) {
                    TilingType = (WindowTilingType) message.data;
                }
                else if (message.data == -1) {
                    int currentType = (int) TilingType;
                    currentType++;
                    if (currentType >= Enum.GetValues(typeof(WindowTilingType)).Length) {
                        currentType = 0;
                    }
                    TilingType = (WindowTilingType) currentType;
                }
                else {
                    TilingType = TilingType == WindowTilingType.Normal ? WindowTilingType.FullScreen : WindowTilingType.Normal;
                }
            }
            else if (message.Message == Message.TopMost) {
                if (message.data > 0) {
                    TopMost = message.data != 0;
                }
                else {
                    TopMost = !TopMost;
                }
            }
            else if (message.Message == Message.WindowChrome) {
                if (message.data > 0) {
                    ShowCaption = message.data != 0;
                }
                else {
                    ShowCaption = !ShowCaption;
                }
            }
        }

        public void Close() {
            Win32API.SendMessage(_handle, Win32API.WM_SYSCOMMAND, Win32API.SC_CLOSE, IntPtr.Zero);
        }

        public bool Equals(Window other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return other._handle.Equals(_handle);
        }

        public override int GetHashCode() {
            unchecked {
                int result = _handle.GetHashCode();
                result = (result * 397) ^ (_title != null ? _title.GetHashCode() : 0);
                result = (result * 397) ^ _visible.GetHashCode();
                result = (result * 397) ^ (_process != null ? _process.GetHashCode() : 0);
                result = (result * 397) ^ _maximized.GetHashCode();
                result = (result * 397) ^ (_className != null ? _className.GetHashCode() : 0);
                result = (result * 397) ^ _location.GetHashCode();
                result = (result * 397) ^ lastTitleUpdate.GetHashCode();
                return result;
            }
        }
    }
}