using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TWiME;
using Extensions;

namespace WindowRules {
    public enum Rules {
        tag,        //Window should be tagged with this when it opens
        stack,      //Window should take this place in the stack
        monitor,    //Window should be on this monitor
        ignore      //Window should not be managed by TWiME at all
    }
    public class Rule {
        private Rules _rule;
        private int _data;
        public Rules rule { get { return _rule; } }
        public int data { get { return _data; } }

        public Rule(Rules rule, int data) {
            _rule = rule;
            _data = data;
        }
    }
    public class Match {
        private string _class, _title;
        public string Class { get { return _class; } }
        public string Title { get { return _title; } }

        public Match(string windowClass, string windowTitle) {
            _class = windowClass;
            _title = windowTitle;
        }

        public bool windowMatches(Window window) {
            if (window.title.Glob(_title) && window.className.Glob(_class)) {
                return true;
            }
            return false;
        }
    }
}
