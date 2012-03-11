using Extensions;

namespace TWiME {
    public enum WindowRules {
        tag,            //Window should be tagged with this when it opens
        tag2 = tag,
        tag3 = tag,
        tag4 = tag,
        tag5 = tag,
        tag6 = tag,
        tag7 = tag,
        tag8 = tag,
        tag9 = tag,
        stack,          //Window should take this place in the stack
        monitor,        //Window should be on this monitor
        ignore,         //Window should not be managed by TWiME at all
        noResize,       //Window should not be resized
        stripBorders,   //Window borders should be stripped
        tilingStyle,    //How the window should be tiled
        topmost,        //Whether the window should be topmost-by-default
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
        public long _style;
        private bool _negative;

        public long Style {
            get { return _style; }
        }

        public string Class {
            get { return _class; }
        }

        public string Title {
            get { return _title; }
        }

        public WindowMatch(string windowClass, string windowTitle, long style, bool negate = false) {
            _class = windowClass;
            _title = windowTitle;
            _style = style;
            _negative = negate;
        }

        public bool windowMatches(Window window) {
            bool passesTitle = false, passesStyle = false;
            if (window.Title.Glob(_title) && window.ClassName.Glob(_class)) {
                passesTitle = true;
            }
            if ((window.Style & _style) == _style) {
                passesStyle = true;
            }
            if (!_negative) {
                return (passesStyle && passesTitle);
            }
            return !(passesStyle && passesTitle);
        }
        public override string ToString() {
            return "{0} - {1} - {2}{3}".With(_class, _title, _style, _negative ? " (Negated)" : "");
        }
    }
}