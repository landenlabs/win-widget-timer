// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace WinWidgetTimer.Models;

public enum TimerType
{
    Countdown,  // counts down from a set duration to zero
    Elapsed,    // stopwatch — counts up from zero
    Alarm       // triggers at a specific time of day
}

public enum TimerState
{
    Idle,
    Running,
    Paused,
    Done
}

public class TimerEntry : INotifyPropertyChanged
{
    // ── Persisted fields ────────────────────────────────────────────────────

    public string Id { get; set; } = Guid.NewGuid().ToString();

    private string _name = "Timer";
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    private TimerType _timerType = TimerType.Countdown;
    public TimerType TimerType
    {
        get => _timerType;
        set { _timerType = value; OnPropertyChanged(); }
    }

    private string _color = "#00FF88";
    public string Color
    {
        get => _color;
        set { _color = value; _colorBrush = null; OnPropertyChanged(); OnPropertyChanged(nameof(ColorBrush)); }
    }

    // Total seconds for Countdown timers (default 5 min)
    public int DurationSeconds { get; set; } = 300;

    // HH:mm string for Alarm timers (e.g. "09:00")
    public string AlarmTimeStr { get; set; } = "09:00";

    // Day of week for Alarm timers: -1 = daily, 0 = Sunday … 6 = Saturday
    public int AlarmDayOfWeek { get; set; } = -1;

    // Display format string (DateTime / TimeSpan tokens, e.g. "HH:mm", "ddd HH:mm")
    public string TimeFormat { get; set; } = "HH:mm";

    // Notification on completion ("" = none, "system" = system beep, else filename in C:\Windows\Media\)
    public string SoundFile { get; set; } = "Alarm01.wav";
    public bool FlashOnEnd { get; set; } = true;

    // ── Runtime state (not persisted) ───────────────────────────────────────

    [JsonIgnore] public TimerState State { get; private set; } = TimerState.Idle;
    [JsonIgnore] private DateTime _startedAt;
    [JsonIgnore] private TimeSpan _pausedAccumulated;
    [JsonIgnore] private DateTime _lastTriggeredDate = DateTime.MinValue;

    [JsonIgnore] private SolidColorBrush? _colorBrush;
    [JsonIgnore] public SolidColorBrush ColorBrush => _colorBrush ??= ParseBrush();

    public void InvalidateBrush() { _colorBrush = null; }

    private SolidColorBrush ParseBrush()
    {
        try { return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(_color)); }
        catch { return System.Windows.Media.Brushes.White; }
    }

    // ── Timer control ───────────────────────────────────────────────────────

    public void Start()
    {
        if (State == TimerState.Done) Reset();
        if (State == TimerState.Paused)
        {
            // Resume: new reference point, keep accumulated elapsed
            _startedAt = DateTime.Now;
        }
        else if (State == TimerState.Idle)
        {
            _startedAt = DateTime.Now;
            _pausedAccumulated = TimeSpan.Zero;
        }
        State = TimerState.Running;
    }

    public void Pause()
    {
        if (State != TimerState.Running) return;
        _pausedAccumulated += DateTime.Now - _startedAt;
        State = TimerState.Paused;
    }

    public void Reset()
    {
        State = TimerState.Idle;
        _startedAt = DateTime.MinValue;
        _pausedAccumulated = TimeSpan.Zero;
    }

    /// <summary>Single-click toggle: Idle→Run, Run→Pause, Pause→Idle(reset), Done→Idle(reset).</summary>
    public void Toggle()
    {
        switch (State)
        {
            case TimerState.Idle:
                Start();
                break;
            case TimerState.Running:
                Pause();
                break;
            case TimerState.Paused:
                Reset();
                break;
            case TimerState.Done:
                Reset();
                break;
        }
    }

    // ── Time queries ────────────────────────────────────────────────────────

    public TimeSpan GetElapsed()
    {
        return State switch
        {
            TimerState.Running => _pausedAccumulated + (DateTime.Now - _startedAt),
            TimerState.Paused  => _pausedAccumulated,
            _                  => _pausedAccumulated
        };
    }

    public TimeSpan GetRemaining()
    {
        var total = TimeSpan.FromSeconds(DurationSeconds);
        var rem = total - GetElapsed();
        return rem < TimeSpan.Zero ? TimeSpan.Zero : rem;
    }

    public TimeSpan ParseAlarmTime()
    {
        if (TimeSpan.TryParse(AlarmTimeStr, out var ts)) return ts;
        return TimeSpan.FromHours(9);
    }

    /// <summary>Returns true if the timer just transitioned to Done.</summary>
    public bool CheckAndTrigger()
    {
        if (State != TimerState.Running) return false;

        if (_timerType == TimerType.Countdown && GetRemaining() <= TimeSpan.Zero)
        {
            State = TimerState.Done;
            _pausedAccumulated = TimeSpan.FromSeconds(DurationSeconds);
            return true;
        }

        if (_timerType == TimerType.Alarm)
        {
            var now = DateTime.Now;
            if (AlarmDayOfWeek >= 0 && (int)now.DayOfWeek != AlarmDayOfWeek)
                return false;
            var alarmToday = now.Date + ParseAlarmTime();
            if (now >= alarmToday && _lastTriggeredDate.Date != now.Date)
            {
                State = TimerState.Done;
                _lastTriggeredDate = now;
                return true;
            }
        }
        return false;
    }

    // ── Helpers for Alarm display ────────────────────────────────────────────

    public DateTime GetNextAlarmDateTime()
    {
        var now = DateTime.Now;
        var alarmTime = ParseAlarmTime();

        if (AlarmDayOfWeek < 0)
        {
            var today = now.Date + alarmTime;
            return today > now ? today : today.AddDays(1);
        }

        // Specific day of week
        int daysUntil = ((AlarmDayOfWeek - (int)now.DayOfWeek) + 7) % 7;
        var candidate = now.Date.AddDays(daysUntil) + alarmTime;
        if (daysUntil == 0 && candidate <= now) candidate = candidate.AddDays(7);
        return candidate;
    }

    // ── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
