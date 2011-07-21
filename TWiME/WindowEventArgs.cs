using System;
using System.Windows.Forms;

namespace TWiME {
    public class WindowEventArgs : EventArgs {
        private Screen _monitor;

        public Screen monitor {
            get { return _monitor; }
        }

        public WindowEventArgs(Screen monitor) {
            _monitor = monitor;
        }
    }
}