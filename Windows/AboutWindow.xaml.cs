// Copyright (c) 2026 LanDen Labs - Dennis Lang
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace WinWidgetTimer.Windows;

public partial class AboutWindow : Window
{
    private bool _closing;

    public AboutWindow()
    {
        InitializeComponent();
        Topmost = true;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
        VersionText.Text = $"v{version}  ·  Timer / Countdown / Alarm widget";

        var mp4 = Path.Combine(AppContext.BaseDirectory, "Assets", "landenlabs.mp4");
        if (File.Exists(mp4))
        {
            LogoPlayer.MediaEnded += (_, _) =>
            {
                if (_closing) return;
                try { LogoPlayer.Position = TimeSpan.Zero; LogoPlayer.Play(); }
                catch { }
            };
            LogoPlayer.Visibility = Visibility.Visible;
            // Defer source + play until the window is rendered; the MediaElement must be
            // in the visual tree before the WPF media pipeline will accept commands.
            Loaded += (_, _) =>
            {
                if (_closing) return;
                try { LogoPlayer.Source = new Uri(mp4); LogoPlayer.Play(); }
                catch { }
            };
        }
        else
        {
            var png = Path.Combine(AppContext.BaseDirectory, "Assets", "landenlabs.png");
            if (File.Exists(png))
            {
                LogoFallback.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(png));
                LogoFallback.Visibility = Visibility.Visible;
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _closing = true;
        try { LogoPlayer.Stop(); } catch { }
        LogoPlayer.Source = null;
        base.OnClosing(e);
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
