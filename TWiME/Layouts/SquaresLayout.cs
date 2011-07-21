using System;
using System.Collections.Generic;
using System.Drawing;

namespace TWiME {
    internal class SquaresLayout : Layout, ILayout {
        private const string _name = "Docked";
        private Image _symbol;
        private Rectangle _owned;
        private List<Window> _windowList;
        public float splitter = 0.6f; //Don't use this one here
        public float vsplitter = 0.6f;
        private TagScreen _parent;

        public SquaresLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
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
            double numRows = Math.Ceiling(Math.Pow(_windowList.Count, 0.5));
            double numColumns = Math.Ceiling(_windowList.Count / numRows);
            int winWidth = (int) (_owned.Width / numRows);
            int winHeight = (int) (_owned.Height / numColumns);
            int row = 0, column = 0;
            foreach (Window window in _windowList) {
                if (column == numColumns - 1) {
                    if ((numRows * numColumns) != _windowList.Count) {
                        int shortfall = (int) ((numRows * numColumns) - _windowList.Count);
                        winWidth = (int) (_owned.Width / (numRows - shortfall));
                    }
                }
                int thisWinLeft = _owned.Left + (winWidth * row);
                int thisWinTop = _owned.Top + (winHeight * column);
                Rectangle thisWinRect = new Rectangle(thisWinLeft, thisWinTop, winWidth, winHeight);
                layouts[window] = thisWinRect;
                if (++row >= numRows) {
                    row = 0;
                    column++;
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