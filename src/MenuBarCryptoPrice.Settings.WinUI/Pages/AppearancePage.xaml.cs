using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MenuBarCryptoPrice.Settings.WinUI.Pages;

public sealed partial class AppearancePage : Page
{
    public AppearancePage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnLayoutDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.LayoutIndex = combo.SelectedIndex;
        }
    }

    private void OnFontSizeDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.FontSizeIndex = combo.SelectedIndex;
        }
    }

    private void OnColorSchemeDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.ColorSchemeIndex = combo.SelectedIndex;
        }
    }

    private void OnPositionDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.PositionIndex = combo.SelectedIndex;
        }
    }

    private void OnAnchorDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.AnchorIndex = combo.SelectedIndex;
        }
    }

    private async void OnRestoreClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "恢复默认设置？",
            Content = "当前币种、数量与外观设置将被默认配置替换。",
            PrimaryButtonText = "恢复",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            ViewModel.RestoreDefaults();
        }
    }
}
