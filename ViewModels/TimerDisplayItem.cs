// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public void Update()
    {
        IsDone = Entry.State == TimerState.Done;

        TypeIcon = Entry.TimerType switch
        {
            TimerType.Countdown => "⏱",
            TimerType.Elapsed   => "⏹",
            TimerType.Alarm     => "⏰",
            _                   => "⏱"
        };

        StateIcon = Entry.State switch
        {
            TimerState.Idle    => Entry.TimerType == TimerType.Alarm ? "⏰" : "▶",
            TimerState.Running => Entry.TimerType == TimerType.Alarm ? "⏰" : "⏸",
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
        if (Entry.State == TimerState.Idle)
            return FormatTimeSpan(TimeSpan.FromSeconds(Entry.DurationSeconds));
        if (Entry.State == TimerState.Done)
            return "DONE!";
        return FormatTimeSpan(Entry.GetRemaining());
    }

    private string GetElapsedDisplay()
        => FormatTimeSpan(Entry.GetElapsed());

    private string GetAlarmDisplay()
    {
        if (Entry.State == TimerState.Done)
            return $"🔔 {Entry.AlarmTimeStr}";

        var until = Entry.GetNextAlarmDateTime() - DateTime.Now;
        return $"{Entry.AlarmTimeStr}  {FormatUntil(until)}";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private static string FormatUntil(TimeSpan ts)
    {
        if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
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
