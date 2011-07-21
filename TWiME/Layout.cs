using System;
using System.Collections.Generic;
using System.Drawing;

namespace TWiME {
    public interface ILayout {
        void Assert();
        string Name();
        Image Symbol();
        Image StateImage(Size dimensions);
        void UpdateWindowList(List<Window> newList);
        void MoveSplitter(float offset, bool vertical = false);
        float GetSplitter(bool vertical = false);
    }

    public class Layout : ILayout {
        public void Assert() {
            throw new NotImplementedException();
        }

        public string Name() {
            throw new NotImplementedException();
        }

        public Image Symbol() {
            throw new NotImplementedException();
        }

        public Image StateImage(Size dimensions) {
            throw new NotImplementedException();
        }

        public void UpdateWindowList(List<Window> newList) {
            throw new NotImplementedException();
        }

        public void MoveSplitter(float offset, bool vertical = false) {
            throw new NotImplementedException();
        }

        public float GetSplitter(bool vertical = false) {
            throw new NotImplementedException();
        }
    }
}