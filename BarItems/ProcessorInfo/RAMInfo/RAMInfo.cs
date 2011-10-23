using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Extensions;
using TWiME;

namespace ProcessorInfo {
    public class RAMInfo : IPluginBarItem {
        private string _prepend = "", _append = "";
        private Brush _foreground, _background;
        private string[] _doNotShows = null;
        private string[] _onlyShows = null;

        private Font _font;
        private int _height;

        public RAMInfo(Bar parent) {
            _background = new SolidBrush(
                Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.Bar.UnselectedBackgroundColour")));
            _foreground = new SolidBrush(
                Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Bar.SelectedForeground")));

            _font = parent.TitleFont;
            _height = parent.BarHeight;
        }

        public Bitmap Draw(string argument) {
            ulong bytes;
            if (argument == "total") {
                bytes = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            }
            else {
                bytes = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory;
            }
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "EB" };
            int magnitude = 0;
            double endresult = bytes;
            while (endresult > 1024.0f) {
                endresult /= 1024.0f;
                magnitude++;
            }
            string outString = "{0}{1:0.00}{2}{3}".With(_prepend, endresult, sizes[magnitude], _append);
            Bitmap image = makeImage(outString);
            return image;
        }

        private Bitmap makeImage(string output) {
            int itemWidth = output.Width(_font);
            Bitmap itemMap = new Bitmap(itemWidth + 5, _height);

            using (Graphics gr = Graphics.FromImage(itemMap)) {
                gr.FillRectangle(_background, 0, 0, itemMap.Width, itemMap.Height);
                gr.DrawString(output, _font, _foreground, 2, 0);
            }

            return itemMap;
        }

        public void SetPrepend(string prepend) {
            _prepend = prepend;
        }

        public void SetAppend(string append) {
            _append = append;
        }

        public void SetDoNotShow(string[] doNotShows) {
            _doNotShows = doNotShows;
        }

        public void SetOnlyShows(string[] onlyShows) {
            _onlyShows = onlyShows;
        }

        public void SetBackColour(Brush backcolour) {
            _background = backcolour;
        }

        public void SetForeColour(Brush forecolour) {
            _foreground = forecolour;
        }
    }
}
