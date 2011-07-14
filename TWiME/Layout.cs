using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TWiME {
    internal interface ILayout {
        void assert();
        string name();
        Image symbol();
        void updateWindowList(List<Window> newList);
    }
    class Layout : ILayout {
        public void assert() {
            throw new NotImplementedException();
        }

        public string name() {
            throw new NotImplementedException();
        }

        public Image symbol() {
            throw new NotImplementedException();
        }

        public void updateWindowList(List<Window> newList) {
            throw new NotImplementedException();
        }
    }
}
