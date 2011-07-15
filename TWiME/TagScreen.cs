using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    class TagScreen{
        List<Window> windowList = new List<Window>();
        private ILayout layout;
        private Monitor _parent;
        private int _tag;
        public TagScreen(Monitor parent, int tag) {
            _parent = parent;
            _tag = tag;
            layout = new DefaultLayout(windowList);
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
                Console.WriteLine("Removing window: "+toRemove);
                layout.updateWindowList(windowList);
                layout.assert();
            }
        }

        void Manager_WindowCreate(object sender, WindowEventArgs args) {
            if (args.monitor.DeviceName == _parent.name && _parent.tagsEnabled[_tag]) {
                Window newWindow = (Window) sender;
                windowList.Insert(0, newWindow);
                Console.WriteLine("Adding Window: " + newWindow.className + " "+newWindow);
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
                        Console.WriteLine(oldIndex);
                        Console.WriteLine(newIndex);
                        Window oldWindow = windowList[oldIndex];
                        Window newWindow = windowList[newIndex];
                        windowList[oldIndex] = newWindow;
                        windowList[newIndex] = oldWindow;
                        layout.assert();
                    }
                }
                if (message.message == Message.Close) {
                    foreach (Window window in windowList) {
                        window.visible = true;
                        window.maximised = false;
                    }
                }
            }
            else {
                foreach (Window window in windowList) {
                    window.catchMessage(message);
                }
            }
        }

        public void activate() {
            if (windowList.Count > 0) {
                windowList[0].activate();
            }
            else {
                _parent.bar.Activate();
            }
        }
    }
}
