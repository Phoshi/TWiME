using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    public class TagScreen{
        List<Window> windowList = new List<Window>();
        public List<Window> windows { get { return windowList; } }
        private ILayout layout;
        private Monitor _parent;
        private int _tag;
        public int tag { get { return _tag; } }
        public Monitor parent { get { return _parent; } }
        public TagScreen(Monitor parent, int tag) {
            _parent = parent;
            _tag = tag;
            layout = new DefaultLayout(windowList, _parent._controlled, this);
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy+= Manager_WindowDestroy;
        }

        [DllImport("user32.dll")]
        private static extern
            IntPtr GetForegroundWindow();

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Window newWindow = (Window) sender;
            IEnumerable<Window> deleteList = (from window in windowList where window.handle == newWindow.handle select window);
            if (deleteList.Count() > 0) {
                Window toRemove = deleteList.First();
                windowList.Remove(toRemove);
                Manager.log("Removing window: {0} {1}".With(toRemove.className, toRemove.title), 1);
                layout.updateWindowList(windowList);
                layout.assert();
            }
        }

        void Manager_WindowCreate(object sender, WindowEventArgs args) {
            if (args.monitor.DeviceName == _parent.name && _parent.tagEnabled == _tag) {
                Window newWindow = (Window) sender;
                windowList.Insert(0, newWindow);
                Manager.log("Adding Window: " + newWindow.className + " "+newWindow, 1);
                layout.updateWindowList(windowList);
                layout.assert();
            }
        }

        ~TagScreen() {
            foreach (Window window in windowList) {
                window.visible = true;
                window.maximised = false;
            }
        }

        private int getFocusedWindowIndex() {
            IntPtr hWnd = GetForegroundWindow();
            for (int i = 0; i < windowList.Count; i++) {
                if (windowList[i].handle == hWnd) {
                    return i;
                }
            }
            return -1;
        }

        public void catchMessage(HotkeyMessage message) {
            if (message.level == Level.screen) {
                if (message.message == Message.Focus) {
                    if (windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.name) {
                        int newIndex = getFocusedWindowIndex() + message.data;
                        if (newIndex >= windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = windowList.Count - 1;
                        }
                        Console.WriteLine(newIndex);
                        windowList[newIndex].activate();
                    }
                }
                if (message.message == Message.Switch) {
                    if (windowList.Count == 0) {
                        return;
                    }
                    if (Screen.FromHandle(message.handle).DeviceName == _parent.name) {
                        int oldIndex = getFocusedWindowIndex();
                        int newIndex = oldIndex + message.data;
                        if (newIndex >= windowList.Count) {
                            newIndex = 0;
                        }
                        else if (newIndex < 0) {
                            newIndex = windowList.Count - 1;
                        }
                        Window oldWindow = windowList[oldIndex];
                        Window newWindow = windowList[newIndex];
                        windowList[oldIndex] = newWindow;
                        windowList[newIndex] = oldWindow;
                        layout.assert();
                    }
                }
                if (message.message == Message.SwitchThis) {
                    int oldIndex = getFocusedWindowIndex();
                    int newIndex = message.data;
                    Window oldWindow = windowList[oldIndex];
                    Window newWindow = windowList[newIndex];
                    windowList[oldIndex] = newWindow;
                    windowList[newIndex] = oldWindow;
                    layout.assert();
                }
                if (message.message == Message.FocusThis) {
                    windowList[message.data].activate();
                }
                if (message.message == Message.Monitor) {
                    int newMonitorIndex = Manager.getFocussedMonitorIndex() + message.data;
                    if (newMonitorIndex < 0) {
                        newMonitorIndex = Manager.monitors.Count - 1;
                    }
                    else if (newMonitorIndex >= Manager.monitors.Count) {
                        newMonitorIndex = 0;
                    }
                    Monitor newMonitor = Manager.monitors[newMonitorIndex];
                    Window focussedWindow = getFocusedWindow();
                    newMonitor.catchWindow(this.throwWindow(focussedWindow));
                    layout.assert();
                    newMonitor.getActiveScreen().enable();
                }
                if (message.message == Message.MonitorMoveThis) {
                    Manager.monitors[message.data].catchWindow(throwWindow(getFocusedWindow()));
                    layout.assert();
                    Manager.monitors[message.data].getActiveScreen().activate();
                }
                if (message.message == Message.Splitter) {
                    layout.moveSplitter(message.data / 100.0f);
                }
                if (message.message == Message.Close) {
                    foreach (Window window in windowList) {
                        window.visible = true;
                        window.maximised = false;
                    }
                }
            }
            else {
                getFocusedWindow().catchMessage(message);
                layout.assert();
            }
        }

        public Window throwWindow(Window window) {
            windowList.Remove(window);
            return window;
        }

        public Window getFocusedWindow() {
            int index = getFocusedWindowIndex();
            if (index==-1) {
                return null;
            }
            return windowList[index];
        }

        public void activate() {
            if (windowList.Count > 0) {
                windowList[0].activate();
            }
            else {
                _parent.bar.Activate();
            }
        }

        public void catchWindow(Window window) {
            windowList.Add(window);
        }

        public Image getStateImage(Size previewSize) {
            return layout.stateImage(previewSize);
        }

        public void disable() {
            foreach (Window window in windows) {
                window.visible = false;
            }
        }
        public void enable() {
            foreach (Window window in windows) {
                window.visible = true;
            }
            layout.assert();
        }
        public void assertLayout() {
            layout.assert();
        }
    }
}
