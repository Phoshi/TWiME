using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    class Monitor {
        private Rectangle _controlled;
        private Bar bar;
        private TagScreen[] tagScreens = new TagScreen[9];
        public string name { get; internal set; }
        public Dictionary<int, bool> tagsEnabled { get; internal set; }
        public Monitor(Screen screen) {
            createBar(screen);
            createTagScreens();
            _controlled = screen.WorkingArea;
            name = screen.DeviceName;
        }

        private void createBar(Screen screen) {
            bar = new Bar(screen);
            bar.Show();
        }

        private void createTagScreens() {
            tagsEnabled = new Dictionary<int, bool>();
            for (int i = 0; i < 9; i++) {
                tagScreens[i] = new TagScreen(this, i);
                tagsEnabled[i] = false;
            }
            tagsEnabled[1] = true;
        }
    }
}
