using MenuBarCryptoPrice.Core;
using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;

namespace MenuBarCryptoPrice.Taskbar;

public sealed class TickerStripViewModel : ObservableObject
{
    private readonly TickerStore _store;
    private readonly ITextMeasurer _measurer;
    private readonly ObservableCollection<StripItemViewModel> _items = new();
    private readonly List<StripItemViewModel> _candidates = new();

    private AppConfig _config;
    private double _desiredWidth = 120;
    private double _height = 26;
    private int _overflowCount;
    private string _overflowText = string.Empty;
    private string _overflowTooltip = string.Empty;
    private bool _isEmpty;
    private bool _widthChanged;
    private double _availableWidthDip = double.PositiveInfinity;

    public TickerStripViewModel(AppConfig config, TickerStore store, ITextMeasurer? measurer = null)
    {
        _config = config;
        _store = store;
        _measurer = measurer ?? new WpfTextMeasurer();
        Rebuild(double.PositiveInfinity);
    }

    public ObservableCollection<StripItemViewModel> Items => _items;

    public double DesiredWidth
    {
        get => _desiredWidth;
        private set => SetProperty(ref _desiredWidth, value);
    }

    public double Height
    {
        get => _height;
        private set => SetProperty(ref _height, value);
    }

    public int OverflowCount
    {
        get => _overflowCount;
        private set
        {
            if (SetProperty(ref _overflowCount, value))
            {
                OnPropertyChanged(nameof(OverflowVisibility));
            }
        }
    }

    public string OverflowText
    {
        get => _overflowText;
        private set => SetProperty(ref _overflowText, value);
    }

    public string OverflowTooltip
    {
        get => _overflowTooltip;
        private set => SetProperty(ref _overflowTooltip, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set
        {
            if (SetProperty(ref _isEmpty, value))
            {
                OnPropertyChanged(nameof(EmptyVisibility));
                OnPropertyChanged(nameof(ContentVisibility));
            }
        }
    }

    public Visibility OverflowVisibility => OverflowCount > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyVisibility => IsEmpty ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ContentVisibility => IsEmpty ? Visibility.Collapsed : Visibility.Visible;

    public string EmptyText => "⚙ 未配置";

    public double FontSizeDip => _config.FontSize.ToFontSizeDip();

    public void ApplyConfig(AppConfig config)
    {
        _config = config;
        _widthChanged = true;
        Rebuild(_availableWidthDip);
    }

    public void Refresh()
    {
        if (IsEmpty)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var item in _items)
        {
            if (item.Update(_store.Get(item.Key), _config.ColorScheme, now))
            {
                _widthChanged = true;
            }
        }

        if (OverflowCount > 0)
        {
            UpdateOverflowTooltip();
        }
    }

    public bool TakeWidthChanged()
    {
        var value = _widthChanged;
        _widthChanged = false;
        return value;
    }

    public void Rebuild(double availableWidthDip)
    {
        _availableWidthDip = availableWidthDip;
        _items.Clear();
        _candidates.Clear();

        var fontSize = _config.FontSize.ToFontSizeDip();
        var twoLine = _config.Layout == LayoutMode.CompactTwoLine;
        Height = Math.Ceiling(twoLine ? (fontSize * 2) + 12 : fontSize + 14);
        IsEmpty = _config.Items.Count == 0;

        if (IsEmpty)
        {
            OverflowCount = 0;
            OverflowText = string.Empty;
            OverflowTooltip = string.Empty;
            DesiredWidth = _measurer.Measure(EmptyText, fontSize, FontWeights.SemiBold) + 26;
            return;
        }

        var maxVisible = Math.Clamp(_config.MaxVisibleItems, 1, 8);
        var now = DateTimeOffset.UtcNow;

        foreach (var itemConfig in _config.Items.Take(maxVisible))
        {
            var item = new StripItemViewModel(itemConfig)
            {
                FontSizeDip = fontSize,
                IsTwoLine = twoLine,
                ItemHeight = Math.Max(18, Height - 4)
            };

            item.Update(_store.Get(item.Key), _config.ColorScheme, now);
            item.LabelWidth = MeasureLabel(item, fontSize);
            item.PriceWidth = MeasurePrice(item, fontSize);
            item.ChangeWidth = _measurer.Measure("▼ +100.00%", Math.Max(10, fontSize - 1), FontWeights.Normal, "Consolas") + 2;
            _candidates.Add(item);
        }

        var total = 18.0;
        var visible = new List<StripItemViewModel>();
        foreach (var item in _candidates)
        {
            if (visible.Count == 0
                || double.IsPositiveInfinity(availableWidthDip)
                || total + item.TotalWidth <= availableWidthDip)
            {
                visible.Add(item);
                total += item.TotalWidth;
            }
            else
            {
                break;
            }
        }

        OverflowCount = _candidates.Count - visible.Count;
        OverflowText = OverflowCount > 0 ? $"+{OverflowCount}" : string.Empty;

        var overflowWidth = OverflowCount > 0
            ? _measurer.Measure(OverflowText, fontSize, FontWeights.SemiBold, "Consolas") + 20
            : 0;

        foreach (var item in visible)
        {
            _items.Add(item);
        }

        DesiredWidth = total + overflowWidth;
        UpdateOverflowTooltip();
    }

    private void UpdateOverflowTooltip()
    {
        if (OverflowCount <= 0)
        {
            OverflowTooltip = string.Empty;
            return;
        }

        var visibleKeys = _items.Select(item => item.Key).ToHashSet();
        var maxVisible = Math.Clamp(_config.MaxVisibleItems, 1, 8);
        var builder = new StringBuilder();
        builder.Append("全部币种（").Append(_config.Items.Count).Append("）");

        for (var index = 0; index < _config.Items.Count; index++)
        {
            var item = _config.Items[index];
            var quote = _store.Get(item.Key);
            var isVisible = visibleKeys.Contains(item.Key);
            var state = index >= maxVisible ? "待命" : isVisible ? "显示中" : "超宽隐藏";
            var price = quote is null ? "—" : PriceFormatter.FormatPrice(quote.Price, item.Precision);
            var change = quote is null ? string.Empty : "  " + PriceFormatter.FormatChangeWithArrow(quote.Change24h);

            builder.Append(Environment.NewLine)
                .Append(isVisible ? "●" : "○").Append(' ')
                .Append(item.BaseCoin).Append('/').Append(item.QuoteCoin)
                .Append(" · ").Append(item.Market.ToDisplayName())
                .Append("  ").Append(price).Append(change)
                .Append("  [").Append(state).Append(']');
        }

        builder.Append(Environment.NewLine).Append("● 显示中   ○ 待命 / 超宽隐藏");
        OverflowTooltip = builder.ToString();
    }

    private double MeasureLabel(StripItemViewModel item, double fontSize)
    {
        var label = _measurer.Measure(item.Label, fontSize, FontWeights.SemiBold);
        var badge = _measurer.Measure(item.MarketBadge, Math.Max(9, fontSize - 2), FontWeights.Normal);
        return label + 3 + badge + 2;
    }

    private double MeasurePrice(StripItemViewModel item, double fontSize)
    {
        var digits = item.PriceIntegerDigits < 1 ? 1 : item.PriceIntegerDigits;
        var sample = BuildPriceSample(digits, item.Config.Precision);
        return _measurer.Measure(sample, fontSize, FontWeights.Normal, "Consolas") + 2;
    }

    private static string BuildPriceSample(int integerDigits, int precision)
    {
        var digits = Math.Clamp(integerDigits, 1, 9);
        var raw = new string('0', digits);
        var builder = new StringBuilder();
        for (var index = 0; index < raw.Length; index++)
        {
            if (index > 0 && (raw.Length - index) % 3 == 0)
            {
                builder.Append(',');
            }

            builder.Append(raw[index]);
        }

        var decimals = Math.Clamp(precision, 0, 8);
        if (decimals > 0)
        {
            builder.Append('.').Append('0', decimals);
        }

        return builder.ToString();
    }
}
