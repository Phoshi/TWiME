using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    public partial class Bar : Form {
        public Window bar { get; internal set; }
        const UInt32 WS_OVERLAPPED = 0;
        const UInt32 WS_POPUP = 0x80000000;
        const UInt32 WS_CHILD = 0x40000000;
        const UInt32 WS_MINIMIZE = 0x20000000;
        const UInt32 WS_VISIBLE = 0x10000000;
        const UInt32 WS_DISABLED = 0x8000000;
        const UInt32 WS_CLIPSIBLINGS = 0x4000000;
        const UInt32 WS_CLIPCHILDREN = 0x2000000;
        const UInt32 WS_MAXIMIZE = 0x1000000;
        const UInt32 WS_CAPTION = 0xC00000;      // WS_BORDER or WS_DLGFRAME  
        const UInt32 WS_BORDER = 0x800000;
        const UInt32 WS_DLGFRAME = 0x400000;
        const UInt32 WS_VSCROLL = 0x200000;
        const UInt32 WS_HSCROLL = 0x100000;
        const UInt32 WS_SYSMENU = 0x80000;
        const UInt32 WS_THICKFRAME = 0x40000;
        const UInt32 WS_GROUP = 0x20000;
        const UInt32 WS_TABSTOP = 0x10000;
        const UInt32 WS_MINIMIZEBOX = 0x20000;
        const UInt32 WS_MAXIMIZEBOX = 0x10000;
        const UInt32 WS_TILED = WS_OVERLAPPED;
        const UInt32 WS_ICONIC = WS_MINIMIZE;
        const UInt32 WS_SIZEBOX = WS_THICKFRAME;
        private const int GWL_EXSTYLE = (-20);
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;


        private const int barHeight = 15;

        [DllImport("user32.dll")]
        private static extern
            int EnumWindows(EnumWindowsProc ewp, int lParam);
        [DllImport("user32.dll")]
        private static extern
            int GetWindowText(int hWnd, StringBuilder title, int size);
        [DllImport("user32.dll")]
        private static extern
            int GetWindowModuleFileName(int hWnd, StringBuilder title, int size);
        [DllImport("user32.dll")]
        private static extern
            bool IsWindowVisible(int hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr newLong);

        public delegate bool EnumWindowsProc(int hWnd, int lParam);

        private Screen _screen;
        private Monitor _parent;
        private Dictionary<MouseButtons, Dictionary<Rectangle, Action>> clicks = new Dictionary<MouseButtons, Dictionary<Rectangle, Action>>();

        public Bar(Monitor monitor) {
            InitializeComponent();
            _screen = monitor.screen;
            _parent = monitor;
            //this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = _screen.Bounds.Location;
            this.Width = _screen.Bounds.Width;
            this.Height = 10;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DesktopLocation = this.Location;
            //RegisterBar();
            this.BackColor = Color.DarkGray;

            this.ShowInTaskbar = false;
            bar = new Window("", this.Handle, "", "", true);
        }

        private void Bar_Load(object sender, EventArgs e) {
            Window thisWindow = new Window(this.Text, this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            thisWindow.Location = rect;
            RegisterBar();
            Manager.WindowFocusChange += new Manager.WindowEventHandler(Manager_WindowFocusChange);
            Timer t = new Timer();
            t.Tick += new EventHandler((object parent, EventArgs args)=>this.redraw());
            t.Interval = 10000;
            t.Start();

            int winStyles = (int)GetWindowLong(this.Handle, GWL_EXSTYLE);
            winStyles |= WS_EX_TOOLWINDOW;
            SetWindowLong(this.Handle, GWL_EXSTYLE, (IntPtr) winStyles);
        }

        void Manager_WindowFocusChange(object sender, WindowEventArgs args) {
            redraw();
        }





        [StructLayout(LayoutKind.Sequential)]
        struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        enum ABMsg : int {
            ABM_NEW = 0,
            ABM_REMOVE,
            ABM_QUERYPOS,
            ABM_SETPOS,
            ABM_GETSTATE,
            ABM_GETTASKBARPOS,
            ABM_ACTIVATE,
            ABM_GETAUTOHIDEBAR,
            ABM_SETAUTOHIDEBAR,
            ABM_WINDOWPOSCHANGED,
            ABM_SETSTATE
        }
        enum ABNotify : int {
            ABN_STATECHANGE = 0,
            ABN_POSCHANGED,
            ABN_FULLSCREENAPP,
            ABN_WINDOWARRANGE
        }
        enum ABEdge : int {
            ABE_LEFT = 0,
            ABE_TOP,
            ABE_RIGHT,
            ABE_BOTTOM
        }

        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);
        [DllImport("USER32")]
        static extern int GetSystemMetrics(int Index);
        [DllImport("User32.dll", ExactSpelling = true,
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool MoveWindow
            (IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string msg);

        private bool fBarRegistered;
        private int uCallBack;

        private void RegisterBar() {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            if (!fBarRegistered) {
                uCallBack = RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = uCallBack;

                uint ret = SHAppBarMessage((int)ABMsg.ABM_NEW, ref abd);
                fBarRegistered = true;

                ABSetPos();
            }
            else {
                SHAppBarMessage((int)ABMsg.ABM_REMOVE, ref abd);
                fBarRegistered = false;
            }
        }
        private void ABSetPos() {
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(abd);
            abd.hWnd = this.Handle;
            abd.uEdge = (int)ABEdge.ABE_TOP;

            abd.rc.left = Screen.FromHandle(this.Handle).Bounds.Left;
            abd.rc.right = Screen.FromHandle(this.Handle).Bounds.Width;
            abd.rc.top = Screen.FromHandle(this.Handle).Bounds.Top;
            abd.rc.bottom = Size.Height;
                

            SHAppBarMessage((int)ABMsg.ABM_QUERYPOS, ref abd);

            switch (abd.uEdge) {
                case (int)ABEdge.ABE_LEFT:
                    abd.rc.right = abd.rc.left + Size.Width;
                    break;
                case (int)ABEdge.ABE_RIGHT:
                    abd.rc.left = abd.rc.right - Size.Width;
                    break;
                case (int)ABEdge.ABE_TOP:
                    abd.rc.bottom = abd.rc.top + this.Size.Height;
                    break;
                case (int)ABEdge.ABE_BOTTOM:
                    abd.rc.top = abd.rc.bottom - Size.Height;
                    break;
            }

            SHAppBarMessage((int)ABMsg.ABM_SETPOS, ref abd);
            MoveWindow(abd.hWnd, abd.rc.left, abd.rc.top,
                    abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top, true);
        }

        private void Bar_FormClosing(object sender, FormClosingEventArgs e) {
            RegisterBar();
        }

        private void Bar_Shown(object sender, EventArgs e) {
            this.Top = Screen.FromHandle(this.Handle).Bounds.Top;
        }

        private void Bar_LocationChanged(object sender, EventArgs e) {
            Window thisWindow = new Window("", this.Handle, "", "", true);
            Rectangle rect = thisWindow.Location;
            rect.Height = barHeight;
            rect.Y = Screen.FromHandle(this.Handle).Bounds.Y;
            thisWindow.maximised = false;
            thisWindow.Location = rect;
        }
        private void addMouseAction(MouseButtons button, Rectangle area, Action action) {
            if (!clicks.ContainsKey(button)) {
                clicks[button] = new Dictionary<Rectangle, Action>();
            }
            clicks[button][area] = action;
        }

        private void Bar_Paint(object sender, PaintEventArgs e) {
            Font titleFont = new Font("Segoe UI", barHeight * 0.6f);
            Font boldFont = new Font(titleFont, FontStyle.Bold);
            Brush foregroundBrush = new SolidBrush(Color.Black);
            Brush foregroundBrush2 = new SolidBrush(Color.LightGray);
            Brush backgroundBrush = new SolidBrush(Color.DarkGray);
            Brush backgroundBrush2 = new SolidBrush(Color.Black);
            Brush selectedBrush = new SolidBrush(Color.FromArgb(128, Color.White));

            clicks.Clear();

            Pen seperatorPen = new Pen(Color.Blue, 3);
            Manager.log(new string('=', 30));
            Manager.log("Starting draw");

            //Draw the tags display

            int height = this.Height;
            int screenHeight = _screen.Bounds.Height;
            int screenWidth = _screen.Bounds.Width;
            Manager.log("Screen is {0}x{1}".With(screenWidth, screenHeight));
            int width = (int) ((screenWidth * height) / (float)screenHeight);
            Manager.log("Each tag display is {0}x{1}".With(width, height));
            int currentWidth = 0;
            Size previewSize = new Size(width, height);
            int tag = 1;
            foreach (TagScreen screen in _parent.screens) {
                Rectangle drawTangle = new Rectangle(currentWidth, 0, width - 1, this.Height - 1);

                int tag1 = tag - 1;
                addMouseAction(MouseButtons.Left, drawTangle, (() => Manager.sendMessage(Message.Screen, Level.monitor, tag1)));
                addMouseAction(MouseButtons.Middle, drawTangle, (() => Manager.sendMessage(Message.LayoutRelative, Level.monitor, tag1)));
                addMouseAction(MouseButtons.Right, drawTangle, (() => Manager.sendMessage(Message.SwapTagWindow, Level.monitor, tag1)));

                Image state = screen.getStateImage(previewSize);
                e.Graphics.DrawRectangle(new Pen(Color.White), drawTangle);
                PointF tagPos = new PointF();
                tagPos.X = (currentWidth) + (width / 2) - (tag.ToString().Width(titleFont) / 2);
                tagPos.Y = height / 2 - tag.ToString().Height(titleFont) / 2;
                e.Graphics.DrawImage(state, drawTangle);
                if (tag1 == _parent.tagEnabled) {
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
            Manager.log("Between the tag display and date display, there is {0}px remaining for windows".With(remainingWidth));
            List<Bitmap> windowTiles = new List<Bitmap>();
            int selectedWindowID = -1;
            int index = 0;
            foreach (Window window in _parent.getActiveScreen().windows) {
                Window focussedWindow = Manager.getFocussedMonitor().getActiveScreen().getFocusedWindow();
                if (focussedWindow != null) {
                    if (focussedWindow.handle == window.handle) {
                        selectedWindowID = index;
                        Manager.log("There is a focussed window. It is \"{0}\"".With(window.title));
                        break;
                    }
                }
                index++;
            }
            int totalWidth = 0;
            index = 0;
            int numWindows = _parent.getActiveScreen().windows.Count;
            Manager.log("There are {0} windows under this bar's monitor's active screen's domain".With(numWindows));
            if (numWindows > 0) {
                int originalRoom = remainingWidth / numWindows;
                int room = originalRoom;
                Manager.log("This gives us {0}px for each window".With(room));
                if (selectedWindowID != -1) {
                    if (selectedWindowID != -1) {
                        int selectedWidth = Math.Max(_parent.getActiveScreen().windows[selectedWindowID].title.Width(titleFont), room);
                        room = (remainingWidth - selectedWidth) / _parent.getActiveScreen().windows.Count();
                        Manager.log("After adjusting for giving the active window more space, each window gets {0}px".With(room));
                    }
                }
                foreach (Window window in _parent.getActiveScreen().windows) {
                    bool drawFocussed = false;
                    if (index == selectedWindowID) {
                        drawFocussed = true;
                        window.updateTitle();
                    }
                    int windowLength = window.title.Width(titleFont);
                    Bitmap windowMap;
                    string windowTitle = window.title;
                    if (windowTitle == "") {
                        windowTitle = window.className;
                    }
                    windowMap = new Bitmap(windowTitle.Width(titleFont), height);
                    Graphics windowGraphics = Graphics.FromImage(windowMap);
                    windowGraphics.FillRectangle(drawFocussed ? backgroundBrush : backgroundBrush2, 0, 0,
                                                 windowMap.Width, windowMap.Height);
                    windowGraphics.DrawString(window.title, titleFont, drawFocussed ? foregroundBrush : foregroundBrush2,
                                              0, 0);
                    windowGraphics.Dispose();
                    windowTiles.Add(windowMap);
                    totalWidth += windowLength;
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
                        int remainingRoom;
                        int i = 0;
                        int totalNotMe = 0;
                        foreach (Bitmap tile in windowTiles) {
                            if (i++ != drawIndex) {
                                totalNotMe += room;
                            }
                        }
                        remainingRoom = remainingWidth - totalNotMe;
                        drawRect = new Rectangle(currentWidth, 0, Math.Max(windowTile.Width, remainingRoom), height);
                    }
                    e.Graphics.FillRectangle(drawIndex != selectedWindowID ? backgroundBrush2 : backgroundBrush , drawRect);
                    e.Graphics.DrawImageUnscaled(windowTile, drawRect);

                    int index1 = drawIndex;
                    addMouseAction(MouseButtons.Left, drawRect, (() => Manager.sendMessage(Message.FocusThis, Level.screen, index1)));
                    addMouseAction(MouseButtons.Middle, drawRect, (() => Manager.sendMessage(Message.Close, Level.screen, index1)));
                    addMouseAction(MouseButtons.Right, drawRect, (() => {
                                                 Manager.sendMessage(Message.FocusThis, Level.screen, 0);
                                                 Manager.sendMessage(Message.SwitchThis, Level.screen, index1);
                                                 Manager.sendMessage(Message.FocusThis, Level.screen, 0);
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

            if (Manager.getFocussedMonitor() != _parent) {
                Brush coverBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
                e.Graphics.FillRectangle(coverBrush, this.ClientRectangle);
            }
        }

        public void redraw() {
            this.Invalidate();
        }

        private void Bar_MouseDown(object sender, MouseEventArgs e) {
            if (Manager.getFocussedMonitor() != _parent) {
                bar.activate();
            }
            Manager.log("Caught click event on bar", 4);
            if (clicks.ContainsKey(e.Button)) {
                Dictionary<Rectangle, Action> clickType = clicks[e.Button];
                foreach (KeyValuePair<Rectangle, Action> click in clickType) {
                    if (click.Key.ContainsPoint(this.PointToClient(MousePosition))) {
                        Manager.log(
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
