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

    // Colored stopwatch icon drawn at the system small-icon size (DPI aware) so it
    // stays crisp and visible on both light and dark taskbars.
    private static Icon CreateTimerIcon()
    {
        try
        {
            int s = Math.Max(16, SystemInformation.SmallIconSize.Width);
            float u = s / 16f;                       // 1 "pixel" at 16px design size

            using var bmp = new Bitmap(s, s, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // Crown button (red) and stem
                using (var crownBrush = new SolidBrush(System.Drawing.Color.FromArgb(235, 70, 60)))
                {
                    g.FillRectangle(crownBrush, 5.5f * u, 0.3f * u, 5f * u, 1.8f * u);
                    g.FillRectangle(crownBrush, 7.2f * u, 1.8f * u, 1.6f * u, 1.8f * u);
                }

                // Body: orange gradient ring with white face
                var body = new RectangleF(1.2f * u, 3.0f * u, 13.6f * u, 12.6f * u);
                using (var ringBrush = new LinearGradientBrush(body,
                           System.Drawing.Color.FromArgb(255, 170, 50),
                           System.Drawing.Color.FromArgb(230, 90, 20), 90f))
                    g.FillEllipse(ringBrush, body);

                var face = RectangleF.Inflate(body, -1.8f * u, -1.8f * u);
                using (var faceBrush = new SolidBrush(System.Drawing.Color.White))
                    g.FillEllipse(faceBrush, face);

                float cx = face.X + face.Width / 2f, cy = face.Y + face.Height / 2f;

                // Elapsed wedge (green) from 12 to ~4 o'clock
                using (var wedgeBrush = new SolidBrush(System.Drawing.Color.FromArgb(200, 40, 180, 90)))
                    g.FillPie(wedgeBrush, face.X + 0.8f * u, face.Y + 0.8f * u,
                              face.Width - 1.6f * u, face.Height - 1.6f * u, -90f, 120f);

                // Hand (dark blue) and hub
                using (var handPen = new Pen(System.Drawing.Color.FromArgb(30, 60, 140), 1.4f * u)
                           { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(handPen, cx, cy, cx, face.Y + 1.2f * u);
                float hub = 1.1f * u;
                using (var hubBrush = new SolidBrush(System.Drawing.Color.FromArgb(30, 60, 140)))
                    g.FillEllipse(hubBrush, cx - hub, cy - hub, 2 * hub, 2 * hub);
            }

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
