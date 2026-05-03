// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Windows;
using WinWidgetTimer.Models;
using WinWidgetTimer.Services;
using WinWidgetTimer.Windows;

namespace WinWidgetTimer;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private TrayIconService? _trayIcon;
    private readonly List<WidgetWindow> _widgetWindows = [];

    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, "WinWidgetTimer_UniqueInstance_v1", out bool isNew);
        if (!isNew)
        {
            System.Windows.MessageBox.Show("WinWidgetTimer is already running.",
                "WinWidgetTimer", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        Settings = SettingsService.Load();

        if (Settings.Widgets.Count == 0)
            Settings.Widgets.Add(new WidgetSettings());

        foreach (var widget in Settings.Widgets)
            CreateAndShowWidget(widget);

        _trayIcon = new TrayIconService(
            onAddWidget:      AddWidget,
            getWidgets:       () => Settings.Widgets,
            onWidgetSettings: id => _widgetWindows.FirstOrDefault(w => w.WidgetId == id)?.OpenSettings(),
            onWidgetRemove:   RemoveWidget,
            onAbout:          OpenAbout,
            onExit:           Shutdown
        );
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private void CreateAndShowWidget(WidgetSettings settings)
    {
        var window = new WidgetWindow(settings);
        _widgetWindows.Add(window);
        window.Show();
    }

    public void AddWidget()
    {
        var newWidget = new WidgetSettings
        {
            X = 100 + (_widgetWindows.Count * 20),
            Y = 100 + (_widgetWindows.Count * 20)
        };
        Settings.Widgets.Add(newWidget);
        SettingsService.Save(Settings);
        CreateAndShowWidget(newWidget);
        _trayIcon?.RebuildMenu();
    }

    public void RemoveWidget(string widgetId)
    {
        var window = _widgetWindows.FirstOrDefault(w => w.WidgetId == widgetId);
        if (window != null)
        {
            _widgetWindows.Remove(window);
            window.Close();
        }

        var settings = Settings.Widgets.FirstOrDefault(w => w.Id == widgetId);
        if (settings != null)
        {
            Settings.Widgets.Remove(settings);
            SettingsService.Save(Settings);
        }
        _trayIcon?.RebuildMenu();
    }

    private void OpenAbout()
    {
        Dispatcher.Invoke(() => new AboutWindow().ShowDialog());
    }
}
