// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinWidgetTimer.Models;
using WinWidgetTimer.Services;

namespace WinWidgetTimer.Windows;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private readonly WidgetSettings _widget;
    private readonly WidgetWindow? _livePreviewTarget;

    // Snapshot for Cancel restore
    private readonly string _origBgColor;
    private readonly int    _origBgOpacityPercent;
    private readonly int    _origFontScalePercent;

    // ── Bindable: widget-level appearance ────────────────────────────────────

    private string _widgetName = "";
    public string WidgetName
    {
        get => _widgetName;
        set { _widgetName = value; OnPropertyChanged(); }
    }

    private string _bgColorHex = "#1E1E2E";
    public string BgColorHex
    {
        get => _bgColorHex;
        set { _bgColorHex = value; _bgColorBrush = null; OnPropertyChanged(); OnPropertyChanged(nameof(BgColorBrush)); LivePreviewBackground(); }
    }

    private SolidColorBrush? _bgColorBrush;
    public SolidColorBrush BgColorBrush => _bgColorBrush ??= ParseBrush(_bgColorHex);

    private int _bgOpacityPercent = 85;
    public int BgOpacityPercent
    {
        get => _bgOpacityPercent;
        set { _bgOpacityPercent = value; OnPropertyChanged(); LivePreviewBackground(); }
    }

    private int _fontScalePercent = 100;
    public int FontScalePercent
    {
        get => _fontScalePercent;
        set { _fontScalePercent = value; OnPropertyChanged(); _livePreviewTarget?.ApplyFontScale(value); }
    }

    private bool _embedInWallpaper;
    public bool EmbedInWallpaper
    {
        get => _embedInWallpaper;
        set { _embedInWallpaper = value; OnPropertyChanged(); }
    }

    private bool _showTitleBar;
    public bool ShowTitleBar
    {
        get => _showTitleBar;
        set { _showTitleBar = value; OnPropertyChanged(); _livePreviewTarget?.ApplyShowTitleBar(value); }
    }

    // ── Bindable: timers list ────────────────────────────────────────────────

    public ObservableCollection<TimerListItem> Timers { get; } = [];

    private TimerListItem? _selectedTimerItem;
    public TimerListItem? SelectedTimer
    {
        get => _selectedTimerItem;
        set
        {
            _selectedTimerItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTimer));
            LoadTimerIntoRightPanel();
        }
    }

    public bool HasSelectedTimer => _selectedTimerItem != null;

    // ── Bindable: per-timer properties (right panel) ─────────────────────────

    private string _selectedTimerName = "";
    public string SelectedTimerName
    {
        get => _selectedTimerName;
        set
        {
            _selectedTimerName = value;
            OnPropertyChanged();
            if (_selectedTimerItem != null)
            {
                _selectedTimerItem.Entry.Name = value;
                _selectedTimerItem.NotifyNameChanged();
            }
        }
    }

    private string _selectedTimerColorHex = "#00FF88";
    public string SelectedTimerColorHex
    {
        get => _selectedTimerColorHex;
        set
        {
            _selectedTimerColorHex = value;
            _selectedTimerColorBrush = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTimerColorBrush));
            if (_selectedTimerItem != null)
            {
                _selectedTimerItem.Entry.Color = value;
                _selectedTimerItem.Entry.InvalidateBrush();
                _selectedTimerItem.NotifyColorChanged();
            }
            UpdateTimerColorHexLabel();
        }
    }

    private SolidColorBrush? _selectedTimerColorBrush;
    public SolidColorBrush SelectedTimerColorBrush => _selectedTimerColorBrush ??= ParseBrush(_selectedTimerColorHex);

    // Sound file list: each entry is (DisplayLabel, StoredValue)
    private List<(string Label, string Value)> _soundEntries = [];

    // ── Constructor ──────────────────────────────────────────────────────────

    public SettingsWindow(WidgetSettings widget, WidgetWindow? livePreviewTarget = null)
    {
        _widget            = widget;
        _livePreviewTarget = livePreviewTarget;

        InitializeComponent();
        Topmost = true;

        // Snapshot originals for Cancel
        _origBgColor          = string.IsNullOrEmpty(widget.BackgroundColor) ? "#1E1E2E" : widget.BackgroundColor;
        _origBgOpacityPercent = (int)Math.Round(widget.BackgroundOpacity * 100);
        if (_origBgOpacityPercent == 0) _origBgOpacityPercent = 85;
        _origFontScalePercent = widget.FontScalePercent > 0 ? widget.FontScalePercent : 100;

        // Load working copies
        _widgetName       = widget.Name;
        _bgColorHex       = _origBgColor;
        _bgOpacityPercent = _origBgOpacityPercent;
        _fontScalePercent = _origFontScalePercent;
        _embedInWallpaper = widget.EmbedInWallpaper;
        _showTitleBar     = widget.ShowTitleBar;

        // Populate timer list
        foreach (var t in widget.Timers)
            Timers.Add(new TimerListItem(t));

        OnPropertyChanged(nameof(WidgetName));
        OnPropertyChanged(nameof(BgColorHex));
        OnPropertyChanged(nameof(BgColorBrush));
        OnPropertyChanged(nameof(BgOpacityPercent));
        OnPropertyChanged(nameof(FontScalePercent));
        OnPropertyChanged(nameof(EmbedInWallpaper));
        OnPropertyChanged(nameof(ShowTitleBar));
        UpdateBgColorHexLabel();

        PopulateSoundCombo();

        FormatHelpText.Text =
            "yyyy    4-digit year           2026\n" +
            "yy      2-digit year           26\n" +
            "MMMM    Full month name        January\n" +
            "MMM     3-char month abbr.     Jan\n" +
            "MM      2-digit month          01 – 12\n" +
            "dddd    Full day name          Monday\n" +
            "ddd     3-char day abbr.       Mon\n" +
            "dd      2-digit day            01 – 31\n" +
            "HH      24-hour, padded        00 – 23\n" +
            "H       24-hour                0 – 23\n" +
            "hh      12-hour, padded        01 – 12\n" +
            "h       12-hour                1 – 12\n" +
            "mm      Minutes                00 – 59\n" +
            "ss      Seconds                00 – 59\n" +
            "tt      AM / PM designator     AM · PM\n" +
            "fff     Milliseconds           001 – 999";

        // Select first timer if available
        if (Timers.Count > 0)
            SelectedTimer = Timers[0];
    }

    private void PopulateSoundCombo()
    {
        _soundEntries = [("(None)", ""), ("(System Alert)", "system")];

        var mediaDir = @"C:\Windows\Media";
        if (Directory.Exists(mediaDir))
        {
            foreach (var path in Directory.GetFiles(mediaDir, "*.wav").Order())
                _soundEntries.Add((Path.GetFileName(path), Path.GetFileName(path)));
        }

        SoundCombo.Items.Clear();
        foreach (var (label, _) in _soundEntries)
            SoundCombo.Items.Add(label);
    }

    private void SetSoundComboTo(string storedValue)
    {
        int idx = _soundEntries.FindIndex(e => e.Value == storedValue);
        if (idx < 0) idx = _soundEntries.FindIndex(e => e.Value == "system");
        if (idx < 0) idx = 0;
        SoundCombo.SelectionChanged -= SoundCombo_SelectionChanged;
        SoundCombo.SelectedIndex = idx;
        SoundCombo.SelectionChanged += SoundCombo_SelectionChanged;
    }

    // ── Right panel: load timer properties ───────────────────────────────────

    private bool _suppressDurationEvents;

    private void LoadTimerIntoRightPanel()
    {
        var entry = _selectedTimerItem?.Entry;

        if (entry == null)
        {
            CountdownSection.Visibility = Visibility.Collapsed;
            AlarmSection.Visibility     = Visibility.Collapsed;
            return;
        }

        _suppressDurationEvents = true;

        _selectedTimerName = entry.Name;
        OnPropertyChanged(nameof(SelectedTimerName));

        _selectedTimerColorHex = entry.Color;
        _selectedTimerColorBrush = null;
        OnPropertyChanged(nameof(SelectedTimerColorHex));
        OnPropertyChanged(nameof(SelectedTimerColorBrush));
        UpdateTimerColorHexLabel();

        // Set ComboBox selection without firing our handler
        TimerTypeCombo.SelectionChanged -= TimerTypeCombo_SelectionChanged;
        TimerTypeCombo.SelectedIndex = (int)entry.TimerType;
        TimerTypeCombo.SelectionChanged += TimerTypeCombo_SelectionChanged;

        UpdateTypeSpecificSections(entry.TimerType);

        // Duration fields
        int total = entry.DurationSeconds;
        DurHours.Text   = (total / 3600).ToString();
        DurMinutes.Text = ((total % 3600) / 60).ToString();
        DurSeconds.Text = (total % 60).ToString();
        UpdateDurationPreview();

        // Alarm day of week
        AlarmDayCombo.SelectionChanged -= AlarmDay_SelectionChanged;
        AlarmDayCombo.SelectedIndex = entry.AlarmDayOfWeek < 0 ? 0 : entry.AlarmDayOfWeek + 1;
        AlarmDayCombo.SelectionChanged += AlarmDay_SelectionChanged;

        // Alarm time fields
        var parts = entry.AlarmTimeStr.Split(':');
        AlarmHour.Text   = parts.Length > 0 ? parts[0] : "9";
        AlarmMinute.Text = parts.Length > 1 ? parts[1] : "00";
        UpdateAlarmPreview();

        // Time format
        FormatBox.TextChanged -= TimerFormat_TextChanged;
        FormatBox.Text = entry.TimeFormat;
        FormatBox.TextChanged += TimerFormat_TextChanged;

        // Completion notification fields
        FlashCheck.IsChecked = entry.FlashOnEnd;
        SetSoundComboTo(entry.SoundFile);

        _suppressDurationEvents = false;
    }

    private void UpdateTypeSpecificSections(TimerType type)
    {
        CountdownSection.Visibility  = type == TimerType.Countdown ? Visibility.Visible : Visibility.Collapsed;
        AlarmSection.Visibility      = type == TimerType.Alarm     ? Visibility.Visible : Visibility.Collapsed;
        CompletionSection.Visibility = Visibility.Visible;
    }

    // ── Timer type combo ─────────────────────────────────────────────────────

    private void TimerTypeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        var newType = (TimerType)TimerTypeCombo.SelectedIndex;
        _selectedTimerItem.Entry.TimerType = newType;
        _selectedTimerItem.NotifyTypeChanged();
        UpdateTypeSpecificSections(newType);
    }

    // ── Duration change handlers ─────────────────────────────────────────────

    private void Duration_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        if (int.TryParse(DurHours.Text,   out int h) &&
            int.TryParse(DurMinutes.Text, out int m) &&
            int.TryParse(DurSeconds.Text, out int s))
        {
            m = Math.Min(59, Math.Max(0, m));
            s = Math.Min(59, Math.Max(0, s));
            _selectedTimerItem.Entry.DurationSeconds = h * 3600 + m * 60 + s;
            _selectedTimerItem.NotifyDurationChanged();
            UpdateDurationPreview();
        }
    }

    private void UpdateDurationPreview()
    {
        if (_selectedTimerItem == null) { DurationPreviewLabel.Text = ""; return; }
        var ts = TimeSpan.FromSeconds(_selectedTimerItem.Entry.DurationSeconds);
        DurationPreviewLabel.Text = ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s"
            : $"{ts.Minutes}m {ts.Seconds}s";
    }

    // ── Alarm time change handlers ────────────────────────────────────────────

    private void AlarmTime_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        if (int.TryParse(AlarmHour.Text,   out int h) &&
            int.TryParse(AlarmMinute.Text, out int m))
        {
            h = Math.Min(23, Math.Max(0, h));
            m = Math.Min(59, Math.Max(0, m));
            _selectedTimerItem.Entry.AlarmTimeStr = $"{h:D2}:{m:D2}";
            _selectedTimerItem.NotifyAlarmChanged();
            UpdateAlarmPreview();
        }
    }

    private void UpdateAlarmPreview()
    {
        if (_selectedTimerItem == null) { AlarmPreviewLabel.Text = ""; return; }
        var next = _selectedTimerItem.Entry.GetNextAlarmDateTime();
        var until = next - DateTime.Now;
        AlarmPreviewLabel.Text = until.TotalDays >= 1
            ? $"in {(int)until.TotalDays}d {until.Hours}h"
            : until.TotalHours >= 1
                ? $"in {(int)until.TotalHours}h {until.Minutes}m"
                : $"in {until.Minutes}m";
    }

    // ── Alarm day-of-week handler ─────────────────────────────────────────────

    private void AlarmDay_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        // Index 0 = Daily (-1), Index 1..7 = Sunday(0)..Saturday(6)
        int idx = AlarmDayCombo.SelectedIndex;
        _selectedTimerItem.Entry.AlarmDayOfWeek = idx <= 0 ? -1 : idx - 1;
        _selectedTimerItem.NotifyAlarmChanged();
        UpdateAlarmPreview();
    }

    // ── Time format handler ────────────────────────────────────────────────────

    private void TimerFormat_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        _selectedTimerItem.Entry.TimeFormat = FormatBox.Text;
        _selectedTimerItem.NotifyFormatChanged();
    }

    private void FormatHelp_Click(object sender, RoutedEventArgs e)
        => FormatHelpPopup.IsOpen = !FormatHelpPopup.IsOpen;

    // ── Completion notification handlers ─────────────────────────────────────

    private void FlashCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        _selectedTimerItem.Entry.FlashOnEnd = FlashCheck.IsChecked == true;
    }

    private void SoundCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_suppressDurationEvents || _selectedTimerItem == null) return;
        int idx = SoundCombo.SelectedIndex;
        if (idx >= 0 && idx < _soundEntries.Count)
            _selectedTimerItem.Entry.SoundFile = _soundEntries[idx].Value;
    }

    private void TestSound_Click(object sender, RoutedEventArgs e)
    {
        int idx = SoundCombo.SelectedIndex;
        if (idx < 0 || idx >= _soundEntries.Count) return;
        var value = _soundEntries[idx].Value;
        try
        {
            if (string.IsNullOrEmpty(value)) return;
            if (value == "system") { SystemSounds.Exclamation.Play(); return; }
            var path = Path.Combine(@"C:\Windows\Media", value);
            if (File.Exists(path)) new SoundPlayer(path).Play();
        }
        catch { }
    }

    // ── Drag-to-reorder ──────────────────────────────────────────────────────

    private TimerListItem? _pendingDrag;
    private TimerListItem? _dragItem;
    private System.Windows.Point _dragStart;

    private void TimersList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = e.OriginalSource as DependencyObject;
        while (element != null && element != TimersList)
        {
            if (element is FrameworkElement fe && fe.Tag as string == "DragHandle"
                && fe.DataContext is TimerListItem item)
            {
                _pendingDrag = item;
                _dragStart = (System.Windows.Point)e.GetPosition(null);
                return;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        _pendingDrag = null;
    }

    private void TimersList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_pendingDrag == null || e.LeftButton != MouseButtonState.Pressed) return;

        var pos = (System.Windows.Point)e.GetPosition(null);
        var diff = pos - _dragStart;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var item = _pendingDrag;
        _pendingDrag = null;
        _dragItem = item;
        DragDrop.DoDragDrop(TimersList, item, System.Windows.DragDropEffects.Move);
        _dragItem = null;
    }

    private void TimersList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        => _pendingDrag = null;

    private void TimersList_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = _dragItem != null ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void TimersList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (_dragItem == null) return;

        var target = GetTimerAtDropPoint(e.GetPosition(TimersList));
        if (target != null && target != _dragItem)
        {
            int from = Timers.IndexOf(_dragItem);
            int to   = Timers.IndexOf(target);
            if (from >= 0 && to >= 0)
            {
                Timers.Move(from, to);
                var entry = _widget.Timers[from];
                _widget.Timers.RemoveAt(from);
                _widget.Timers.Insert(to, entry);
            }
        }
        SelectedTimer = _dragItem;
        _dragItem = null;
    }

    private TimerListItem? GetTimerAtDropPoint(System.Windows.Point point)
    {
        var element = TimersList.InputHitTest(point) as DependencyObject;
        while (element != null)
        {
            if (element is System.Windows.Controls.ListBoxItem lbi && lbi.Content is TimerListItem item)
                return item;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    // ── Add / Delete ─────────────────────────────────────────────────────────

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var newEntry = new TimerEntry
        {
            Name            = $"Timer {Timers.Count + 1}",
            TimerType       = TimerType.Countdown,
            DurationSeconds = 300,
            Color           = "#89B4FA"
        };
        _widget.Timers.Add(newEntry);
        var item = new TimerListItem(newEntry);
        Timers.Add(item);
        SelectedTimer = item;
        TimersList.ScrollIntoView(item);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTimerItem == null) return;
        if (Timers.Count == 1)
        {
            System.Windows.MessageBox.Show("At least one timer is required.",
                "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        int idx = Timers.IndexOf(_selectedTimerItem);
        _widget.Timers.Remove(_selectedTimerItem.Entry);
        Timers.Remove(_selectedTimerItem);
        SelectedTimer = Timers.Count > 0 ? Timers[Math.Min(idx, Timers.Count - 1)] : null;
    }

    // ── Color pickers ────────────────────────────────────────────────────────

    private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
        => OpenTimerColorPicker();

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        => OpenTimerColorPicker();

    private void OpenTimerColorPicker()
    {
        var picker = new ColorPickerWindow(_selectedTimerColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            SelectedTimerColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void UpdateTimerColorHexLabel()
    {
        if (TimerColorHexLabel != null)
            TimerColorHexLabel.Text = _selectedTimerColorHex.ToUpperInvariant();
    }

    private void BgColorSwatch_Click(object sender, MouseButtonEventArgs e)
        => OpenBgColorPicker();

    private void BgColorSwatch_Click(object sender, RoutedEventArgs e)
        => OpenBgColorPicker();

    private void OpenBgColorPicker()
    {
        var picker = new ColorPickerWindow(_bgColorHex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            var c = picker.SelectedColor;
            BgColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private void UpdateBgColorHexLabel()
    {
        if (BgColorHexLabel != null)
            BgColorHexLabel.Text = _bgColorHex.ToUpperInvariant();
    }

    private void LivePreviewBackground()
    {
        _livePreviewTarget?.ApplyBackground(_bgColorHex, _bgOpacityPercent / 100.0);
        UpdateBgColorHexLabel();
    }

    // ── Save / Cancel ────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _widget.Name              = _widgetName;
        _widget.BackgroundColor   = _bgColorHex;
        _widget.BackgroundOpacity = _bgOpacityPercent / 100.0;
        _widget.FontScalePercent  = _fontScalePercent;
        _widget.EmbedInWallpaper  = _embedInWallpaper;
        _widget.ShowTitleBar      = _showTitleBar;
        SettingsService.Save(App.Settings);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _livePreviewTarget?.ApplyBackground(_origBgColor, _origBgOpacityPercent / 100.0);
        _livePreviewTarget?.ApplyFontScale(_origFontScalePercent);
        DialogResult = false;
        Close();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SolidColorBrush ParseBrush(string hex)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); }
        catch { return Brushes.White; }
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Thin INPC wrapper around TimerEntry for display in the settings list.
/// Notifies the ListBox when specific fields change without binding to TimerEntry directly.
/// </summary>
public class TimerListItem : System.ComponentModel.INotifyPropertyChanged
{
    public readonly TimerEntry Entry;

    public TimerListItem(TimerEntry entry) { Entry = entry; }

    public string Name            => Entry.Name;
    public SolidColorBrush ColorBrush => Entry.ColorBrush;
    public string DurationPreview => BuildDurationPreview();
    public string Format          => Entry.TimeFormat;
    public string TypeIcon => Entry.TimerType switch
    {
        TimerType.Countdown => "⏱",
        TimerType.Elapsed   => "⏹",
        TimerType.Alarm     => "⏰",
        _                   => "⏱"
    };

    public void NotifyNameChanged()     { OnPropertyChanged(nameof(Name)); }
    public void NotifyColorChanged()    { OnPropertyChanged(nameof(ColorBrush)); }
    public void NotifyTypeChanged()     { OnPropertyChanged(nameof(TypeIcon)); OnPropertyChanged(nameof(DurationPreview)); }
    public void NotifyDurationChanged() { OnPropertyChanged(nameof(DurationPreview)); }
    public void NotifyAlarmChanged()    { OnPropertyChanged(nameof(DurationPreview)); }
    public void NotifyFormatChanged()   { OnPropertyChanged(nameof(Format)); }

    private string BuildDurationPreview()
    {
        return Entry.TimerType switch
        {
            TimerType.Countdown => FormatSeconds(Entry.DurationSeconds),
            TimerType.Elapsed   => "stopwatch",
            TimerType.Alarm     => BuildAlarmPreview(),
            _                   => ""
        };
    }

    private string BuildAlarmPreview()
    {
        if (Entry.AlarmDayOfWeek < 0) return Entry.AlarmTimeStr;
        string[] abbr = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
        return $"{abbr[Entry.AlarmDayOfWeek]} {Entry.AlarmTimeStr}";
    }

    private static string FormatSeconds(int total)
    {
        var ts = TimeSpan.FromSeconds(total);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes:D2}m"
            : $"{ts.Minutes}m {ts.Seconds:D2}s";
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}
