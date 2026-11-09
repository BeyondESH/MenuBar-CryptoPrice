using MenuBarCryptoPrice.Settings.WinUI.Dialogs;
using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MenuBarCryptoPrice.Settings.WinUI.Pages;

public sealed partial class MarketPage : Page
{
    public MarketPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private void OnMaxVisibleDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo && combo.SelectedIndex >= 0)
        {
            ViewModel.MaxVisibleIndex = combo.SelectedIndex;
        }
    }

    private void OnMarketDropDownClosed(object sender, object e)
    {
        if (sender is ComboBox combo
            && combo.DataContext is ItemRowViewModel row
            && combo.SelectedIndex >= 0)
        {
            row.MarketIndex = combo.SelectedIndex;
        }
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var dialog = new AddInstrumentDialog(ViewModel) { XamlRoot = XamlRoot };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.AddedItem is { } item)
        {
            ViewModel.AddItem(item);
        }
    }

    private async void OnRefreshCatalogClick(object sender, RoutedEventArgs e)
        => await ViewModel.RefreshCatalogsAsync();

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ItemRowViewModel row })
        {
            ViewModel.Move(row, -1);
        }
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ItemRowViewModel row })
        {
            ViewModel.Move(row, 1);
        }
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ItemRowViewModel row })
        {
            ViewModel.Remove(row);
        }
    }
}
