// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTimer.Models;

public class DisplayConfiguration
{
    public string ConfigurationHash { get; set; } = string.Empty;
    public int MonitorCount { get; set; }
    public int TotalWidth { get; set; }
    public int TotalHeight { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime LastUsed { get; set; }

    public override bool Equals(object? obj)
        => obj is DisplayConfiguration c && c.ConfigurationHash == ConfigurationHash;

    public override int GetHashCode() => ConfigurationHash.GetHashCode();
    public override string ToString() => $"{Description} [{ConfigurationHash}]";
}

public class DisplayPosition
{
    public string ConfigurationHash { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime LastSet { get; set; }
}
