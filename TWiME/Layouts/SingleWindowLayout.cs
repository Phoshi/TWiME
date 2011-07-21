using System;
using System.Collections.Generic;
using System.Drawing;

namespace TWiME {
    internal class SingleWindowLayout : Layout, ILayout {
        private const string _name = "Single Window";
        private Image _symbol;
        private Rectangle _owned;
        private List<Window> _windowList;
        private float _splitter = 0.6f; //Don't use this one here
        private float _vSplitter = 0.6f;
        private TagScreen _parent;

        public SingleWindowLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
            _windowList = windowList;
            _owned = area;
            _parent = parent;

            _splitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.Screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "Splitter"));
            _vSplitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.Screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "VSplitter"));
        }

        public new void UpdateWindowList(List<Window> windowList) {
            _windowList = windowList;
        }

        private Dictionary<Window, Rectangle> generateLayout() {
            Dictionary<Window, Rectangle> layouts = new Dictionary<Window, Rectangle>();
            Rectangle rect = _owned;
            rect.Width -= 1;
            foreach (Window window in _windowList) {
                layouts[window] = rect;
            }
            return layouts;
        }

        public new void Assert() {
            var windows = generateLayout();
            List<Window> workingList = new List<Window>(_windowList);
            workingList.Reverse();
            foreach (Window window in workingList) {
                window.Location = windows[window];
            }
        }


        public new string Name() {
            return _name;
        }

        public new Image Symbol() {
            throw new NotImplementedException();
        }

        public new void MoveSplitter(float offset, bool vertical = false) {
            float newSplitter = (vertical ? _vSplitter : _splitter) + offset;
            if (newSplitter < 0) {
                newSplitter = 0;
            }
            if (newSplitter > 1) {
                newSplitter = 1;
            }
            if (!vertical) {
                _splitter = newSplitter;
            }
            else {
                _vSplitter = newSplitter;
            }
            Assert();
        }

        public new float GetSplitter(bool vertical = false) {
            return vertical ? _vSplitter : _splitter;
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
                if (_parent.getFocusedWindow() == pair.Key) {
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