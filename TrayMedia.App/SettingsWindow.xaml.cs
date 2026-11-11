using System.Windows;
using TrayMedia.Services;

namespace TrayMedia;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        AutostartCheck.IsChecked = settings.Autostart || AutostartService.IsEnabled();
        PinToTrayCheck.IsChecked = settings.PinToTray;
    }

    public bool Saved { get; private set; }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _settings.Autostart = AutostartCheck.IsChecked == true;
        _settings.PinToTray = PinToTrayCheck.IsChecked == true;
        _settings.Save();

        AutostartService.SetEnabled(_settings.Autostart);
        Saved = true;
        DialogResult = true;
        Close();
    }
}
