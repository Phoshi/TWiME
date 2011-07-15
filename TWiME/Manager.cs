using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    static class Manager {
        static List<Window> windowList = new List<Window>();
        static HashSet<IntPtr> handles = new HashSet<IntPtr>();
        public static List<Monitor> monitors = new List<Monitor>();
        static globalKeyboardHook globalHook = new globalKeyboardHook();
        //static Dictionary<Keys, Action> hooked = new Dictionary<Keys, Action>();
        //static Dictionary<Keys, Action> shiftHooked = new Dictionary<Keys, Action>();
        //static Dictionary<Keys, Action> altHooked = new Dictionary<Keys, Action>();
        static Dictionary<Keys,Dictionary<Keys,Action>> hooked = new Dictionary<Keys, Dictionary<Keys, Action>>();

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        private static bool isWinKeyDown = false;
        private static bool isShiftKeyDown = false;
        private static bool isAltKeyDown = false;
        private static bool isControlKeyDown = false;
        public static void setup() {
            setupHotkeys();
            setupMonitors();
            setupTimers();

            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
        }

        static void Application_ApplicationExit(object sender, EventArgs e) {
            sendMessage(Message.Close, Level.screen, 0);
        }

        private static void setupHotkeys() {
            globalHook.HookedKeys.Add(Keys.LWin);
            globalHook.HookedKeys.Add(Keys.LShiftKey);
            globalHook.HookedKeys.Add(Keys.LMenu);
            hook(Keys.Q, (()=>Application.Exit()));
            hook(Keys.R, (()=>Application.Restart()));
            hook(Keys.J, (()=>sendMessage(Message.Focus, Level.screen, 1)));
            hook(Keys.K, (()=>sendMessage(Message.Focus, Level.screen, -1)));
            hook(Keys.J, (() => sendMessage(Message.Switch, Level.screen, 1)), Keys.Shift);
            hook(Keys.K, (() => sendMessage(Message.Switch, Level.screen, -1)), Keys.Shift);

            hook(Keys.J, (() => sendMessage(Message.MonitorSwitch, Level.monitor, 1)), Keys.Alt);
            hook(Keys.K, (() => sendMessage(Message.MonitorSwitch, Level.monitor, -1)), Keys.Alt);

            globalHook.KeyDown += hook_KeyDown;
            globalHook.KeyUp += new KeyEventHandler(globalHook_KeyUp);
        }

        static void sendMessage(Message type, Level level, int data) {
            IntPtr focussed = GetForegroundWindow();
            HotkeyMessage message = new HotkeyMessage(type, level, focussed, data);
            if (message.level == Level.global) {
                catchMessage(message);
            }
            else {
                getFocussedMonitor().catchMessage(message);
            }
        }

        private static void catchMessage(HotkeyMessage message) {
            throw new NotImplementedException();
        }

        static void globalHook_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.LWin) {
                isWinKeyDown = false;
            }
            if (e.KeyCode == Keys.LShiftKey) {
                isShiftKeyDown = false;
            }
            if (e.KeyCode == Keys.LMenu) {
                isAltKeyDown = false;
            }
            if (e.KeyCode == Keys.LControlKey) {
                isControlKeyDown = false;
            }
        }

        private static void hook(Keys key, Action response, Keys modifiers = Keys.None) {
            globalHook.HookedKeys.Add(key);
            if (!hooked.ContainsKey(modifiers)) {
                hooked[modifiers]=new Dictionary<Keys, Action>();
            }
            hooked[modifiers][key] = response;
        }

        static void hook_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.LWin) {
                isWinKeyDown = true;
                return;
            }
            if (e.KeyData == Keys.LShiftKey) {
                isShiftKeyDown = true;
                return;
            }
            if (e.KeyData == Keys.LMenu) {
                isAltKeyDown = true;
                return;
            }
            if (e.KeyData == Keys.LControlKey) {
                isControlKeyDown = true;
                return;
            }

            if (isWinKeyDown) {
                Keys modifier = Keys.None;
                if (isShiftKeyDown)
                    modifier |= Keys.Shift;
                if (isAltKeyDown)
                    modifier |= Keys.Alt;
                if (isControlKeyDown)
                    modifier |= Keys.Control;
                    e.Handled = true;
                if (hooked.ContainsKey(modifier)) {
                    if (hooked[modifier].ContainsKey(e.KeyCode)) {
                        hooked[modifier][e.KeyCode]();
                    }
                }
                e.Handled = true;


            }
        }

        private static void setupMonitors() {
            foreach (Screen screen in Screen.AllScreens) {
                Monitor monitor = new Monitor(screen);
                monitors.Add(monitor);
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

        public static int getFocussedMonitorIndex() {
            IntPtr handle = GetForegroundWindow();
            Screen screen = Screen.FromHandle(handle);
            int index = 0;
            foreach (Monitor monitor in monitors) {
                if (monitor.name == screen.DeviceName) {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static Monitor getFocussedMonitor() {
            return monitors[getFocussedMonitorIndex()];
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
