using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    static class Manager {

        const int LOG_LEVEL = 3;

        static List<Window> windowList = new List<Window>();
        public static List<Window> hiddenWindows = new List<Window>();
        static HashSet<IntPtr> handles = new HashSet<IntPtr>();
        public static List<Monitor> monitors = new List<Monitor>();
        static globalKeyboardHook globalHook = new globalKeyboardHook();
        static Dictionary<Keys,Dictionary<Keys,Action>> hooked = new Dictionary<Keys, Dictionary<Keys, Action>>();
        private static StreamWriter logger;

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowText);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        enum GetWindow_Cmd : uint {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        private static bool isWinKeyDown = false;
        private static bool isShiftKeyDown = false;
        private static bool isAltKeyDown = false;
        private static bool isControlKeyDown = false;
        public static void setup() {
            Taskbar.hidden = true;
            setupHotkeys();
            setupMonitors();
            setupTimers();
            logger = new StreamWriter("log.txt");

            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
        }

        public static void log(string toLog, int logLevel = 0) {
            if (logLevel >= LOG_LEVEL) {
                Console.WriteLine(toLog);
                logger.WriteLine(toLog);
                logger.Flush();
            }
        }

        static void Application_ApplicationExit(object sender, EventArgs e) {
        }

        private static void setupHotkeys() {

            #region Hook Modifiers
            globalHook.HookedKeys.Add(Keys.LWin);
            globalHook.HookedKeys.Add(Keys.LShiftKey);
            globalHook.HookedKeys.Add(Keys.LMenu);
            globalHook.HookedKeys.Add(Keys.LControlKey);
            #endregion
            hook(Keys.Q, (()=>sendMessage(Message.Close, Level.global, 0)));
            hook(Keys.R, (()=>Application.Restart()));
            hook(Keys.Space, (()=>sendMessage(Message.Switch, Level.global, 0)));


            hook(Keys.J, (()=>sendMessage(Message.Focus, Level.screen, 1)));
            hook(Keys.K, (()=>sendMessage(Message.Focus, Level.screen, -1)));
            hook(Keys.Return, (()=>sendMessage(Message.FocusThis, Level.screen, 0)));
            hook(Keys.J, (() => sendMessage(Message.Switch, Level.screen, 1)), Keys.Shift);
            hook(Keys.K, (() => sendMessage(Message.Switch, Level.screen, -1)), Keys.Shift);
            hook(Keys.Return, (() => sendMessage(Message.SwitchThis, Level.screen, 0)), Keys.Shift);
            hook(Keys.C, (() => sendMessage(Message.Close, Level.window, 0)));


            hook(Keys.J, (() => sendMessage(Message.Monitor, Level.screen, 1)), Keys.Shift | Keys.Alt);
            hook(Keys.K, (() => sendMessage(Message.Monitor, Level.screen, -1)), Keys.Shift | Keys.Alt);
            hook(Keys.Return, (() => sendMessage(Message.MonitorMoveThis, Level.screen, 0)), Keys.Shift | Keys.Alt);

            hook(Keys.Left, (() => sendMessage(Message.Splitter, Level.screen, -1)));
            hook(Keys.Left, (() => sendMessage(Message.Splitter, Level.screen, -10)), Keys.Shift);
            hook(Keys.Right, (() => sendMessage(Message.Splitter, Level.screen, 1)));
            hook(Keys.Right, (() => sendMessage(Message.Splitter, Level.screen, 10)), Keys.Shift);

            hook(Keys.J, (() => sendMessage(Message.ScreenRelative, Level.monitor, 1)), Keys.Control);
            hook(Keys.K, (() => sendMessage(Message.ScreenRelative, Level.monitor, -1)), Keys.Control);
            hook(Keys.Return, (() => sendMessage(Message.ScreenRelative, Level.monitor, 0)), Keys.Control);
            hook(Keys.J, (() => sendMessage(Message.SwapTagWindowRelative, Level.monitor, 1)), Keys.Control | Keys.Shift);
            hook(Keys.K, (() => sendMessage(Message.SwapTagWindowRelative, Level.monitor, -1)), Keys.Control | Keys.Shift);
            hook(Keys.Return, (() => sendMessage(Message.SwapTagWindow, Level.monitor, 0)), Keys.Control | Keys.Shift);

            hook(Keys.J, (() => sendMessage(Message.MonitorSwitch, Level.monitor, 1)), Keys.Alt);
            hook(Keys.K, (() => sendMessage(Message.MonitorSwitch, Level.monitor, -1)), Keys.Alt);
            hook(Keys.Return, (() => sendMessage(Message.MonitorFocus, Level.monitor, 0)), Keys.Alt);

            #region TagStuff

            int tagIndex = 0;
            foreach (Keys key in new[] {Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9}) {
                int index = tagIndex++;
                hook(key, (() => sendMessage(Message.Screen, Level.monitor, index)), Keys.Control);
                hook(key, (() => sendMessage(Message.TagWindow, Level.monitor, index)), Keys.Shift | Keys.Control);   
            }
            #endregion

            globalHook.KeyDown += hook_KeyDown;
            globalHook.KeyUp += new KeyEventHandler(globalHook_KeyUp);
        }

        public static void sendMessage(Message type, Level level, int data) {
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
            if (message.message == Message.Close) {
                Manager.log("Beginning shutdown loop", 10);
                foreach (Window window in windowList) {
                    Manager.log("Setting {0} visible and not maximised".With(window), 10);
                    window.visible = true;
                    window.maximised = false;
                }
                Manager.log("Showing taskbar", 10);
                Taskbar.hidden = false;
                Application.Exit();

            }
            if (message.message == Message.Switch) {
                Manager.log("Toggling taskbar");
                Taskbar.hidden = !Taskbar.hidden;
            }
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

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
        const UInt32 WM_KEYDOWN = 0x0100;
        const UInt32 WM_KEYUP = 0x0101;
        const int VK_LWIN = 0x5B;
        const int VK_ESCAPE = 0x1B;


        private static bool executedACommandLately = false;
        public static bool isModifierPressed(Keys key, bool keyDown) {
            Manager.log("==============isModifierPressed running for {0} (KeyDown is {1})".With(key, keyDown), 3);
            Keys modifier = Keys.None;
            if (key == Keys.LWin && keyDown) {
                Manager.log("Key is LWin, and we're pressing it. Return.",3);
                return true;
            }
            else if (key == Keys.LWin) {
                Manager.log("Key is LWin, and it's coming back up", 3);
                if (executedACommandLately) {
                    log("We've executed a command this run. Return.", 3);
                    executedACommandLately = false;
                    PostMessage((IntPtr) 0xFFFF, WM_KEYDOWN, VK_LWIN, 0); //0xFFFF is HWND_BROADCAST - everything.
                    PostMessage((IntPtr) 0xFFFF, WM_KEYUP, VK_LWIN, 0); //0xFFFF is HWND_BROADCAST - everything.
                    return true;
                }
                log("We haven't executed a command this run. Return", 3);
                return false;
            }

            if (isWinKeyDown) {
                log("Windows key state is down - this could be a hotkey", 3);
                if (isShiftKeyDown) {
                    log("Shift key is down", 3);
                    modifier |= Keys.Shift;
                }
                if (isAltKeyDown) {
                    log("Alt key is down", 3);
                    modifier |= Keys.Alt;
                }
                if (isControlKeyDown) {
                    log("Control key is down", 3);
                    modifier |= Keys.Control;
                }
                if (hooked.ContainsKey(modifier)) {
                    log("This modifier combination is a thing", 3);
                    if (hooked[modifier].ContainsKey(key)) {
                        Manager.log("Blocking Key{2} {1}+{0}.".With(key, modifier, keyDown ? "down" : "up"), 3);
                        executedACommandLately = true;
                        return true;
                    }
                }
            }
            Manager.log("Allowing Key{2} {1}+{0}".With(key, modifier, keyDown ? "down" : "up"), 3);
            return false;
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

        private static IntPtr focusTrack = IntPtr.Zero;
        static void pollWindows_Tick(object sender, EventArgs e) {
            globalHook.unhook();
            globalHook.hook();

            if (GetForegroundWindow() != focusTrack) {
                focusTrack = GetForegroundWindow();
                OnWindowFocusChange(getWindowObjectByHandle(focusTrack), new WindowEventArgs(Manager.getFocussedMonitor().screen));
            }
            Windows windows = new Windows();
            List<Window> allWindows = new List<Window>();
            foreach (Window window in windows) {
                if (!handles.Contains(window.handle)) {
                    Manager.log("Found a new window! {0} isn't in the main listing".With(window.title));
                    windowList.Add(window);
                    handles.Add(window.handle);
                    OnWindowCreate(window, new WindowEventArgs(window.screen));
                }
                allWindows.Add(window);
            }
            //if (allWindows.Count < windowList.Count) { //Something's been closed?
                int numClosures = windowList.Count - allWindows.Count;
                Manager.log("Detecting {0} window closure{1}".With(numClosures, numClosures==1? "s" : ""), 1);
                int numFound = 0;
                HashSet<IntPtr> allHandles = new HashSet<IntPtr>(from window in allWindows select window.handle);
                foreach (Window window in new List<Window>(windowList)) {
                    if (!allHandles.Contains(window.handle)) {
                        Manager.log("{0} is no longer open".With(window.title), 1);
                        if (!hiddenWindows.Contains(window)) {
                            Manager.log("{0} is also not hidden - it's closed".With(window.title));
                            windowList.Remove(window);
                            handles.Remove(window.handle);
                            //Screen windowScreen = Screen.FromHandle(window.handle);
                            OnWindowDestroy(window, new WindowEventArgs(window.screen));
                        }
                        else {
                            Manager.log("{0} is just hidden, not closed".With(window.title), 1);
                        }
                    }
                }
            //}
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

        public static IntPtr getPreviousFocussedWindowHandle() {
            return GetWindow(Manager.GetForegroundWindow(), GetWindow_Cmd.GW_HWNDNEXT);
        }
        public static Window getWindowObjectByHandle(IntPtr handle) {
            foreach (Window window in windowList) {
                if (window.handle == handle) {
                    return window;
                }
            }
            return null;
        }

        public delegate void WindowEventHandler(object sender, WindowEventArgs args);

        public static event WindowEventHandler WindowCreate;
        public static event WindowEventHandler WindowDestroy;
        public static event WindowEventHandler WindowFocusChange;
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
        public static void OnWindowFocusChange(object sender, WindowEventArgs args) {
            if (WindowFocusChange != null) {
                WindowFocusChange(sender, args);
            }
        }
    }
}
