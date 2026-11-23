---
title: Privacy Policy
---

# WinWidgetTimer Privacy Policy

**Effective date:** November 20, 2026
**Publisher:** LanDen Labs

WinWidgetTimer is a free Windows desktop widget with countdown, stopwatch and daily alarm timers.
This policy explains what the app reads, what leaves your computer, and what is stored.

## Summary

- WinWidgetTimer has **no accounts, no ads, no analytics, no telemetry, no tracking and no crash
  reporting**.
- It is **free**. There are no fees, subscriptions or in-app purchases.
- LanDen Labs **does not collect, receive or store any of your data**. There is no WinWidgetTimer
  server.
- The app **makes no internet or network connections**. Your timers and alarms exist only on your
  PC.

## Information the app reads on your PC

To run timers and alarms, WinWidgetTimer reads, locally:

- your PC's clock, to count time and to fire alarms at the time you set,
- the list of `.wav` sound files in the Windows sounds folder (`C:\Windows\Media`), so you can pick
  a completion sound. It only plays the sound you pick (or the standard Windows alert sound if that
  file is missing), when a timer finishes or when you press **Test** in Settings. It never records
  sound and doesn't use the microphone,
- the size and layout of your monitors, to place the widget.

None of this is sent to LanDen Labs or anyone else.

## Network activity

WinWidgetTimer doesn't connect to the internet or your local network, and it doesn't check for
updates. If you installed it from the MSIX package, Windows handles updates when you install a newer
release.

## Actions you choose to take

- The **GitHub** link in the **About** dialog opens the project page in your web browser.

## Information stored on your PC

WinWidgetTimer saves your settings in `%AppData%\WinWidgetTimer\settings.json`:

- each widget's name, position on each monitor layout (identified by the monitors' sizes and
  positions), colors, opacity, font scale (also set by resizing the widget), title-bar and
  wallpaper-layer choices,
- your timers: each timer's name, type, color, duration or alarm day and time, display format,
  chosen sound file name, and whether it flashes when done,
- whether *Auto-start on login* is on.

Timer names are whatever you type. Don't put anything in them you wouldn't want saved in a plain
text file on your PC.

If you turn on *Auto-start on login* in the portable (zip) build, the app adds a `WinWidgetTimer`
entry to your user's Windows startup (Run) registry key. In the MSIX build it uses the Windows
*Startup apps* setting instead.

When the app is installed from the MSIX package, Windows keeps this file inside the app's private
package folder (under `%LocalAppData%\Packages\`) instead.

The app keeps no log files or timer history. This data never leaves your PC. To delete it, uninstall
the app (uninstalling the MSIX removes its package folder), or delete the `%AppData%\WinWidgetTimer`
folder.

## Children

WinWidgetTimer is a general-purpose utility. It is not directed at children and collects no personal
information from anyone.

## License

WinWidgetTimer is open source under the
[Apache License 2.0](https://github.com/landenlabs/win-widget-timer/blob/main/LICENSE). You can read
all of its source code on [GitHub](https://github.com/landenlabs/win-widget-timer) and confirm what
this policy describes.

## Changes to this policy

If the app's behavior changes in a way that affects privacy, this page will be updated and the
effective date above will change. The history of this page is public on
[GitHub](https://github.com/landenlabs/win-widget-timer/commits/main/docs/privacy.md).

## Contact

For questions about this policy, open an issue at
[github.com/landenlabs/win-widget-timer/issues](https://github.com/landenlabs/win-widget-timer/issues).
