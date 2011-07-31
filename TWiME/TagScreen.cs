using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
                                                                                     parent.Screen.DeviceName.Replace(
                                                                                         ".", ""), tag.ToString(),
                                                                                     "DefaultLayout"));
            _parent = parent;
            _tag = tag;
            initLayout();
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy += Manager_WindowDestroy;
        }

        public void initLayout() {
            if (!Manager.settings.readOnly) {
                Manager.settings.AddSetting(Manager.GetLayoutNameFromIndex(activeLayout),
                                            parent.Screen.DeviceName.Replace(".", ""), _tag.ToString(), "DefaultLayout");
            }
            Layout instance =
                (Layout)
                Activator.CreateInstance(Manager.layouts[activeLayout],
                                         new object[] {_windowList, _parent.Controlled, this});
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

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Window newWindow = (Window) sender;
            IEnumerable<Window> deleteList =
                (from window in _windowList where window.handle == newWindow.handle select window);
            if (deleteList.Count() > 0) {
                Window toRemove = deleteList.First();
                _windowList.Remove(toRemove);
                Manager.Log("Removing window: {0} {1}".With(toRemove.ClassName, toRemove.Title), 1);
                layout.UpdateWindowList(_windowList);
                if (parent.EnabledTag == tag) {
                    layout.Assert();
                }
            }
        }

        private void Manager_WindowCreate(object sender, WindowEventArgs args) {
            bool rulesThisMonitor = false, rulesThisTag = false;
            int stackPosition =
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(0, "General.Windows.DefaultStackPosition"));
            int monitorPosition =
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(-1, "General.Monitor.DefaultMonitor"));
            List<int> tagsToOpenOn = new List<int>();
            foreach (KeyValuePair<WindowMatch, WindowRule> keyValuePair in Manager.windowRules) {
                if (keyValuePair.Key.windowMatches((Window) sender)) {
                    if (keyValuePair.Value.rule == WindowRules.monitor) {
                        if (Manager.monitors[keyValuePair.Value.data].Name == _parent.Name) {
                            rulesThisMonitor = true;
                        }
                        else {
                            return;
                        }
                    }
                    if (keyValuePair.Value.rule == WindowRules.tag) {
                            tagsToOpenOn.Add(keyValuePair.Value.data - 1);
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
            if ((monitorNameToOpenOn == _parent.Name || rulesThisMonitor) &&
                (_parent.EnabledTag == _tag || tagsToOpenOn.Contains(_tag))) {
                Window newWindow = (Window) sender;
                CatchWindow(newWindow);
                Manager.Log("Adding Window: " + newWindow.ClassName + " " + newWindow, 1);
                layout.UpdateWindowList(_windowList);
                if (_parent.EnabledTag == _tag) {
                    layout.Assert();
                }
                else if (!tagsToOpenOn.Contains(_parent.EnabledTag)) {
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

        private int GetFocusedWindowIndex() {
            IntPtr hWnd = GetForegroundWindow();
            for (int i = 0; i < _windowList.Count; i++) {
                if (_windowList[i].handle == hWnd) {
                    return i;
                }
            }
            return -1;
        }

        public void catchMessage(HotkeyMessage message) {
            if (message.level == Level.screen) {
                if (message.message == Message.Focus) {
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
                if (message.message == Message.Switch) {
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
                        Window oldWindow = _windowList[oldIndex];
                        Window newWindow = _windowList[newIndex];
                        _windowList[oldIndex] = newWindow;
                        _windowList[newIndex] = oldWindow;
                        layout.Assert();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.SwitchThis) {
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
                    layout.Assert();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.FocusThis) {
                    if (message.data < _windowList.Count) {
                        _windowList[message.data].Activate();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.Monitor) {
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
                    layout.Assert();
                    newMonitor.GetActiveScreen().Enable();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.MonitorMoveThis) {
                    Manager.monitors[message.data].CatchWindow(ThrowWindow(GetFocusedWindow()));
                    layout.Assert();
                    Manager.monitors[message.data].GetActiveScreen().Activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.Splitter) {
                    layout.MoveSplitter(message.data / 100.0f);
                    Manager.settings.AddSetting(layout.GetSplitter(), parent.Screen.DeviceName.Replace(".", ""),
                                                _tag.ToString(), "Splitter");
                }
                if (message.message == Message.VSplitter) {
                    layout.MoveSplitter(message.data / 100.0f, true);
                    Manager.settings.AddSetting(layout.GetSplitter(true), parent.Screen.DeviceName.Replace(".", ""),
                                                _tag.ToString(), "VSplitter");
                }
                if (message.message == Message.Close) {
                    foreach (Window window in _windowList) {
                        window.Visible = true;
                        window.Maximised = false;
                    }
                }
                if (message.message == Message.Close) {
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
                Convert.ToInt32(Manager.settings.ReadSettingOrDefault(0, "General.Windows.DefaultStackPosition"));
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
        }

        public Image getStateImage(Size previewSize) {
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
            layout.Assert();
        }

        public void AssertLayout() {
            layout.Assert();
        }
    }
}