using System.Windows.Media;
using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class StripItemViewModel : ObservableObject
{
    private readonly TickerItemConfig _config;

    private string _priceText = "—";
    private string _changeText = "—";
    private Brush _priceBrush = Palette.Stale;
    private Brush _changeBrush = Palette.Stale;
    private string _tooltip = string.Empty;
    private bool _isStale = true;
    private double _labelWidth;
    private double _priceWidth;
    private double _changeWidth;
    private int _priceIntegerDigits = -1;
    private DateTimeOffset _lastUpdated = DateTimeOffset.MinValue;
    private double _fontSizeDip = 12;
    private bool _isTwoLine;
    private double _itemHeight = 22;

    public StripItemViewModel(TickerItemConfig config)
    {
        _config = config;
    }

    public TickerItemConfig Config => _config;

    public InstrumentKey Key => _config.Key;

    public string Label => _config.BaseCoin;

    public string MarketBadge => _config.Market.ToShortName();

    public string QuoteCoin => _config.QuoteCoin;

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

    public Brush PriceBrush
    {
        get => _priceBrush;
        private set => SetProperty(ref _priceBrush, value);
    }

    public Brush ChangeBrush
    {
        get => _changeBrush;
        private set => SetProperty(ref _changeBrush, value);
    }

    public string Tooltip
    {
        get => _tooltip;
        private set => SetProperty(ref _tooltip, value);
    }

    public bool IsStale
    {
        get => _isStale;
        private set => SetProperty(ref _isStale, value);
    }

    public double LabelWidth
    {
        get => _labelWidth;
        set
        {
            if (SetProperty(ref _labelWidth, value))
            {
                OnPropertyChanged(nameof(TotalWidth));
            }
        }
    }

    public double PriceWidth
    {
        get => _priceWidth;
        set
        {
            if (SetProperty(ref _priceWidth, value))
            {
                OnPropertyChanged(nameof(TotalWidth));
            }
        }
    }

    public double ChangeWidth
    {
        get => _changeWidth;
        set
        {
            if (SetProperty(ref _changeWidth, value))
            {
                OnPropertyChanged(nameof(TotalWidth));
            }
        }
    }

    public double TotalWidth => LabelWidth + 4 + PriceWidth + 4 + ChangeWidth + 10;

    public int PriceIntegerDigits => _priceIntegerDigits;

    public double FontSizeDip
    {
        get => _fontSizeDip;
        set
        {
            if (SetProperty(ref _fontSizeDip, value))
            {
                OnPropertyChanged(nameof(SmallFontSizeDip));
            }
        }
    }

    public double SmallFontSizeDip => Math.Max(10, FontSizeDip - 1);

    public bool IsTwoLine
    {
        get => _isTwoLine;
        set
        {
            if (SetProperty(ref _isTwoLine, value))
            {
                OnPropertyChanged(nameof(SingleLineVisibility));
                OnPropertyChanged(nameof(TwoLineVisibility));
            }
        }
    }

    public Visibility SingleLineVisibility => IsTwoLine ? Visibility.Collapsed : Visibility.Visible;

    public Visibility TwoLineVisibility => IsTwoLine ? Visibility.Visible : Visibility.Collapsed;

    public double ItemHeight
    {
        get => _itemHeight;
        set => SetProperty(ref _itemHeight, value);
    }

    public DateTimeOffset LastUpdated => _lastUpdated;

    /// <summary>
    /// Applies the latest quote and returns true when the price column width changed because the
    /// integer digit count changed (which requires a re-layout).
    /// </summary>
    public bool Update(TickerQuote? quote, ColorScheme scheme, DateTimeOffset now)
    {
        if (quote is null)
        {
            PriceText = "—";
            ChangeText = "—";
            PriceBrush = Palette.Stale;
            ChangeBrush = Palette.Stale;
            IsStale = true;
            _lastUpdated = DateTimeOffset.MinValue;
            Tooltip = $"{Label}/{QuoteCoin} · {_config.Market.ToDisplayName()}{Environment.NewLine}等待行情…";
            return false;
        }

        var stale = quote.IsStale(now);
        var direction = PriceFormatter.GetDirection(quote.Change24h);
        var brush = stale ? Palette.Stale : Palette.ForDirection(direction, scheme);

        PriceText = PriceFormatter.FormatPrice(quote.Price, _config.Precision);
        ChangeText = PriceFormatter.FormatChangeWithArrow(quote.Change24h);
        PriceBrush = brush;
        ChangeBrush = brush;
        IsStale = stale;
        _lastUpdated = quote.SourceTime;

        var integerDigits = CountIntegerDigits(quote.Price);
        var widthChanged = integerDigits != _priceIntegerDigits;
        _priceIntegerDigits = integerDigits;

        var tooltip = new StringBuilder();
        tooltip.Append(Label).Append('/').Append(QuoteCoin).Append(" · ").Append(_config.Market.ToDisplayName());
        tooltip.Append(Environment.NewLine)
            .Append("最新  ").Append(PriceText)
            .Append("    ").Append(ChangeText);
        tooltip.Append(Environment.NewLine).Append("24h 高  ").Append(PriceFormatter.FormatPrice(quote.High24h, _config.Precision));
        tooltip.Append(Environment.NewLine).Append("24h 低  ").Append(PriceFormatter.FormatPrice(quote.Low24h, _config.Precision));

        if (quote.QuoteVolume24h is decimal volume)
        {
            tooltip.Append(Environment.NewLine).Append("24h 量  ").Append(PriceFormatter.FormatVolume(volume)).Append(" USDT");
        }

        if (quote.BidPrice is decimal bid && quote.AskPrice is decimal ask)
        {
            tooltip.Append(Environment.NewLine)
                .Append("买一 / 卖一  ")
                .Append(PriceFormatter.FormatPrice(bid, _config.Precision))
                .Append(" / ")
                .Append(PriceFormatter.FormatPrice(ask, _config.Precision));
        }

        if (_config.Market == Market.UsdFutures)
        {
            tooltip.Append(Environment.NewLine).Append("资金费率 ").Append(PriceFormatter.FormatFundingRate(quote.FundingRate));
            if (quote.MarkPrice is not null)
            {
                tooltip.Append(Environment.NewLine)
                    .Append("标记价格 ")
                    .Append(PriceFormatter.FormatPrice(quote.MarkPrice.Value, _config.Precision));
            }
        }

        tooltip.Append(Environment.NewLine)
            .Append(stale ? "最后更新 " : "更新 ")
            .Append(PriceFormatter.FormatTimestamp(quote.SourceTime));

        if (stale)
        {
            tooltip.Append(Environment.NewLine).Append("数据可能已过期");
        }

        Tooltip = tooltip.ToString();
        return widthChanged;
    }

    private static int CountIntegerDigits(decimal price)
    {
        if (price < 0m)
        {
            price = -price;
        }

        if (price < 1m)
        {
            return 1;
        }

        var digits = 0;
        while (price >= 1m && digits < 12)
        {
            price /= 10m;
            digits++;
        }

        return Math.Clamp(digits, 1, 9);
    }
}
