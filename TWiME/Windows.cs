using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    internal class Windows : IEnumerable, IEnumerator {
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
            Win32API.EnumWindows(callback, 0);
        }

        private bool processWindow(int handle, int lparam) {
            if (_showInvisible == false && !Win32API.IsWindowVisible(handle)) {
                return (true);
            }
            if (_justHandles) {
                handleList.Add(handle);
                return true;
            }
            StringBuilder title = new StringBuilder(256);
            StringBuilder className = new StringBuilder(256);

            string module = getWindowModuleName(handle);
            Win32API.GetWindowText(handle, title, 256);
            Win32API.GetClassName(handle, className, 256);

            if (_showNoTitle == false && title.Length == 0) {
                return true;
            }
            if (myHandles.Contains((IntPtr) handle)) {
                return true;
            }
            if (title.ToString().EndsWith("(Not Responding)")) {
                return true;
            }

            /*IntPtr style = GetWindowLongPtr((IntPtr) handle, -16); //-16 is GWL_STYLE
            if (((long) style & WS_POPUP) == WS_POPUP) {
                return true;
            }
            if (((long) style & WS_CAPTION) != WS_CAPTION) {
                return true;
            }
            if (((long)style & WS_THICKFRAME) != WS_THICKFRAME) {
                return true;
            }*/

            if (!ignoreClasses.Contains(className.ToString())) {
                windowList.Add(new Window(title.ToString(), (IntPtr) handle,
                                          module, className.ToString(), Win32API.IsWindowVisible(handle)));
            }

            return true;
        }

        private string getWindowModuleName(int handle) {
            uint processID;
            if (Win32API.GetWindowThreadProcessId(handle, out processID) > 0) {
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