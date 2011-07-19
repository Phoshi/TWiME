TWiME
=====

TWiME - *T*iling *Wi*ndow *M*anager *E*mulator - is a Tiling Window Manager for Windows. It tracks your windows and arranges them into tiles, and offers methods to
manipulate that layout. It's a Tiling Window Manager, if you're here, you probably already know what it's trying to achieve.

Current Layouts:
----------------
Standard: Primary pane on the left, all other windows stacked to the right.  
Reversed: Primary pane on the right, all other windows stacked to the left.  
Docked: Primary pane along the top, all other windows stacked along the bottom.  
Spiral: Windows spiral towards the middle - the primary pane is half the monitor, the second is 1/4, the third is 1/8, so on.  
Single-Window: The focussed window is the only visible window. Maximised, in effect. You can still manipulate the stack as normal, but it will have no effect on this layout.  


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

Additional bindings are:
Win-Q - Quit, making absolutely sure all windows are visible and restoring the taskbar.
Win-Space, to toggle the windows taskbar (Because I have *never* seen a perfect notification tray emulation, and I'm not going to settle for less than perfect)
Win-Left/Right, to move the layout's horizontal splitter
Win-Up/Down, to move the layout's vertical splitter
Though note that the above two may have no effect depending on the loaded layout.
Win-Control-Space and Win-Control-Shift-Space switch between loaded layouts for the active tag.

Disclaimer
----------
I hold no responsibility for any of the functionality contained within TWiME breaking your shit, whether it be hiding a window containing years of unsaved work, or tiling your dog into a space too small for it to fit.
I'm sorry, that's just how it is.

By
--
Application by me, Phoshi. 
Pinvoke.net has been an invaluable resource for figuring out the WinAPI functions neccesary to make this all come together.
Any code taken from other sources, or heavily based on other sources, is marked as such in a comment at the top of the area.
Window focussing logic was partially translated from AutoHotKey's WinActivate function.

Inspiration comes from the great TWMs that came before, like Awesome, or DWM. Direct inspiration for **starting** comes from bug.n, an excellent attempt at a windows TWM, and well worth checking out.
