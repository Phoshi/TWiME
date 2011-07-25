using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Extensions;
using Microsoft.Win32;
using Tree;

namespace TWiME {
    public sealed partial class Bar : Form {
        public Window bar { get; internal set; }

        private const int GWL_EXSTYLE = (-20);
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private int barHeight = 15;
        private Font titleFont;
        private Font boldFont;
        private Brush foregroundBrush;
        private Brush foregroundBrush2;
        private Brush backgroundBrush;
        private Brush backgroundBrush2;
        private Brush selectedBrush;
        private Pen seperatorPen;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr newLong);

        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        private Screen _screen;
        private Monitor _parent;

        private Dictionary<MouseButtons, Dictionary<Rectangle, Action>> _clicks =
            new Dictionary<MouseButtons, Dictionary<Rectangle, Action>>();
        private Dictionary<string, BarItem> _items = new Dictionary<string, BarItem>();

        private ContextMenuStrip menu;

        public Bar(Monitor monitor) {
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
            Color seperatorColour =
                Color.FromName(Manager.settings.ReadSettingOrDefault("Blue", "General.Bar.SeperatorColour"));
            int seperatorWidth = int.Parse(Manager.settings.ReadSettingOrDefault("3", "General.Bar.SeperatorWidth"));
            seperatorPen = new Pen(seperatorColour, seperatorWidth);
            _screen = monitor.Screen;
            _parent = monitor;
            //this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = _screen.Bounds.Location;
            this.Width = _screen.Bounds.Width;
            this.Height = 10;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DesktopLocation = this.Location;
            //RegisterBar();
            Color bColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Bar.BackColour"));
            this.BackColor = bColor;
            this.ShowInTaskbar = false;
            bar = new Window("", this.Handle, "", "", true);
            loadAdditionalItems();
            generateMenu();
        }

        private void generateMenu() {
            menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;
            menu.Font = titleFont;
            menu.BackColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Menu.Background"));
            menu.ForeColor =
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
                menu.Items.Add(collection[0]);
            }
        }

        [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra,
           [Out] StringBuilder pszOut, [In][Out] ref uint pcchOut);

        [Flags]
        enum AssocF {
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200
        }

        enum AssocStr {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic
        }

        public string GetAssociation(string doctype) {
            uint pcchOut = 0;   // size of output buffer

            // First call is to get the required size of output buffer
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, null, ref pcchOut);

            // Allocate the output buffer
            StringBuilder pszOut = new StringBuilder((int)pcchOut);

            // Get the full pathname to the program in pszOut
            AssocQueryString(AssocF.Verify, AssocStr.Executable, doctype, null, pszOut, ref pcchOut);
            string doc = pszOut.ToString();
            return doc;
        }


        private void runCommand(string command) {
            Dictionary<string, Action> specialCommands = new Dictionary<string, Action>();
            specialCommands["Edit TWiMErc"] = (() => {
                                                   Process.Start(GetAssociation(@".txt"), "_TWiMErc");
                                               });
            specialCommands["Quit"] = (() => Manager.SendMessage(Message.Close, Level.global, 0));
            specialCommands["Restart"] = (() => Manager.SendMessage(Message.Close, Level.global, 1));

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

        private ToolStripMenuItem buildMenu(Node<Action> root, int depth = 0) {
            if (root.Children.Count > 0) {
                ToolStripMenuItem thisItem = new ToolStripMenuItem(root.Name);
                ((ToolStripDropDownMenu) (thisItem.DropDown)).ShowImageMargin = false;
                thisItem.Font = titleFont;
                thisItem.BackColor = Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.Menu.Background"));
                thisItem.ForeColor =
                    Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Menu.Foreground"));
                foreach (Node<Action> child in root.Children) {
                    thisItem.DropDownItems.Add(buildMenu(child, depth + 1));
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
                    string itemName = list[1];
                    string settingName = list[2];
                    string value = Manager.settings.ReadSetting(list.ToArray());
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
                                string monitorName = Screen.AllScreens[int.Parse(value)].DeviceName;
                                if (_parent.Screen.DeviceName != monitorName) {
                                    _items.Remove(itemName);
                                    continue;
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
            bar = thisWindow;
            registerBar();
            Manager.WindowFocusChange += Manager_WindowFocusChange;
            Timer t = new Timer();
            t.Tick += (parent, args) => this.Redraw();
            t.Interval = int.Parse(Manager.settings.ReadSettingOrDefault("10000", "General.Bar.Refresh"));
            t.Start();

            int winStyles = (int) GetWindowLong(this.Handle, GWL_EXSTYLE);
            winStyles |= WS_EX_TOOLWINDOW;
            SetWindowLong(this.Handle, GWL_EXSTYLE, (IntPtr) winStyles);
        }

        private void Manager_WindowFocusChange(object sender, WindowEventArgs args) {
            Redraw();
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        private enum ABMsg {
            ABM_NEW = 0,
            ABM_REMOVE,
            ABM_QUERYPOS,
            ABM_SETPOS,
        }

        private enum ABEdge {
            ABE_LEFT = 0,
            ABE_TOP,
            ABE_RIGHT,
            ABE_BOTTOM
        }

        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        [DllImport("User32.dll", ExactSpelling = true,
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool MoveWindow
            (IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string msg);

        private bool fBarRegistered;
        private int uCallBack;

        private void registerBar() {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            if (!fBarRegistered) {
                uCallBack = RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = uCallBack;

                SHAppBarMessage((int) ABMsg.ABM_NEW, ref abd);
                fBarRegistered = true;

                abSetPos();
            }
            else {
                SHAppBarMessage((int) ABMsg.ABM_REMOVE, ref abd);
                fBarRegistered = false;
            }
        }

        private void abSetPos() {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            abd.uEdge = (int) ABEdge.ABE_TOP;

            abd.rc.left = Screen.FromHandle(this.Handle).Bounds.Left;
            abd.rc.right = Screen.FromHandle(this.Handle).Bounds.Width;
            abd.rc.top = Screen.FromHandle(this.Handle).Bounds.Top;
            abd.rc.bottom = Size.Height;


            SHAppBarMessage((int) ABMsg.ABM_QUERYPOS, ref abd);

            switch (abd.uEdge) {
                case (int) ABEdge.ABE_LEFT:
                    abd.rc.right = abd.rc.left + Size.Width;
                    break;
                case (int) ABEdge.ABE_RIGHT:
                    abd.rc.left = abd.rc.right - Size.Width;
                    break;
                case (int) ABEdge.ABE_TOP:
                    abd.rc.bottom = abd.rc.top + this.Size.Height;
                    break;
                case (int) ABEdge.ABE_BOTTOM:
                    abd.rc.top = abd.rc.bottom - Size.Height;
                    break;
            }

            SHAppBarMessage((int) ABMsg.ABM_SETPOS, ref abd);
            MoveWindow(abd.hWnd, abd.rc.left, abd.rc.top,
                       abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, true);
        }

        private void Bar_FormClosing(object sender, FormClosingEventArgs e) {
            registerBar();
        }

        private void Bar_Shown(object sender, EventArgs e) {
            this.Top = Screen.FromHandle(this.Handle).Bounds.Top;
        }

        private void Bar_LocationChanged(object sender, EventArgs e) {
            Window thisWindow = new Window("", this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            rect.Y = Screen.FromHandle(this.Handle).Bounds.Y;
            rect.Width = thisWindow.Screen.WorkingArea.Width;
            thisWindow.Maximised = false;
            thisWindow.Location = rect;
        }

        private void addMouseAction(MouseButtons button, Rectangle area, Action action) {
            if (!_clicks.ContainsKey(button)) {
                _clicks[button] = new Dictionary<Rectangle, Action>();
            }
            _clicks[button][area] = action;
        }

        private void Bar_Paint(object sender, PaintEventArgs e) {
            _clicks.Clear();

            Manager.Log(new string('=', 30));
            Manager.Log("Starting draw");

            //Draw the tags display

            int height = this.Height;
            int screenHeight = _screen.Bounds.Height;
            int screenWidth = _screen.Bounds.Width;
            Manager.Log("Screen is {0}x{1}".With(screenWidth, screenHeight));
            int width = (int) ((screenWidth * height) / (float) screenHeight);
            Manager.Log("Each tag display is {0}x{1}".With(width, height));
            int currentWidth = 20;

            addMouseAction(MouseButtons.Left, new Rectangle(0,0,20,barHeight),menu.Show);

            Size previewSize = new Size(width, height);
            int tag = 1;
            foreach (TagScreen screen in _parent.screens) {
                if (screen == null) {
                    return; //We're not set up yet, just give up and try again later
                }
                Rectangle drawTangle = new Rectangle(currentWidth, 0, width - 1, this.Height - 1);

                int tag1 = tag - 1;
                addMouseAction(MouseButtons.Left, drawTangle,
                               (() => Manager.SendMessage(Message.Screen, Level.monitor, tag1)));
                addMouseAction(MouseButtons.Middle, drawTangle,
                               (() => Manager.SendMessage(Message.LayoutRelative, Level.monitor, tag1)));
                addMouseAction(MouseButtons.Right, drawTangle,
                               (() => Manager.SendMessage(Message.SwapTagWindow, Level.monitor, tag1)));

                Image state = screen.getStateImage(previewSize);
                e.Graphics.DrawRectangle(new Pen(Color.White), drawTangle);
                PointF tagPos = new PointF();
                tagPos.X = (currentWidth) + (width / 2) - (tag.ToString().Width(titleFont) / 2);
                tagPos.Y = height / 2 - tag.ToString().Height(titleFont) / 2;
                e.Graphics.DrawImage(state, drawTangle);
                if (tag1 == _parent.EnabledTag) {
                    e.Graphics.FillRectangle(selectedBrush, drawTangle);
                    e.Graphics.DrawString(tag++.ToString(), boldFont, foregroundBrush, tagPos);
                }
                else {
                    e.Graphics.DrawString(tag++.ToString(), titleFont, foregroundBrush, tagPos);
                }
                currentWidth += width;
            }

            //Generate the additional items
            List<Image> additionalImages = new List<Image>();
            foreach (BarItem item in (from kvPair in _items select kvPair.Value)) {

                if (item.LastRenew > DateTime.Now.Ticks - item.RenewInterval.Ticks) {
                    additionalImages.Add(item.Value);
                    continue;
                }
                if (item.IsBuiltIn) {
                    if (item.Path == "time") {
                        DateTime now = DateTime.Now;
                        string dateString = item.PrependValue+now.ToString(item.Argument)+item.AppendValue;
                        int dateWidth = dateString.Width(titleFont);
                        Bitmap timeMap = new Bitmap(dateWidth + 5, height);

                        using (Graphics gr = Graphics.FromImage(timeMap)) {
                            gr.FillRectangle(item.BackColour, 0, 0, timeMap.Width, timeMap.Height);
                            gr.DrawString(dateString, titleFont, item.ForeColour, 0, 0);
                        }

                        additionalImages.Add(timeMap);
                        item.Value = timeMap;
                    }
                    if (item.Path == "Layout") {
                        Image layoutSymbol = _parent.GetActiveScreen().GetLayoutSymbol(previewSize);
                        additionalImages.Add(layoutSymbol);
                        item.Value = layoutSymbol;
                    }
                    if (item.Path == "Window Count") {
                        string countString = item.PrependValue + Manager.Windows.Count + item.AppendValue;
                        int countWidth = countString.Width(titleFont);
                        Bitmap countMap = new Bitmap(countWidth + 5, height);

                        using (Graphics gr = Graphics.FromImage(countMap)) {
                            gr.FillRectangle(item.BackColour, 0, 0, countMap.Width, countMap.Height);
                            gr.DrawString(countString, titleFont, item.ForeColour, 0, 0);
                        }
                        additionalImages.Add(countMap);
                        item.Value = countMap;
                    }
                }
                else {
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.FileName = item.Path;
                    process.StartInfo.Arguments = item.Argument;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    output = "{0}{1}{2}".With(item.PrependValue, output, item.AppendValue);
                    int itemWidth = output.Width(titleFont);
                    Bitmap itemMap = new Bitmap(itemWidth+5, height);

                    using (Graphics gr = Graphics.FromImage(itemMap)) {
                        gr.FillRectangle(item.BackColour, 0, 0, itemMap.Width, itemMap.Height);
                        gr.DrawString(output, titleFont, item.ForeColour, 2, 0);
                    }

                    additionalImages.Add(itemMap);
                    item.Value = itemMap;
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
                    Bitmap windowMap = new Bitmap("{0}) {1}".With(index + 1, windowTitle).Width(titleFont), height);
                    Graphics windowGraphics = Graphics.FromImage(windowMap);
                    windowGraphics.FillRectangle(drawFocussed ? backgroundBrush : backgroundBrush2, 0, 0,
                                                 windowMap.Width, windowMap.Height);
                    windowGraphics.DrawString("{0}) {1}".With(index + 1, windowTitle), titleFont,
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
                                   (() => Manager.SendMessage(Message.FocusThis, Level.screen, index1)));
                    addMouseAction(MouseButtons.Middle, drawRect,
                                   (() => Manager.SendMessage(Message.Close, Level.screen, index1)));
                    addMouseAction(MouseButtons.Right, drawRect, (() => {
                                                                      Manager.SendMessage(Message.FocusThis,
                                                                                          Level.screen, 0);
                                                                      Manager.SendMessage(Message.SwitchThis,
                                                                                          Level.screen, index1);
                                                                      Manager.SendMessage(Message.FocusThis,
                                                                                          Level.screen, 0);
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
                        action = (() => Manager.SendMessage(Message.LayoutRelative, Level.monitor, _parent.EnabledTag));
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
                Brush coverBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
                e.Graphics.FillRectangle(coverBrush, this.ClientRectangle);
            }
        }

        public void Redraw() {
            this.Invalidate();
        }

        private void Bar_MouseDown(object sender, MouseEventArgs e) {
            if (Manager.GetFocussedMonitor() != _parent) {
                bar.Activate();
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
                param.ExStyle = (param.ExStyle | WS_EX_NOACTIVATE);
                return param;
            }
        }
    }
    class BarItem {
        public int MaximumWidth, MinimumWidth;
        public bool IsBuiltIn;
        public string Path;
        public string Argument;
        public Brush ForeColour, BackColour;
        public Image Value;
        public long LastRenew;
        public TimeSpan RenewInterval;
        public string ClickExecutePath = "";
        public string PrependValue;
        public string AppendValue;

        public BarItem(string path, string argument="", bool builtIn = false, int minWidth = -1, int maxWidth = -1, Brush forecolour = null, Brush backcolour = null) {
            Path = path;
            Argument = argument;
            IsBuiltIn = builtIn;
            MinimumWidth = minWidth;
            MaximumWidth = maxWidth;
            BackColour = backcolour ?? new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.Bar.UnselectedBackgroundColour")));
            ForeColour = forecolour ?? new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Bar.SelectedForeground")));
            RenewInterval = new TimeSpan(0, 0, 0, 5); //5 seconds
        }
    }
}