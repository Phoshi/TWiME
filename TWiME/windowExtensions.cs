using System.Collections.Generic;
using System.Linq;

namespace TWiME {
    public static class windowExtensions { //Why extensions? Because I want to keep the Window class standalone
        public static Monitor Monitor(this Window window) {
            foreach (Monitor monitor in Manager.monitors) {
                if ((from screen in monitor.screens select screen.windows).SelectMany(wnd => wnd).Contains(window)) {
                    return monitor;
                }
            }
            return Manager.monitors.First();
        }

        public static List<TagScreen> TagScreens(this Window window) {
            return (from screen in window.Monitor().screens where screen.windows.Contains(window) select screen).ToList();
        }
    }
}
