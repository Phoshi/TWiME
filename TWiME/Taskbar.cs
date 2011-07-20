using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TWiME {
    public static class Taskbar {
        private static IntPtr taskbar;
        public static IntPtr orb;
        private static bool _hidden;

        public static bool hidden {
            get { return _hidden; }
            set {
                if (value) {
                    Hide();
                }
                else {
                    Show();
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern
            int GetWindowText(IntPtr hWnd, StringBuilder title, int size);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        public delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;


        static Taskbar() {
            IntPtr taskHandle = FindWindow("Shell_TrayWnd", null);
            taskbar = taskHandle;
            int PID;
            GetWindowThreadProcessId(taskHandle, out PID);
            Process taskbarProcess = Process.GetProcessById(PID);
            foreach (ProcessThread thread in taskbarProcess.Threads) {
                EnumThreadWindows(thread.Id, EnumThreadWindowsProcessor, IntPtr.Zero);
            }
        }

        private static bool EnumThreadWindowsProcessor(IntPtr hwnd, IntPtr lparam) {
            StringBuilder title = new StringBuilder(256);
            if (GetWindowText(hwnd, title, 256) > 0) {
                string windowTitle = title.ToString();
                if (windowTitle == "Start") {
                    orb = hwnd;
                    return false;
                }
            }
            return true;
        }

        private static void Hide() {
            ShowWindow(taskbar, SW_HIDE);
            ShowWindow(orb, SW_HIDE);
            _hidden = true;
        }

        private static void Show() {
            ShowWindow(taskbar, SW_SHOW);
            ShowWindow(orb, SW_SHOW);
            _hidden = false;
        }
    }
}