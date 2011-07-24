using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Extensions;
using HumanReadableSettings;

namespace TWiME {
    public static class Manager {
        private const int LOG_LEVEL = 99;

        private static List<Window> _windowList = new List<Window>();
        public static List<Window> Windows {
            get { return _windowList; }
        }
        public static List<Window> hiddenWindows = new List<Window>();
        private static HashSet<IntPtr> _handles = new HashSet<IntPtr>();
        public static List<Monitor> monitors = new List<Monitor>();
        private static globalKeyboardHook _globalHook = new globalKeyboardHook();

        private static Dictionary<Keys, Dictionary<Keys, Action>> hooked =
            new Dictionary<Keys, Dictionary<Keys, Action>>();

        public static List<Type> layouts = new List<Type>();
        private static StreamWriter logger;
        private static Settings userSettingsOverride = new Settings("_TWiMErc", true);
        public static Settings settings;
        public static Dictionary<WindowMatch, WindowRule> windowRules = new Dictionary<WindowMatch, WindowRule>();

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        private const UInt32 WM_KEYDOWN = 0x0100;
        private const UInt32 WM_KEYUP = 0x0101;
        private const int VK_LWIN = 0x5B;

        private static bool isWinKeyDown;
        private static bool isShiftKeyDown;
        private static bool isAltKeyDown;
        private static bool isControlKeyDown;

        public static void Setup() {
            Taskbar.hidden = true;
            bool settingsReadOnly =
                !Convert.ToBoolean(userSettingsOverride.ReadSettingOrDefault("false", "General.Main.AutoSave"));
            settings = new Settings("_runtimesettings", settingsReadOnly);
            settings.OverwriteWith(userSettingsOverride);
            setupWindowRules();
            setupHotkeys();
            setupLayouts();
            setupMonitors();
            setupTimers();
            logger = new StreamWriter("log.txt");

            Application.ApplicationExit += Application_ApplicationExit;
        }

        private static void setupWindowRules() {
            if (!settings.sections.Contains("Window Rules")) {
                return;
            }
            foreach (List<string> list in settings.KeysUnderSection("Window Rules")) {
                string winClass = list[1];
                string winTitle = list[2];
                string winRule = list[3];
                int value = Convert.ToInt32(settings.ReadSetting(list.ToArray()));
                WindowRules rule;
                if (WindowRules.TryParse(winRule, true, out rule)) {
                    WindowRule newRule = new WindowRule(rule, value);
                    WindowMatch newMatch = new WindowMatch(winClass, winTitle);
                    windowRules[newMatch] = newRule;
                }
            }
        }

        public static void CenterMouseOnActiveWindow() {
            bool moveMouse = Convert.ToBoolean(settings.ReadSettingOrDefault("false", "General.Main.MouseFollowsInput"));
            if (moveMouse) {
                IntPtr pointer = GetForegroundWindow();
                Window window = GetWindowObjectByHandle(pointer);
                if (window != null) {
                    Cursor.Position = window.Location.Center();
                }
            }
        }

        public static void Log(string toLog, int logLevel = 0) {
            if (logLevel >= LOG_LEVEL) {
                Console.WriteLine(toLog);
                logger.WriteLine(toLog);
                logger.Flush();
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e) {
            if (!settings.readOnly) {
                settings.save();
            }
        }

        private static void setupLayouts() {
            foreach (string file in Directory.GetFiles("Layouts", "*.dll")) {
                Assembly asm = Assembly.UnsafeLoadFrom(Path.Combine(Directory.GetCurrentDirectory(), file));
                foreach (Type type in asm.GetTypes()) {
                    if (type.BaseType == typeof(Layout)) {
                        layouts.Add(type);
                    }
                }
            }
        }

        public static int GetLayoutIndexFromName(string name) {
            int index = 0;
            foreach (Type layout in layouts) {
                if (name == layout.Name) {
                    return index;
                }
                index++;
            }
            return 0;
        }

        public static string GetLayoutNameFromIndex(int index) {
            return layouts[index].Name;
        }

        private static void setupHotkeys() {
            #region Hook Modifiers

            _globalHook.HookedKeys.Add(Keys.LWin);
            _globalHook.HookedKeys.Add(Keys.LShiftKey);
            _globalHook.HookedKeys.Add(Keys.LMenu);
            _globalHook.HookedKeys.Add(Keys.LControlKey);

            #endregion

            hook(Keys.Q, (() => SendMessage(Message.Close, Level.global, 0)));
            hook(Keys.Space, (() => SendMessage(Message.Switch, Level.global, 0)));


            hook(Keys.J, (() => SendMessage(Message.Focus, Level.screen, 1)));
            hook(Keys.K, (() => SendMessage(Message.Focus, Level.screen, -1)));
            hook(Keys.Return, (() => SendMessage(Message.FocusThis, Level.screen, 0)));
            hook(Keys.J, (() => SendMessage(Message.Switch, Level.screen, 1)), Keys.Shift);
            hook(Keys.K, (() => SendMessage(Message.Switch, Level.screen, -1)), Keys.Shift);
            hook(Keys.Return, (() => SendMessage(Message.SwitchThis, Level.screen, 0)), Keys.Shift);
            hook(Keys.C, (() => SendMessage(Message.Close, Level.window, 0)));


            hook(Keys.J, (() => SendMessage(Message.Monitor, Level.screen, 1)), Keys.Shift | Keys.Alt);
            hook(Keys.K, (() => SendMessage(Message.Monitor, Level.screen, -1)), Keys.Shift | Keys.Alt);
            hook(Keys.Return, (() => SendMessage(Message.MonitorMoveThis, Level.screen, 0)), Keys.Shift | Keys.Alt);

            hook(Keys.Left, (() => SendMessage(Message.Splitter, Level.screen, -1)));
            hook(Keys.Left, (() => SendMessage(Message.Splitter, Level.screen, -10)), Keys.Shift);
            hook(Keys.Right, (() => SendMessage(Message.Splitter, Level.screen, 1)));
            hook(Keys.Right, (() => SendMessage(Message.Splitter, Level.screen, 10)), Keys.Shift);
            hook(Keys.Up, (() => SendMessage(Message.VSplitter, Level.screen, -1)));
            hook(Keys.Up, (() => SendMessage(Message.VSplitter, Level.screen, -10)), Keys.Shift);
            hook(Keys.Down, (() => SendMessage(Message.VSplitter, Level.screen, 1)));
            hook(Keys.Down, (() => SendMessage(Message.VSplitter, Level.screen, 10)), Keys.Shift);

            hook(Keys.J, (() => SendMessage(Message.ScreenRelative, Level.monitor, 1)), Keys.Control);
            hook(Keys.K, (() => SendMessage(Message.ScreenRelative, Level.monitor, -1)), Keys.Control);
            hook(Keys.Return, (() => SendMessage(Message.ScreenRelative, Level.monitor, 0)), Keys.Control);
            hook(Keys.J, (() => SendMessage(Message.SwapTagWindowRelative, Level.monitor, 1)), Keys.Control | Keys.Shift);
            hook(Keys.K, (() => SendMessage(Message.SwapTagWindowRelative, Level.monitor, -1)),
                 Keys.Control | Keys.Shift);
            hook(Keys.Return, (() => SendMessage(Message.SwapTagWindow, Level.monitor, 0)), Keys.Control | Keys.Shift);

            hook(Keys.J, (() => SendMessage(Message.MonitorSwitch, Level.monitor, 1)), Keys.Alt);
            hook(Keys.K, (() => SendMessage(Message.MonitorSwitch, Level.monitor, -1)), Keys.Alt);
            hook(Keys.Return, (() => SendMessage(Message.MonitorFocus, Level.monitor, 0)), Keys.Alt);

            hook(Keys.Space,
                 (() => SendMessage(Message.LayoutRelative, Level.monitor, GetFocussedMonitor().GetActiveScreen().tag)),
                 Keys.Control);
            hook(Keys.Space,
                 (() =>
                  SendMessage(Message.LayoutRelativeReverse, Level.monitor, GetFocussedMonitor().GetActiveScreen().tag)),
                 Keys.Control | Keys.Shift);

            #region TagStuff

            int tagIndex = 0;
            foreach (Keys key in new[] {Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9}
                ) {
                int index = tagIndex++;
                hook(key, (() => SendMessage(Message.Screen, Level.monitor, index)), Keys.Control);
                hook(key, (() => SendMessage(Message.TagWindow, Level.monitor, index)), Keys.Shift | Keys.Control);
                hook(key, (() => SendMessage(Message.FocusThis, Level.screen, index)));
                hook(key, (() => SendMessage(Message.SwitchThis, Level.screen, index)), Keys.Shift);
                hook(key, (() => SendMessage(Message.MonitorMoveThis, Level.screen, index)), Keys.Shift | Keys.Alt);
                hook(key, (() => SendMessage(Message.MonitorFocus, Level.monitor, index)), Keys.Alt);
            }

            #endregion

            _globalHook.KeyDown += hook_KeyDown;
            _globalHook.KeyUp += globalHook_KeyUp;
        }

        public static void SendMessage(Message type, Level level, int data) {
            IntPtr focussed = GetForegroundWindow();
            HotkeyMessage message = new HotkeyMessage(type, level, focussed, data);
            if (message.level == Level.global) {
                CatchMessage(message);
            }
            else {
                GetFocussedMonitor().CatchMessage(message);
            }
        }

        private static void CatchMessage(HotkeyMessage message) {
            if (message.message == Message.Close) {
                Manager.Log("Beginning shutdown loop", 10);
                foreach (Window window in _windowList) {
                    Manager.Log("Setting {0} visible and not maximised".With(window), 10);
                    window.Visible = true;
                    window.Maximised = false;
                }
                Manager.Log("Showing taskbar", 10);
                Taskbar.hidden = false;
                Application.Exit();
            }
            if (message.message == Message.Restart) {
                Manager.Log("Beginning shutdown loop", 10);
                foreach (Window window in _windowList) {
                    Manager.Log("Setting {0} visible and not maximised".With(window), 10);
                    window.Visible = true;
                    window.Maximised = false;
                }
                Manager.Log("Showing taskbar", 10);
                Taskbar.hidden = false;
                Application.Restart();
            }
            if (message.message == Message.Switch) {
                Manager.Log("Toggling taskbar");
                Taskbar.hidden = !Taskbar.hidden;
            }
        }

        private static void globalHook_KeyUp(object sender, KeyEventArgs e) {
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
            _globalHook.HookedKeys.Add(key);
            if (!hooked.ContainsKey(modifiers)) {
                hooked[modifiers] = new Dictionary<Keys, Action>();
            }
            hooked[modifiers][key] = response;
        }

        private static void hook_KeyDown(object sender, KeyEventArgs e) {
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
                if (isShiftKeyDown) {
                    modifier |= Keys.Shift;
                }
                if (isAltKeyDown) {
                    modifier |= Keys.Alt;
                }
                if (isControlKeyDown) {
                    modifier |= Keys.Control;
                }
                e.Handled = true;
                if (hooked.ContainsKey(modifier)) {
                    if (hooked[modifier].ContainsKey(e.KeyCode)) {
                        hooked[modifier][e.KeyCode]();
                    }
                }
                e.Handled = true;
            }
        }


        private static bool executedACommandLately;

        public static bool IsModifierPressed(Keys key, bool keyDown) {
            Manager.Log("==============isModifierPressed running for {0} (KeyDown is {1})".With(key, keyDown), 3);
            Keys modifier = Keys.None;
            if (key == Keys.LWin && keyDown) {
                Manager.Log("Key is LWin, and we're pressing it. Return.", 3);
                return true;
            }
            if (key == Keys.LWin) {
                Manager.Log("Key is LWin, and it's coming back up", 3);
                if (executedACommandLately) {
                    Log("We've executed a command this run. Return.", 3);
                    executedACommandLately = false;
                    PostMessage((IntPtr) 0xFFFF, WM_KEYDOWN, VK_LWIN, 0); //0xFFFF is HWND_BROADCAST - everything.
                    PostMessage((IntPtr) 0xFFFF, WM_KEYUP, VK_LWIN, 0); //0xFFFF is HWND_BROADCAST - everything.
                    return true;
                }
                Log("We haven't executed a command this run. Return", 3);
                return false;
            }

            if (isWinKeyDown) {
                Log("Windows key state is down - this could be a hotkey", 3);
                if (isShiftKeyDown) {
                    Log("Shift key is down", 3);
                    modifier |= Keys.Shift;
                }
                if (isAltKeyDown) {
                    Log("Alt key is down", 3);
                    modifier |= Keys.Alt;
                }
                if (isControlKeyDown) {
                    Log("Control key is down", 3);
                    modifier |= Keys.Control;
                }
                if (hooked.ContainsKey(modifier)) {
                    Log("This modifier combination is a thing", 3);
                    if (hooked[modifier].ContainsKey(key)) {
                        Manager.Log("Blocking Key{2} {1}+{0}.".With(key, modifier, keyDown ? "down" : "up"), 3);
                        executedACommandLately = true;
                        return true;
                    }
                }
            }
            Manager.Log("Allowing Key{2} {1}+{0}".With(key, modifier, keyDown ? "down" : "up"), 3);
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
            pollTimer.Interval = Convert.ToInt32(Manager.settings.ReadSettingOrDefault("1000", "General.Main.Poll"));
            pollTimer.Tick += pollWindows_Tick;
            pollTimer.Start();
        }

        private static IntPtr focusTrack = IntPtr.Zero;
        private static int checkCount;
        private static void pollWindows_Tick(object sender, EventArgs e) {
            _globalHook.unhook();
            _globalHook.hook();

            if (++checkCount % 10 == 0) {
                foreach (Monitor monitor in monitors) {
                    monitor.GetActiveScreen().AssertLayout();
                }
            }

            if (GetForegroundWindow() != focusTrack) {
                focusTrack = GetForegroundWindow();
                OnWindowFocusChange(GetWindowObjectByHandle(focusTrack),
                                    new WindowEventArgs(Manager.GetFocussedMonitor().Screen));
            }
            Windows windows = new Windows();
            List<Window> allCurrentlyVisibleWindows = new List<Window>();
            List<Window> hiddenNotShownByMeWindows = new List<Window>();
            foreach (Window window in windows) {
                bool windowIgnored = false;
                if ((from win in hiddenWindows select win.handle).Contains(window.handle)) {
                    hiddenNotShownByMeWindows.Add(window);
                }
                if (!_handles.Contains(window.handle)) {
                    Manager.Log("Found a new window! {0} isn't in the main listing".With(window.Title));
                    foreach (KeyValuePair<WindowMatch, WindowRule> kvPair in windowRules) {
                        if (kvPair.Key.windowMatches(window)) {
                            WindowRule rule = kvPair.Value;
                            if (rule.rule == WindowRules.ignore) {
                                windowIgnored = true;
                            }
                        }
                    }
                    if (windowIgnored) {
                        continue;
                    }
                    _windowList.Add(window);
                    _handles.Add(window.handle);
                    OnWindowCreate(window, new WindowEventArgs(window.Screen));
                }
                allCurrentlyVisibleWindows.Add(window);
            }
            foreach (Window window in hiddenNotShownByMeWindows) {
                window.Activate();
                Window window1 = window;
                var screensWithWindow =
                    (from screen in Manager.GetFocussedMonitor().screens
                     where screen.windows.Contains(window1)
                     select screen);
                TagScreen firstScreenWithWindow = screensWithWindow.First();
                SendMessage(Message.Screen, Level.monitor, firstScreenWithWindow.tag);
            }
            foreach (Window window in new List<Window>(_windowList)) {
                if (!allCurrentlyVisibleWindows.Contains(window)) {
                    Manager.Log("{0} is no longer open".With(window.Title), 1);
                    if (!hiddenWindows.Contains(window)) {
                        Manager.Log("{0} is also not hidden - it's closed".With(window.Title));
                        _windowList.Remove(window);
                        _handles.Remove(window.handle);
                        //Screen windowScreen = Screen.FromHandle(window.handle);
                        OnWindowDestroy(window, new WindowEventArgs(window.Screen));
                    }
                    else {
                        Manager.Log("{0} is just hidden, not closed".With(window.Title), 1);
                    }
                }
            }
        }

        public static int GetFocussedMonitorIndex() {
            IntPtr handle = GetForegroundWindow();
            Screen screen = Screen.FromHandle(handle);
            int index = 0;
            foreach (Monitor monitor in monitors) {
                if (monitor.Name == screen.DeviceName) {
                    return index;
                }
                index++;
            }
            return -1;
        }

        public static Monitor GetFocussedMonitor() {
            return monitors[GetFocussedMonitorIndex()];
        }

        public static Window GetWindowObjectByHandle(IntPtr handle) {
            return _windowList.FirstOrDefault(window => window.handle == handle);
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