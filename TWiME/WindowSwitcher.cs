using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Extensions;

namespace TWiME {
    public partial class WindowSwitcher : Form {
        private Font _windowFont;
        private Brush _foreground;
        private Brush _inactiveForeground;
        private Brush _selected;
        private Brush _background;
        private int _selectedIndex;
        private List<Window> _currentVisibleWindows = new List<Window>();
        public WindowSwitcher() {
            InitializeComponent();
        }

        private void WindowSwitcher_Load(object sender, EventArgs e) {
            string fontName = Manager.settings.ReadSettingOrDefault("Segoe UI", "General.WindowSwitcher.Font.Name");
            int fontSize = int.Parse(Manager.settings.ReadSettingOrDefault("8", "General.WindowSwitcher.Font.Size"));
            _windowFont = new Font(fontName, fontSize);

            _foreground =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("White", "General.WindowSwitcher.Foreground")));
            _inactiveForeground =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("Gray", "General.WindowSwitcher.Inactive")));
            _background =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.WindowSwitcher.Background")));
            _selected =
                new SolidBrush(
                    Color.FromName(Manager.settings.ReadSettingOrDefault("DarkGray", "General.WindowSwitcher.Selected")));

            filter.Focus();
        }

        private void filter_TextChanged(object sender, EventArgs e) {
            this.Invalidate();
            _selectedIndex = 0;
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData) {
            if (keyData == Keys.Tab) {
                _selectedIndex++;
                if (_selectedIndex >= _currentVisibleWindows.Count) {
                    _selectedIndex = 0;
                }
                this.Invalidate();
            }
            if (keyData == (Keys.Shift | Keys.Tab)) {
                _selectedIndex--;
                if (_selectedIndex < 0) {
                    _selectedIndex = _currentVisibleWindows.Count - 1;
                }
                this.Invalidate();
            }
            if (keyData == Keys.Return) {
                _currentVisibleWindows[_selectedIndex].ForceVisible();
                _currentVisibleWindows[_selectedIndex].Activate();
                this.Hide();
                Manager.ForcePoll();
            }
            if (keyData == (Keys.Shift | Keys.Return)) {
                _currentVisibleWindows[_selectedIndex].Visible = true;
                Manager.GetFocussedMonitor().GetActiveScreen().CatchWindow(_currentVisibleWindows[_selectedIndex]);
                Manager.GetFocussedMonitor().GetActiveScreen().AssertLayout();
                _currentVisibleWindows[_selectedIndex].Activate();
                Manager.ForcePoll();
                this.Hide();
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void WindowSwitcher_Paint(object sender, PaintEventArgs e) {
            int startPosition = filter.Location.X + filter.Size.Height + 5;
            int currentHeight = startPosition;

            e.Graphics.FillRectangle(_background, this.ClientRectangle);

            _currentVisibleWindows.Clear();
            int drawIndex = 0;
            foreach (Window window in (from window in (from screen in Manager.GetFocussedMonitor().screens select screen.windows).SelectMany(window=>window)
                                       orderby window.Visible descending, window.Title
                                       where window.Title.Glob(filter.Text)
                                       select window)) {
                if (drawIndex == _selectedIndex) {
                    e.Graphics.FillRectangle(_selected, 0, currentHeight, Width, window.Title.Height(_windowFont));
                }
                var activeTags =
                    (from tag in (from monitor in Manager.monitors select monitor.screens).SelectMany(x => x)
                        where tag.windows.Contains(window)
                        select tag.tag+1);
                int numTags = Manager.GetFocussedMonitor().screens.Length;
                int reverseWidthPosition = Width;
                for (int i = numTags; i > 0; i--) {
                    int numberWidth = i.ToString().Width(_windowFont);
                    reverseWidthPosition -= numberWidth;
                    e.Graphics.DrawString(i.ToString(), _windowFont, activeTags.Contains(i) ? _foreground : _inactiveForeground, reverseWidthPosition, currentHeight);
                }
                e.Graphics.DrawString(window.Title, _windowFont, _foreground, 0, currentHeight);
                _currentVisibleWindows.Add(window);
                currentHeight += window.Title.Height(_windowFont);
                drawIndex++;
            }
            if (Manager.GetFocussedMonitor().Screen.Bounds.Height < currentHeight) {
                currentHeight = Manager.GetFocussedMonitor().Screen.Bounds.Height;
            }
            Height = currentHeight;
        }

        private void WindowSwitcher_VisibleChanged(object sender, EventArgs e) {
            filter.Text = "";
            _selectedIndex = 0;
            Bar currentMonitorBar = Manager.GetFocussedMonitor().Bar;
            Location = new Point(currentMonitorBar.Left, currentMonitorBar.Bottom);
            Width = currentMonitorBar.Width;
        }

        private void WindowSwitcher_Deactivate(object sender, EventArgs e) {
            this.Hide();
        }
    }
}
