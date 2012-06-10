using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TWiME {
    public static class Taskbar {
        private static IntPtr taskbar;
        public static IntPtr orb;
        private static bool _hidden;

        public static bool Hidden {
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


        static Taskbar() {
            IntPtr taskHandle = Win32API.FindWindow("Shell_TrayWnd", null);
            taskbar = taskHandle;
            int PID;
            Win32API.GetWindowThreadProcessId(taskHandle, out PID);
            Process taskbarProcess = Process.GetProcessById(PID);
            foreach (ProcessThread thread in taskbarProcess.Threads) {
                Win32API.EnumThreadWindows(thread.Id, EnumThreadWindowsProcessor, IntPtr.Zero);
            }
        }

        private static void Hide() {
            Win32API.ShowWindow(taskbar, Win32API.SW_HIDE);
            Win32API.ShowWindow(orb, Win32API.SW_HIDE);
            _hidden = true;
        }

        private static void Show() {
            Win32API.ShowWindow(taskbar, Win32API.SW_SHOW);
            Win32API.ShowWindow(orb, Win32API.SW_SHOW);
            _hidden = false;
        }

        private static bool EnumThreadWindowsProcessor(IntPtr hwnd, IntPtr lparam) {
            StringBuilder title = new StringBuilder(256);
            if (Win32API.GetWindowText(hwnd, title, 256) > 0) {
                string windowTitle = title.ToString();
                if (windowTitle == "Start") {
                    orb = hwnd;
                    return false;
                }
            }
            return true;
        }
    }
}