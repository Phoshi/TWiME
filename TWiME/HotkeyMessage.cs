using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TWiME {
    public enum Message {
        Focus,          //Focus window
        Move,           //Move window to new position
        Switch,         //Swap active window with this
        Monitor,        //Swap window to other monitor
        Layout,         //Change to a new layout style
        Splitter,       //Move the layout splitter
        ScreenOn,       //Turn this tag view on
        ScreenOff,      //Turn this tag view off
        TagWindow,      //Tag window with this tag
        UntagWindow,    //Untag window with this tag
        MonitorSwitch,  //Switch focus to another monitor
        MonitorMove,    //Move the entire monitor object elsewhere
        Close           //Close TWiME
    }

    public enum Level {
        global,
        monitor,
        screen,
        window
    }

    public class HotkeyMessage {
        public Message message { get; internal set; }
        public Level level { get; internal set; }
        public int data { get; internal set; }
        public IntPtr handle { get; internal set; }

        public HotkeyMessage(Message messageType, Level messageLevel, IntPtr hWnd, int Data) {
            message = messageType;
            level = messageLevel;
            handle = hWnd;
            data = Data;
        }

    }
}
