
<table border="0">
  <tr>
    <td>
      3-Apr-2026<br>
      Windows<br>
      <a href="https://landenlabs.com/android/index.html">WebSite</a>
    </td>
    <td>
      <a href="https://landenlabs.com/android/index.html">
        <img src="screens/landenlabs.webp" width="300" alt="Logo">
      </a>
    </td>
  </tr>
</table>

# WinWidgetTimer
[![Build and Package](https://github.com/landenlabs/win-widget-time/actions/workflows/build.yml/badge.svg)](https://github.com/landenlabs/win-widget-time/actions/workflows/build.yml)
![Platform](https://img.shields.io/badge/platform-Windows%2011-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-Apache-green)

A lightweight Windows desktop timer widget that floats on your wallpaper as a transparent overlay.
Supports three independent timer types — countdown, stopwatch, and daily alarm — each with its own
color, sound, and flash-on-completion settings.

**By [LanDen Labs](https://github.com/landenlabs)**

## Widget

The widget displays all configured timers in a compact, always-visible list.
Click any row to start, pause, or reset that timer. Hover to reveal the settings and about buttons.
Drag anywhere on the widget to reposition it.

![Timer widget](screens/timer1.png)

| Column | Meaning |
|--------|---------|
| Colored dot | Per-timer color indicator |
| Name | Timer label |
| Time | Current countdown / elapsed / alarm time |
| Icon | ▶ running · ⏸ paused · ↺ idle · ✓ done · 🔔 alarm |

**Right-click** the widget for Settings, About, Add/Remove widget, and Exit.

---

## Settings

![Settings dialog](screens/timer-settings.png)

### Timer list (left panel)

- **Widget Name** — label shown in the title bar and system tray
- **Timers** — all timers for this widget; click to select and edit
- **+ Add / − Delete** — add a new countdown timer or remove the selected one

### Timer properties (right panel)

| Field | Description |
|-------|-------------|
| Timer Name | Display label for the row |
| Timer Type | Countdown, Elapsed, or Alarm (see below) |
| Row Color | Color of the dot and name text |
| Duration | HH : MM : SS — for Countdown timers |
| Alarm Time | HH : MM — for Alarm timers |
| Flash widget | Flashes the widget border red when the timer completes |
| Sound | WAV file from `C:\Windows\Media\` to play on completion; includes `(None)` and `(System Alert)` |
| ▶ Test | Preview the selected sound immediately |

### Timer types

| Type | Icon | Behavior |
|------|------|----------|
| **Countdown** | ⏱ | Counts down from the configured duration to zero, then fires |
| **Elapsed** | ⏹ | Stopwatch — counts up from zero; click to start / pause / reset |
| **Alarm** | 🔔 | Fires once per day at the configured time of day |

### Widget Appearance (bottom panel)

| Control | Description |
|---------|-------------|
| Background | Background fill color |
| Opacity | Background transparency (0 – 100 %) |
| Font Scale | Text size relative to the default (50 – 200 %) |
| Embed in wallpaper layer | Anchors the widget behind all desktop icons (requires restart) |
| Show title bar | Toggles the widget name header |

---

## Features

- **Three timer types** — countdown, stopwatch, and daily alarm in a single widget
- **Per-timer notifications** — individual sound file and flash-on-end setting for each timer
- **Multiple widgets** — add as many independent timer groups as needed via the system tray
- **Multi-monitor aware** — remembers position per display configuration
- **Wallpaper layer** — embeds behind desktop icons using the Windows `WorkerW` technique
- **Dark theme** — Catppuccin Mocha palette throughout
- **Persistent settings** — saved to `%AppData%\WinWidgetTimer\settings.json`

---

## Usage

| Action | How |
|--------|-----|
| Start / pause a timer | Click the timer row |
| Reset a paused or done timer | Click the timer row again |
| Move the widget | Drag anywhere on the widget background |
| Open settings | Hover → click ⚙, or right-click → Settings |
| Add a widget | System tray → Add Widget |
| Remove a widget | Right-click → Remove Widget |
| Exit | Right-click → Exit |

---

## Installation

```bat
install.bat
```

Publishes a framework-dependent build and copies it to `C:\opt\bin\winwidgets\`.
A launcher script `WinWidgetTimer.bat` is created at `C:\opt\bin\`.

**Requirements:** .NET 10 runtime (Windows)

---

## Build

```bat
dotnet build -c Release
```

```bat
dotnet publish WinWidgetTimer.csproj -c Release --self-contained false -o publish
```

The GitHub Actions workflow (`.github/workflows/build.yml`) builds and packages on every push to
`main` and creates a GitHub Release with `WinWidgetTimer.zip` on every `v*` tag.

---

## License

Copyright © 2026 [LanDen Labs — Dennis Lang](https://landenlabs.com)

Licensed under the [Apache License 2.0](LICENSE.txt).