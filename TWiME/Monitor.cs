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
        private List<int> _enabledTags = new List<int>();
        private int _activeTag;
        public Screen Screen { get; internal set; }

        public bool IsTagEnabled(int tagNumber) {
            return _enabledTags.Contains(tagNumber);
        }

        public void SetTagState(int tagNumber, bool state, bool exclusive = true) {
            if (state) {
                if (!_enabledTags.Contains(tagNumber)) {
                    int index = _enabledTags.IndexOf(GetActiveTag());
                    if (index == -1) {
                        index = 0;
                    }
                    _enabledTags.Insert(index, tagNumber);
                }
                if (exclusive) {
                    tagScreens[GetActiveTag()].Disable(tagScreens[tagNumber]);
                    _enabledTags.Remove(GetActiveTag());
                }
                tagScreens[tagNumber].Enable();
                Bar.bar.Activate();
                _activeTag = tagNumber;
                tagScreens[tagNumber].Activate();
            }
            else if (_enabledTags.Count > 1) {
                tagScreens[tagNumber].Disable();
                _enabledTags.Remove(tagNumber);
            }
            reorganiseActiveTagSpaces();
        }
        
        public List<int> GetEnabledTags() {
            return _enabledTags;
        }
        public List<int> GetDisabledTags() {
            return (from screen in tagScreens where !IsTagEnabled(screen.tag) select screen.tag).ToList();
        }

        public IEnumerable<TagScreen> GetEnabledScreens() {
            return from screen in tagScreens where IsTagEnabled(screen.tag) orderby _enabledTags.IndexOf(screen.tag) select screen;
        }

        public TagScreen GetActiveScreen() {
            TagScreen activeScreen = tagScreens[GetActiveTag()];
            return activeScreen;
        }

        public int GetActiveTag() {
            return _activeTag;
        }

        private Dictionary<TagScreen, Rectangle> generateLayout() {
            Dictionary<TagScreen, Rectangle> layouts = new Dictionary<TagScreen, Rectangle>();
            double numRows = Math.Ceiling(Math.Pow(GetEnabledScreens().Count(), 0.5));
            double numColumns = Math.Ceiling(GetEnabledScreens().Count() / numRows);
            int winWidth = (int)(Controlled.Width / numRows);
            int winHeight = (int)(Controlled.Height / numColumns);
            int row = 0, column = 0;
            foreach (TagScreen screen in GetEnabledScreens()) {
                if (column == numColumns - 1) {
                    if ((numRows * numColumns) != GetEnabledScreens().Count()) {
                        int shortfall = (int)((numRows * numColumns) - GetEnabledScreens().Count());
                        winWidth = (int)(Controlled.Width / (numRows - shortfall));
                    }
                }
                int thisWinLeft = Controlled.Left + (winWidth * row);
                int thisWinTop = Controlled.Top + (winHeight * column);
                Rectangle thisWinRect = new Rectangle(thisWinLeft, thisWinTop, winWidth, winHeight);
                layouts[screen] = thisWinRect;
                if (++row >= numRows) {
                    row = 0;
                    column++;
                }
            }
            return layouts;
        }


        private void reorganiseActiveTagSpaces() {
            int numActiveSpaces = GetEnabledTags().Count;
            if (numActiveSpaces == 0) {
                return;
            }
            Dictionary<TagScreen, Rectangle> layouts = generateLayout();
            foreach (TagScreen screen in GetEnabledScreens()) {
                screen.UpdateControlledArea(layouts[screen]);
            }
        }

        public List<Window> GetVisibleWindows() {
            var windows = from screen in GetEnabledScreens() select screen.windows;
            return windows.SelectMany(window => window).ToList();
        }

        public int GetFocussedTag() {
            if (GetEnabledTags().Count == 1) {
                return GetActiveTag();
            }
            else {
                foreach (TagScreen screen in GetEnabledScreens()) {
                    if (screen.GetFocusedWindowIndex() > -1) {
                        return screen.tag;
                    }
                }
            }
            return -1;
        }

        public TagScreen GetFocussedScreen() {
            int focussed = GetFocussedTag();
            if (focussed !=-1) {
                return tagScreens[focussed];
            }
            else {
                return null;
            }
        }

        public Monitor(Screen newscreen) {
            _enabledTags.Add(0);
            _activeTag = 0;
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
            Manager.WindowFocusChange += new Manager.WindowEventHandler(Manager_WindowFocusChange);
        }

        void Manager_WindowFocusChange(object sender, WindowEventArgs args) {
            if (GetEnabledTags().Count > 1) {
                int focussedTag = GetFocussedTag();
                if (focussedTag != -1) {
                    _activeTag = focussedTag;
                }
            }
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
            if (message.level == Level.Monitor) {
                if (message.Message == Message.MonitorSwitch) {
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
                if (message.Message == Message.MonitorFocus) {
                    Manager.monitors[message.data].activate();
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.Message == Message.Screen) {
                    if (!IsTagEnabled(message.data)) {
                        if (message.data < 0) {
                            message.data = _lastFocussedTagScreen;
                        }
                        _lastFocussedTagScreen = GetEnabledTags().First();
                        SetTagState(message.data, !IsTagEnabled(message.data));
                        Manager.CenterMouseOnActiveWindow();
                    }
                    else if (GetActiveTag() != message.data) {
                        _activeTag = message.data;
                        GetActiveScreen().Activate();
                    }
                    else {
                        SetTagState(message.data, true, false);
                    }
                }
                if (message.Message == Message.ScreenRelative) {
                    int newIndex;
                    if (GetEnabledTags().Count <= 1) {
                        newIndex = GetActiveTag() + message.data;
                    }
                    else {
                        List<int> enabledTags = GetEnabledTags();
                        enabledTags.Sort();
                        int newIndexPosition = enabledTags.IndexOf(GetActiveTag()) + message.data;
                        if (newIndexPosition <= GetEnabledTags().Count-1 && newIndexPosition >= 0) {
                            newIndex = enabledTags[newIndexPosition];
                        }
                        else if (newIndexPosition == 0) {
                            newIndex = enabledTags.Last();
                        }
                        else {
                            newIndex = enabledTags.First();
                        }
                    }
                    if (newIndex < 0) {
                        newIndex = tagScreens.Count() - 1;
                    }
                    else if (newIndex >= tagScreens.Count()) {
                        newIndex = 0;
                    }
                    CatchMessage(new HotkeyMessage(Message.Screen, Level.Monitor, message.handle, newIndex));
                }
                if (message.Message == Message.TagWindow) {
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
                if (message.Message == Message.SwapTagWindow) {
                    if (GetActiveScreen().GetFocusedWindow().Equals(Bar.bar)) {
                        return;
                    }
                    if (message.data == GetActiveScreen().tag) {
                        return;
                    }
                    Window thrown = GetActiveScreen().ThrowWindow(GetActiveScreen().GetFocusedWindow());
                    GetActiveScreen().AssertLayout();
                    tagScreens[message.data].CatchWindow(thrown);
                    if (!IsTagEnabled(message.data)) {
                        thrown.Visible = false;
                    }
                    Manager.CenterMouseOnActiveWindow();
                }
                if (message.Message == Message.SwapTagWindowRelative) {
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
                    CatchMessage(new HotkeyMessage(Message.SwapTagWindow, Level.Monitor, message.handle, newIndex));
                    CatchMessage(new HotkeyMessage(Message.Screen, Level.Monitor, message.handle, newIndex));
                }
                if (message.Message == Message.Layout) {
                    int newIndex = message.data;
                    if (newIndex < Manager.layouts.Count) {
                        TagScreen switchScreen = this.GetActiveScreen();
                        switchScreen.activeLayout = newIndex;
                        switchScreen.InitLayout();
                        if (IsTagEnabled(switchScreen.tag)) {
                            switchScreen.AssertLayout();
                            Manager.CenterMouseOnActiveWindow();
                        }
                    }
                }
                if (message.Message == Message.LayoutRelative) {
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
                if (message.Message == Message.LayoutRelativeReverse) {
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
                if (message.Message == Message.Split) {
                    var inactiveTags = GetDisabledTags();
                    if (inactiveTags.Count > 0) {
                        SetTagState(inactiveTags.First(), true, false);
                    }
                }
                if (message.Message == Message.OnlyShow) {
                    foreach (int enabledTag in new List<int>(GetEnabledTags())) {
                        if (enabledTag != GetActiveTag()) {
                            SetTagState(enabledTag, false);
                        }
                    }
                }
            }
            else {
                GetActiveScreen().CatchMessage(message);
            }
            Bar.Redraw();
        }

        private void activate() {
            GetActiveScreen().Activate();
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