using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    class SingleWindowLayout : Layout, ILayout {
        private string _name = "Single Window";
        private Image _symbol = null;
        private Rectangle _owned;
        private List<Window> _windowList;
        public float splitter = 0.6f; //Don't use this one here
        public float vsplitter = 0.6f;
        private TagScreen _parent;
        public SingleWindowLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
            _windowList = windowList;
            _owned = area;
            _parent = parent;

            splitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "Splitter"));
            vsplitter =
                float.Parse(Manager.settings.ReadSettingOrDefault(0.5f, parent.parent.screen.DeviceName.Replace(".", ""),
                                                                  parent.tag.ToString(), "VSplitter"));
        }

        public new void updateWindowList(List<Window> windowList) {
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

        public new void assert() {
            var windows = generateLayout();
            List<Window> workingList = new List<Window>(_windowList);
            workingList.Reverse();
            foreach (Window window in workingList) {
                window.Location = windows[window];
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
        public new float getSplitter(bool vertical = false) {
            if (vertical) {
                return vsplitter;
            }
            else {
                return splitter;
            }
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
