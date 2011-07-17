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
        Image stateImage(Size dimensions);
        void updateWindowList(List<Window> newList);
        void moveSplitter(float offset);
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

        public Image stateImage(Size dimensions) {
            throw new NotImplementedException();
        }

        public void updateWindowList(List<Window> newList) {
            throw new NotImplementedException();
        }

        public void moveSplitter(float offset) {
            throw new NotImplementedException();
        }
    }
}
