// Copyright (c) 2026 LanDen Labs - Dennis Lang
using WinWidgetTimer.Models;

namespace WinWidgetTimer.Services;

public static class DisplayService
{
    public static DisplayConfiguration GetCurrentDisplayConfiguration()
    {
        var screens = GetAllScreens();
        int minX = 0, minY = 0, maxX = 0, maxY = 0;

        foreach (var s in screens)
        {
            minX = Math.Min(minX, s.Bounds.X);
            minY = Math.Min(minY, s.Bounds.Y);
            maxX = Math.Max(maxX, s.Bounds.X + s.Bounds.Width);
            maxY = Math.Max(maxY, s.Bounds.Y + s.Bounds.Height);
        }

        var hash = GenerateHash(screens);
        int count = screens.Count;
        return new DisplayConfiguration
        {
            ConfigurationHash = hash,
            MonitorCount = count,
            TotalWidth  = maxX - minX,
            TotalHeight = maxY - minY,
            Description = count == 1
                ? $"1 Monitor: {screens[0].Bounds.Width}x{screens[0].Bounds.Height}"
                : $"{count} Monitors: {maxX - minX}x{maxY - minY}",
            LastUsed = DateTime.UtcNow
        };
    }

    public static (int X, int Y) GetDisplayPosition(WidgetSettings settings, DisplayConfiguration config)
    {
        if (settings.DisplayPositions.TryGetValue(config.ConfigurationHash, out var pos))
            return (pos.X, pos.Y);
        return (settings.X, settings.Y);
    }

    public static void SaveDisplayPosition(WidgetSettings settings, DisplayConfiguration config, int x, int y)
    {
        settings.DisplayPositions[config.ConfigurationHash] = new DisplayPosition
        {
            ConfigurationHash = config.ConfigurationHash,
            X = x, Y = y,
            LastSet = DateTime.UtcNow
        };
        settings.LastDisplayConfigurationHash = config.ConfigurationHash;
        settings.X = x;
        settings.Y = y;
    }

    // ── Internals ────────────────────────────────────────────────────────────

    private static List<ScreenInfo> GetAllScreens()
    {
        try
        {
            return System.Windows.Forms.Screen.AllScreens
                .Select(s => new ScreenInfo
                {
                    Bounds      = new System.Drawing.Rectangle(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
                    IsPrimary   = s.Primary,
                    DeviceName  = s.DeviceName
                }).ToList();
        }
        catch
        {
            return [new ScreenInfo { Bounds = new System.Drawing.Rectangle(0, 0, 1920, 1080), IsPrimary = true, DeviceName = "Primary" }];
        }
    }

    private static string GenerateHash(List<ScreenInfo> screens)
    {
        if (screens.Count == 0) return "DEFAULT";
        var parts = screens
            .OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y)
            .Select(s => $"{s.Bounds.X}_{s.Bounds.Y}_{s.Bounds.Width}x{s.Bounds.Height}_{s.IsPrimary}");
        var combined = string.Join("|", parts);
        return $"DISP_{Math.Abs(combined.GetHashCode()):X8}";
    }
}

internal class ScreenInfo
{
    public System.Drawing.Rectangle Bounds { get; set; }
    public bool IsPrimary { get; set; }
    public string DeviceName { get; set; } = string.Empty;
}
