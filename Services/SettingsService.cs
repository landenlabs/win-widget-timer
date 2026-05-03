// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.IO;
using System.Text.Json;
using WinWidgetTimer.Models;

namespace WinWidgetTimer.Services;

public static class SettingsService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WinWidgetTimer"
    );

    private static readonly string SettingsFile = Path.Combine(AppDataPath, "settings.json");

    static SettingsService()
    {
        if (!Directory.Exists(AppDataPath))
            Directory.CreateDirectory(AppDataPath);
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    // Ensure each widget has at least one default timer
                    foreach (var w in settings.Widgets)
                    {
                        if (w.Timers.Count == 0)
                            w.Timers.AddRange(CreateDefaultTimers());
                    }
                    return settings;
                }
            }
        }
        catch { /* fallback to defaults */ }

        return CreateDefaultSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFile, json);
        }
        catch { /* silently ignore */ }
    }

    private static AppSettings CreateDefaultSettings()
    {
        var widget = new WidgetSettings { Name = "Timers" };
        widget.Timers.AddRange(CreateDefaultTimers());
        return new AppSettings { Widgets = [widget] };
    }

    private static IEnumerable<TimerEntry> CreateDefaultTimers() =>
    [
        new TimerEntry { Name = "5 Min", TimerType = TimerType.Countdown, DurationSeconds = 300,  Color = "#00FF88" },
        new TimerEntry { Name = "Stopwatch", TimerType = TimerType.Elapsed,    DurationSeconds = 0,    Color = "#89B4FA" },
        new TimerEntry { Name = "Morning",   TimerType = TimerType.Alarm,      AlarmTimeStr = "09:00", Color = "#FFD700" },
    ];
}
