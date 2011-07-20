using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    static class rectangleExtensions {
        public static bool ContainsPoint(this Rectangle rect, Point point) {
            if (point.X > rect.Left && point.X < rect.Right) {
                if (point.Y > rect.Top && point.Y < rect.Bottom) {
                    return true;

                }
            }
            return false;
        }
        public static Point Center(this Rectangle rect) {
            return new Point(rect.Left + rect.Width/2, rect.Top + rect.Height/2);
        }
    }
}
