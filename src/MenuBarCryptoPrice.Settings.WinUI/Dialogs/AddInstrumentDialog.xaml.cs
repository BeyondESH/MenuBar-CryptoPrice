using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;
using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MenuBarCryptoPrice.Settings.WinUI.Dialogs;

public sealed partial class AddInstrumentDialog : ContentDialog
{
    private readonly SettingsViewModel _viewModel;
    private readonly List<CatalogEntry> _all = new();
    private readonly List<CatalogEntry> _results = new();
    private bool _opened;
    private bool _loading;

    public AddInstrumentDialog(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        Opened += OnOpened;
    }

    public TickerItemConfig? AddedItem { get; private set; }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        Opened -= OnOpened;
        _opened = true;
        await LoadAsync();
    }

    private async void OnMarketChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_opened)
        {
            return;
        }

        await LoadAsync();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void OnFilterToggled(object sender, RoutedEventArgs e) => ApplyFilter();

    private async Task LoadAsync()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        LoadingRing.IsActive = true;
        StatusText.Text = "正在加载币种目录…";

        try
        {
            var market = MarketBox.SelectedIndex == 1 ? Market.UsdFutures : Market.Spot;
            var entries = await _viewModel.GetCatalogAsync(market, false);
            _all.Clear();
            _all.AddRange(entries);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _all.Clear();
            _results.Clear();
            ResultsList.ItemsSource = null;
            StatusText.Text = "目录加载失败：" + ex.Message;
        }
        finally
        {
            _loading = false;
            LoadingRing.IsActive = false;
        }
    }

    private void ApplyFilter()
    {
        if (!_opened)
        {
            return;
        }

        IEnumerable<CatalogEntry> source = _all;
        if (!AllQuotesToggle.IsOn)
        {
            source = source.Where(entry => entry.IsUsdtQuoted);
        }

        var query = (SearchBox.Text ?? string.Empty).Trim();
        if (query.Length > 0)
        {
            source = source.Where(entry =>
                entry.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase)
                || entry.BaseCoin.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        _results.Clear();
        _results.AddRange(source.Take(20));
        ResultsList.ItemsSource = null;
        ResultsList.ItemsSource = _results;
        ResultsList.SelectedIndex = _results.Count > 0 ? 0 : -1;
        StatusText.Text = _all.Count == 0
            ? "没有可用的交易对"
            : $"{_results.Count} 个匹配（共 {_all.Count} 个交易对）";
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (ResultsList.SelectedItem is not CatalogEntry entry)
        {
            args.Cancel = true;
            StatusText.Text = "请选择一个币种";
            return;
        }

        if (!entry.IsOnline)
        {
            args.Cancel = true;
            StatusText.Text = "该币种当前不可交易";
            return;
        }

        if (_viewModel.Contains(entry))
        {
            args.Cancel = true;
            StatusText.Text = "该币种已在列表中";
            return;
        }

        AddedItem = new TickerItemConfig
        {
            Market = entry.Market,
            Symbol = entry.Symbol,
            Precision = entry.Precision
        };
    }
}
