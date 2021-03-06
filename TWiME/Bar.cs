﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Extensions;
using Microsoft.Win32;
using Tree;

namespace TWiME {
    public sealed partial class Bar : Form {
        public Window BarWindow { get; internal set; }

        private int barHeight = 15;
        private Font titleFont;
        private Font boldFont;
        private Brush foregroundBrush;
        private Brush foregroundBrush2;
        private Brush backgroundBrush;
        private Brush backgroundBrush2;
        private Brush selectedBrush;
        private Pen seperatorPen;
        private Brush coverBrush;
        private string tagStyle;

        public Font TitleFont {
            get { return titleFont; }
        }
        public int BarHeight {
            get { return barHeight; }
        }

        private Screen _screen;
        private Monitor _parent;

        public new Monitor Parent {
            get { return _parent; }
        }

        private Dictionary<MouseButtons, Dictionary<Rectangle, Action>> _clicks =
            new Dictionary<MouseButtons, Dictionary<Rectangle, Action>>();
        private Dictionary<string, BarItem> _items = new Dictionary<string, BarItem>();

        public new ContextMenuStrip Menu { get; private set; }

        public bool InternalClosing { get; set; }

        Dictionary<string, Type> _barPlugins = new Dictionary<string, Type>();

        Dictionary<WindowTilingType, String> _tilingStyleSymbols = new Dictionary<WindowTilingType, string> {
                                                                                                                {WindowTilingType.Normal, ")"},
                                                                                                                {WindowTilingType.FullTag, "}"},
                                                                                                                {WindowTilingType.FullScreen, ">"},
                                                                                                                {WindowTilingType.Floating, "$"}
                                                                                                            }; 

        public Bar(Monitor monitor) {
            InternalClosing = false;
            InitializeComponent();

            barHeight = Convert.ToInt32(Manager.settings.ReadSettingOrDefault(15, "General.Bar.Height"));
            titleFont = new Font(Manager.settings.ReadSettingOrDefault("Segoe UI", "General.Bar.Font"), barHeight * 0.6f);
            boldFont = new Font(titleFont, FontStyle.Bold);
            foregroundBrush =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.Bar.UnselectedForeground")));
            foregroundBrush2 =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Bar.SelectedForeground")));
            backgroundBrush =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Bar.SelectedItemColour")));
            backgroundBrush2 =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("Black",
                                                                         "General.Bar.UnselectedBackgroundColour")));
            selectedBrush =
                new SolidBrush(Color.FromArgb(128,
                                              Color.FromName(Manager.settings.ReadSettingOrDefault("White",
                                                                                                   "General.Bar.SelectedTagColour"))));
            coverBrush = new SolidBrush(Color.FromArgb(
                int.Parse(Manager.settings.ReadSettingOrDefault("128", "General.Bar.Inactive.Opacity")),
                Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.Bar.Inactive.Colour"))
                ));

            Color seperatorColour =
                Color.FromName(Manager.settings.ReadSettingOrDefault("Blue", "General.Bar.SeperatorColour"));
            int seperatorWidth = int.Parse(Manager.settings.ReadSettingOrDefault("3", "General.Bar.SeperatorWidth"));
            seperatorPen = new Pen(seperatorColour, seperatorWidth);
            _screen = monitor.Screen;
            _parent = monitor;

            tagStyle = Manager.settings.ReadSettingOrDefault("numbers", _parent.SafeName, "Bar", "TagStyle");
            this.StartPosition = FormStartPosition.Manual;
            this.Location = _screen.Bounds.Location;
            this.Width = _screen.Bounds.Width;
            this.Height = 10;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DesktopLocation = this.Location;
            Color bColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Bar.BackColour"));
            this.BackColor = bColor;
            this.ShowInTaskbar = false;
            BarWindow = new Window("", this.Handle, "", "", true);
            loadPluginItems();
            loadAdditionalItems();
            generateMenu();

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) {
            this.Close();
            Manager.SendMessage(Message.Close, Level.Global, 0);
        }

        private void generateMenu() {
            Menu = new ContextMenuStrip();
            Menu.ShowImageMargin = false;
            Menu.Font = titleFont;
            Menu.BackColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Menu.Background"));
            Menu.ForeColor =
                Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Menu.Foreground"));
            Node<Action> root = new Node<Action>();
            if (Manager.settings.sections.Contains("Menu Items")) {
                foreach (List<string> list in Manager.settings.KeysUnderSection("Menu Items")) {
                    string itemName = list.Last();
                    string itemValue = Manager.settings.ReadSetting(list.ToArray());
                    //Drop the first element, flip it round, drop the last, and flip it back.
                    List<string> treeNodes = list.Skip(1).Reverse().Skip(1).Reverse().ToList();
                    List<Node<Action>> currentNodes = new List<Node<Action>>();
                    currentNodes.Add(root);
                    foreach (string node in treeNodes) {
                        if (currentNodes.Last().ContainsNamedChildNode(node)) {
                            currentNodes.Add(currentNodes.Last().GetNamedChildNode(node));
                        }
                        else {
                            Node<Action> newNode = new Node<Action>(node, null);
                            currentNodes.Last().Add(newNode);
                            currentNodes.Add(newNode);
                        }
                    }
                    Node<Action> bottomMostNode = new Node<Action>(itemName, (()=>runCommand(itemValue)));
                    currentNodes.Last().Add(bottomMostNode);
                }
            }
            ToolStripMenuItem items = buildMenu(root);
            ToolStripItemCollection collection = items.DropDownItems;
            int collectionCount = collection.Count;
            for (int i = 0; i < collectionCount; i++) {
                Menu.Items.Add(collection[0]);
            }
        }

        private void loadPluginItems() {
            foreach (string file in Directory.GetFiles("BarItems", "*.dll")) {
                Assembly asm = Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), file));
                foreach (Type type in asm.GetTypes()) {
                    if (typeof(IPluginBarItem).IsAssignableFrom(type)) {
                        _barPlugins[type.Name] = type;
                    }
                }
            }
        }

        public string GetAssociation(string doctype) {
            uint pcchOut = 0;   // size of output buffer

            // First call is to get the required size of output buffer
            Win32API.AssocQueryString(Win32API.AssocF.Verify, Win32API.AssocStr.Executable, doctype, null, null, ref pcchOut);

            // Allocate the output buffer
            StringBuilder pszOut = new StringBuilder((int)pcchOut);

            // Get the full pathname to the program in pszOut
            Win32API.AssocQueryString(Win32API.AssocF.Verify, Win32API.AssocStr.Executable, doctype, null, pszOut, ref pcchOut);
            string doc = pszOut.ToString();
            return doc;
        }


        private void runCommand(string command) {
            Dictionary<string, Action> specialCommands = new Dictionary<string, Action>();
            specialCommands["Edit TWiMErc"] = (() => Process.Start(GetAssociation(@".txt"), "_TWiMErc"));
            specialCommands["Quit"] = (() => Manager.SendMessage(Message.Close, Level.Global, 0));
            specialCommands["Restart"] = (() => Manager.SendMessage(Message.Close, Level.Global, 1));

            if (specialCommands.ContainsKey(command)) {
                specialCommands[command]();
            }
            else {
                try {
                    Process.Start(command);
                }
                catch (Win32Exception) {
                    //how do I handle this? :(
                }
            }
        }

        private ToolStripMenuItem buildMenu(Node<Action> root) {
            if (root.Children.Count > 0) {
                ToolStripMenuItem thisItem = new ToolStripMenuItem(root.Name);
                ((ToolStripDropDownMenu) (thisItem.DropDown)).ShowImageMargin = false;
                thisItem.Font = titleFont;
                thisItem.BackColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Menu.Background"));
                thisItem.ForeColor =
                    Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Menu.Foreground"));
                foreach (Node<Action> child in root.Children) {
                    thisItem.DropDownItems.Add(buildMenu(child));
                }
                if (root.Data != null) {
                    thisItem.Click += ((sender, eventArgs) => root.Data());
                }

                return thisItem;
            }
            else {
                ToolStripMenuItem thisItem = new ToolStripMenuItem(root.Name, null, ((sender, eventargs)=>root.Data()));
                thisItem.DisplayStyle = ToolStripItemDisplayStyle.Text;
                thisItem.Font = titleFont;
                thisItem.BackColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Menu.Background"));
                thisItem.ForeColor =
                    Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Menu.Foreground"));
                return thisItem;
            }
        }

        private void loadAdditionalItems() {
            if (Manager.settings.sections.Contains("Bar Items")) {
                foreach (List<string> list in Manager.settings.KeysUnderSection("Bar Items")) {
                    string itemName, settingName, value;
                    try {
                        itemName = list[1];
                        settingName = list[2];
                        value = Manager.settings.ReadSetting(list.ToArray());
                    }
                    catch (IndexOutOfRangeException) {
                        continue;
                    }
                    if (_items.ContainsKey(itemName)) {
                        BarItem item = _items[itemName];
                        switch (settingName) {
                            case "MaximumWidth":
                                item.MaximumWidth = int.Parse(value);
                                break;
                            case "MinimumWidth":
                                item.MinimumWidth = int.Parse(value);
                                break;
                            case "IsBuiltIn":
                                item.IsBuiltIn = bool.Parse(value);
                                break;
                            case "Foreground":
                                item.ForeColour = new SolidBrush(Color.FromName(value));
                                break;
                            case "Background":
                                item.BackColour = new SolidBrush(Color.FromName(value));
                                break;
                            case "Argument":
                                item.Argument = value;
                                break;
                            case "Monitor":
                                if (Screen.AllScreens.Length > int.Parse(value)) {
                                    string monitorName = Screen.AllScreens[int.Parse(value)].DeviceName;
                                    if (_parent.Screen.DeviceName != monitorName) {
                                        _items.Remove(itemName);
                                        continue;
                                    }
                                }
                                break;
                            case "Interval":
                                TimeSpan newInterval = new TimeSpan(0, 0, 0, int.Parse(value));
                                item.RenewInterval = newInterval;
                                break;
                            case "Click":
                                item.ClickExecutePath = value;
                                break;
                            case "Prepend":
                                item.PrependValue = value;
                                break;
                            case "Append":
                                item.AppendValue = value;
                                break;
                            case "DoNotShow":
                                item.DoNotShowMatch = value.Split(';');
                                break;
                            case "OnlyShow":
                                item.OnlyShowOnMatch = value.Split(';');
                                break;
                        }
                        _items[itemName] = item;
                    }
                    else {
                        if (settingName == "Path") {
                            _items[itemName] = new BarItem(value);
                        }
                    }
                }
            }
        }

        private void Bar_Load(object sender, EventArgs e) {
            Window thisWindow = new Window(this.Text, this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            thisWindow.AsyncResizing = false;
            thisWindow.Location = rect;
            BarWindow = thisWindow;
            Manager.WindowFocusChange += Manager_WindowFocusChange;
            Timer t = new Timer();
            t.Tick += (parent, args) => this.Redraw();
            t.Interval = int.Parse(Manager.settings.ReadSettingOrDefault("10000", "General.Bar.Refresh"));
            t.Start();

            int winStyles = (int) Win32API.GetWindowLong(this.Handle, Win32API.GWL_EXSTYLE);
            winStyles |= Win32API.WS_EX_TOOLWINDOW;
            Win32API.SetWindowLong(this.Handle, Win32API.GWL_EXSTYLE, winStyles);
        }

        private void Manager_WindowFocusChange(object sender, WindowEventArgs args) {
            Redraw();
        }

        private void Bar_FormClosing(object sender, FormClosingEventArgs e) {
            if (!InternalClosing) {
                Manager.SendMessage(Message.Close, Level.Global, 0);
            }
        }

        private void Bar_Shown(object sender, EventArgs e) {
            this.Top = Screen.FromHandle(this.Handle).Bounds.Top;
        }

        private void Bar_LocationChanged(object sender, EventArgs e) {
            forceLocation();
        }

        private void forceLocation() {
            Window thisWindow = new Window("", this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            rect.Y = Screen.FromHandle(this.Handle).Bounds.Y;
            rect.Width = thisWindow.Screen.WorkingArea.Width;
            thisWindow.Maximised = false;
            thisWindow.Location = rect;

            if (_parent.Bar != null)
            {
                Rectangle temp = _parent.Screen.WorkingArea;
                temp.Height = _parent.Screen.Bounds.Height - _parent.Bar.BarHeight;
                temp.Y = _parent.Screen.Bounds.Top + _parent.Bar.BarHeight;
                temp.Width = _parent.Bar.Width;
                _parent.Controlled = temp;
            }
        }

        private void addMouseAction(MouseButtons button, Rectangle area, Action action) {
            if (!_clicks.ContainsKey(button)) {
                _clicks[button] = new Dictionary<Rectangle, Action>();
            }
            _clicks[button][area] = action;
        }

        private void Bar_Paint(object sender, PaintEventArgs e) {
            _clicks.Clear();
            forceLocation();
            Manager.Log(new string('=', 30));
            Manager.Log("Starting draw");

            //Draw the tags display

            int height = this.Height;
            int screenHeight = _screen.Bounds.Height;
            int screenWidth = _screen.Bounds.Width;
            Manager.Log("Screen is {0}x{1}".With(screenWidth, screenHeight));
            int width = (int) ((screenWidth * height) / (float) screenHeight);
            Manager.Log("Each tag display is {0}x{1}".With(width, height));
            int currentWidth = 0;

            if (_parent.Screen == Screen.PrimaryScreen) { //We're primary
                Bitmap menuMap = new Bitmap(width, barHeight);
                using (Graphics gr = Graphics.FromImage(menuMap)) {
                    gr.DrawRectangle(seperatorPen, 0, 0, width - 1, barHeight - 1);
                    gr.DrawString("T", titleFont, foregroundBrush, (width / 2) - "T".Width(titleFont) / 2,
                                  height / 2 - "T".Height(titleFont) / 2);
                }
                e.Graphics.DrawImage(menuMap, 0, 0);
                addMouseAction(MouseButtons.Left, new Rectangle(0, 0, width, barHeight),
                               (() => Menu.Show(new Point(0, 0))));
                currentWidth += width;
            }

            Size previewSize = new Size(width, height);
            int tag = 1;
            int originalWidth = width;
            if (_parent.screens.Any(screen => screen == null) || !Manager.monitors.Contains(_parent)) {
                return;
            }
            if (_parent.screens.Length > 1) {
                foreach (TagScreen screen in _parent.screens) {
                    width = originalWidth;
                    string tagName = getTagName(tag);
                    if (tagName.Width(titleFont) > width) {
                        width = (int) (tagName.Width(titleFont) * 1.2);
                    }
                    Rectangle drawTangle = new Rectangle(currentWidth, 0, width - 1, this.Height - 1);

                    int tag1 = tag - 1;
                    addMouseAction(MouseButtons.Left, drawTangle,
                                   (() => Manager.SendMessage(Message.Screen, Level.Monitor, tag1)));
                    addMouseAction(MouseButtons.Middle, drawTangle,
                                   (() => Manager.SendMessage(Message.LayoutRelative, Level.Monitor, tag1)));
                    addMouseAction(MouseButtons.Right, drawTangle,
                                   (() => Manager.SendMessage(Message.SwapTagWindow, Level.Monitor, tag1)));

                    Image state = screen.getStateImage(previewSize);
                    PointF tagPos = new PointF();
                    tagPos.X = (currentWidth) + (width / 2) - (tagName.Width(titleFont) / 2);
                    tagPos.Y = height / 2 - tagName.Height(titleFont) / 2;

                    e.Graphics.DrawRectangle(new Pen(Color.White), drawTangle);
                    e.Graphics.DrawImage(state, drawTangle);
                    if (_parent.IsTagEnabled(tag1)) {
                        e.Graphics.FillRectangle(selectedBrush, drawTangle);
                        e.Graphics.DrawString(tagName, tag1 == _parent.GetActiveTag() ? boldFont : titleFont, foregroundBrush, tagPos);
                    }
                    else {
                        e.Graphics.DrawString(tagName, titleFont, foregroundBrush, tagPos);
                    }
                    currentWidth += width;
                    tag++;
                }
            }

            //Generate the additional items
            List<Image> additionalImages = new List<Image>();
            foreach (BarItem item in (from kvPair in _items select kvPair.Value)) {

                if (item.LastRenew > DateTime.Now.Ticks - item.RenewInterval.Ticks) {
                    additionalImages.Add(item.Value);
                    continue;
                }
                if (item.IsBuiltIn) {
                    try {
                        if (!_barPlugins.ContainsKey(item.Path)) {
                            continue;
                        }
                        IPluginBarItem barItem = (IPluginBarItem)Activator.CreateInstance(
                            _barPlugins[item.Path],
                            new object[] { this });

                        barItem.SetAppend(item.AppendValue);
                        barItem.SetPrepend(item.PrependValue);
                        barItem.SetBackColour(item.BackColour);
                        barItem.SetForeColour(item.ForeColour);
                        barItem.SetDoNotShow(item.DoNotShowMatch);
                        barItem.SetOnlyShows(item.OnlyShowOnMatch);
                        Bitmap bitmap = barItem.Draw(item.Argument);
                        if (bitmap != null) {
                            additionalImages.Add(bitmap);
                            item.Value = bitmap;
                        }
                    }
                    catch {
                        //Oh god everything is exploding
                    }

                }
                else {
                    try {
                        Process process = new Process();
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.FileName = item.Path;
                        process.StartInfo.Arguments = item.Argument;
                        process.StartInfo.CreateNoWindow = true;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        if (output.Length == 0 ||
                            (item.DoNotShowMatch!=null && item.DoNotShowMatch.Any(match => Regex.IsMatch(output, match))) ||
                            (item.OnlyShowOnMatch!=null && !item.OnlyShowOnMatch.Any(match=>Regex.IsMatch(output, match)))
                            ) {
                                continue;
                        }
                        output = "{0}{1}{2}".With(item.PrependValue, output, item.AppendValue);
                        int itemWidth = output.Width(titleFont);
                        Bitmap itemMap = new Bitmap(itemWidth + 5, height);

                        using (Graphics gr = Graphics.FromImage(itemMap)) {
                            gr.FillRectangle(item.BackColour, 0, 0, itemMap.Width, itemMap.Height);
                            gr.DrawString(output, titleFont, item.ForeColour, 2, 0);
                        }

                        additionalImages.Add(itemMap);
                        item.Value = itemMap;
                    }
                    catch (Win32Exception) {
                        continue; //Oh god everything is exploding (404: File Not Found)
                    }
                }
                item.LastRenew = DateTime.Now.Ticks;
            }

            //Draw the window list in the space remaining
            int startingPoint = currentWidth;
            int remainingWidth = screenWidth - startingPoint - (from image in additionalImages select image.Width).Sum();
            Manager.Log(
                "Between the tag display and date display, there is {0}px remaining for windows".With(remainingWidth));
            List<Bitmap> windowTiles = new List<Bitmap>();
            int selectedWindowID = -1;
            int index = 0;
            foreach (Window window in _parent.GetActiveScreen().windows) {
                Window focussedWindow = Manager.GetFocussedMonitor().GetActiveScreen().GetFocusedWindow();
                if (focussedWindow != null) {
                    if (focussedWindow.handle == window.handle) {
                        selectedWindowID = index;
                        Manager.Log("There is a focussed window. It is \"{0}\"".With(window.Title));
                        break;
                    }
                }
                index++;
            }
            index = 0;
            int numWindows = _parent.GetActiveScreen().windows.Count;
            Manager.Log("There are {0} windows under this bar's monitor's active screen's domain".With(numWindows));
            if (numWindows > 0) {
                int originalRoom = remainingWidth / numWindows;
                int room = originalRoom;
                Manager.Log("This gives us {0}px for each window".With(room));
                if (selectedWindowID != -1) {
                    if (selectedWindowID != -1) {
                        int selectedWidth =
                            Math.Max(_parent.GetActiveScreen().windows[selectedWindowID].Title.Width(titleFont), room);
                        room = (remainingWidth - selectedWidth) / _parent.GetActiveScreen().windows.Count();
                        Manager.Log(
                            "After adjusting for giving the active window more space, each window gets {0}px".With(room));
                    }
                }
                foreach (Window window in _parent.GetActiveScreen().windows) {
                    bool drawFocussed = false;
                    if (index == selectedWindowID) {
                        drawFocussed = true;
                        window.UpdateTitle();
                    }
                    string windowTitle = window.Title;
                    if (windowTitle == "") {
                        windowTitle = window.ClassName;
                    }
                    Bitmap windowMap = new Bitmap("{3}{0}{2} {1}".With(index + 1, windowTitle, _tilingStyleSymbols[window.TilingType],
                                                                       window.TopMost ? "+" : "").Width(titleFont), height);
                    Graphics windowGraphics = Graphics.FromImage(windowMap);
                    windowGraphics.FillRectangle(drawFocussed ? backgroundBrush : backgroundBrush2, 0, 0,
                                                 windowMap.Width, windowMap.Height);
                    windowGraphics.DrawString(
                        "{3}{0}{2} {1}".With(index + 1, windowTitle, _tilingStyleSymbols[window.TilingType], window.TopMost ? "+" : ""), titleFont,
                        drawFocussed ? foregroundBrush : foregroundBrush2,
                        0, 0);
                    windowGraphics.Dispose();
                    windowTiles.Add(windowMap);
                    index++;
                }
                int drawIndex = 0;
                foreach (Bitmap windowTile in windowTiles) {
                    Rectangle drawRect;
                    Color fadeOutColor = Color.Empty;
                    if (drawIndex != selectedWindowID) {
                        drawRect = new Rectangle(currentWidth, 0, room, height);
                        fadeOutColor = Color.Black;
                    }
                    else {
                        int totalNotMe = 0;
                        for (int i = 0; i < windowTiles.Count; i++) {
                            if (i != drawIndex) {
                                totalNotMe += room;
                            }
                        }
                        int remainingRoom = remainingWidth - totalNotMe;
                        drawRect = new Rectangle(currentWidth, 0, Math.Max(windowTile.Width, remainingRoom), height);
                    }
                    e.Graphics.FillRectangle(drawIndex != selectedWindowID ? backgroundBrush2 : backgroundBrush,
                                             drawRect);
                    e.Graphics.DrawImageUnscaled(windowTile, drawRect);

                    int index1 = drawIndex;
                    addMouseAction(MouseButtons.Left, drawRect,
                                   (() => Manager.SendMessage(Message.FocusThis, Level.Screen, index1)));
                    addMouseAction(MouseButtons.Middle, drawRect,
                                   (() => {
                                        Manager.SendMessage(Message.FocusThis, Level.Screen, index1);
                                        Manager.SendMessage(Message.Close, Level.Screen, index1);
                                    }));
                    addMouseAction(MouseButtons.Right, drawRect, (() => {
                                                                      Manager.SendMessage(Message.FocusThis,
                                                                                          Level.Screen, 0);
                                                                      Manager.SendMessage(Message.SwitchThis,
                                                                                          Level.Screen, index1);
                                                                      Manager.SendMessage(Message.FocusThis,
                                                                                          Level.Screen, 0);
                                                                  }));

                    Rectangle newRect = drawRect;
                    newRect.Width = 30;
                    newRect.X = drawRect.Right - 30;
                    Brush gradientBrush = new LinearGradientBrush(newRect, Color.FromArgb(0, fadeOutColor), fadeOutColor,
                                                                  0.0);
                    e.Graphics.FillRectangle(gradientBrush, newRect);
                    drawIndex++;
                    //if (drawIndex < windowTiles.Count)
                    e.Graphics.DrawLine(seperatorPen, drawRect.Right - 1, 0, drawRect.Right - 1, drawRect.Height);
                    currentWidth += drawRect.Width;
                }
            }

            currentWidth = Width - (from image in additionalImages select image.Width).Sum();

            //Draw the additional bits to the form
            int itemIndex = 0;
            foreach (Image additionalImage in additionalImages) {
                e.Graphics.DrawImage(additionalImage, currentWidth, 0);
                BarItem item = _items.ElementAt(itemIndex++).Value;
                if (item.ClickExecutePath != "") {
                    Rectangle drawTangle = new Rectangle(currentWidth, 0, additionalImage.Width, height);
                    Action action;
                    if (item.ClickExecutePath == "Next Layout") {
                        action = (() => Manager.SendMessage(Message.LayoutRelative, Level.Monitor, _parent.GetEnabledTags().First()));
                    }
                    else if (item.ClickExecutePath == "Switcher") {
                        action = (() => Manager.Switcher.Show());
                    }
                    else {
                        action = (() => Process.Start(item.ClickExecutePath));
                    }
                    addMouseAction(MouseButtons.Left, drawTangle, action);
                }
                currentWidth += additionalImage.Width;
                e.Graphics.DrawLine(seperatorPen, currentWidth - 1, 0, currentWidth - 1, barHeight);
            }

            if (Manager.GetFocussedMonitor() != _parent) {
                e.Graphics.FillRectangle(coverBrush, this.ClientRectangle);
            }
        }

        private string getTagName(int tag) {
            if (tagStyle == "roman") {
                return RomanNumeral.ToRoman(tag);
            }
            Func<int, string> tagFromStartingPoint = (start => (start + (tag - 1)).ToString());
            if (tagStyle == "ALPHABET") {
                return tagFromStartingPoint('A');
            }
            if (tagStyle == "alphabet") {
                return tagFromStartingPoint('a');
            }
            if (tagStyle == "symbol") {
                return tagFromStartingPoint('!');
            }
            if (tagStyle == "thing") {
                return tagFromStartingPoint(172);
            }

            if (tagStyle == "custom") {
                return Manager.settings.ReadSettingOrDefault(tag.ToString(), _parent.SafeName, tag.ToString(), "Name");
            }
            return tag.ToString();

        }

        public void Redraw() {
            this.Invalidate();
        }

        private void Bar_MouseDown(object sender, MouseEventArgs e) {
            if (Manager.GetFocussedMonitor() != _parent) {
                BarWindow.Activate();
            }
            Manager.Log("Caught click event on bar", 4);
            if (_clicks.ContainsKey(e.Button)) {
                Dictionary<Rectangle, Action> clickType = _clicks[e.Button];
                foreach (KeyValuePair<Rectangle, Action> click in clickType) {
                    if (click.Key.ContainsPoint(this.PointToClient(MousePosition))) {
                        Manager.Log(
                            "Click ({1}) was over a clickable area ({0})".With(click.Key,
                                                                               this.PointToClient(MousePosition)), 4);
                        click.Value();
                    }
                }
            }
        }

        protected override CreateParams CreateParams {
            get {
                CreateParams param = base.CreateParams;
                param.ExStyle = (param.ExStyle | Win32API.WS_EX_NOACTIVATE);
                return param;
            }
        }
    }
}