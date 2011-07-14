using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    static class Manager {
        static List<Window> windowList = new List<Window>();
        static HashSet<IntPtr> handles = new HashSet<IntPtr>();
        static List<Monitor> monitors = new List<Monitor>();

        public static void setup() {
            foreach (Window window in new Windows()) {
                windowList.Add(window);
            }
            setupMonitors();
            setupTimers();
        }
        private static void setupMonitors() {
            foreach (Screen screen in Screen.AllScreens) {
                Monitor monitor = new Monitor(screen);
            }
        }

        private static void setupTimers() {
                        Timer pollTimer = new Timer();
            pollTimer.Interval = 1000;
            pollTimer.Tick += pollWindows_Tick;
            pollTimer.Start();
        }

        static void pollWindows_Tick(object sender, EventArgs e) {
            Windows windows = new Windows();
            List<Window> allWindows = new List<Window>();
            foreach (Window window in windows) {
                if (!handles.Contains(window.handle)) {
                    windowList.Add(window);
                    handles.Add(window.handle);
                    OnWindowCreate(window, new WindowEventArgs(window.screen));
                }
                allWindows.Add(window);
            }
            if (allWindows.Count < windowList.Count) { //Something's been closed
                int numClosures = windowList.Count - allWindows.Count;
                int numFound = 0;
                HashSet<IntPtr> allHandles = new HashSet<IntPtr>(from window in allWindows select window.handle);
                foreach (Window window in new List<Window>(windowList)) {
                    if (!allHandles.Contains(window.handle)) {
                        windowList.Remove(window);
                        Screen windowScreen = Screen.FromHandle(window.handle);
                        OnWindowDestroy(window, new WindowEventArgs(window.screen));
                        if (++numFound == numClosures) {
                            break;
                        }
                    }
                }
            }
        }

        public delegate void WindowEventHandler(object sender, WindowEventArgs args);

        public static event WindowEventHandler WindowCreate;
        public static event WindowEventHandler WindowDestroy;
        private static void OnWindowCreate(object sender, WindowEventArgs args) {
            if (WindowCreate != null) {
                WindowCreate(sender, args);
            }
        }

        private static void OnWindowDestroy(object sender, WindowEventArgs args) {
            if (WindowDestroy != null) {
                WindowDestroy(sender, args);
            }
        }
    }
}
