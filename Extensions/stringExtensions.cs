using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Extensions {
    public static class stringExtensions {
        public static int Width(this string str, Font withFont) {
            int widasdth = TextRenderer.MeasureText(str, withFont).Width;
            return width;
        }
    }
}
