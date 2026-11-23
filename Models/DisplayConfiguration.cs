// Copyright (c) 2026 LanDen Labs - Dennis Lang
namespace WinWidgetTimer.Models;

/// <summary>The current monitor layout, identified by a stable fingerprint.</summary>
public class DisplayConfiguration
{
    /// <summary>Stable layout key from <see cref="Services.WidgetGeometry.Fingerprint"/>.</summary>
    public string ConfigurationHash { get; set; } = string.Empty;

    public int MonitorCount { get; set; }

    /// <summary>Virtual-screen size (all monitors combined), in pixels.</summary>
    public int TotalWidth { get; set; }
    public int TotalHeight { get; set; }

    public override bool Equals(object? obj)
        => obj is DisplayConfiguration c && c.ConfigurationHash == ConfigurationHash;

    public override int GetHashCode() => ConfigurationHash.GetHashCode();
    public override string ToString() => ConfigurationHash;
}

/// <summary>Widget top-left (screen pixels) for one monitor layout.</summary>
public class DisplayPosition
{
    public string ConfigurationHash { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime LastSet { get; set; }
}
