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

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        AppSettings? settings = null;
        try
        {
            if (File.Exists(SettingsFile))
                settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile));
        }
        catch
        {
            // Corrupt or unreadable file: start from defaults rather than failing to launch.
        }

        if (settings == null)
            return CreateDefaultSettings();

        foreach (var w in settings.Widgets)
        {
            // Ensure each widget has at least one default timer
            if (w.Timers.Count == 0)
                w.Timers.AddRange(CreateDefaultTimers());
            DisplayService.PruneStalePositions(w);
        }
        return settings;
    }

    /// <summary>
    /// Writes to a temp file then swaps it in, so a crash or power loss mid-write can't leave a
    /// truncated settings.json behind. Failures are swallowed (saving is called on every drag/resize).
    /// </summary>
    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);
            var tmp = SettingsFile + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(tmp, SettingsFile, overwrite: true);
        }
        catch
        {
            // Best effort.
        }
    }

    private static AppSettings CreateDefaultSettings()
    {
        var widget = new WidgetSettings { Name = "Timers" };
        widget.Timers.AddRange(CreateDefaultTimers());
        return new AppSettings { Widgets = [widget] };
    }

    /// <summary>The starter timers every new widget gets (first run, tray "Add widget", empty list on load).</summary>
    public static IEnumerable<TimerEntry> CreateDefaultTimers() =>
    [
        new TimerEntry { Name = "5 Min", TimerType = TimerType.Countdown, DurationSeconds = 300,  Color = "#00FF88" },
        new TimerEntry { Name = "Stopwatch", TimerType = TimerType.Elapsed,    DurationSeconds = 0,    Color = "#89B4FA" },
        new TimerEntry { Name = "Morning",   TimerType = TimerType.Alarm,      AlarmTimeStr = "09:00", Color = "#FFD700" },
    ];
}
