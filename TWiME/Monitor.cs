using System;
using System.Collections.Generic;
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

        private int _lastFocussedTagScreen;

        public string Name { get; internal set; }
        private int _enabledTag;
        public Screen Screen { get; internal set; }

        public bool IsTagEnabled(int tagNumber) {
            return tagNumber == _enabledTag;
        }

        public void SetTagState(int tagNumber, bool state, bool exclusive = true) {
            if (state) {
                tagScreens[tagNumber].Enable();
                tagScreens[_enabledTag].Disable(tagScreens[tagNumber]);
                Bar.bar.Activate();
                _enabledTag = tagNumber;
                tagScreens[tagNumber].Activate();

            }
        }
        
        public List<int> GetEnabledTags() {
            return new List<int>() {_enabledTag};
        }


        public Monitor(Screen newscreen) {
            _enabledTag = 0;
            Screen = newscreen;
            createBar();
            Rectangle temp = Screen.WorkingArea;
            temp.Height = Screen.Bounds.Height - Bar.Height;
            temp.Y = Bar.Bottom;
            Controlled = temp;
            Name = Screen.DeviceName;
            createTagScreens();
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
                    Manager.monitors[newIndex].activate();
                    Manager.monitors[newIndex].Bar.Redraw();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.MonitorFocus) {
                    Manager.monitors[message.data].activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.message == Message.Screen) {
                    if (!IsTagEnabled(message.data)) {
                        if (message.data < 0) {
                            message.data = _lastFocussedTagScreen;
                        }
                        _lastFocussedTagScreen = GetEnabledTags().First();
                        SetTagState(message.data, true);
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
                if (message.message == Message.ScreenRelative) {
                    int newIndex = this._enabledTag + message.data;
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
                        int numTags =(from screen in tagScreens select screen.windows).SelectMany(window => window).Where(
                            window => window == focussedWindow).Count();
                        if (numTags == 1) {
                            return;
                        }
                        tagScreens[message.data].ThrowWindow(focussedWindow);
                        focussedWindow.Visible = false;
                        if (IsTagEnabled(message.data)) {
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
                    if (message.data == GetActiveScreen().tag) {
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
                        switchScreen.InitLayout();
                        if (switchScreen.tag == _enabledTag) {
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
                    switchScreen.InitLayout();
                    if (IsTagEnabled(switchScreen.tag)) {
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
                    switchScreen.InitLayout();
                    if (IsTagEnabled(switchScreen.tag)) {
                        switchScreen.AssertLayout();
                        Manager.CenterMouseOnActiveWindow();
                    }
                }
            }
            else {
                GetActiveScreen().catchMessage(message);
            }
            Bar.Redraw();
        }

        private void activate() {
            GetActiveScreen().Activate();
        }

        public TagScreen GetActiveScreen() {
            TagScreen activeScreen = tagScreens[GetEnabledTags().First()];
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