
<table border="0">
  <tr>
    <td>
      23-Nov-2026<br>
      Windows<br>
      <a href="https://landenlabs.com/index.html">Home</a>
    </td>
    <td>
      <a href="https://landenlabs.com/index.html">
        <img src="screens/landenlabs.webp" width="300" alt="Logo">
      </a>
    </td>
  </tr>
</table>

# WinWidgetTimer

[![Build and Package](https://github.com/landenlabs/win-widget-timer/actions/workflows/build.yml/badge.svg)](https://github.com/landenlabs/win-widget-timer/actions/workflows/build.yml)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-Apache%202.0-green)

A lightweight Windows desktop timer widget that floats on your wallpaper as a transparent overlay.
Supports three independent timer types — countdown, stopwatch, and daily alarm — each with its own
color, sound, and flash-on-completion settings.

**By [LanDen Labs](https://github.com/landenlabs) (2026)**

---

## Screenshots

**Timer widget on desktop**

![Timer widget](screens/timer1.png)

**Settings dialog**

![Settings dialog](screens/timer-settings.png)

---

## Features

- **Three timer types** — countdown, stopwatch (elapsed), and daily alarm in a single widget
- **Per-timer notifications** — individual sound file and flash-on-completion for each timer
- **Multiple widgets** — add as many independent timer groups as needed via the system tray
- **Transparent overlay** — sits directly on the desktop wallpaper, no taskbar clutter
- **Per-timer colors** — assign a custom color to each timer row
- **Drag to reorder** — reorder timers in the settings list via drag handle
- **Multi-monitor aware** — position saved per monitor layout; snaps to a safe position if the saved location is off-screen
- **Screen-map position picker** — drag a scaled widget marker across a miniature monitor map inside Settings to reposition the widget *(Windows 10 & 11 — see [Windows 10 notes](#windows-10-notes))*
- **Drag to reposition** — click and drag the widget anywhere on the desktop *(Windows 11)*
- **Corner scaling** — drag a bottom corner (any corner when the title bar is hidden) to scale the whole widget; this sets the font scale
- **Background color & opacity** — live-preview color and transparency while Settings is open
- **Font scale** — adjust text size 50–200%
- **Show / hide title bar** — toggle the widget header on or off
- **Wallpaper embed mode** — render the widget at the wallpaper layer (below all windows)
- **Auto-start on login** — optional launch with Windows (a *Startup apps* entry when installed from the MSIX)
- **Dark theme** — Catppuccin Mocha palette throughout
- **Persistent settings** — saved to `%AppData%\WinWidgetTimer\settings.json`

---

## Requirements

- Windows 10 or Windows 11 (x64 or ARM64)
- No .NET install needed for the release builds — the MSIX and zip are self-contained
- Framework-dependent builds from source need the [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

---

## Windows 10 Notes

### Drag limitation

On **Windows 10**, the desktop widget cannot be dragged directly on screen. This is caused by a Windows 10 incompatibility between WPF's `AllowsTransparency` and `WindowStyle="None"` — the combination that transparent widgets require. The drag operation silently fails.

**Windows 11** does not have this limitation; direct drag works normally.

### Workaround — Screen-map position picker

Open **Settings** (hover the widget and click ⚙, or right-click → Settings) and scroll to the **Widget Position** panel at the bottom:

```
Widget Position ─────────────────────────────── X: 120  Y: 200
┌──────────────────────────────────────────────────────────────┐
│  ┌────────────────────────────┐  ┌─────────────────────┐    │
│  │  Primary                   │  │  2560×1440          │    │
│  │        ▓▓▓▓▓               │  └─────────────────────┘    │
│  └────────────────────────────┘                              │
└──────────────────────────────────────────────────────────────┘
  Drag the blue marker to reposition the widget — it moves live.
```

- The canvas shows **all connected monitors** scaled to fit
- The **blue marker** represents the widget at its current position
- Drag the marker to the desired location — **the widget moves live** as you drag
- Click **Save** to keep the new position, or **Cancel** to restore it
- The X / Y coordinates update in real-time as you drag

This approach works on Windows 10 because the Settings dialog is a normal opaque window that does not require transparency.

---

## Installation

See **[how-to-install.md](how-to-install.md)** for full details — every install option, why the MSIX is signed with a self-signed certificate, which Windows security features (SmartScreen, Smart App Control) can still block the app, and how to uninstall.

### Option A — MSIX installer (recommended)

1. From [Releases](https://github.com/landenlabs/win-widget-timer/releases) download `WinWidgetTimer-<version>-x64.msix` (or `-arm64.msix`) and `install-msix.ps1` into the same folder
2. Run it from a normal (non-admin) PowerShell:
   `powershell -ExecutionPolicy Bypass -File .\install-msix.ps1`
   It picks the `.msix` matching your CPU, trusts its self-signed certificate (one UAC prompt), installs, and launches the app. It shows up in the Start menu as **WinWidgetTimer**.

> Windows only installs an MSIX whose signing certificate is trusted, so the self-signed certificate must be added to **Local Machine → Trusted People** first — the script does this for you.
> To remove the app and the certificate later: `powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1`  
> The certificate is shared by all LanDen Labs widgets, so it is kept while any of them is still installed (add `-Force` to remove it anyway).

### Option B — Portable release zip

1. Download `WinWidgetTimer.zip` from [Releases](https://github.com/landenlabs/win-widget-timer/releases)
2. Right-click the zip → **Properties** → tick **Unblock** → **OK**
3. Extract to any folder (e.g. `C:\opt\bin\winwidgets\`) and run `WinWidgetTimer.exe`

> The zip contains a single self-contained `WinWidgetTimer.exe` (assets bundled inside) — no .NET runtime needed. The exe is unsigned, so SmartScreen may warn (**More info → Run anyway**).

### Option C — Build from source

```cmd
git clone https://github.com/landenlabs/win-widget-timer.git
cd win-widget-timer
build.bat C:\opt\bin
```

`build.bat <dest-dir>` publishes a self-contained single-file exe to `<dest-dir>\winwidgets\` and writes a `<dest-dir>\WinWidgetTimer.bat` launcher. With no argument it uses the `BINDIR` environment variable.

---

## Usage

### Widget controls

| Action | Result |
|--------|--------|
| **Click timer row** | Start, pause, or reset that timer |
| **Hover** | Reveals ⚙ Settings and ? About buttons |
| **Drag body** | Repositions the widget *(Windows 11 only — use Settings on Windows 10)* |
| **Drag bottom corner** | Scales the widget (sets the font scale); top corners also work when the title bar is hidden |
| **Right-click** | Opens context menu (Settings / About / Remove Widget / Exit) |
| **Tray icon** | Per-widget Settings / Remove Widget, Add Widget, About, Exit |

### Timer row icons

| Icon | State |
|------|-------|
| ▶ | Running |
| ⏸ | Paused |
| ↺ | Idle / reset |
| ✓ | Countdown complete |
| 🔔 | Alarm fired |

---

## Settings

Open Settings via the hover button or right-click menu.

### Timer list (left panel)

- **Widget Name** — label shown in the title bar and system tray
- **Timers** — all timers for this widget; click to select and edit
- **+ Add / − Delete** — add a new countdown timer or remove the selected one
- **Drag** the grip handle (⠿) to reorder timers

### Timer properties (right panel)

| Field | Description |
|-------|-------------|
| Timer Name | Display label for the row |
| Timer Type | Countdown, Elapsed, or Alarm (see below) |
| Row Color | Color of the dot and name text |
| Duration | HH : MM : SS — for Countdown timers |
| Alarm Schedule | Day of week + HH : MM — for Alarm timers |
| Time Format | Display format string (e.g. `HH:mm:ss` or `ddd HH:mm`) |
| Flash widget | Flashes the widget border red when the timer completes |
| Sound | WAV file from `C:\Windows\Media\` to play on completion |
| ▶ Test | Preview the selected sound immediately |

### Timer types

| Type | Icon | Behavior |
|------|------|----------|
| **Countdown** | ⏱ | Counts down from the configured duration to zero, then fires |
| **Elapsed** | ⏹ | Stopwatch — counts up from zero; click to start / pause / reset |
| **Alarm** | 🔔 | Fires once per day (or on a specific weekday) at the configured time |

### Widget Appearance (bottom panel)

| Setting | Description |
|---------|-------------|
| Background | Background fill color |
| Opacity | Background transparency 0–100% — updates live |
| Font Scale | Text size relative to default 50–200% — updates live (also set by dragging a corner) |
| Embed in wallpaper layer | Anchors the widget behind all desktop icons (requires restart) |
| Show title bar | Toggles the widget name header |
| Auto-start on login | Launch the widget when Windows starts |

### Widget Position (bottom of Settings — Windows 10 workaround)

A miniature map of your monitor layout. Drag the **blue marker** to move the widget anywhere on any screen. The widget repositions live as you drag. Coordinates are shown in the header. Changes are applied on **Save** and reverted on **Cancel**.

Settings are saved to `%APPDATA%\WinWidgetTimer\settings.json`.

---

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Windows (WPF requires a Windows build host)

### Build

```cmd
dotnet build WinWidgetTimer.csproj -c Release
```

### Publish (FDD single-file, win-x64)

```cmd
dotnet publish WinWidgetTimer.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\`

This produces a **single `WinWidgetTimer.exe`** (all managed assemblies bundled) plus the `Assets\` folder. Users need only the .NET 10 Desktop Runtime — no SDK required.

### Build and install via batch script

```cmd
build.bat <dest-dir>
```

Kills any running instance, runs the unit tests, publishes a self-contained, compressed single-file exe (the same build as the release zip), copies it to `<dest-dir>\winwidgets\`, and writes a `<dest-dir>\WinWidgetTimer.bat` launcher. `<dest-dir>` defaults to the `BINDIR` environment variable.

### Build the MSIX locally

```powershell
powershell -ExecutionPolicy Bypass -File .github\scripts\build-msix.ps1 -Arch x64 -Version 6.11.20 -PfxPath cert.pfx -PfxPassword secret
```

The certificate subject must equal the package Publisher (default `CN=LanDen Labs`). The package is written to `dist\`. Requires the Windows 10/11 SDK (`makeappx.exe`, `signtool.exe`). Add `-Store` (and drop `-PfxPath`) to build an unsigned Store package instead; see below.

### Unit tests

```cmd
dotnet run --project tests\WinWidgetTimer.Tests
```

The tests use xUnit v3 and run in-process. `dotnet test` also works (and Visual Studio's Test Explorer), but on machines whose security software inspects localhost traffic it can stall for 90 s with "Failed to negotiate protocol".

Covers the window geometry that has regressed before: resize-corner hit testing (top corners are the drag zone while the title bar is shown), drag, corner resize (anchoring, min/max, DPI scaling, proportional scaling), pulling a widget back from an unplugged monitor, and the stable monitor-layout key used to save positions. The math lives in `Services/WidgetGeometry.cs` (no WPF dependencies) so it can be tested directly; the release workflow and `build.bat` both run these tests and stop on failure.

### Releases and the Microsoft Store

Pushing a `v*` tag runs [.github/workflows/build.yml](.github/workflows/build.yml). It attaches the portable zip, the signed MSIX packages (x64, arm64), the install scripts and (for self-signed builds) `WinWidgetTimer.cer` to a GitHub release. If the repo secrets `MSIX_CERT_BASE64` (base64 `.pfx`) and `MSIX_CERT_PASSWORD` are set, that certificate signs the packages; otherwise a self-signed `CN=LanDen Labs` certificate is generated for the build.

The same run also builds **unsigned** Store packages, `WinWidgetTimer-<ver>-<arch>-store.msix`, and puts them in the build artifact only. Upload them in Partner Center, which signs and hosts them. Their identity comes from the repo Actions variables `MSIX_IDENTITY_NAME`, `MSIX_PUBLISHER` and `MSIX_PUBLISHER_DISPLAY_NAME`, which must match Partner Center > Product identity. If the variables aren't set, the step is skipped. Listing text, URLs and the Store logo are in [store-assets/](store-assets/store-listing.md).

The project site and privacy policy are served by GitHub Pages from `docs/` at https://landenlabs.github.io/win-widget-timer/.

> **TODO:** the app has no `timer.ico` yet, so `WinWidgetTimer.exe` shows the default Windows icon. Create `timer.ico` (e.g. from `screens/icon.png`, 16–256 px) in the project root and add `<ApplicationIcon>timer.ico</ApplicationIcon>` to `WinWidgetTimer.csproj`. Also still needed: `store-assets/tile-300x300.png` (Store logo).

---

## Project Structure

```
WinWidgetTimer/
├── Models/
│   ├── AppSettings.cs          # Top-level settings (list of widgets)
│   ├── DisplayConfiguration.cs # Multi-monitor position tracking
│   └── TimerEntry.cs           # Per-timer data model
├── Services/
│   ├── AutoStartService.cs     # Run on login (Run key, or MSIX StartupTask)
│   ├── DesktopService.cs       # Wallpaper embed / Win32 window helpers
│   ├── DisplayService.cs       # Monitor enumeration, per-layout position save/restore
│   ├── WidgetGeometry.cs       # Pure drag / resize / on-screen math (unit tested)
│   ├── SettingsService.cs      # JSON settings persistence (atomic writes)
│   └── TrayIconService.cs      # System tray icon and menu
├── ViewModels/
│   └── TimerDisplayItem.cs     # Live timer binding per row
├── Windows/
│   ├── AboutWindow.xaml        # About dialog
│   ├── ColorPickerWindow.xaml  # Color picker dialog
│   ├── SettingsWindow.xaml     # Settings dialog (incl. screen-map position picker)
│   └── WidgetWindow.xaml       # Main widget overlay
├── Assets/
│   ├── landenlabs.mp4          # Animated logo (About dialog)
│   └── landenlabs.png          # Static logo fallback
├── .github/
│   ├── workflows/build.yml     # Release build: zip + signed MSIX (x64, arm64)
│   └── scripts/build-msix.ps1  # MSIX packaging and signing (or unsigned Store package)
├── tests/WinWidgetTimer.Tests/ # xUnit tests for WidgetGeometry
├── docs/                       # GitHub Pages site: index.md (home page), privacy.md (privacy policy)
├── store-assets/               # Microsoft Store listing text (and logo, still to be added)
├── screens/icon.png            # Source image for the MSIX logos
├── build.bat                   # Build and install portable exe locally
├── install-msix.ps1            # Trust certificate + install MSIX (end users)
├── uninstall-msix.ps1          # Remove MSIX + its self-signed certificate(s)
└── how-to-install.md           # Install options, certificate and security notes
```

---

## Credits

| Component | Source |
|-----------|--------|
| Timer logic | Custom countdown / stopwatch / alarm engine |
| Desktop embedding | Win32 `WorkerW` technique |

---

## Privacy

No accounts, ads, analytics, telemetry or tracking. The [privacy policy](https://landenlabs.github.io/win-widget-timer/privacy.html) ([source](docs/privacy.md)) lists exactly what the app reads, stores and (if anything) sends.

---

## License

Apache 2.0 © [LanDen Labs](https://github.com/landenlabs) 2026
