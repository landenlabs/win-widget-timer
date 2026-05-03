// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WinWidgetTimer.Models;

namespace WinWidgetTimer.Services;

public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private readonly Action _onAddWidget;
    private readonly Func<List<WidgetSettings>> _getWidgets;
    private readonly Action<string> _onWidgetSettings;
    private readonly Action<string> _onWidgetRemove;
    private readonly Action _onAbout;
    private readonly Action _onExit;

    public TrayIconService(
        Action onAddWidget,
        Func<List<WidgetSettings>> getWidgets,
        Action<string> onWidgetSettings,
        Action<string> onWidgetRemove,
        Action onAbout,
        Action onExit)
    {
        _onAddWidget      = onAddWidget;
        _getWidgets       = getWidgets;
        _onWidgetSettings = onWidgetSettings;
        _onWidgetRemove   = onWidgetRemove;
        _onAbout          = onAbout;
        _onExit           = onExit;

        InitializeTrayIcon();
    }

    private static Icon CreateTimerIcon()
    {
        try
        {
            using var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Timer body (circle)
            using var bodyPen = new Pen(System.Drawing.Color.FromArgb(220, 220, 220), 1.5f);
            g.DrawEllipse(bodyPen, 2, 4, 12, 11);

            // Timer stem / top
            using var stemPen = new Pen(System.Drawing.Color.FromArgb(180, 180, 180), 1.5f);
            g.DrawLine(stemPen, 6, 2, 10, 2);
            g.DrawLine(stemPen, 8, 2, 8, 4);

            // Clock hands (orange/amber)
            using var handPen = new Pen(System.Drawing.Color.FromArgb(255, 170, 80), 1.5f);
            g.DrawLine(handPen, 8, 10, 8, 6);  // minute hand up
            g.DrawLine(handPen, 8, 10, 11, 10); // hour hand right

            return Icon.FromHandle(bmp.GetHicon());
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon    = CreateTimerIcon(),
            Visible = true,
            Text    = "Timer Widget"
        };
        _notifyIcon.DoubleClick += (_, _) => Invoke(_onAddWidget);
        BuildMenu();
    }

    public void RebuildMenu() => BuildMenu();

    private void BuildMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        var widgets = _getWidgets();
        bool canRemove = widgets.Count > 1;

        foreach (var w in widgets)
        {
            var sub = new ToolStripMenuItem(w.Name);
            string wid = w.Id;

            sub.DropDownItems.Add("Settings", null, (_, _) => Invoke(() => _onWidgetSettings(wid)));

            var removeItem = new ToolStripMenuItem("Remove Widget", null,
                (_, _) => Invoke(() => _onWidgetRemove(wid)));
            removeItem.Enabled = canRemove;
            sub.DropDownItems.Add(removeItem);

            menu.Items.Add(sub);
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Add Widget", null, (_, _) => Invoke(_onAddWidget));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About",      null, (_, _) => Invoke(_onAbout));
        menu.Items.Add("Exit",       null, (_, _) => Invoke(_onExit));

        _notifyIcon.ContextMenuStrip = menu;
    }

    private static void Invoke(Action action)
    {
        if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
            action();
        else
            System.Windows.Application.Current?.Dispatcher.Invoke(action);
    }

    public void Dispose() => _notifyIcon?.Dispose();
}
