using System;
using System.Collections.Generic;
using System.Drawing;

namespace TWiME {
    internal class DefaultLayout : Layout, ILayout {
        private const string _name = "Default";
        private Image _symbol;
        private Rectangle _owned;
        private List<Window> _windowList;
        public float splitter = 0.6f;
        public float vsplitter = 0.5f; //Doesn't matter, we don't use it here anyway
        private TagScreen _parent;

        public DefaultLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
            _windowList = windowList;
            _owned = area;
            _parent = parent;

            splitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.Screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "Splitter"));
            vsplitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.Screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "VSplitter"));
        }

        public new void UpdateWindowList(List<Window> windowList) {
            _windowList = windowList;
        }

        private Dictionary<Window, Rectangle> generateLayout() {
            Dictionary<Window, Rectangle> layouts = new Dictionary<Window, Rectangle>();
            if (_windowList.Count == 0) {
                return layouts;
            }
            Window mainWindow = _windowList[0];
            int width = (int) (_owned.Width * splitter);
            if (_windowList.Count == 1) {
                width = _owned.Width - 1;
            }
            int height = _owned.Height;
            int x = _owned.X;
            int y = _owned.Y;
            Rectangle newRect = new Rectangle(x, y, width, height);
            layouts[mainWindow] = newRect;

            if (_windowList.Count > 1) {
                int secondaryHeight = _owned.Height / (_windowList.Count - 1);
                for (int i = 1; i < _windowList.Count; i++) {
                    Window window = _windowList[i];
                    int nx = _owned.Left + width;
                    int ny = _owned.Top + secondaryHeight * (i - 1);
                    int nwidth = _owned.Width - width;
                    Rectangle secondaryRect = new Rectangle(nx, ny, nwidth, secondaryHeight);
                    layouts[window] = secondaryRect;
                }
            }
            return layouts;
        }

        public new void Assert() {
            foreach (KeyValuePair<Window, Rectangle> pair in generateLayout()) {
                pair.Key.Location = pair.Value;
            }
        }


        public new string Name() {
            return _name;
        }

        public new Image Symbol() {
            throw new NotImplementedException();
        }

        public new void MoveSplitter(float offset, bool vertical = false) {
            float newSplitter = (vertical ? vsplitter : splitter) + offset;
            if (newSplitter < 0) {
                newSplitter = 0;
            }
            if (newSplitter > 1) {
                newSplitter = 1;
            }
            if (!vertical) {
                splitter = newSplitter;
            }
            else {
                vsplitter = newSplitter;
            }
            Assert();
        }

        public new float GetSplitter(bool vertical = false) {
            return vertical ? vsplitter : splitter;
        }

        private Image generateStateImage(Size dimensions) {
            Bitmap state = new Bitmap(dimensions.Width, dimensions.Height);
            Graphics gr = Graphics.FromImage(state);
            //1680 * x = 40
            float scaleFactor = (float) (dimensions.Width) / (_owned.Width);
            foreach (KeyValuePair<Window, Rectangle> pair in generateLayout()) {
                Rectangle newRect = pair.Value;
                if (newRect.X < 0) {
                    newRect.Offset(-newRect.X, 0);
                }
                if (newRect.Y < 0) {
                    newRect.Offset(0, -newRect.Y);
                }
                newRect.Width = (int) (newRect.Width * scaleFactor);
                newRect.Height = (int) (newRect.Height * scaleFactor);
                newRect.X = (int) (newRect.X * scaleFactor);
                newRect.Y = (int) (newRect.Y * scaleFactor);
                Color winColor = Color.FromArgb(192, Color.White);
                if (_parent.GetFocusedWindow() == pair.Key) {
                    winColor = Color.FromArgb(255, Color.LightGray);
                }
                gr.FillRectangle(new SolidBrush(winColor), newRect);
                gr.DrawRectangle(new Pen(Color.Black), newRect);
            }
            gr.Dispose();
            return state;
        }

        public new Image StateImage(Size dimensions) {
            _symbol = generateStateImage(dimensions);
            return _symbol;
        }
    }
}