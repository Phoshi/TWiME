using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Extensions;

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr newLong);

        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        private Screen _screen;
        private Monitor _parent;

        private Dictionary<MouseButtons, Dictionary<Rectangle, Action>> clicks =
            new Dictionary<MouseButtons, Dictionary<Rectangle, Action>>();

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
        }

        private void Bar_Load(object sender, EventArgs e) {
            Window thisWindow = new Window(this.Text, this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            thisWindow.Location = rect;
            registerBar();
            Manager.WindowFocusChange += Manager_WindowFocusChange;
            Timer t = new Timer();
            t.Tick += (parent, args) => this.Redraw();
            t.Interval = 10000;
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
            thisWindow.Maximised = false;
            thisWindow.Location = rect;
        }

        private void addMouseAction(MouseButtons button, Rectangle area, Action action) {
            if (!clicks.ContainsKey(button)) {
                clicks[button] = new Dictionary<Rectangle, Action>();
            }
            clicks[button][area] = action;
        }

        private void Bar_Paint(object sender, PaintEventArgs e) {
            clicks.Clear();

            Pen seperatorPen = new Pen(Color.Blue, 3);
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
            Size previewSize = new Size(width, height);
            int tag = 1;
            foreach (TagScreen screen in _parent.screens) {
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

            //Draw the datetime display

            DateTime now = DateTime.Now;
            string dateString = now.ToString("yyyy/MM/dd | HH:mm");
            int dateWidth = dateString.Width(titleFont);
            Bitmap dateMap = new Bitmap(dateWidth + 15, height);

            using (Graphics gr = Graphics.FromImage(dateMap)) {
                gr.FillRectangle(backgroundBrush2, 0, 0, dateMap.Width, dateMap.Height);
                gr.DrawString(dateString, titleFont, foregroundBrush2, 0, 0);
            }

            //Draw the window list in the space remaining
            int startingPoint = currentWidth;
            int remainingWidth = screenWidth - startingPoint - dateMap.Width;
            Manager.Log(
                "Between the tag display and date display, there is {0}px remaining for windows".With(remainingWidth));
            List<Bitmap> windowTiles = new List<Bitmap>();
            int selectedWindowID = -1;
            int index = 0;
            foreach (Window window in _parent.GetActiveScreen().windows) {
                Window focussedWindow = Manager.GetFocussedMonitor().GetActiveScreen().getFocusedWindow();
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

            //Draw the time bit to the form
            e.Graphics.DrawImage(dateMap, screenWidth - dateMap.Width, 0);

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
            if (clicks.ContainsKey(e.Button)) {
                Dictionary<Rectangle, Action> clickType = clicks[e.Button];
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
}