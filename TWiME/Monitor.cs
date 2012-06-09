using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TWiME {
    public class Monitor {
        private Rectangle _controlled;
        public Rectangle Controlled {
            get {
                if (!Manager.monitors.Contains(this)) {
                    return _controlled;
                }
                if (!Taskbar.hidden && (this==Manager.monitors[0])) {
                    Rectangle newRect = _controlled;
                    newRect.Height -= 25;
                    return newRect;
                }
                return _controlled;
            }
            set { _controlled = value; }
        }
        public Bar Bar;
        private TagScreen[] tagScreens;

        public TagScreen[] screens {
            get { return tagScreens; }
        }

        private int _lastFocussedTagScreen;

        public string Name { get; internal set; }
        private List<int> _enabledTags = new List<int>();
        private int _activeTag;
        private float _splitter = 0.8f;
        public Screen Screen { get; internal set; }

        public bool IsTagEnabled(int tagNumber) {
            return _enabledTags.Contains(tagNumber);
        }

        public string SafeName {
            get { return Screen.DeviceName.Replace(".", ""); }
        }

        public void SetTagState(int tagNumber, bool state, bool exclusive = true, bool surpressLayoutUpdate = false) {
            if (tagNumber >= tagScreens.Count()) {
                ChangeNumberOfTagScreens(tagNumber + 1);
            }
            if (state) {
                if (!_enabledTags.Contains(tagNumber)) {
                    int index = _enabledTags.IndexOf(GetActiveTag());
                    if (index == -1) {
                        index = 0;
                    }
                    if (surpressLayoutUpdate) {
                        index = _enabledTags.Count - 1;
                    }
                    _enabledTags.Insert(index + 1, tagNumber);
                }
                int currentActiveTag = GetActiveTag();
                tagScreens[tagNumber].Enable();
                Bar.bar.Activate();
                _activeTag = tagNumber;
                tagScreens[tagNumber].Activate();
                if (exclusive) {
                    _enabledTags.Remove(currentActiveTag);
                    tagScreens[currentActiveTag].Disable(tagScreens[tagNumber]);
                    tagScreens[currentActiveTag].UpdateControlledArea(Controlled);
                }
                if (!surpressLayoutUpdate) {
                    reorganiseActiveTagSpaces();
                }
            }
            else if (_enabledTags.Count > 1) {
                tagScreens[tagNumber].UpdateControlledArea(Controlled);
                tagScreens[tagNumber].Disable();
                _enabledTags.Remove(tagNumber);
                _activeTag = _enabledTags.First();
            }
            if (!surpressLayoutUpdate) {
                reorganiseActiveTagSpaces();
            }

            string newVisibleTags = String.Join(",", (from tag in _enabledTags select (tag + 1).ToString()));
            if (!Manager.settings.readOnly) {
                Manager.settings.AddSetting(newVisibleTags, SafeName, "VisibleTags");
            }
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
            if (GetEnabledScreens().Count() > 3) {
                double numRows = Math.Ceiling(Math.Pow(GetEnabledScreens().Count(), 0.5));
                double numColumns = Math.Ceiling(GetEnabledScreens().Count() / numRows);
                int winWidth = (int) (Controlled.Width / numRows);
                int winHeight = (int) (Controlled.Height / numColumns);
                int row = 0, column = 0;
                foreach (TagScreen screen in GetEnabledScreens()) {
                    if (column == numColumns - 1) {

                        if ((numRows * numColumns) != GetEnabledScreens().Count()) {
                            int shortfall = (int) ((numRows * numColumns) - GetEnabledScreens().Count());
                            winWidth = (int) (Controlled.Width / (numRows - shortfall)) + 1;
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
            }
            else {
                TagScreen mainWindow = GetEnabledScreens().First();
                int width = (int)(Controlled.Width * _splitter) + 1;
                int height = Controlled.Height;
                int x = Controlled.X;
                int y = Controlled.Y;
                Rectangle newRect = new Rectangle(x, y, width, height);
                layouts[mainWindow] = newRect;

                if (GetEnabledScreens().Count() > 1) {
                    int secondaryHeight = Controlled.Height / (GetEnabledScreens().Count() - 1);
                    for (int i = 1; i < GetEnabledScreens().Count(); i++) {
                        TagScreen window = GetEnabledScreens().ElementAt(i);
                        int nx = Controlled.Left + width - 1;
                        int ny = Controlled.Top + secondaryHeight * (i - 1);
                        int nwidth = Controlled.Width - width + 1;
                        Rectangle secondaryRect = new Rectangle(nx, ny, nwidth, secondaryHeight);
                        layouts[window] = secondaryRect;
                    }
                }
            }
            return layouts;
        }


        private void reorganiseActiveTagSpaces() {
            int numActiveSpaces = GetEnabledTags().Count;
            if (numActiveSpaces == 0) {
                return;
            }
            if (numActiveSpaces == 1) {
                GetEnabledScreens().First().UpdateControlledArea(Controlled);
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
            foreach (TagScreen screen in GetEnabledScreens()) {
                if (screen.GetFocusedWindowIndex() > -1) {
                    return screen.tag;
                }
            }
            return -1;
        }

        public TagScreen GetFocussedScreen() {
            int focussed = GetFocussedTag();
            if (focussed !=-1) {
                return tagScreens[focussed];
            }
            return null;
        }

        public Monitor(Screen newscreen) {
            Screen = newscreen;
            Name = Screen.DeviceName;
            int numTagScreens = int.Parse(Manager.settings.ReadSettingOrDefault(9, SafeName, "NumberOfTags"));
            tagScreens = new TagScreen[numTagScreens];
            _activeTag = 0;
            createTagScreens();
            createBar();
            Rectangle temp = Screen.WorkingArea;
            temp.Height = Screen.Bounds.Height - Bar.bar.Location.Height;
            temp.Y = Screen.Bounds.Top + Bar.bar.Location.Height;
            temp.Width += 1;
            Controlled = temp;
            _splitter = float.Parse(Manager.settings.ReadSettingOrDefault("0.5", Screen.DeviceName.Replace(".", ""), "Splitter"));
            string activeTags = Manager.settings.ReadSettingOrDefault("1", SafeName, "VisibleTags");
            string[] tags = activeTags.Split(',');
            foreach (string tag in tags) {
                try {
                    int tagNumber = int.Parse(tag) - 1;
                    if (tagNumber >= 0 && tagNumber <= tagScreens.Length) {
                        _enabledTags.Add(tagNumber);
                    }
                }
                catch (FormatException) {
                    //Swallow
                }
            }
            if (_enabledTags.Count == 0) { //If we did have a definition, but it's all junk, screw it and just use zero
                _enabledTags.Add(0);
            }
            _activeTag = _enabledTags.First();
            reorganiseActiveTagSpaces();
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy += Manager_WindowDestroy;
            Manager.WindowFocusChange += Manager_WindowFocusChange;
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
            for (int i = 0; i < tagScreens.Length; i++) {
                tagScreens[i] = new TagScreen(this, i);
            }
        }

        public void ChangeNumberOfTagScreens(int newNumber) {
            if (newNumber < 1) {
                return;
            }
            if (IsTagEnabled(newNumber)) {
                SetTagState(0, true);
            }
            TagScreen[] newTS = new TagScreen[newNumber];
            if (tagScreens.Length > newNumber) {
                for (int i = newNumber; i < tagScreens.Length; i++) {
                    tagScreens[i].Disown();
                }
                Array.Copy(tagScreens, newTS, newNumber);
            }
            else {
                Array.Copy(tagScreens, newTS, newNumber - (newNumber - tagScreens.Length));
            }
            for (int i = tagScreens.Length; i < newTS.Length; i++) {
                newTS[i] = new TagScreen(this, i);
            }
            tagScreens = newTS;
            Manager.settings.AddSetting(newNumber.ToString(), SafeName, "NumberOfTags");
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
                    if (message.data >= tagScreens.Count()) {
                        ChangeNumberOfTagScreens(message.data + 1);
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
                    int newIndex;

                    if (GetEnabledTags().Count == 1) {
                        newIndex = GetActiveScreen().tag + message.data;
                    }
                    else {
                        List<int> enabledTags = GetEnabledTags();
                        enabledTags.Sort();
                        int newIndexPosition = enabledTags.IndexOf(GetActiveTag()) + message.data;
                        if (newIndexPosition <= GetEnabledTags().Count - 1 && newIndexPosition >= 0) {
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
                        SetTagState((from tag in inactiveTags orderby tagScreens[tag].windows.Count() > 0 descending select tag).First(), true, false);
                    }
                }
                if (message.Message == Message.OnlyShow) {
                    foreach (int enabledTag in new List<int>(GetEnabledTags())) {
                        if (enabledTag != GetActiveTag()) {
                            SetTagState(enabledTag, false, true, true);
                        }
                    }
                    reorganiseActiveTagSpaces();
                }
                if (message.Message == Message.Hide) {
                    int activeTag = GetActiveTag();
                    if (GetEnabledTags().Count == 2) {
                        TagScreen newActiveScreen = GetEnabledScreens().First(screen=>screen.tag!=activeTag);
                        _activeTag = newActiveScreen.tag;
                        Manager.SendMessage(Message.OnlyShow, Level.Monitor, 0);
                        return;
                    }
                    SetTagState(activeTag, false);
                }
                if (message.Message == Message.ShowAll) {
                    foreach (TagScreen tagScreen in tagScreens) {
                        if (tagScreen.windows.Count > 0) {
                            SetTagState(tagScreen.tag, true, false, true);
                        }
                    }
                    reorganiseActiveTagSpaces();
                }
                if (message.Message == Message.Splitter) {
                    _splitter += message.data / 100.0f;
                    Manager.settings.AddSetting(_splitter, Screen.DeviceName.Replace(".", ""), "Splitter");
                    reorganiseActiveTagSpaces();
                }
                if (message.Message == Message.SplitRotate) {
                    if (message.data > 0) {
                        int poppedTag = _enabledTags.ElementAt(0);
                        _enabledTags.RemoveAt(0);
                        _enabledTags.Add(poppedTag);
                    }
                    else {
                        int poppedTag = _enabledTags.Last();
                        _enabledTags.RemoveAt(_enabledTags.Count - 1);
                        _enabledTags.Insert(0, poppedTag);
                    }
                    reorganiseActiveTagSpaces();
                }
                if (message.Message == Message.ReindexTagScreens) {
                    int newNumber = tagScreens.Length + message.data;
                    ChangeNumberOfTagScreens(newNumber);
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

        public void Disown() {
            foreach (TagScreen tagScreen in tagScreens) {
                tagScreen.Disown();
            }
            Manager.WindowCreate -= Manager_WindowCreate;
            Manager.WindowDestroy -= Manager_WindowDestroy;
            Manager.WindowFocusChange -= Manager_WindowFocusChange;

            Bar.InternalClosing = true;
            Bar.Close();
        }

        public void AssertTagLayout() {
            reorganiseActiveTagSpaces();
        }

        public TagScreen GetMouseoveredTagScreen() {
            foreach (TagScreen screen in GetEnabledScreens()) {
                if (screen.Controlled.Contains(Control.MousePosition)) {
                    return screen;
                }
            }
            return null;
        }

        public Window ThrowWindow(Window window) {
            foreach (TagScreen tagScreen in tagScreens) {
                if (tagScreen.windows.Contains(window)) {
                    tagScreen.ThrowWindow(window);
                }
            }
            return window;
        }
    }
}