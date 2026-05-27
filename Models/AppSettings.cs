// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTimer.Models;

public class AppSettings
{
    public List<WidgetSettings> Widgets { get; set; } = [];
    public bool AutoStart { get; set; } = false;
}

public class WidgetSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Timers";

    // Fallback position (also updated on every save for backward compatibility)
    public int X { get; set; } = 100;
    public int Y { get; set; } = 100;

    // Appearance
    public string BackgroundColor { get; set; } = "#1E1E2E";
    public double BackgroundOpacity { get; set; } = 0.85;
    public int FontScalePercent { get; set; } = 100;
    public bool EmbedInWallpaper { get; set; } = true;
    public bool ShowTitleBar { get; set; } = true;

    // Timer list for this widget
    public List<TimerEntry> Timers { get; set; } = [];

    // Per-display-config positions
    public Dictionary<string, DisplayPosition> DisplayPositions { get; set; } = [];
    public string LastDisplayConfigurationHash { get; set; } = string.Empty;
}
