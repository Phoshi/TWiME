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
            temp.Y = bar.Bottom;
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
                        tagScreens[message.data].enable();
                        tagScreens[tagEnabled].disable(tagScreens[message.data]);
                        bar.bar.activate();
                        tagEnabled = message.data;
                    }
                }
                if (message.message == Message.ScreenRelative) {
                    int newIndex = this.tagEnabled + message.data;
                    if (newIndex < 0) {
                        newIndex = tagScreens.Count() - 1;
                    }
                    else if (newIndex >= tagScreens.Count()) {
                        newIndex = 0;
                    }
                    catchMessage(new HotkeyMessage(Message.Screen, Level.monitor, message.handle, newIndex));
                }
                if (message.message == Message.TagWindow) {
                    if (getActiveScreen().getFocusedWindow().Equals(bar.bar)) {
                        return;
                    }
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
                    getActiveScreen().assertLayout();
                }
                if (message.message == Message.SwapTagWindow) {
                    if (getActiveScreen().getFocusedWindow().Equals(bar.bar)) {
                        return;
                    }
                    Window thrown = getActiveScreen().throwWindow(getActiveScreen().getFocusedWindow());
                    getActiveScreen().assertLayout();
                    tagScreens[message.data].catchWindow(thrown);
                    thrown.visible = false;
                }
                if (message.message == Message.SwapTagWindowRelative) {
                    if (getActiveScreen().getFocusedWindow().Equals(bar.bar)) {
                        return;
                    }
                    int newIndex = getActiveScreen().tag + message.data;
                    if (newIndex < 0) {
                        newIndex = tagScreens.Count() - 1;
                    }
                    else if (newIndex >= tagScreens.Count()) {
                        newIndex = 0;
                    }
                    catchMessage(new HotkeyMessage(Message.SwapTagWindow, Level.monitor, message.handle, newIndex));
                    catchMessage(new HotkeyMessage(Message.Screen, Level.monitor, message.handle, newIndex));
                }
                if (message.message == Message.LayoutRelative) {
                    TagScreen switchScreen = tagScreens[message.data];
                    int newIndex = switchScreen.activeLayout + 1;
                    if (newIndex < 0) {
                        newIndex = Manager.layouts.Count() - 1;
                    }
                    else if (newIndex >= Manager.layouts.Count) {
                        newIndex = 0;
                    }
                    switchScreen.activeLayout = newIndex;
                    switchScreen.initLayout();
                    if (switchScreen.tag == tagEnabled) {
                        switchScreen.assertLayout();
                    }
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
        public override bool Equals(object obj) {
            if (((Monitor)obj).name == name) {
                return true;
            }
            else {
                return false;
            }
            //return base.Equals(obj);
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
