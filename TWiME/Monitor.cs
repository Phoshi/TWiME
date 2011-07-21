using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TWiME {
    public class Monitor {
        public Rectangle Controlled { get; internal set; }
        public Bar Bar;
        private TagScreen[] tagScreens = new TagScreen[9];

        public TagScreen[] screens {
            get { return tagScreens; }
        }

        public string Name { get; internal set; }
        public int EnabledTag;
        public Screen Screen { get; internal set; }

        public Monitor(Screen newscreen) {
            EnabledTag = 0;
            Screen = newscreen;
            createBar();
            Rectangle temp = Screen.WorkingArea;
            temp.Height = Screen.Bounds.Height - Bar.Height;
            temp.Y = Bar.Bottom;
            Controlled = temp;
            createTagScreens();
            Name = Screen.DeviceName;

            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy += Manager_WindowDestroy;
        }

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Bar.Redraw();
        }

        private void Manager_WindowCreate(object sender, WindowEventArgs args) {
            Bar.Redraw();
        }

        private void createBar() {
            Bar = new Bar(this);
            Bar.Show();
        }

        private void createTagScreens() {
            for (int i = 0; i < 9; i++) {
                tagScreens[i] = new TagScreen(this, i);
            }
        }

        public void CatchMessage(HotkeyMessage message) {
            if (message.level == Level.monitor) {
                if (message.message == Message.MonitorSwitch) {
                    int newIndex = Manager.GetFocussedMonitorIndex() + message.data;
                    if (newIndex < 0) {
                        newIndex = Manager.monitors.Count - 1;
                    }
                    if (newIndex >= Manager.monitors.Count) {
                        newIndex = 0;
                    }
                    Console.WriteLine("Switching to window {0}", Manager.monitors[newIndex].Name);
                    Manager.monitors[newIndex].activate();
                    Manager.monitors[newIndex].Bar.Redraw();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.MonitorFocus) {
                    Manager.monitors[message.data].activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.Screen) {
                    if (EnabledTag != message.data) {
                        tagScreens[message.data].Enable();
                        tagScreens[EnabledTag].Disable(tagScreens[message.data]);
                        Bar.bar.Activate();
                        EnabledTag = message.data;
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.ScreenRelative) {
                    int newIndex = this.EnabledTag + message.data;
                    if (newIndex < 0) {
                        newIndex = tagScreens.Count() - 1;
                    }
                    else if (newIndex >= tagScreens.Count()) {
                        newIndex = 0;
                    }
                    CatchMessage(new HotkeyMessage(Message.Screen, Level.monitor, message.handle, newIndex));
                }
                if (message.message == Message.TagWindow) {
                    if (GetActiveScreen().GetFocusedWindow().Equals(Bar.bar)) {
                        return;
                    }
                    if (tagScreens[message.data].windows.Contains(GetActiveScreen().GetFocusedWindow())) {
                        Window focussedWindow = GetActiveScreen().GetFocusedWindow();
                        tagScreens[message.data].ThrowWindow(focussedWindow);
                        focussedWindow.Visible = false;
                        if (message.data == EnabledTag) {
                            GetActiveScreen().Enable();
                        }
                    }
                    else {
                        tagScreens[message.data].CatchWindow(GetActiveScreen().GetFocusedWindow());
                    }
                    GetActiveScreen().AssertLayout();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.SwapTagWindow) {
                    if (GetActiveScreen().GetFocusedWindow().Equals(Bar.bar)) {
                        return;
                    }
                    Window thrown = GetActiveScreen().ThrowWindow(GetActiveScreen().GetFocusedWindow());
                    GetActiveScreen().AssertLayout();
                    tagScreens[message.data].CatchWindow(thrown);
                    thrown.Visible = false;
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.SwapTagWindowRelative) {
                    if (GetActiveScreen().GetFocusedWindow().Equals(Bar.bar)) {
                        return;
                    }
                    int newIndex = GetActiveScreen().tag + message.data;
                    if (newIndex < 0) {
                        newIndex = tagScreens.Count() - 1;
                    }
                    else if (newIndex >= tagScreens.Count()) {
                        newIndex = 0;
                    }
                    CatchMessage(new HotkeyMessage(Message.SwapTagWindow, Level.monitor, message.handle, newIndex));
                    CatchMessage(new HotkeyMessage(Message.Screen, Level.monitor, message.handle, newIndex));
                }
                if (message.message == Message.Layout) {
                    int newIndex = message.data;
                    if (newIndex < Manager.layouts.Count) {
                        TagScreen switchScreen = this.GetActiveScreen();
                        switchScreen.activeLayout = newIndex;
                        switchScreen.initLayout();
                        if (switchScreen.tag == EnabledTag) {
                            switchScreen.AssertLayout();
                            Manager.CenterMouseOnActiveWindow();
                        }
                    }
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
                    if (switchScreen.tag == EnabledTag) {
                        switchScreen.AssertLayout();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.LayoutRelativeReverse) {
                    TagScreen switchScreen = tagScreens[message.data];
                    int newIndex = switchScreen.activeLayout - 1;
                    if (newIndex < 0) {
                        newIndex = Manager.layouts.Count() - 1;
                    }
                    else if (newIndex >= Manager.layouts.Count) {
                        newIndex = 0;
                    }
                    switchScreen.activeLayout = newIndex;
                    switchScreen.initLayout();
                    if (switchScreen.tag == EnabledTag) {
                        switchScreen.AssertLayout();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
            }
            else {
                tagScreens[EnabledTag].catchMessage(message);
            }
            Bar.Redraw();
        }

        private void activate() {
            GetActiveScreen().Activate();
        }

        public TagScreen GetActiveScreen() {
            TagScreen activeScreen = tagScreens[EnabledTag];
            return activeScreen;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != typeof (Monitor)) {
                return false;
            }
            return Equals((Monitor) obj);
        }

        public void CatchWindow(Window window) {
            Rectangle location = window.Location;
            location.X = Controlled.X;
            location.Y = Controlled.Y;
            window.Location = location;
            GetActiveScreen().CatchWindow(window);
            Bar.Redraw();
        }

        public bool Equals(Monitor other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return Equals(other.Name, Name);
        }

        public override int GetHashCode() {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}