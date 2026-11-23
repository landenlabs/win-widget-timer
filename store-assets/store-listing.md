# Microsoft Store submission: WinWidgetTimer

Copy-paste material for Partner Center (https://partner.microsoft.com/dashboard). Fill in the
sections in the order Partner Center shows them.

## Product identity (Product management > Product identity)

Copy these three values into the repo Actions **Variables** (Settings > Secrets and variables >
Actions > Variables) so the release workflow builds a matching Store package:

| Partner Center field | Repo variable |
| --- | --- |
| Package/Identity/Name | `MSIX_IDENTITY_NAME` |
| Package/Identity/Publisher (`CN=...`) | `MSIX_PUBLISHER` |
| Package/Properties/PublisherDisplayName | `MSIX_PUBLISHER_DISPLAY_NAME` |

Then push a `v*` tag. The workflow's build artifact contains `dist-store/WinWidgetTimer-<ver>-x64-store.msix`
and `-arm64-store.msix`. They are unsigned, which is what the Store expects. Upload both under
**Packages**.

## Properties

| Field | Value |
| --- | --- |
| Category | Utilities + tools (subcategory: none) |
| Privacy policy URL | https://landenlabs.github.io/win-widget-timer/privacy.html |
| Website | https://landenlabs.github.io/win-widget-timer/ |
| Support contact | https://github.com/landenlabs/win-widget-timer/issues |
| Collects personal information? | No |
| Product declarations | No, it does not access, collect or transmit personal information |
| System requirements | Windows 10 version 1809 (build 17763) or later; x64 or ARM64 |

## Age ratings (IARC questionnaire)

Category: **Utility / productivity**. Answer **No** to everything: no violence, no user
interaction or chat, no sharing of location or personal info, no purchases, no unrestricted web
access. Expected rating: 3+ / Everyone.

## Pricing and availability

Free. No in-app purchases. All markets.

## Restricted capability justification

Partner Center asks why the package declares `runFullTrust`. Suggested text:

> WinWidgetTimer is a WPF desktop app packaged with the Desktop Bridge. It needs runFullTrust to run
> as a normal Win32 process: it draws a transparent, always-on-desktop widget window (optionally
> embedded in the wallpaper layer with Win32 window APIs), shows a system tray icon, and plays the
> Windows .wav sounds in C:\Windows\Media when a timer finishes. It makes no internet connections
> and collects no user data.

## Store listing (English, United States)

**Product name:** WinWidgetTimer

**Short description** (up to 270 characters):

> A lightweight, transparent desktop widget with countdown, stopwatch and daily alarm timers. Each
> timer has its own color, sound and flash alert. No ads, no account, no telemetry.

**Description:**

> WinWidgetTimer puts a small, transparent timer panel right on your desktop, so your countdowns,
> stopwatches and alarms are always in view without a window in the way.
>
> Mix three kinds of timer in one widget: a countdown that alerts you when it reaches zero, a
> stopwatch that counts up, and an alarm that fires every day, or on a chosen weekday, at the time
> you set. Click a row to start, pause or reset it. Each timer can play its own Windows sound and
> flash the widget when it finishes.
>
> Make it yours: give each timer a name and color, pick the background color, opacity and font
> scale, hide the title bar, and drag a corner to scale the widget. Run several widgets, place them
> on any monitor, and WinWidgetTimer remembers where each one goes for each monitor layout. It can
> start automatically when you sign in.
>
> Private by design: WinWidgetTimer has no account, ads, analytics or telemetry, and it never
> connects to the internet. Your timers stay on your PC.
>
> Free and open source under the Apache 2.0 license.

**What's new in this version:** (fill in per release)

**Product features** (one per line, up to 20):

```
Countdown, stopwatch and daily alarm timers in one widget
Per-timer color, sound and flash-on-finish alert
Alarms every day or on a chosen weekday
Transparent widget that sits on your wallpaper
Custom background, opacity and font scale; drag a corner to scale
Multiple widgets, multi-monitor aware
Optional start at sign-in
No ads, no account, no telemetry, no internet connections
```

**Search terms** (up to 7):

```
timer
countdown timer
stopwatch
alarm clock
desktop widget
kitchen timer
desktop timer
```

**Copyright and trademark info:** © 2026 LanDen Labs

**Additional license terms:** Apache License 2.0,
https://github.com/landenlabs/win-widget-timer/blob/main/LICENSE

## Images

| Partner Center slot | File |
| --- | --- |
| Store logo, 1:1 (300 x 300) | `store-assets/tile-300x300.png` (**TODO: not created yet**) |
| Screenshots (Desktop; at least 1, recommended 1366 x 768 or larger) | See note below |

**TODO:** `store-assets/tile-300x300.png` still needs to be created: a 300 x 300 PNG of the timer
logo (for example `screens/icon.png` scaled onto a 300 x 300 canvas).

The Store requires desktop screenshots of at least 1366 x 768. The images in `screens/` are cropped
to single windows and are too small. Capture full-screen shots with the widget on the desktop, for
example one with the widget on a wallpaper and one with Settings open.
