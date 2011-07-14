using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TWiME {
    class WindowEventArgs : EventArgs {
        private Screen _monitor;

        public Screen monitor { get { return _monitor; } }

        public WindowEventArgs(Screen monitor) {
            _monitor = monitor;
        }
    }
}
