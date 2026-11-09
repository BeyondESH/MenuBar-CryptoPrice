using CommunityToolkit.Mvvm.ComponentModel;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.MarketData;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MenuBarCryptoPrice.Settings.WinUI.ViewModels;

public sealed class ItemRowViewModel : ObservableObject
{
    private static readonly SolidColorBrush UpBrush = CreateBrush(0x4E, 0xD8, 0xA0);
    private static readonly SolidColorBrush DownBrush = CreateBrush(0xFF, 0x7B, 0x72);
    private static readonly SolidColorBrush StaleBrush = CreateBrush(0xA1, 0xA7, 0xAD);

    private readonly Action<ItemRowViewModel, Market> _marketChanged;

    private int _index;
    private bool _canMoveUp;
    private bool _canMoveDown;
    private bool _isBeyondLimit;
    private string _priceText = "—";
    private string _changeText = "—";
    private Brush _changeBrush = StaleBrush;
    private string _statusText = string.Empty;
    private Visibility _rangeVisibility = Visibility.Collapsed;
    private double _rangeDotLeft;
    private string _rangeTooltip = string.Empty;
    private TickerQuote? _lastQuote;
    private ColorScheme _lastScheme = ColorScheme.GreenUp;

    public ItemRowViewModel(int index, TickerItemConfig item, Action<ItemRowViewModel, Market> marketChanged)
    {
        _index = index;
        Item = item;
        _marketChanged = marketChanged;
    }

    public TickerItemConfig Item { get; }

    public string DisplayName => $"{Item.BaseCoin} / {Item.QuoteCoin}";

    public string MarketName => Item.Market.ToDisplayName();

    public string DescriptionText
        => $"{MarketName} · 精度 {Item.Precision}"
            + (string.IsNullOrEmpty(StatusText) ? string.Empty : $" · {StatusText}");

    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    public bool CanMoveUp
    {
        get => _canMoveUp;
        set => SetProperty(ref _canMoveUp, value);
    }

    public bool CanMoveDown
    {
        get => _canMoveDown;
        set => SetProperty(ref _canMoveDown, value);
    }

    public bool IsBeyondLimit
    {
        get => _isBeyondLimit;
        set
        {
            if (SetProperty(ref _isBeyondLimit, value))
            {
                OnPropertyChanged(nameof(RowOpacity));
                OnPropertyChanged(nameof(LimitVisibility));
            }
        }
    }

    public double RowOpacity => IsBeyondLimit ? 0.5 : 1.0;

    public Visibility LimitVisibility => IsBeyondLimit ? Visibility.Visible : Visibility.Collapsed;

    public int MarketIndex
    {
        get => (int)Item.Market;
        set
        {
            if (value < 0 || value > 1 || value == (int)Item.Market)
            {
                return;
            }

            _marketChanged(this, (Market)value);
            OnPropertyChanged();
        }
    }

    public string PriceText
    {
        get => _priceText;
        private set => SetProperty(ref _priceText, value);
    }

    public string ChangeText
    {
        get => _changeText;
        private set => SetProperty(ref _changeText, value);
    }

    public Brush ChangeBrush
    {
        get => _changeBrush;
        private set => SetProperty(ref _changeBrush, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (SetProperty(ref _statusText, value))
            {
                OnPropertyChanged(nameof(DescriptionText));
            }
        }
    }

    public Visibility RangeVisibility
    {
        get => _rangeVisibility;
        private set => SetProperty(ref _rangeVisibility, value);
    }

    public double RangeDotLeft
    {
        get => _rangeDotLeft;
        private set => SetProperty(ref _rangeDotLeft, value);
    }

    public string RangeTooltip
    {
        get => _rangeTooltip;
        private set => SetProperty(ref _rangeTooltip, value);
    }

    public void ApplyMarket(Market market, int precision)
    {
        Item.Market = market;
        Item.Precision = precision;
        OnPropertyChanged(nameof(MarketIndex));
        OnPropertyChanged(nameof(MarketName));
        OnPropertyChanged(nameof(DescriptionText));
        ApplyQuote(_lastQuote, _lastScheme);
    }

    public void ApplyQuote(TickerQuote? quote, ColorScheme scheme)
    {
        _lastQuote = quote;
        _lastScheme = scheme;

        if (quote is null)
        {
            PriceText = "—";
            ChangeText = "—";
            ChangeBrush = StaleBrush;
            StatusText = "无行情";
            RangeVisibility = Visibility.Collapsed;
            RangeTooltip = string.Empty;
            return;
        }

        var stale = quote.IsStale(DateTimeOffset.UtcNow);
        PriceText = PriceFormatter.FormatPrice(quote.Price, Item.Precision);
        ChangeText = PriceFormatter.FormatChangeWithArrow(quote.Change24h);
        ChangeBrush = ResolveBrush(PriceFormatter.GetDirection(quote.Change24h), scheme);
        StatusText = stale ? "数据延迟" : string.Empty;

        if (quote.High24h > quote.Low24h)
        {
            var span = quote.High24h - quote.Low24h;
            var ratio = (quote.Price - quote.Low24h) / span;
            ratio = ratio < 0m ? 0m : ratio > 1m ? 1m : ratio;
            RangeDotLeft = Math.Round((double)ratio * 52, 1);
            RangeVisibility = Visibility.Visible;
            RangeTooltip = $"24h 区间 {PriceFormatter.FormatPrice(quote.Low24h, Item.Precision)} — "
                + PriceFormatter.FormatPrice(quote.High24h, Item.Precision);
        }
        else
        {
            RangeVisibility = Visibility.Collapsed;
            RangeTooltip = string.Empty;
        }
    }

    public void ReapplyColorScheme(ColorScheme scheme) => ApplyQuote(_lastQuote, scheme);

    private static Brush ResolveBrush(ChangeDirection direction, ColorScheme scheme)
        => direction switch
        {
            ChangeDirection.Up => scheme == ColorScheme.GreenUp ? UpBrush : DownBrush,
            ChangeDirection.Down => scheme == ColorScheme.GreenUp ? DownBrush : UpBrush,
            _ => StaleBrush
        };

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue)
        => new(Color.FromArgb(0xFF, red, green, blue));
}
