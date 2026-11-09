using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MenuBarCryptoPrice.Settings.WinUI.Pages;

public sealed partial class NetworkPage : Page
{
    public NetworkPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnProxyModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.ProxyModeIndex = combo.SelectedIndex;
        }
    }

    private void OnProxyUrlLostFocus(object sender, RoutedEventArgs e)
        => ViewModel.SetProxyUrl(ProxyUrlBox.Text);

    private void OnRestBaseUrlLostFocus(object sender, RoutedEventArgs e)
        => ViewModel.SetRestBaseUrl(RestBaseUrlBox.Text);

    private void OnWsUrlLostFocus(object sender, RoutedEventArgs e)
        => ViewModel.SetWsUrl(WsUrlBox.Text);

    private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
    {
        var valid = ViewModel.SetProxyUrl(ProxyUrlBox.Text);
        valid &= ViewModel.SetRestBaseUrl(RestBaseUrlBox.Text);
        valid &= ViewModel.SetWsUrl(WsUrlBox.Text);
        if (valid)
        {
            await ViewModel.TestConnectionAsync();
        }
    }

    private void OnRestoreNetworkClick(object sender, RoutedEventArgs e)
        => ViewModel.RestoreNetworkDefaults();
}
