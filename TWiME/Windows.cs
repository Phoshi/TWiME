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
    class Windows : IEnumerable, IEnumerator {
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
        static extern uint GetWindowThreadProcessId(int hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        private const long WS_POPUP = 0x80000000L;
        private const long WS_CAPTION = 0x00C00000L;

        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        private int _position = -1;

        List<Window> windowList = new List<Window>();

        private bool _showInvisible = false;
        private bool _showNoTitle = false;

        private List<IntPtr> myHandles = new List<IntPtr>();

        private string[] ignoreClasses = new[] {"Progman", "Button"};

        public Windows(bool showInvisible = false, bool showNoTitle = false) {
            _showNoTitle = showNoTitle;
            _showInvisible = showInvisible;

            foreach (Form openForm in Application.OpenForms) {
                myHandles.Add(openForm.Handle);
            }

            enumerateWindows();
        }
        private void enumerateWindows() {
            EnumWindowsProc callback = new EnumWindowsProc(processWindow);
            EnumWindows(callback, 0);
        }

        private bool processWindow(int handle, int lparam) {
            if (_showInvisible == false && !IsWindowVisible(handle))
                return (true);

            StringBuilder title = new StringBuilder(256);
            StringBuilder className = new StringBuilder(256);

            string module = getWindowModuleName(handle);
            GetWindowText(handle, title, 256);
            GetClassName(handle, className, 256);

            if (_showNoTitle == false && title.Length == 0)
                return true;
            if (myHandles.Contains((IntPtr)handle)) {
                return true;
            }
            IntPtr style = GetWindowLongPtr((IntPtr) handle, -16); //-16 is GWL_STYLE
            if (((long)style & WS_POPUP) == WS_POPUP) {
                return true;
            }
            if (((long)style & WS_CAPTION) != WS_CAPTION) {
                return true;
            }

            if (!ignoreClasses.Contains(className.ToString())) {
                windowList.Add(new Window(title.ToString(), (IntPtr) handle,
                                          module.ToString(), className.ToString()));
            }

            return true;
        }

        private string getWindowModuleName(int handle) {
            uint processID;
            if (GetWindowThreadProcessId(handle, out processID) > 0) {
                try {
                    return Process.GetProcessById((int)processID).MainModule.FileName;
                }
                catch (Win32Exception) {
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
            if (_position < windowList.Count) {
                return true;
            }
            else {
                return false;
            }
        }

        public void Reset() {
            _position = -1;
        }

        public object Current {
            get { return windowList[_position]; }
        }
    }
}
