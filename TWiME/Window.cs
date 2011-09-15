using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TWiME {
    public class Window {
        [DllImport("user32.dll")]
        private static extern
            bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern
            bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern
            bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern
            bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        private static extern
            bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern
            int GetWindowText(IntPtr hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
                                                uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr handle, uint command);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, IntPtr lParam);

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int _Left;
            public int _Top;
            public int _Right;
            public int _Bottom;
        }

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_SHOW = 5;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWDEFAULT = 10;

        public const int HWND_TOP = 0;
        public const int HWND_BOTTOM = 1;
        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;

        public const int SWP_ASYNCWINDOWPOS = 0x4000;
        public const int SWP_DEFERERASE = 0x2000;
        public const int SWP_DRAWFRAME = 0x0020;
        public const int SWP_FRAMECHANGED = 0x0020;
        public const int SWP_HIDEWINDOW = 0x0080;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_NOCOPYBITS = 0x0100;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOOWNERZORDER = 0x0200;
        public const int SWP_NOREDRAW = 0x0008;
        public const int SWP_NOREPOSITION = 0x0200;
        public const int SWP_NOSENDCHANGING = 0x0400;
        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_SHOWWINDOW = 0x0040;


        private const uint SPI_GETFOREGROUNDLOCKTIMEOUT = 0x2000;
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;


        private IntPtr _handle;
        private string _title;
        private bool _visible;
        private string _process;
        private bool _maximized;
        private string _className;
        private Rectangle _location;
        private long lastTitleUpdate = DateTime.Now.Ticks;

        public IntPtr handle {
            get { return _handle; }
        }

        public bool AsyncResizing = true;

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
            GetWindowText(_handle, windowTitle, 256);
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
                ShowWindowAsync(_handle, _maximized ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL);
                _visible = true;
            }
        }

        public bool Visible {
            get { return _visible; }

            set {
                Action visibleUpdateAction = (() => {
                                                  if (value) {
                                                      if (!_visible) {
                                                          ShowWindow(_handle, Maximised ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL);
                                                          _visible = true;
                                                      }
                                                      if (Manager.hiddenWindows.Contains(this)) {
                                                          Manager.hiddenWindows.Remove(this);
                                                      }
                                                  }
                                                  else {
                                                      _maximized = IsZoomed(_handle);
                                                      ShowWindow(_handle, SW_HIDE);
                                                      _visible = false;
                                                      Manager.hiddenWindows.Add(this);
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
            if (_handle == GetForegroundWindow()) {
                return;
            }

            if (IsIconic(_handle)) {
                ShowWindowAsync(_handle, SW_RESTORE);
            }

            attemptSetForeground(_handle, GetForegroundWindow());
            bool meAttachedToFore, foreAttachedToTarget;

            IntPtr foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            IntPtr thisThread = (IntPtr) Thread.CurrentThread.ManagedThreadId;
            IntPtr targetThread = GetWindowThreadProcessId(_handle, IntPtr.Zero);

            meAttachedToFore = AttachThreadInput(thisThread, foregroundThread, true);
            foreAttachedToTarget = AttachThreadInput(foregroundThread, targetThread, true);
            IntPtr foreground = GetForegroundWindow();
            BringWindowToTop(_handle);
            for (int i = 0; i < 5; i++) {
                attemptSetForeground(_handle, foreground);
                if (GetForegroundWindow() == _handle) {
                    break;
                }
            }

            //SetForegroundWindow(_handle);
            AttachThreadInput(foregroundThread, thisThread, false);
            AttachThreadInput(foregroundThread, targetThread, false);

            if (GetForegroundWindow() != _handle) {
                // Code by Daniel P. Stasinski
                // Converted to C# by Kevin Gale
                IntPtr Timeout = IntPtr.Zero;
                SystemParametersInfo(SPI_GETFOREGROUNDLOCKTIMEOUT, 0, Timeout, 0);
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, 0x1);
                BringWindowToTop(_handle); // IE 5.5 related hack
                SetForegroundWindow(_handle);
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, Timeout, 0x1);
            }

            if (meAttachedToFore) {
                AttachThreadInput(thisThread, foregroundThread, false);
            }
            if (foreAttachedToTarget) {
                AttachThreadInput(foregroundThread, targetThread, false);
            }
            Visible = true;
        }

        private void attemptSetForeground(IntPtr target, IntPtr foreground) {
            SetForegroundWindow(target);
            Thread.Sleep(10);
            IntPtr newFore = GetForegroundWindow();
            if (newFore == target) {
                return;
            }
            if (newFore != foreground && target == GetWindow(newFore, 4)) {
                //4 is GW_OWNER - the window parent
                return;
            }
            return;
        }

        private void updatePosition() {
            RECT newRect;
            GetWindowRect(handle, out newRect);
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
                ShowWindowAsync(handle, SW_SHOWMAXIMIZED);
                Thread moveThread = new Thread((()=>assertLocation(value)));
                moveThread.Start();
                if (!AsyncResizing) {
                    moveThread.Join();
                }
                _location = value;
            }
        }

        private void assertLocation(Rectangle whereTo) {
            SetWindowPos(handle, (IntPtr) HWND_TOP, whereTo.X, whereTo.Y, whereTo.Width, whereTo.Height,
                            SWP_NOACTIVATE);
        }

        public bool Maximised {
            get { return _maximized; }
            set {
                if (value == _maximized) {
                    return;
                }
                if (value) {
                    ShowWindowAsync(handle, SW_SHOWMAXIMIZED);
                    _maximized = true;
                }
                else {
                    ShowWindowAsync(handle, SW_RESTORE);
                    _maximized = false;
                }
            }
        }

        public void CatchMessage(HotkeyMessage message) {
            if (message.Message == Message.Close) {
                this.Close();
            }
        }

        public void Close() {
            SendMessage(_handle, WM_SYSCOMMAND, SC_CLOSE, IntPtr.Zero);
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