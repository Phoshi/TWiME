using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    public class Window {

        [DllImport("user32.dll")]
        private static extern
            bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
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
            uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        private static extern
            IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);
        [DllImport("user32.dll")]
        private static extern
            int GetWindowText(IntPtr hWnd, StringBuilder title, int size);
        [DllImport("user32.dll")]
        private static extern
            int GetClassName(IntPtr hWnd, StringBuilder className, int size);
        [DllImport("User32.dll", ExactSpelling = true,
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool MoveWindow
            (IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT{
            public int _Left;
            public int _Top;
            public int _Right;
            public int _Bottom;
        }

        private const int SW_HIDE = 0;
        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;
        private const int SW_SHOWMAXIMIZED = 3;
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

        private IntPtr _handle;
        private string _title;
        private bool _visible;
        private string _process;
        private bool _maximized;
        private string _className;
        private Rectangle _location;

        public IntPtr handle { get { return _handle; } }
        public string title { get { return _title; } }
        public string process { get { return _process; } }
        public string className { get { return _className; } }
        public Screen screen { get; internal set; }

        public bool visible { 
            get {
                return _visible;
            } 

            set {
                if (value) {
                    if (ShowWindowAsync(_handle, _maximized ? SW_SHOWMAXIMIZED : SW_SHOWNORMAL)) {
                        _visible = true;
                    }
                }
                else {
                    _maximized = IsZoomed(_handle);
                    if (ShowWindowAsync(_handle, SW_HIDE)) {
                        _visible = false;
                    }
                }
            }
        }

        public Window(string title, IntPtr handle, string process, string className) {
            _title = title;
            _handle = handle;
            _process = process;
            _className = className;
            screen = Screen.FromHandle(handle);
        }

        public override string ToString() {
            if (string.IsNullOrEmpty(title)) {
                return _process;
            }
            else {
                return _title;
            }
        }
        public bool Equals(Window otherWindow) {
            if (otherWindow == null) {
                return false;
            }
            if (_handle == otherWindow.handle) {
                return true;
            }
            else {
                return false;
            }
        }
        public void activate() {
            if (_handle == GetForegroundWindow()) {
                return;
            }

            IntPtr foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            IntPtr thisThread = GetWindowThreadProcessId(_handle, IntPtr.Zero);

            if (foregroundThread != thisThread) {
                AttachThreadInput(foregroundThread, thisThread, 1);
                SetForegroundWindow(_handle);
                AttachThreadInput(foregroundThread, thisThread, 0);
            }
            else {
                SetForegroundWindow(_handle);
            }

            if (IsIconic(_handle)) {
                ShowWindowAsync(_handle, SW_RESTORE);
            }
            else {
                ShowWindowAsync(_handle, SW_SHOWNORMAL);
            }
        }

        private string getWindowModuleName(IntPtr handle) {
            uint processID;
            if (GetWindowThreadProcessId(handle, out processID) > 0) {
                return Process.GetProcessById((int)processID).MainModule.FileName;
            }
            return "";
        }

        private void updatePosition() {
            RECT newRect = new RECT();
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
                _location = value;
                SetWindowPos(handle, (IntPtr)HWND_TOP, _location.X, _location.Y, _location.Width, _location.Height, SWP_NOACTIVATE);
            }
        } 

    }
}
