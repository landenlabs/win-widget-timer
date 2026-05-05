// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using WinWidgetTimer.Models;

namespace WinWidgetTimer.ViewModels;

/// <summary>ViewModel wrapper around a TimerEntry for display in the widget.</summary>
public class TimerDisplayItem : INotifyPropertyChanged
{
    public readonly TimerEntry Entry;

    public TimerDisplayItem(TimerEntry entry)
    {
        Entry = entry;
        Update();
    }

    public string Name => Entry.Name;
    public SolidColorBrush ColorBrush => Entry.ColorBrush;

    private string _timeDisplay = "";
    public string TimeDisplay
    {
        get => _timeDisplay;
        private set { if (_timeDisplay != value) { _timeDisplay = value; OnPropertyChanged(); } }
    }

    private string _stateIcon = "▶";
    public string StateIcon
    {
        get => _stateIcon;
        private set { if (_stateIcon != value) { _stateIcon = value; OnPropertyChanged(); } }
    }

    private string _typeIcon = "⏱";
    public string TypeIcon
    {
        get => _typeIcon;
        private set { if (_typeIcon != value) { _typeIcon = value; OnPropertyChanged(); } }
    }

    private bool _isDone;
    public bool IsDone
    {
        get => _isDone;
        private set { if (_isDone != value) { _isDone = value; OnPropertyChanged(); OnPropertyChanged(nameof(RowColor)); } }
    }

    // When done, flash red; otherwise use the timer's own color
    public SolidColorBrush RowColor => IsDone
        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 80, 80))
        : ColorBrush;

    private bool _isActive;
    public FontWeight RowFontWeight => _isActive ? FontWeights.Bold : FontWeights.Normal;

    public void Update()
    {
        IsDone = Entry.State == TimerState.Done;

        bool active = Entry.State == TimerState.Running || Entry.State == TimerState.Done;
        if (_isActive != active)
        {
            _isActive = active;
            OnPropertyChanged(nameof(RowFontWeight));
        }

        TypeIcon = Entry.TimerType switch
        {
            TimerType.Countdown => "⏱",
            TimerType.Elapsed   => "⏹",
            TimerType.Alarm     => "⏰",
            _                   => "⏱"
        };

        StateIcon = Entry.State switch
        {
            TimerState.Idle    => "▶",
            TimerState.Running => "⏸",
            TimerState.Paused  => "↺",
            TimerState.Done    => Entry.TimerType == TimerType.Alarm ? "🔔" : "✓",
            _                  => "▶"
        };

        TimeDisplay = Entry.TimerType switch
        {
            TimerType.Countdown => GetCountdownDisplay(),
            TimerType.Elapsed   => GetElapsedDisplay(),
            TimerType.Alarm     => GetAlarmDisplay(),
            _                   => ""
        };
    }

    private string GetCountdownDisplay()
    {
        if (Entry.State == TimerState.Done)
            return "DONE!";
        var ts = Entry.State == TimerState.Idle
            ? TimeSpan.FromSeconds(Entry.DurationSeconds)
            : Entry.GetRemaining();
        return FormatDuration(ts);
    }

    private string GetElapsedDisplay()
        => FormatDuration(Entry.GetElapsed());

    private string GetAlarmDisplay()
    {
        if (Entry.State == TimerState.Done)
            return $"🔔 {FormatAlarmTime(Entry.GetNextAlarmDateTime())}";

        var next = Entry.GetNextAlarmDateTime();
        var until = next - DateTime.Now;
        return $"{FormatAlarmTime(next)}  {FormatUntil(until)}";
    }

    private string FormatDuration(TimeSpan ts)
    {
        // Apply the entry's TimeFormat by mapping to a DateTime with the duration as time-of-day.
        // Falls back to HH:mm:ss when the format is invalid or for durations >= 24h.
        try
        {
            int totalHours = (int)ts.TotalHours;
            if (totalHours < 24)
            {
                var dt = new DateTime(2000, 1, 1, totalHours, ts.Minutes, ts.Seconds);
                return dt.ToString(Entry.TimeFormat);
            }
        }
        catch { }

        // Fallback
        int h = (int)ts.TotalHours;
        return h >= 1
            ? $"{h:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private string FormatAlarmTime(DateTime alarmDt)
    {
        try { return alarmDt.ToString(Entry.TimeFormat); }
        catch { return alarmDt.ToString("HH:mm"); }
    }

    private static string FormatUntil(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
        if (ts.TotalDays >= 1)
            return $"in {(int)ts.TotalDays}d {ts.Hours}h";
        if (ts.TotalHours >= 1)
            return $"in {(int)ts.TotalHours}h {ts.Minutes:D2}m";
        if (ts.TotalMinutes >= 1)
            return $"in {ts.Minutes}m {ts.Seconds:D2}s";
        return $"in {ts.Seconds}s";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
