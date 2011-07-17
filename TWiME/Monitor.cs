using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    public class Monitor {
        public Rectangle _controlled { get; internal set; }
        public Bar bar;
        private TagScreen[] tagScreens = new TagScreen[9];
        public TagScreen[] screens { get { return tagScreens; } }
        public string name { get; internal set; }
        public int tagEnabled = 0;
        public Screen screen { get; internal set; }
        public Monitor(Screen newscreen) {
            screen = newscreen;
            createBar();
            Rectangle temp = screen.WorkingArea;
            temp.Height = screen.Bounds.Height - bar.Height;
            _controlled = temp;
            createTagScreens();
            name = screen.DeviceName;

            Manager.WindowCreate += new Manager.WindowEventHandler(Manager_WindowCreate);
            Manager.WindowDestroy += new Manager.WindowEventHandler(Manager_WindowDestroy);

        }

        void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            bar.redraw();
        }

        void Manager_WindowCreate(object sender, WindowEventArgs args) {
            bar.redraw();
        }

        private void createBar() {
            bar = new Bar(this);
            bar.Show();
        }

        private void createTagScreens() {
            for (int i = 0; i < 9; i++) {
                tagScreens[i] = new TagScreen(this, i);
            }
        }

        public void catchMessage(HotkeyMessage message) {
            if (message.level == Level.monitor) {
                if (message.message == Message.MonitorSwitch) {
                    int newIndex = Manager.getFocussedMonitorIndex() + message.data;
                    if (newIndex < 0) {
                        newIndex = Manager.monitors.Count - 1;
                    }
                    if (newIndex >= Manager.monitors.Count) {
                        newIndex = 0;
                    }
                    Console.WriteLine("Switching to window "+Manager.monitors[newIndex].name);
                    Manager.monitors[newIndex].activate();
                    Manager.monitors[newIndex].bar.redraw();
                }
                if (message.message == Message.MonitorFocus) {
                    Manager.monitors[message.data].activate();
                }
                if (message.message == Message.Screen) {
                    if (tagEnabled != message.data) {
                        tagScreens[tagEnabled].disable();
                        tagScreens[message.data].enable();
                        tagEnabled = message.data;
                    }
                }
                if (message.message == Message.TagWindow) {
                    if (tagScreens[message.data].windows.Contains(getActiveScreen().getFocusedWindow())) {
                        Window focussedWindow = getActiveScreen().getFocusedWindow();
                        tagScreens[message.data].throwWindow(focussedWindow);
                        focussedWindow.visible = false;
                        if (message.data == tagEnabled) {
                            getActiveScreen().enable();
                        }
                    }
                    else {
                        tagScreens[message.data].catchWindow(getActiveScreen().getFocusedWindow());
                    }
                    //getActiveScreen().activate();
                    getActiveScreen().assertLayout();
                }
            }
            else {
                tagScreens[tagEnabled].catchMessage(message);
            }
            bar.redraw();
        }

        private void activate() {
            getActiveScreen().activate();
        }

        public TagScreen getActiveScreen() {
            TagScreen screen = tagScreens[tagEnabled];
            return screen;
        }

        public void catchWindow(Window window) {
            Rectangle location = window.Location;
            location.X = _controlled.X;
            location.Y = _controlled.Y;
            window.Location = location;
            getActiveScreen().catchWindow(window);
            bar.redraw();
        }
    }
}
