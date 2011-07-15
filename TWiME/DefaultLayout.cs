using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    class DefaultLayout : Layout, ILayout {
        private string _name;
        private Image _symbol;
        private List<Window> _windowList;
        private float splitter = 0.7f;
        public DefaultLayout(List<Window> windowList) {
            _windowList = windowList;

        }

        public new void updateWindowList(List<Window> windowList) {
            _windowList = windowList;
        }

        public new void assert() {
            if (_windowList.Count == 0) {
                return;
            }
            if (_windowList.Count == 1) {
                _windowList[0].Location = _windowList[0].screen.WorkingArea;
                return;
            }
            Window mainWindow = _windowList[0];
            int width = (int)(mainWindow.screen.WorkingArea.Width * splitter);
            int height = mainWindow.screen.WorkingArea.Height;
            int x = mainWindow.screen.WorkingArea.X;
            int y = mainWindow.screen.WorkingArea.Y;
            Rectangle newRect = new Rectangle(x, y, width, height);
            mainWindow.Location = newRect;

            int secondaryHeight = mainWindow.screen.WorkingArea.Height / (_windowList.Count - 1);
            for (int i = 1; i < _windowList.Count; i++) {
                Window window = _windowList[i];
                int nx = window.screen.WorkingArea.Left + width;
                int ny = window.screen.WorkingArea.Top + secondaryHeight * (i - 1);
                int nwidth = window.screen.WorkingArea.Width - width;
                Rectangle secondaryRect = new Rectangle(nx, ny, nwidth, secondaryHeight);
                window.Location = secondaryRect;
            }
        }

        public string name() {
            throw new NotImplementedException();
        }

        public Image symbol() {
            throw new NotImplementedException();
        }
    }
}
