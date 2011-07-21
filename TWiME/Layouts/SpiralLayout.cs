using System;
using System.Collections.Generic;
using System.Drawing;

namespace TWiME {
    internal class SpiralLayout : Layout, ILayout {
        private const string _name = "Spiral";
        private Image _symbol;
        private Rectangle _owned;
        private List<Window> _windowList;
        public float splitter = 0.5f;
        public float vsplitter = 0.5f;
        private TagScreen _parent;

        public SpiralLayout(List<Window> windowList, Rectangle area, TagScreen parent) {
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

        private enum Direction {
            Left,
            Right,
            Up,
            Down
        }

        private Dictionary<Window, Rectangle> generateLayout() {
            Dictionary<Window, Rectangle> layouts = new Dictionary<Window, Rectangle>();
            Rectangle rectangle = _owned;
            rectangle.Width -= 1; //Work around the "Windows set to as large as the screenspace are maximised" issue
            Direction direction = Direction.Right;
            int workingIndex = 0;
            foreach (Window window in _windowList) {
                if (workingIndex + 1 == _windowList.Count) {
                    layouts[window] = rectangle;
                    break;
                }
                if (direction == Direction.Right) {
                    Rectangle winTangle = rectangle;
                    winTangle.Width = (int) (winTangle.Width * splitter);
                    layouts[window] = winTangle;
                    rectangle.Width -= winTangle.Width;
                    rectangle.X = winTangle.Right;
                    direction = Direction.Down;
                }
                else if (direction == Direction.Down) {
                    Rectangle winTangle = rectangle;
                    winTangle.Height = (int) (winTangle.Height * vsplitter);
                    layouts[window] = winTangle;
                    rectangle.Height -= winTangle.Height;
                    rectangle.Y = winTangle.Bottom;
                    direction = Direction.Left;
                }
                else if (direction == Direction.Left) {
                    Rectangle winTangle = rectangle;
                    winTangle.Width = (int) (winTangle.Width * splitter);
                    winTangle.X += rectangle.Width - winTangle.Width;
                    layouts[window] = winTangle;
                    rectangle.Width -= winTangle.Width;
                    direction = Direction.Up;
                }
                else if (direction == Direction.Up) {
                    Rectangle winTangle = rectangle;
                    winTangle.Height = (int) (winTangle.Height * vsplitter);
                    winTangle.Y += rectangle.Height - winTangle.Height;
                    layouts[window] = winTangle;
                    rectangle.Height -= winTangle.Height;
                    direction = Direction.Right;
                }
                workingIndex++;
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