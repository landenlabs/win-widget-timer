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
    private readonly WidgetSettings _settings;
    private List<TimerDisplayItem> _items = [];

    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _displayCheckTimer;
    private DisplayConfiguration _currentDisplayConfig;

    private bool _isEmbedded;
    private bool _isDragging;
    private System.Windows.Point _dragOffset;

    private readonly DispatcherTimer _flashTimer;
    private int _flashCount;
    private static readonly SolidColorBrush _flashBrush     = new(Color.FromRgb(0xFF, 0x33, 0x33));
    private static readonly SolidColorBrush _normalBorder   = new(Color.FromArgb(0xFF, 0x45, 0x45, 0x70));
    private bool _flashState;

    private SoundPlayer? _soundPlayer;

    private string _bgColorHex;
    private double _bgOpacity;

    public string WidgetId => _settings.Id;

    public WidgetWindow(WidgetSettings settings)
    {
        _settings = settings;
        _bgColorHex = string.IsNullOrEmpty(settings.BackgroundColor) ? "#1E1E2E" : settings.BackgroundColor;
        _bgOpacity  = settings.BackgroundOpacity > 0 ? settings.BackgroundOpacity : 0.85;

        InitializeComponent();

        _currentDisplayConfig = DisplayService.GetCurrentDisplayConfiguration();
        var (x, y) = DisplayService.GetDisplayPosition(settings, _currentDisplayConfig);
        Left = x;
        Top  = y;

        LoadItems();

        _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += (_, _) => Tick();
        _updateTimer.Start();

        _displayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _displayCheckTimer.Tick += (_, _) => CheckDisplayConfigurationChanged();
        _displayCheckTimer.Start();

        _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _flashTimer.Tick += (_, _) => FlashTick();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        ApplyBackgroundInternal(_bgOpacity);
        ApplyFontScale(_settings.FontScalePercent > 0 ? _settings.FontScalePercent : 100);
        ApplyShowTitleBar(_settings.ShowTitleBar);

        if (_settings.EmbedInWallpaper)
        {
            _isEmbedded = DesktopService.EmbedInWallpaper(this);
            if (_isEmbedded)
                DesktopService.MoveEmbeddedWindow(this, (int)Left, (int)Top);
            else
                DesktopService.SetAlwaysOnBottom(this);
        }
        else
        {
            DesktopService.SetAlwaysOnBottom(this);
        }
    }

    // ── Load / Reload ────────────────────────────────────────────────────────

    public void LoadItems()
    {
        _items = _settings.Timers.Select(t => new TimerDisplayItem(t)).ToList();
        TimerList.ItemsSource = _items;
        TitleText.Text = $"⏱ {_settings.Name}";
        ApplyFontScale(_settings.FontScalePercent > 0 ? _settings.FontScalePercent : 100);
        ApplyShowTitleBar(_settings.ShowTitleBar);
        if (RemoveMenuItem != null)
            RemoveMenuItem.IsEnabled = App.Settings.Widgets.Count > 1;
    }

    // ── Tick: update displays and check triggers ─────────────────────────────

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

    private void CheckDisplayConfigurationChanged()
    {
        var newConfig = DisplayService.GetCurrentDisplayConfiguration();
        if (newConfig.ConfigurationHash == _currentDisplayConfig.ConfigurationHash) return;

        _currentDisplayConfig = newConfig;
        var (x, y) = DisplayService.GetDisplayPosition(_settings, _currentDisplayConfig);
        if (_isEmbedded)
            DesktopService.MoveEmbeddedWindow(this, x, y);
        else { Left = x; Top = y; }
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
    }

    public void ApplyShowTitleBar(bool show)
    {
        TitleBarGrid.Visibility      = show ? Visibility.Visible : Visibility.Collapsed;
        TitleBarSeparator.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
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

    // ── Dragging ─────────────────────────────────────────────────────────────

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled) return; // a timer row already handled this
        if (IsClickOnInteractiveElement(e.OriginalSource)) return;

        if (_isEmbedded)
        {
            var cursor = DesktopService.GetCursorPosition();
            var bounds = DesktopService.GetWindowBounds(this);
            _dragOffset = new System.Windows.Point(cursor.X - bounds.Left, cursor.Y - bounds.Top);
            _isDragging = true;
            WidgetBorder.CaptureMouse();
        }
        else
        {
            try { DragMove(); } catch { }
            SaveCurrentPosition();
        }
        e.Handled = true;
    }

    private void Widget_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging || !_isEmbedded) return;
        var cursor = DesktopService.GetCursorPosition();
        DesktopService.MoveEmbeddedWindow(this, cursor.X - (int)_dragOffset.X, cursor.Y - (int)_dragOffset.Y);
    }

    private void Widget_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        WidgetBorder.ReleaseMouseCapture();
        if (_isEmbedded)
        {
            var bounds = DesktopService.GetWindowBounds(this);
            DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfig, bounds.Left, bounds.Top);
            SettingsService.Save(App.Settings);
        }
    }

    private void SaveCurrentPosition()
    {
        DisplayService.SaveDisplayPosition(_settings, _currentDisplayConfig, (int)Left, (int)Top);
        SettingsService.Save(App.Settings);
    }

    private static bool IsClickOnInteractiveElement(object? source)
    {
        if (source is System.Windows.Controls.Button) return true;
        if (source is FrameworkElement fe)
        {
            var parent = VisualTreeHelper.GetParent(fe);
            while (parent != null)
            {
                if (parent is System.Windows.Controls.Button) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return false;
    }

    // ── Settings / About ─────────────────────────────────────────────────────

    public void OpenSettings()
    {
        var dlg = new SettingsWindow(_settings, livePreviewTarget: this);
        if (dlg.ShowDialog() == true)
            LoadItems();
        else
        {
            ApplyBackground(_settings.BackgroundColor, _settings.BackgroundOpacity);
            ApplyFontScale(_settings.FontScalePercent > 0 ? _settings.FontScalePercent : 100);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) => OpenSettings();

    private void About_Click(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
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
