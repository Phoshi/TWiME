using Extensions;

namespace TWiME {
    public enum WindowRules {
        tag,        //Window should be tagged with this when it opens
        tag2 = tag,
        tag3 = tag,
        tag4 = tag,
        tag5 = tag,
        tag6 = tag,
        tag7 = tag,
        tag8 = tag,
        tag9 = tag,
        stack,      //Window should take this place in the stack
        monitor,    //Window should be on this monitor
        ignore      //Window should not be managed by TWiME at all
    }

    public class WindowRule {
        private WindowRules _rule;
        private int _data;

        public WindowRules rule {
            get { return _rule; }
        }

        public int data {
            get { return _data; }
        }

        public WindowRule(WindowRules rule, int data) {
            _rule = rule;
            _data = data;
        }
        public override string ToString() {
            return "{0}: {1}".With(_rule, _data);
        }
    }

    public class WindowMatch {
        private string _class, _title;

        public string Class {
            get { return _class; }
        }

        public string Title {
            get { return _title; }
        }

        public WindowMatch(string windowClass, string windowTitle) {
            _class = windowClass;
            _title = windowTitle;
        }

        public bool windowMatches(Window window) {
            if (window.Title.Glob(_title) && window.ClassName.Glob(_class)) {
                return true;
            }
            return false;
        }
        public override string ToString() {
            return "{0} - {1}".With(_class, _title);
        }
    }
}