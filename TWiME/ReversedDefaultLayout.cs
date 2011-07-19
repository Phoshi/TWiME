using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    class ReversedDefaultLayout : Layout, ILayout {
        private string _name;
        private Image _symbol = null;
        private Rectangle _owned;
        private List<Window> _windowList;
        public float splitter = 0.6f;
        public float vsplitter = 0.6f; //Don't use this anyway
        private TagScreen _parent;
        public ReversedDefaultLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
            _windowList = windowList;
            _owned = area;
            _parent = parent;

        }

        public new void updateWindowList(List<Window> windowList) {
            _windowList = windowList;
        }

        private Dictionary<Window, Rectangle> generateLayout() {
            Dictionary<Window, Rectangle> layouts = new Dictionary<Window, Rectangle>();
            if (_windowList.Count == 0) {
                return layouts;
            }
            Window mainWindow = _windowList[0];
            int width = (int)(_owned.Width * splitter);
            if (_windowList.Count == 1) {
                width = _owned.Width - 1;
            }
            int height = _owned.Height;
            int x = _owned.Left + _owned.Width - width;
            int y = _owned.Y;
            Rectangle newRect = new Rectangle(x, y, width, height);
            layouts[mainWindow] = newRect;

            if (_windowList.Count > 1) {
                int secondaryHeight = _owned.Height / (_windowList.Count - 1);
                for (int i = 1; i < _windowList.Count; i++) {
                    Window window = _windowList[i];
                    int nx = _owned.Left;
                    int ny = _owned.Top + secondaryHeight * (i - 1);
                    int nwidth = _owned.Width - width;
                    Rectangle secondaryRect = new Rectangle(nx, ny, nwidth, secondaryHeight);
                    layouts[window] = secondaryRect;
                }
            }
            return layouts;
        }

        public new void assert() {
            foreach (KeyValuePair<Window, Rectangle> pair in generateLayout()) {
                pair.Key.Location = pair.Value;
            }
        }


        public new string name() {
            return _name;
        }

        public new Image symbol() {
            throw new NotImplementedException();
        }
        public new void moveSplitter(float offset, bool vertical = false) {
            float newSplitter = (vertical ? vsplitter : splitter) + offset;
            if (newSplitter < 0) {
                newSplitter = 0;
            }
            if (newSplitter > 1) {
                newSplitter = 1;
            }
            if (!vertical)
                splitter = newSplitter;
            else {
                vsplitter = newSplitter;
            }
            assert();
        }
        private Image generateStateImage(Size dimensions) {
            Bitmap state = new Bitmap(dimensions.Width, dimensions.Height);
            Graphics gr = Graphics.FromImage(state);
            //1680 * x = 40
            float scaleFactor = (float)(dimensions.Width) / (_owned.Width);
            foreach (KeyValuePair<Window, Rectangle> pair in generateLayout()) {
                Rectangle newRect = pair.Value;
                if (newRect.X < 0) {
                    newRect.Offset(-newRect.X, 0);
                }
                if (newRect.Y < 0) {
                    newRect.Offset(0, -newRect.Y);
                }
                newRect.Width = (int)(newRect.Width * scaleFactor);
                newRect.Height = (int)(newRect.Height * scaleFactor);
                newRect.X = (int)(newRect.X * scaleFactor);
                newRect.Y = (int)(newRect.Y * scaleFactor);
                Color winColor = Color.FromArgb(192, Color.White);
                if (_parent.getFocusedWindow() == pair.Key) {
                    winColor = Color.FromArgb(255, Color.LightGray);
                }
                gr.FillRectangle(new SolidBrush(winColor), newRect);
                gr.DrawRectangle(new Pen(Color.Black), newRect);
            }
            gr.Dispose();
            return state;

        }
        public new Image stateImage(Size dimensions) {
            _symbol = generateStateImage(dimensions);
            return _symbol;
        }
    }
}
