TWiME
=====

TWiME - *T*iling *Wi*ndow *M*anager *E*mulator - is a Tiling Window Manager for Windows. It tracks your windows and arranges them into tiles, and offers methods to
manipulate that layout. It's a Tiling Window Manager, if you're here, you probably already know what it's trying to achieve.

Current Layouts:
----------------
DefaultLayout: Primary pane on the left, all other windows stacked to the right.  
ReversedDefaultLayout: Primary pane on the right, all other windows stacked to the left.  
VerticalDockLayout: Primary pane along the top, all other windows stacked along the bottom.  
SpiralLayout: Windows spiral towards the middle - the primary pane is half the monitor, the second is 1/4, the third is 1/8, so on.  
SingleWindowLayout: The focussed window is the only visible window. Maximised, in effect. You can still manipulate the stack as normal, but it will have no effect on this layout.  
SquaresLayout: The windows are laid out in equal tiles.
HalfSquaresLayout: The primary window is given the left hand pane, all others are tiled equally on the right.


Usage:
------
TWiME's (default) key layout is based on a simple system. J/K scrolls between things, the number buttons address specific items, and return addresses the primary item.  
Windows key is the main modifier.  
Without any additional modifiers, you're acting on window focus - Win-J moves focus down one in the stack, and Win-K moves it up one. Win-Return focusses the layout's primary window.  
With Shift, you're acting on window positions - Win-Shift-J moves the active window down one in the stack, etc. Win-Shift-Return moves the current window to the primary position on the stack, and Win-Number swaps the active window with the window at that posiiton on the stack.  
With Alt, you're acting on monitors - Win-Alt-J focusses the next monitor in the list, so on.
With Control, you're acting on tags - Win-Control-Shift-J moves a window to the next tag, so on. Win-Control-Shift-Number *toggles* the tag for a window, as a window can be on 
many tagspaces.

Combinations of these modifiers work generally as you'd expect - Win-Alt-Shift-J moves the active window to the next monitor, for example.

You can also split the monitor to display multiple tags at once:
Win-S - Split - creates a new split to the right of the currently focused tag, and puts the next inactive tagspace in it.
Win-A - Split All - shows all non-empty tags
Win-D - Show one - Hides all tags other than the currently selected one
Win-X - Hide - closes the currently selected tag

Additional bindings are:
Win-Q - Quit, making absolutely sure all windows are visible and restoring the taskbar.
Win-Space, to toggle the windows taskbar (Because I have *never* seen a perfect notification tray emulation, and I'm not going to settle for less than perfect)
Win-Left/Right, to move the layout's horizontal splitter
Win-Up/Down, to move the layout's vertical splitter
Though note that the above two may have no effect depending on the loaded layout, and you can hold shift while pressing either to move further in each jump.
Win-Control-Space and Win-Control-Shift-Space switch between loaded layouts for the active tag.
Win-Control-Left/Right moves the monitor splitter for displaying multiple tags

Configuration:
--------------
Configuration is available through a `_TWiMErc` file located in the same directory as the executable. It is a simple text-based file formatted like so:  
    [Category]
        Setting.Name=Value

There are many different settings you can change here.  
[General]  
    Windows.DefaultStackPosition - the position in the stack new windows open at. Default is 0, or the main window. Use negative indexes to go from the end of the stack.  

    Bar.Height - The height of the taskbars, in pixels. Default is 15px.  
    Bar.Font - The name of the font to use. The default is Segoe UI  
    Bar.Refresh - refresh rate for the bar, in ms. It will always refresh on window actions or focus changes, but also on this timer. Default 10000
  Many settings for colouring the bar - takes names of colours:  
    Bar.UnselectedForeground - default Black  
    Bar.SelectedForeground - default LightGray  
    Bar.SelectedItemColour - default DarkGray  
    Bar.UnselectedBackgroundColour - default Black  
    Bar.SelectedTagColour - default White  
    Bar.BackColour - default DarkGray   
    Bar.SeperatorColour - default Blue

    Bar.SperatorWidth - the width of the seperator. Default 3.

    Menu.Foreground - Foreground colour for the main menu. Default LightGray
    Menu.Background - Background colour for the main menu, Default DarkGray
    
    Main.AutoSave - "true" or "false", decides whether layout state is saved at the end of the session. TWiMErc values still override saved settings, however. Default false.  
    Main.MouseFollowsInput - "true" or "false", decides whether the mouse is moved whenever TWiME switches focus. Default false.  
    Main.Poll - an integer value, decides how often the main polling loop runs, in ms. Lower values produce faster detection of windows, but with an increase in required processor time. 1000ms is default. You probably won't need to mess with this.  

    WindowSwitcher.Font.Name - The font to use in the window switcher, Default Segoe UI
    WindowSwitcher.Font.Size - the size of that font, Default 8
    WindowSwitcher.Foreground - the colour of the windowswitcher's text
    WindowSwitcher.Background - the colour of the windowswitcher's background
    WindowSwitcher.Inactive - the colour of inactive items, Default Gray
    WindowSwitcher.Selected - the colour of selected items, default DarkGray

[Window Rules]  
    Window Rules are quite simple, they follow this format:  
    WindowClass.WindowTitle.rule=value  
    Where Window Class is the window class, window title is the window title, and both allow wildcard matches (*).  
    The rule can be:  
        tag - defines what tag the window should default to  
        stack - defines what position in the stack the window should open at - ovverides the default above  
        monitor - defines what monitor the window should open on, as an index where the primary monitor is 0  
        ignore - set this to completely ignore the window and don't take it into account for anything  
    A window can have multiple rules, they are all applied.  

[Display Specific Rules]  
    The header for these rules is the display name - most likely something like `\\\DISPLAY1`. Under that header, the format is as follows:    
    tagNumber.setting=value  
    where setting is one of:  
        DefaultLayout - Layout Name, where the names are listed above  
        Splitter - The horizontal splitter, from 0-1  
        VSplitter - the vertical splitter, from 0-1  
        Wallpaper - the wallpaper to use for this tag
    Setting the `Splitter` for a monitor changes the splitter for showing multiple tags at the same time
  
[Menu Items]
    Menu items are also quite simple, the items follow this format:
        Category.Subcategory.Subcategory.Item=Path Or Command to run
    An item can have as many subcategories as you want. If the path to run is not a special command, that path will be executed.
    Categories will produce a menu with submenus as you'd expect.
    Windows handles executing things quite well, give it a file and it'll open the associated application, give it a path and you'll get an explorer window there, give it an executable and you'll launch that.

    Special commands are:
        Edit TWiMErc - opens the TWiMErc in your default text editor
        Restart - what it says on the tin
        Quit - as before


If General.Main.AutoSave is true, a `_runtimerc` file will be created on exit. This is exactly the same format as the `_TWiMErc`, and is automatically generated from the settings at the time.
Disclaimer
----------
I hold no responsibility for any of the functionality contained within TWiME breaking your shit, whether it be hiding a window containing years of unsaved work, or tiling your dog into a space too small for it to fit.
I'm sorry, that's just how it is.

By
--
Application by me, Phoshi, or PY. 
Pinvoke.net has been an invaluable resource for figuring out the WinAPI functions neccesary to make this all come together.
Any code taken from other sources, or heavily based on other sources, is marked as such in a comment at the top of the area.
Window focussing logic was partially translated from AutoHotKey's WinActivate function.

Inspiration comes from the great TWMs that came before, like Awesome, or DWM. Direct inspiration for **starting** comes from bug.n, an excellent attempt at a windows TWM, and well worth checking out.
