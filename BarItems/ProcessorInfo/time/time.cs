﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Extensions;
using TWiME;

namespace ProcessorInfo {
    public class time : IPluginBarItem {
        private string _prepend = "", _append = "";
        private Brush _foreground, _background;
        private string[] _doNotShows = null;
        private string[] _onlyShows = null;

        private Font _font;
        private int _height;
        private Bar _parent;

        public time(Bar parent) {
            _background = new SolidBrush(
                Color.FromName(Manager.settings.ReadSettingOrDefault("Black", "General.Bar.UnselectedBackgroundColour")));
            _foreground = new SolidBrush(
                Color.FromName(Manager.settings.ReadSettingOrDefault("LightGray", "General.Bar.SelectedForeground")));

            _font = parent.TitleFont;
            _height = parent.BarHeight;
            _parent = parent;
        }

        public Bitmap Draw(string argument) {
            DateTime now = DateTime.Now;
            string dateString = now.ToString(argument);
            string outString = "{0}{1}{2}".With(_prepend, dateString, _append);
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
