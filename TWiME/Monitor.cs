using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    class Monitor {
        private Rectangle _controlled;
        public Bar bar;
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

        public void catchMessage(HotkeyMessage message) {
            if (message.level == Level.monitor) {
                if (message.message == Message.MonitorSwitch) {
                    int newIndex = Manager.getFocussedMonitorIndex() + message.data;
                    if (newIndex < 0) {
                        newIndex = Manager.monitors.Count - 1;
                    }
                    if (newIndex >= Manager.monitors.Count) {
                        newIndex = 0;
                    }
                    Console.WriteLine("Switching to window "+Manager.monitors[newIndex].name);
                    Manager.monitors[newIndex].activate();
                }
            }
            else {
                foreach (TagScreen tagScreen in (from kvPair in tagsEnabled where kvPair.Value == true select tagScreens[kvPair.Key])) {
                    tagScreen.catchMessage(message);
                }
            }
        }

        private void activate() {
            var activeTags = from tag in tagsEnabled where tag.Value select tag.Key;
            if (activeTags.Count() > 0) {
                TagScreen screen = tagScreens[activeTags.ElementAt(0)];
                screen.activate();
            }
        }
    }
}
