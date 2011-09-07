using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    internal class Windows : IEnumerable, IEnumerator {
        [DllImport("user32.dll")]
        private static extern
            int GetWindowText(int hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        private static extern
            int GetClassName(int hWnd, StringBuilder className, int size);

        [DllImport("user32.dll")]
        private static extern
            int EnumWindows(EnumWindowsProc ewp, int lParam);

        [DllImport("user32.dll")]
        private static extern
            bool IsWindowVisible(int hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(int hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        private const long WS_POPUP = 0x80000000L;
        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_THICKFRAME = 0x00040000L;

        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        private int _position = -1;

        private List<Window> windowList = new List<Window>();
        private List<int> handleList = new List<int>();

        private bool _showInvisible;
        private bool _showNoTitle;
        private bool _justHandles;

        private List<IntPtr> myHandles = new List<IntPtr>();

        private string[] ignoreClasses = new[] {"Progman", "Button"};

        public Windows(bool justGiveMeHandles = false, bool showInvisible = false, bool showNoTitle = false) {
            _showNoTitle = showNoTitle;
            _showInvisible = showInvisible;
            _justHandles = justGiveMeHandles;

            foreach (Form openForm in Application.OpenForms) {
                myHandles.Add(openForm.Handle);
            }

            enumerateWindows();
        }

        private void enumerateWindows() {
            EnumWindowsProc callback = processWindow;
            EnumWindows(callback, 0);
        }

        private bool processWindow(int handle, int lparam) {
            if (_showInvisible == false && !IsWindowVisible(handle)) {
                return (true);
            }
            if (_justHandles) {
                handleList.Add(handle);
                return true;
            }
            StringBuilder title = new StringBuilder(256);
            StringBuilder className = new StringBuilder(256);

            string module = getWindowModuleName(handle);
            GetWindowText(handle, title, 256);
            GetClassName(handle, className, 256);

            if (_showNoTitle == false && title.Length == 0) {
                return true;
            }
            if (myHandles.Contains((IntPtr) handle)) {
                return true;
            }
            if (title.ToString().EndsWith("(Not Responding)")) {
                return true;
            }

            IntPtr style = GetWindowLongPtr((IntPtr) handle, -16); //-16 is GWL_STYLE
            if (((long) style & WS_POPUP) == WS_POPUP) {
                return true;
            }
            if (((long) style & WS_CAPTION) != WS_CAPTION) {
                return true;
            }
            if (((long)style & WS_THICKFRAME) == 0) {
                return true;
            }

            if (!ignoreClasses.Contains(className.ToString())) {
                windowList.Add(new Window(title.ToString(), (IntPtr) handle,
                                          module, className.ToString(), IsWindowVisible(handle)));
            }

            return true;
        }

        private string getWindowModuleName(int handle) {
            uint processID;
            if (GetWindowThreadProcessId(handle, out processID) > 0) {
                try {
                    return Process.GetProcessById((int) processID).MainModule.FileName;
                }
                catch (ArgumentException) {
                    return "";
                }
            }
            return "";
        }

        public IEnumerator GetEnumerator() {
            return this;
        }

        public bool MoveNext() {
            _position++;
            if (!_justHandles) {
                if (_position < windowList.Count) {
                    return true;
                }
            }
            else {
                if (_position > handleList.Count) {
                    return true;
                }
            }
            return false;
        }

        public void Reset() {
            _position = -1;
        }

        public object Current {
            get {
                if (_justHandles) {
                    return handleList[_position];
                }
                return windowList[_position];
            }
        }
    }
}