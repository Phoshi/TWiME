using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TWiME {
    class TagScreen {
        List<Window> windowList = new List<Window>();
        private ILayout layout;
        private Monitor _parent;
        private int _tag;
        public TagScreen(Monitor parent, int tag) {
            _parent = parent;
            _tag = tag;
            layout = new DefaultLayout(windowList);
            Manager.WindowCreate += Manager_WindowCreate;
            Manager.WindowDestroy+= Manager_WindowDestroy;
        }

        private void Manager_WindowDestroy(object sender, WindowEventArgs args) {
            Window newWindow = (Window) sender;
            IEnumerable<Window> deleteList = (from window in windowList where window.handle == newWindow.handle select window);
            if (deleteList.Count() > 0) {
                Window toRemove = deleteList.First();
                windowList.Remove(toRemove);
                Console.WriteLine("Removing window: "+toRemove);
                layout.updateWindowList(windowList);
                layout.assert();
            }
        }

        void Manager_WindowCreate(object sender, WindowEventArgs args) {
            if (args.monitor.DeviceName == _parent.name && _parent.tagsEnabled[_tag]) {
                Window newWindow = (Window) sender;
                windowList.Insert(0, newWindow);
                Console.WriteLine("Adding Window: " + newWindow.className + " "+newWindow);
                layout.updateWindowList(windowList);
                layout.assert();
            }
        }
    }
}
