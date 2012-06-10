using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    public class TagScreen {
        private List<Window> _windowList = new List<Window>();

        public List<Window> windows {
            get { return _windowList; }
        }

        private ILayout layout;
        private readonly Monitor _parent;
        private Rectangle _controlled;
        private readonly int _tag;
        public int activeLayout;

        public int tag {
            get { return _tag; }
        }

        public Monitor parent {
            get { return _parent; }
        }

        public TagScreen(Monitor parent, int tag) {
            activeLayout =
                Manager.GetLayoutIndexFromName(Manager.settings.ReadSettingOrDefault("DefaultLayout",
                                                                                     parent.SafeName, (tag).ToString(),
                                                                                     "DefaultLayout"));
            _parent = parent;
            _tag = tag;
            _controlled = _parent.Controlled;
            InitLayout();
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy += Manager_WindowDestroy;
        }

        public void UpdateControlledArea(Rectangle newArea = new Rectangle()) {
            if (newArea.IsEmpty) {
                _controlled = _parent.Controlled;
            }
            else {
                _controlled = newArea;
            }
            InitLayout();
            if (_parent.IsTagEnabled(_tag)) {
                AssertLayout();
            }
        }

        public void InitLayout() {
            Manager.settings.AddSetting(Manager.GetLayoutNameFromIndex(activeLayout),
                                        parent.SafeName, (_tag).ToString(), "DefaultLayout");
            Layout instance =
                (Layout)
                Activator.CreateInstance(Manager.layouts[activeLayout],
                                         new object[] {_windowList, _controlled, this});
            layout = instance;
        }

        public Image GetLayoutSymbol(Size size) {
            List<Window> tempWindowList = new List<Window>();
            for (int i = 0; i < 5; i++) { //5 seems like a good number
                Window tempWindow = new Window("", (IntPtr) i, "", "", true); //The window object will never /do/ anything so it doesn't need a real handle, just a unique one.
                tempWindowList.Add(tempWindow);
            }
            layout.UpdateWindowList(tempWindowList);
            Image layoutSymbol = layout.StateImage(size);
            layout.UpdateWindowList(_windowList);
            return layoutSymbol;
        }

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Window newWindow = (Window) sender;
            IEnumerable<Window> deleteList =
                (from window in _windowList where window.handle == newWindow.handle select window);
            if (deleteList.Count() > 0) {
                Window toRemove = deleteList.First();
                _windowList.Remove(toRemove);
                Manager.Log("Removing window: {0} {1}".With(toRemove.ClassName, toRemove.Title), 1);
                layout.UpdateWindowList(_windowList);
                if (parent.IsTagEnabled(tag)) {
                    AssertLayout();
                }
            }
        }

        private void Manager_WindowCreate(object sender, WindowEventArgs args) {
            bool rulesThisMonitor = false;
            int monitorPosition =
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(-1, "General.Monitor.DefaultMonitor"));
            List<int> tagsToOpenOn = new List<int>();
            foreach (KeyValuePair<WindowMatch, WindowRule> keyValuePair in Manager.windowRules) {
                if (keyValuePair.Key.windowMatches((Window) sender)) {
                    if (keyValuePair.Value.rule == WindowRules.monitor) {
                        if (keyValuePair.Value.data < Manager.monitors.Count && keyValuePair.Value.data >= 0) {
                            if (Manager.monitors[keyValuePair.Value.data].Name == _parent.Name) {
                                rulesThisMonitor = true;
                            }
                            else {
                                return;
                            }
                        }
                    }
                    if (keyValuePair.Value.rule == WindowRules.tag) {
                        if (_parent.screens.Count() >= keyValuePair.Value.data) {
                            tagsToOpenOn.Add(keyValuePair.Value.data - 1);
                        }
                    }
                }
            }
            string monitorNameToOpenOn;
            if (monitorPosition == -1) { //no preference, grab the monitor the window opened on
                monitorNameToOpenOn = args.monitor.DeviceName;
            }
            else {
                monitorNameToOpenOn = Manager.monitors[monitorPosition].Screen.DeviceName;
            }
            if (!tagsToOpenOn.Contains(_tag) && tagsToOpenOn.Count > 0) {
                return;
            }
            Window newWindow = (Window) sender;
            if ((monitorNameToOpenOn == _parent.Name || rulesThisMonitor) &&
                (_parent.IsTagEnabled(_tag) || tagsToOpenOn.Contains(_tag))) {
                if (_parent.GetActiveTag() == _tag || tagsToOpenOn.Contains(_tag)) {
                    CatchWindow(newWindow);
                }
                Manager.Log("Adding Window: " + newWindow.ClassName + " " + newWindow, 1);
                layout.UpdateWindowList(_windowList);
                if (_parent.IsTagEnabled(_tag)) {
                    AssertLayout();
                }
                else if ((from tag in tagsToOpenOn where _parent.GetFocussedTag() == tag select tag).Count() == 0) {
                    newWindow.Visible = false;
                }
            }
        }

        ~TagScreen() {
            foreach (Window window in _windowList) {
                window.Visible = true;
                window.Maximised = false;
            }
        }

        public int GetFocusedWindowIndex() {
            IntPtr hWnd = Win32API.GetForegroundWindow();
            for (int i = 0; i < _windowList.Count; i++) {
                if (_windowList[i].handle == hWnd) {
                    return i;
                }
            }
            return -1;
        }

        public void CatchMessage(HotkeyMessage message) {
            if (message.level == Level.Screen) {
                if (message.Message == Message.Focus) {
                    if (_windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.Name) {
                        int newIndex = GetFocusedWindowIndex() + message.data;
                        if (newIndex >= _windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = _windowList.Count - 1;
                        }
                        Console.WriteLine(newIndex);
                        _windowList[newIndex].Activate();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.Message == Message.Switch) {
                    if (_windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.Name) {
                        int oldIndex = GetFocusedWindowIndex();
                        int newIndex = oldIndex + message.data;
                        if (newIndex >= _windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = _windowList.Count - 1;
                        }
                        if (oldIndex >= 0) {
                            Window oldWindow = _windowList[oldIndex];
                            Window newWindow = _windowList[newIndex];
                            _windowList[oldIndex] = newWindow;
                            _windowList[newIndex] = oldWindow;
                            AssertLayout();
                            Manager.CenterMouseOnActiveWindow();
                        }
                    }
                }
                if (message.Message == Message.SwitchThis) {
                    int oldIndex = GetFocusedWindowIndex();
                    if (oldIndex == -1) {
                        oldIndex = 0;
                    }
                    int newIndex = message.data;
                    Window newWindow, oldWindow;
                    try {
                        oldWindow = _windowList[oldIndex];
                        newWindow = _windowList[newIndex];
                    }
                    catch (ArgumentOutOfRangeException) {
                        return;
                    }
                    _windowList[oldIndex] = newWindow;
                    _windowList[newIndex] = oldWindow;
                    AssertLayout();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.Message == Message.FocusThis) {
                    if (message.data < _windowList.Count) {
                        _windowList[message.data].Activate();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.Message == Message.Monitor) {
                    int newMonitorIndex = Manager.GetFocussedMonitorIndex() + message.data;
                    if (newMonitorIndex < 0) {
                        newMonitorIndex = Manager.monitors.Count - 1;
                    }
                    else if (newMonitorIndex >= Manager.monitors.Count) {
                        newMonitorIndex = 0;
                    }
                    Monitor newMonitor = Manager.monitors[newMonitorIndex];
                    Window focussedWindow = GetFocusedWindow();
                    newMonitor.CatchWindow(this.ThrowWindow(focussedWindow));
                    _parent.screens.Where(screen => screen.windows.Contains(focussedWindow)).ToList().ForEach(screen => screen._windowList.Remove(focussedWindow));
                    AssertLayout();
                    newMonitor.GetActiveScreen().Enable();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.Message == Message.MonitorMoveThis) {
                    Manager.monitors[message.data].CatchWindow(ThrowWindow(GetFocusedWindow()));
                    AssertLayout();
                    Manager.monitors[message.data].GetActiveScreen().Activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.Message == Message.Splitter) {
                    layout.MoveSplitter(message.data / 100.0f);
                    Manager.settings.AddSetting(layout.GetSplitter(), parent.SafeName,
                                                (_tag).ToString(), "Splitter");
                }
                if (message.Message == Message.VSplitter) {
                    layout.MoveSplitter(message.data / 100.0f, true);
                    Manager.settings.AddSetting(layout.GetSplitter(true), parent.SafeName,
                                                (_tag).ToString(), "VSplitter");
                }
                if (message.Message == Message.Close) {
                    foreach (Window window in _windowList) {
                        window.Visible = true;
                        window.Maximised = false;
                    }
                }
                if (message.Message == Message.Close) {
                    _windowList[message.data].Close();
                }
            }
            else {
                GetFocusedWindow().CatchMessage(message);
                AssertLayout();
            }
        }

        public Window ThrowWindow(Window window) {
            _windowList.Remove(window);
            return window;
        }

        public Window GetFocusedWindow() {
            int index = GetFocusedWindowIndex();
            if (index == -1) {
                return new Window("", parent.Bar.Handle, "", "", true);
            }
            return _windowList[index];
        }

        public void Activate() {
            if (_windowList.Count > 0) {
                _windowList[0].Activate();
            }
            else {
                new Window("", _parent.Bar.Handle, "", "", true).Activate();
            }
        }

        public void CatchWindow(Window window) {
            int stackPosition =
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(0, _parent.SafeName, "DefaultStackPosition"));
            foreach (KeyValuePair<WindowMatch, WindowRule> kvPair in Manager.windowRules) {
                if (kvPair.Key.windowMatches(window)) {
                    if (kvPair.Value.rule == WindowRules.stack) {
                        stackPosition = kvPair.Value.data;
                    }
                }
            }
            if (stackPosition < 0) {
                stackPosition = _windowList.Count - stackPosition;
            }
            if (stackPosition > _windowList.Count) {
                stackPosition = _windowList.Count - 1;
            }
            if (stackPosition < 0) {
                stackPosition = 0;
            }
            _windowList.Insert(stackPosition, window);
            if (_parent.IsTagEnabled(tag)) {
                window.Visible = true;
            }
        }

        public Image getStateImage(Size previewSize) {
            layout.UpdateWindowList(_windowList);
            return layout.StateImage(previewSize);
        }

        public void Disable() {
            foreach (Window window in windows) {
                window.Visible = false;
            }
        }

        public void Disable(TagScreen swappingWith) {
            foreach (Window window in windows) {
                if (!swappingWith.windows.Contains(window)) {
                    window.Visible = false;
                }
            }
        }

        public void Enable() {
            foreach (Window window in windows) {
                window.Visible = true;
            }
            AssertLayout();
            string wallpaperPath = Manager.settings.ReadSettingOrDefault("", _parent.SafeName,
                (_tag).ToString(), "Wallpaper");
            if (wallpaperPath != "") {
                Thread wallThread = new Thread((() => Manager.SetWallpaper(wallpaperPath)));
                wallThread.Start();
            }

            bool toggleTaskbar = bool.Parse(Manager.settings.ReadSettingOrDefault("false", "General.Main.ShowTaskbarOnEmptyTags"));
            if (toggleTaskbar) {
                if (windows.Count == 0) {
                    Taskbar.Hidden = false;
                }
                else {
                    Taskbar.Hidden = true;
                }
            }
        }

        public void AssertLayout() {
            List<Window> viableWindowList = (from window in _windowList where window.TilingType == WindowTilingType.Normal select window).ToList();
            layout.UpdateWindowList(viableWindowList);
            layout.Assert();

            foreach (Window window in _windowList.Where(window => window.TilingType == WindowTilingType.FullTag)) {
                window.Location = _controlled;
            }

            foreach (Window window in _windowList.Where(window => window.TilingType == WindowTilingType.FullScreen)) {
                window.Location = _parent.Controlled;
            }
        }

        public void Disown() {
            foreach (Window window in _windowList) {
                Manager.DisownWindow(window);
            }
            _windowList = new List<Window>();
            Manager.WindowCreate -= Manager_WindowCreate;
            Manager.WindowDestroy -= Manager_WindowDestroy;
            layout.UpdateWindowList(_windowList);
        }
    }
}