// Copyright (c) 2026 LanDen Labs - Dennis Lang
using WinWidgetTimer.Models;

namespace WinWidgetTimer.Services;

/// <summary>
/// Monitor enumeration and per-monitor-layout widget positions.
/// All positions are screen physical pixels (see <see cref="WidgetGeometry"/>).
/// </summary>
public static class DisplayService
{
    /// <summary>All attached screens (bounds + work area in physical pixels).</summary>
    public static List<ScreenInfo> GetScreens()
    {
        var list = new List<ScreenInfo>();
        try
        {
            foreach (var s in System.Windows.Forms.Screen.AllScreens)
            {
                list.Add(new ScreenInfo(
                    new PxRect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height),
                    new PxRect(s.WorkingArea.X, s.WorkingArea.Y, s.WorkingArea.Width, s.WorkingArea.Height),
                    s.Primary));
            }
        }
        catch
        {
            // Fall through to the default below.
        }

        if (list.Count == 0)
            list.Add(new ScreenInfo(new PxRect(0, 0, 1920, 1080), new PxRect(0, 0, 1920, 1040), true));
        return list;
    }

    public static DisplayConfiguration GetCurrentDisplayConfiguration()
    {
        var screens = GetScreens();
        int minX = screens.Min(s => s.Bounds.X), minY = screens.Min(s => s.Bounds.Y);
        int maxX = screens.Max(s => s.Bounds.Right), maxY = screens.Max(s => s.Bounds.Bottom);

        return new DisplayConfiguration
        {
            ConfigurationHash = WidgetGeometry.Fingerprint(screens),
            MonitorCount      = screens.Count,
            TotalWidth        = maxX - minX,
            TotalHeight       = maxY - minY,
        };
    }

    /// <summary>Saved position for this monitor layout, else the last position saved on any layout.</summary>
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
        settings.X = x;
        settings.Y = y;
    }

    /// <summary>
    /// Drops layout keys written by older versions (their keys came from string.GetHashCode(), which
    /// changes every run, so they can never match again and only bloat settings.json).
    /// </summary>
    public static void PruneStalePositions(WidgetSettings settings)
    {
        foreach (var key in settings.DisplayPositions.Keys.ToList())
            if (!key.StartsWith(WidgetGeometry.FingerprintPrefix, StringComparison.Ordinal))
                settings.DisplayPositions.Remove(key);
    }
}
