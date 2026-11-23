// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinWidgetTimer.Models;
using WinWidgetTimer.Services;
using WinWidgetTimer.ViewModels;

namespace WinWidgetTimer.Windows;

public partial class WidgetWindow : Window
{
    /// <summary>Font-scale range — must match the Font Scale slider in SettingsWindow.xaml.</summary>
    public const int FontScaleMin = 50;
    public const int FontScaleMax = 200;

    // Smallest "virtual" size a scaling corner-drag may shrink to (DIPs); the font-scale range is the real limit.
    private const double ScaleMinWidth  = 60;
    private const double ScaleMinHeight = 30;

    private readonly WidgetSettings _settings;
    private List<TimerDisplayItem> _items = [];

    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _displayCheckTimer;
    private DisplayConfiguration _currentDisplayConfig;

    private bool _isEmbedded;
    private bool _isCollapsed;

    // Widget top-left in SCREEN PHYSICAL PIXELS — the single source of truth for position,
    // whether embedded (child of WorkerW) or a normal top-level window. See WidgetGeometry.
    private int _posX;
    private int _posY;

    // Drag (move) and corner-scale share the same start snapshot.
    private bool _isDragging;
    private bool _isResizing;
    private ResizeZone _resizeZone;
    private DesktopService.POINT _startCursor;
    private int _startX, _startY;
    private double _startW, _startH;           // DIPs
    private int _startPixelW, _startPixelH;    // pixels
    private int _startPercent;
    private int _livePercent;

    private SettingsWindow? _settingsDialog;

    private readonly DispatcherTimer _flashTimer;
    private int _flashCount;
    private static readonly SolidColorBrush _flashBrush     = new(Color.FromRgb(0xFF, 0x33, 0x33));
    private static readonly SolidColorBrush _normalBorder   = new(Color.FromArgb(0xFF, 0x45, 0x45, 0x70));
    private bool _flashState;

    private SoundPlayer? _soundPlayer;

    private string _bgColorHex;
    private double _bgOpacity;

    public string WidgetId => _settings.Id;

    /// <summary>Current top-left in screen physical pixels.</summary>
    public (int X, int Y) ScreenPosition => (_posX, _posY);

    private double DpiX => VisualTreeHelper.GetDpi(this).DpiScaleX;
    private double DpiY => VisualTreeHelper.GetDpi(this).DpiScaleY;

    public WidgetWindow(WidgetSettings settings)
    {
        _settings = settings;
        _bgColorHex = string.IsNullOrEmpty(settings.BackgroundColor) ? "#1E1E2E" : settings.BackgroundColor;
        _bgOpacity  = settings.BackgroundOpacity > 0 ? settings.BackgroundOpacity : 0.85;

        InitializeComponent();

        // Widgets can be added/removed from the tray at any time, so refresh this each time the menu opens.
        ContextMenuOpening += (_, _) => RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;

        _currentDisplayConfig = DisplayService.GetCurrentDisplayConfiguration();
        (_posX, _posY) = DisplayService.GetDisplayPosition(settings, _currentDisplayConfig);
        // Provisional (assumes 100 %); corrected for the real DPI in OnSourceInitialized before first show.
        Left = _posX;
        Top  = _posY;

        LoadItems();

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += (_, _) => Tick();
        _updateTimer.Start();

        _displayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _displayCheckTimer.Tick += (_, _) => CheckDisplayConfigurationChanged();
        _displayCheckTimer.Start();

        _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _flashTimer.Tick += (_, _) => FlashTick();

        // The content-sized window only has a real size after the first layout pass.
        Loaded += (_, _) =>
        {
            MoveTo(_posX, _posY);
            EnsureOnScreen();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyBackgroundInternal(_bgOpacity);
        ApplyFontScale(CurrentFontScale);
        ApplyShowTitleBar(_settings.ShowTitleBar);

        DesktopService.HideFromAltTab(this);
        _isEmbedded = _settings.EmbedInWallpaper && DesktopService.EmbedInWallpaper(this);
        if (!_isEmbedded)
            DesktopService.SetAlwaysOnBottom(this);

        MoveTo(_posX, _posY);
    }

    private int CurrentFontScale =>
        _settings.FontScalePercent > 0 ? Math.Clamp(_settings.FontScalePercent, FontScaleMin, FontScaleMax) : 100;

    // ── Position (screen pixels) ─────────────────────────────────────────────

    /// <summary>Moves the widget so its top-left is at the given screen pixel position (does not save).</summary>
    public void MoveTo(int x, int y)
    {
        _posX = x;
        _posY = y;
        if (_isEmbedded)
        {
            int w = PixelWidth, h = PixelHeight;
            DesktopService.MoveEmbeddedWindow(this, x, y, w > 0 ? w : null, h > 0 ? h : null);
        }
        else
        {
            Left = x / DpiX;
            Top  = y / DpiY;
        }
    }

    /// <summary>Persists the current position for the current monitor layout.</summary>
    public void SavePosition()
    {
        DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfig, _posX, _posY);
        SettingsService.Save(App.Settings);
    }

    // Window has no chrome and sizes to its content, so the border's size is the window's size.
    private double ContentWidth  => WidgetBorder.ActualWidth  > 0 ? WidgetBorder.ActualWidth  : ActualWidth;
    private double ContentHeight => WidgetBorder.ActualHeight > 0 ? WidgetBorder.ActualHeight : ActualHeight;

    private int PixelWidth  => (int)Math.Round(ContentWidth  * DpiX);
    private int PixelHeight => (int)Math.Round(ContentHeight * DpiY);

    private PxRect PixelBounds => new(_posX, _posY, PixelWidth, PixelHeight);

    /// <summary>
    /// The window sizes to its content; an embedded child HWND needs an explicit MoveWindow to
    /// pick up a new content size (font scale, title bar, collapse, timer list).
    /// </summary>
    private void RefreshEmbeddedSize()
    {
        if (!_isEmbedded) return;
        UpdateLayout();
        MoveTo(_posX, _posY);
    }

    // ── Load / Reload ────────────────────────────────────────────────────────

    public void LoadItems()
    {
        _items = _settings.Timers.Select(t => new TimerDisplayItem(t)).ToList();
        TimerList.ItemsSource = _items;
        UpdateTitle();
        ApplyFontScale(CurrentFontScale);
        ApplyShowTitleBar(_settings.ShowTitleBar);
        if (RemoveMenuItem != null)
            RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;
    }

    // ── Tick: update displays and check triggers ─────────────────────────────

    private void UpdateTitle()
    {
        int active = _items.Count(i => i.Entry.State == TimerState.Running);
        string count = active > 0 ? $" ({active})" : "";
        TitleText.Text = $"⏱ {_settings.Name}{count}";
    }

    private void Tick()
    {
        foreach (var item in _items)
        {
            bool triggered = item.Entry.CheckAndTrigger();
            item.Update();
            if (triggered)
            {
                TriggerAlert(item.Entry);
                if (item.Entry.FlashOnEnd)
                    StartFlash();
            }
        }
        UpdateTitle();
    }

    private void TriggerAlert(TimerEntry entry)
    {
        var sound = entry.SoundFile;
        if (string.IsNullOrEmpty(sound)) return;
        try
        {
            if (sound == "system") { SystemSounds.Exclamation.Play(); return; }
            var path = Path.Combine(@"C:\Windows\Media", sound);
            _soundPlayer?.Stop();
            _soundPlayer?.Dispose();
            if (File.Exists(path))
            {
                _soundPlayer = new SoundPlayer(path);
                _soundPlayer.Play();
            }
            else
            {
                _soundPlayer = null;
                SystemSounds.Exclamation.Play();
            }
        }
        catch { }
    }

    private void StopAlert()
    {
        _soundPlayer?.Stop();
        _soundPlayer = null;
    }

    // ── Flash on completion ──────────────────────────────────────────────────

    private void StartFlash()
    {
        _flashCount = 0;
        _flashState = false;
        if (!_flashTimer.IsEnabled)
            _flashTimer.Start();
    }

    private void FlashTick()
    {
        _flashState = !_flashState;
        _flashCount++;
        WidgetBorder.BorderBrush     = _flashState ? _flashBrush   : _normalBorder;
        WidgetBorder.BorderThickness = _flashState ? new Thickness(3) : new Thickness(1);
        if (_flashCount >= 40) // 20 full cycles × 2 ticks = ~8 seconds
            StopFlash();
    }

    private void StopFlash()
    {
        _flashTimer.Stop();
        WidgetBorder.BorderBrush     = _normalBorder;
        WidgetBorder.BorderThickness = new Thickness(1);
    }

    // ── Display config check ─────────────────────────────────────────────────

    /// <summary>Pulls the widget back onto a screen if it's off every monitor (e.g. after unplugging one).</summary>
    private void EnsureOnScreen()
    {
        var fix = WidgetGeometry.EnsureVisible(PixelBounds, DisplayService.GetScreens());
        if (fix is not { } p) return;
        MoveTo(p.X, p.Y);
        SavePosition();
    }

    private void CheckDisplayConfigurationChanged()
    {
        if (_isDragging || _isResizing) return;

        var newConfig = DisplayService.GetCurrentDisplayConfiguration();
        if (newConfig.ConfigurationHash == _currentDisplayConfig.ConfigurationHash) return;

        _currentDisplayConfig = newConfig;
        var (x, y) = DisplayService.GetDisplayPosition(_settings, _currentDisplayConfig);
        MoveTo(x, y);
        EnsureOnScreen();
    }

    // ── Appearance helpers ───────────────────────────────────────────────────

    public void ApplyBackground(string hexColor, double opacity)
    {
        _bgColorHex = hexColor;
        _bgOpacity  = opacity;
        ApplyBackgroundInternal(opacity);
    }

    private void ApplyBackgroundInternal(double opacity)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_bgColorHex);
            WidgetBorder.Background = new SolidColorBrush(color) { Opacity = opacity };
        }
        catch { }
    }

    public void ApplyFontScale(int percent)
    {
        double factor = Math.Max(0.5, percent / 100.0);
        TimerList.FontSize = Math.Max(8, 13 * factor);
        TitleText.FontSize = Math.Max(6, 11 * factor);
        RefreshEmbeddedSize();
    }

    public void ApplyShowTitleBar(bool show)
    {
        TitleBarGrid.Visibility      = show ? Visibility.Visible : Visibility.Collapsed;
        TitleBarSeparator.Visibility = (show && !_isCollapsed) ? Visibility.Visible : Visibility.Collapsed;
        RefreshEmbeddedSize();
    }

    // ── Title collapse / expand ──────────────────────────────────────────────

    private void TitleText_Click(object sender, MouseButtonEventArgs e)
    {
        _isCollapsed = !_isCollapsed;
        TimerList.Visibility         = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        TitleBarSeparator.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        RefreshEmbeddedSize();
        e.Handled = true;
    }

    // ── Timer row click ──────────────────────────────────────────────────────

    private void TimerRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TimerDisplayItem item)
        {
            bool wasDone = item.Entry.State == TimerState.Done;
            item.Entry.Toggle();
            item.Update();
            if (wasDone)
                StopAlert(); // stop audio immediately on dismiss
            // Stop flash if no Done+FlashOnEnd timers remain
            if (_flashTimer.IsEnabled && !_items.Any(i => i.Entry.State == TimerState.Done && i.Entry.FlashOnEnd))
                StopFlash();
            e.Handled = true; // prevent drag from starting on row click
        }
    }

    // ── Hover: reveal icon buttons ───────────────────────────────────────────

    private void Widget_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Visible;
        ApplyBackgroundInternal(Math.Min(1.0, _bgOpacity + 0.07));
    }

    private void Widget_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IconPanel.Visibility = Visibility.Collapsed;
        ApplyBackgroundInternal(_bgOpacity);
    }

    // ── Dragging (move) ──────────────────────────────────────────────────────

    /// <summary>
    /// Corners are checked in the tunnelling (Preview) phase so a corner press that lands on the
    /// edge of a timer row scales the widget instead of toggling the timer.
    /// </summary>
    private void Widget_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging || _isResizing) return;
        if (IsClickOnInteractiveElement(e.OriginalSource)) return;

        var zone = GetResizeZone(e.GetPosition(WidgetBorder));
        if (zone == ResizeZone.None) return;

        StartResize(zone);
        e.Handled = true;
    }

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled) return; // a timer row already handled this
        if (IsClickOnInteractiveElement(e.OriginalSource)) return;

        // Manual drag (DragMove() fails on Windows 10 with AllowsTransparency=True + WindowStyle=None,
        // and doesn't work at all for a window embedded in WorkerW). Everything is in screen pixels.
        SnapshotStart();
        _isDragging = true;
        SetCapturedCursor(Cursors.SizeAll);
        WidgetBorder.CaptureMouse();
        e.Handled = true;
    }

    private void SnapshotStart()
    {
        _startCursor  = DesktopService.GetCursorPosition();
        _startX       = _posX;
        _startY       = _posY;
        _startW       = ContentWidth;
        _startH       = ContentHeight;
        _startPixelW  = PixelWidth;
        _startPixelH  = PixelHeight;
        _startPercent = CurrentFontScale;
        _livePercent  = _startPercent;
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging)
        {
            var c = DesktopService.GetCursorPosition();
            var (x, y) = WidgetGeometry.DragTo(_startX, _startY, _startCursor.X, _startCursor.Y, c.X, c.Y);
            MoveTo(x, y);
            return;
        }

        if (_isResizing)
        {
            UpdateResize();
            return;
        }

        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
    }

    /// <summary>
    /// The border's normal cursor is the move cursor (XAML) and timer rows show a hand. In a resize
    /// corner the border forces its resize cursor over any child cursor.
    /// </summary>
    private void UpdateHoverCursor(ResizeZone zone)
    {
        bool inCorner = zone != ResizeZone.None;
        WidgetBorder.Cursor = inCorner ? CursorFor(zone) : Cursors.SizeAll;
        WidgetBorder.ForceCursor = inCorner;
    }

    /// <summary>
    /// While the border has mouse capture (drag/resize), it's the element queried for the
    /// cursor, so pin the appropriate cursor for the duration of the operation.
    /// </summary>
    private void SetCapturedCursor(System.Windows.Input.Cursor cursor)
    {
        WidgetBorder.Cursor = cursor;
        WidgetBorder.ForceCursor = true;
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging && !_isResizing) return;
        EndDragOrResize();
        UpdateHoverCursor(GetResizeZone(e.GetPosition(WidgetBorder)));
        e.Handled = true;
    }

    /// <summary>Capture can be lost mid-drag (Alt-Tab, Win key, UAC prompt); finish cleanly instead of sticking.</summary>
    private void Widget_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging || _isResizing)
            EndDragOrResize();
    }

    private void EndDragOrResize()
    {
        bool wasResizing = _isResizing;
        _isDragging = false;
        _isResizing = false;
        _resizeZone = ResizeZone.None;
        if (WidgetBorder.IsMouseCaptured)
            WidgetBorder.ReleaseMouseCapture();
        UpdateHoverCursor(ResizeZone.None);

        if (wasResizing)
            _settings.FontScalePercent = _livePercent;
        EnsureOnScreen();
        SavePosition();
    }

    private bool IsClickOnInteractiveElement(object? source)
    {
        if (source is System.Windows.Controls.Button) return true;
        if (source is FrameworkElement fe)
        {
            var parent = VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent is System.Windows.Controls.Button || parent == IconPanel) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return false;
    }

    // ── Corner scaling ───────────────────────────────────────────────────────

    /// <summary>
    /// Bottom corners always resize. Top corners only resize when the title bar is
    /// hidden — while it's shown, the top of the widget is the drag/move zone instead.
    /// </summary>
    private ResizeZone GetResizeZone(System.Windows.Point pos) =>
        WidgetGeometry.HitTest(pos.X, pos.Y, WidgetBorder.ActualWidth, WidgetBorder.ActualHeight,
                               allowTopCorners: TitleBarGrid.Visibility != Visibility.Visible);

    private static System.Windows.Input.Cursor CursorFor(ResizeZone zone) => zone switch
    {
        ResizeZone.TopLeft or ResizeZone.BottomRight => Cursors.SizeNWSE,
        ResizeZone.TopRight or ResizeZone.BottomLeft => Cursors.SizeNESW,
        _ => Cursors.SizeAll
    };

    private void StartResize(ResizeZone zone)
    {
        SnapshotStart();
        _isResizing = true;
        _resizeZone = zone;
        SetCapturedCursor(CursorFor(zone));
        WidgetBorder.CaptureMouse();
    }

    /// <summary>
    /// The widget sizes to its content, so a corner drag scales the font instead of setting a size:
    /// the "would-be" size from the drag is turned into a font-scale percent. The corner opposite
    /// the dragged one stays fixed.
    /// </summary>
    private void UpdateResize()
    {
        var c = DesktopService.GetCursorPosition();
        var r = WidgetGeometry.Resize(_resizeZone, _startW, _startH, _startX, _startY,
                                      c.X - _startCursor.X, c.Y - _startCursor.Y,
                                      DpiX, DpiY, ScaleMinWidth, ScaleMinHeight);
        int percent = WidgetGeometry.ScalePercentFromResize(_startPercent, _startW, _startH,
                                                            r.Width, r.Height, FontScaleMin, FontScaleMax);
        if (percent == _livePercent) return;

        _livePercent = percent;
        ApplyFontScale(percent);
        UpdateLayout();

        int x = WidgetGeometry.IsWest(_resizeZone)  ? _startX + (_startPixelW - PixelWidth)  : _startX;
        int y = WidgetGeometry.IsNorth(_resizeZone) ? _startY + (_startPixelH - PixelHeight) : _startY;
        MoveTo(x, y);
    }

    // ── Settings / About ─────────────────────────────────────────────────────

    public void OpenSettings()
    {
        // Tray double-click / tray menu / widget menu can all request this while it's already open.
        if (_settingsDialog != null)
        {
            _settingsDialog.Activate();
            return;
        }

        bool ok;
        try
        {
            _settingsDialog = new SettingsWindow(_settings, livePreviewTarget: this);
            ok = _settingsDialog.ShowDialog() == true;
        }
        finally
        {
            _settingsDialog = null;
        }

        if (!ok)
            ApplyBackground(_settings.BackgroundColor, _settings.BackgroundOpacity);
        // Rebuild rows (Cancel restores the timer list), font scale, title bar and title text.
        LoadItems();
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void About_Click(object sender, RoutedEventArgs e)
        => ((App)System.Windows.Application.Current).OpenAbout(this);

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (App.Settings.Widgets.Count <= 1)
        {
            System.Windows.MessageBox.Show(
                "This is the only widget. Use Exit to close the app instead.",
                "Remove Widget", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "Remove this timer widget?", "Confirm Remove",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            ((App)System.Windows.Application.Current).RemoveWidget(_settings.Id);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
        => System.Windows.Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        _updateTimer.Stop();
        _displayCheckTimer.Stop();
        _flashTimer.Stop();
        _soundPlayer?.Stop();
        _soundPlayer?.Dispose();
        base.OnClosed(e);
    }
}
