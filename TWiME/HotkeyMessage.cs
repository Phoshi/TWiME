using System;

namespace TWiME {
    public enum Message {
        Focus, //Focus window
        Move, //Move window to new position
        Switch, //Swap active window with this
        FocusThis, //Focus a particular indexed window
        SwitchThis, //Swap active window with this indexed window
        Monitor, //Swap window to other monitor
        Layout, //Change to a new layout style
        LayoutRelative, //Change to a new layout style offset
        LayoutRelativeReverse, //Change to a new layout style backwards offset
        Splitter, //Move the layout splitter
        VSplitter, //Move the layout vsplitter
        Screen, //Turn this tag view on
        ScreenRelative, //Turn this offset tag view on
        TagWindow, //Tag window with this tag
        SwapTagWindow, //Untag window with the current tag, and tag it with this
        SwapTagWindowRelative, //Untag window with the current tag, and tag it with the offsetted tag
        MonitorSwitch, //Switch focus to another monitor
        MonitorFocus, //Switch focus to another indexed monitor
        MonitorMoveThis, //Move window to another indexed monitor
        MonitorMove, //Move the entire monitor object elsewhere
        Close, //Close TWiME
        Restart, //Restart TWiME
        Split, //Split the working area into multiple tagspaces
        Hide, //Hide the current tagspace
        OnlyShow, //Only show this tagspace
        ShowAll, //Show all tags
        SplitRotate, //Rotate shown tags
    }

    public enum Level {
        Global,
        Monitor,
        Screen,
        Window
    }

    public class HotkeyMessage {
        public Message Message { get; internal set; }
        public Level level { get; internal set; }
        public int data { get; internal set; }
        public IntPtr handle { get; internal set; }

        public HotkeyMessage(Message messageType, Level messageLevel, IntPtr hWnd, int Data) {
            Message = messageType;
            level = messageLevel;
            handle = hWnd;
            data = Data;
        }
    }
}