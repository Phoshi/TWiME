using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    public interface IPluginBarItem {
        Bitmap Draw(string argument);
        void SetPrepend(string prepend);
        void SetAppend(string append);
        void SetForeColour(Brush forecolour);
        void SetBackColour(Brush backcolour);
        void SetDoNotShow(string[] doNotShows);
        void SetOnlyShows(string[] onlyShows);
    }
}
