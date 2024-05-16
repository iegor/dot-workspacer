#r "C:\Program Files\workspacer\workspacer.Shared.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "C:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.Gap;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;

return new Action<IConfigContext>((IConfigContext context) =>
{
  /* Variables */
  var fontSize = 9;
  var barHeight = 16;
  var fontName = "Terminus";
  var background = new Color(0x0, 0x0, 0x0);

  /* Config */
  context.CanMinimizeWindows = true;

  /* Gap */
  //var gap = barHeight - 14;
  var gap = 2;
  var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

  /* Bar */
  context.AddBar(new BarPluginConfig()
  {
    FontSize = fontSize,
    BarHeight = barHeight,
    FontName = fontName,
    DefaultWidgetBackground = background,
    LeftWidgets = () => new IBarWidget[]
    {
      new WorkspaceWidget(), new TextWidget(": "), new TitleWidget() {
        IsShortTitle = true
      }
    },
    RightWidgets = () => new IBarWidget[]
    {
      new BatteryWidget(),
      new TimeWidget(1000, "HH:mm:ss dd-MMM-yyyy"),
      new ActiveLayoutWidget(),
    }
  });

  /* Bar focus indicator */
  context.AddFocusIndicator();

  /* Default layouts */
  Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
  {
    new TallLayoutEngine(),
    new VertLayoutEngine(),
    new HorzLayoutEngine(),
    new FullLayoutEngine(),
  };

  context.DefaultLayouts = defaultLayouts;

  /* Workspaces */
  // Array of workspace names and their layouts
  (string, ILayoutEngine[])[] workspaces =
  {
    ("1", defaultLayouts()),
    ("2", new ILayoutEngine[] { new HorzLayoutEngine(), new TallLayoutEngine() }),
    // ("3", new ILayoutEngine[] { new FullLayoutEngine() }),
    ("3", defaultLayouts()),
    ("4", new ILayoutEngine[] { new HorzLayoutEngine() }),
    ("5", defaultLayouts()),
    ("6", defaultLayouts()),
    ("7", defaultLayouts()),
    ("8", defaultLayouts()),
    ("9", defaultLayouts()),
    // ("scr", defaultLayouts()),
    ("trash", new ILayoutEngine[] { new HorzLayoutEngine(), new TallLayoutEngine() }),
  };

  foreach ((string name, ILayoutEngine[] layouts) in workspaces)
  {
    context.WorkspaceContainer.CreateWorkspace(name, layouts);
  }

  /* Filters */

  context.WindowRouter.IgnoreProcessName("KeePassXC");
  //context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("KeePassXC.exe"));
  
  context.WindowRouter.IgnoreProcessName("acwebhelper");
  context.WindowRouter.IgnoreProcessName("vpnui");

  context.WindowRouter.IgnoreProcessName("FortiClient");
  context.WindowRouter.IgnoreTitle("Windows Security");
  context.WindowRouter.IgnoreTitle("Shut Down Windows");
  context.WindowRouter.IgnoreTitle("Delete Folder");
  context.WindowRouter.IgnoreTitle("Delete File");
  context.WindowRouter.IgnoreTitle("Delete Shortcut");
  context.WindowRouter.IgnoreTitle("Delete Multiple Items");
  context.WindowRouter.IgnoreTitle("Replace or Skip Files");
  context.WindowRouter.IgnoreTitle("Remote Desktop Connection");
  // context.WindowRouter.IgnoreProcessName("Work or school account");
  context.WindowRouter.IgnoreProcessName("Microsoft.AAD.BrokerPlugin");
  context.WindowRouter.IgnoreProcessName("msedgewebview2");

  //context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("pinentry.exe"));

  // The following filter means that Edge will now open on the correct display
  context.WindowRouter.AddFilter((window) => !window.Class.Equals("ShellTrayWnd"));

  /* Routes */
  // context.WindowRouter.RouteProcessName("Lightshot", "scr");
  context.WindowRouter.IgnoreProcessName("Lightshot");
  // context.WindowRouter.RouteProcessName("Teams", "4");

  //context.WindowRouter.RouteProcessName("Spotify", "🎶");
  //context.WindowRouter.RouteTitle("Microsoft To Do", "todo");

  // context.WindowRouter.RouteProcessName("acwebhelper", "trash");
  // context.WindowRouter.RouteProcessName("vpnui", "trash");
  // context.WindowRouter.RouteProcessName("Microsoft.AAD.BrokerPlugin", "trash");
  // context.WindowRouter.RouteProcessName("msedgewebview2", "trash");
  // context.WindowRouter.RouteTitle("Windows Security", "trash");

  /* Action menu */
  var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
  {
    RegisterKeybind = false,
    MenuHeight = barHeight,
    FontSize = fontSize,
    FontName = fontName,
    Background = background,
  });

  /* Action menu builder */
  Func<ActionMenuItemBuilder> createActionMenuBuilder = () =>
  {
    var menuBuilder = actionMenu.Create();

    // Switch to workspace
    menuBuilder.AddMenu("switch", () =>
    {
      var workspaceMenu = actionMenu.Create();
      var monitor = context.MonitorContainer.FocusedMonitor;
      var workspaces = context.WorkspaceContainer.GetWorkspaces(monitor);

      Func<int, Action> createChildMenu = (workspaceIndex) => () =>
      {
        context.Workspaces.SwitchMonitorToWorkspace(monitor.Index, workspaceIndex);
      };

      int workspaceIndex = 0;
      foreach (var workspace in workspaces)
      {
        workspaceMenu.Add(workspace.Name, createChildMenu(workspaceIndex));
        workspaceIndex++;
      }

      return workspaceMenu;
    });

    // Move window to workspace
    menuBuilder.AddMenu("move", () =>
    {
      var moveMenu = actionMenu.Create();
      var focusedWorkspace = context.Workspaces.FocusedWorkspace;

      var workspaces = context.WorkspaceContainer.GetWorkspaces(focusedWorkspace).ToArray();
      Func<int, Action> createChildMenu = (index) => () => { context.Workspaces.MoveFocusedWindowToWorkspace(index); };

      for (int i = 0; i < workspaces.Length; i++)
      {
        moveMenu.Add(workspaces[i].Name, createChildMenu(i));
      }

      return moveMenu;
    });

    // Rename workspace
    menuBuilder.AddFreeForm("rename", (name) =>
    {
      context.Workspaces.FocusedWorkspace.Name = name;
    });

    // Create workspace
    menuBuilder.AddFreeForm("create workspace", (name) =>
    {
      context.WorkspaceContainer.CreateWorkspace(name);
    });

    // Delete focused workspace
    menuBuilder.Add("close", () =>
    {
      context.WorkspaceContainer.RemoveWorkspace(context.Workspaces.FocusedWorkspace);
    });

    // Workspacer
    menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
    menuBuilder.Add("toggle enabled", () => context.Enabled = !context.Enabled);
    menuBuilder.Add("gap increment", () => gapPlugin.IncrementInnerGap());
    menuBuilder.Add("gap decrement", () => gapPlugin.DecrementInnerGap());
    menuBuilder.Add("restart", () => context.Restart());
    menuBuilder.Add("quit", () => context.Quit());

    /*
    menuBuilder.Add("obs", () => {
      //  --disable-updater --disable-shutdown-check --disable-missing-files-check
      Process.Start("C:\\Program Files\\obs-studio\\bin\\64bit\\obs64.exe");
    });
    */

    return menuBuilder;
  };
  var actionMenuBuilder = createActionMenuBuilder();

  /* Keybindings */
  Action setKeybindings = () =>
  {
    KeyModifiers alt = KeyModifiers.Alt;
    KeyModifiers win = KeyModifiers.Win;
    KeyModifiers altShift = KeyModifiers.Alt | KeyModifiers.Shift;
    KeyModifiers altCtrl = KeyModifiers.Alt | KeyModifiers.Control;
    KeyModifiers altCtrlShift = KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift;
    KeyModifiers winShift = KeyModifiers.Win | KeyModifiers.Shift;

    IKeybindManager manager = context.Keybinds;

    var workspaces = context.Workspaces;

    // manager.UnsubscribeAll();
    // manager.Unsubscribe(alt, Keys.Enter);
    manager.Unsubscribe(alt, Keys.Right);
    manager.Unsubscribe(alt, Keys.Left);
    manager.Unsubscribe(altCtrl, Keys.Right);
    manager.Unsubscribe(altCtrl, Keys.Left);
    manager.Unsubscribe(altCtrlShift, Keys.Right);
    manager.Unsubscribe(altCtrlShift, Keys.Left);

    manager.Subscribe(win, Keys.Enter, () => Process.Start("C:\\Program Files\\Git\\git-bash.exe", "--cd /"));
    manager.Subscribe(winShift, Keys.Enter, () => Process.Start("cmd.exe"));

    // manager.Subscribe(MouseEvent.MouseWheel, () => workspaces.SwitchFocusedMonitorToMouseLocation());
    // manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());

    // workspace manipulation
    manager.Subscribe(altCtrl, Keys.PageUp, () => workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
    manager.Subscribe(altCtrl, Keys.PageDown, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");
    manager.Subscribe(altCtrl, Keys.Left, () => workspaces.SwitchToPreviousWorkspace(), "switch to previous workspace");
    manager.Subscribe(altCtrl, Keys.Right, () => workspaces.SwitchToNextWorkspace(), "switch to next workspace");

    // manager.Subscribe(altShift, Keys.PageDown, () => workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to next monitor");
    // manager.Subscribe(altShift, Keys.PageUp, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to previous monitor");

    // H, L keys
    // manager.Subscribe(altShift, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
    // manager.Subscribe(altShift, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

    // manager.Subscribe(altCtrl, Keys.H, () => workspaces.FocusedWorkspace.DecrementNumberOfPrimaryWindows(), "decrement number of primary windows");
    // manager.Subscribe(altCtrl, Keys.L, () => workspaces.FocusedWorkspace.IncrementNumberOfPrimaryWindows(), "increment number of primary windows");

    // manager.Subscribe(alt, Keys.Tab, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next window");
    // manager.Subscribe(altShift, Keys.Tab, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous window");
    // manager.Subscribe(alt, Keys.Tab, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap focus and next window");
    // manager.Subscribe(altShift, Keys.Tab, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap focus and previous window");
    // manager.Subscribe(alt, Keys.Enter, () => workspaces.FocusedWorkspace.SwapFocusAndPrimaryWindow(), "swap focus and primary window");

    // Other shortcuts
    manager.Subscribe(altCtrl, Keys.P, () => actionMenu.ShowMenu(actionMenuBuilder), "show menu");
    manager.Subscribe(alt, Keys.D0, () => context.Enabled = !context.Enabled, "toogle enable/disable workspacer");
    // manager.Subscribe(altShift, Keys.Escape, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");
    // manager.Subscribe(altShift, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");
  };
  setKeybindings();
});
