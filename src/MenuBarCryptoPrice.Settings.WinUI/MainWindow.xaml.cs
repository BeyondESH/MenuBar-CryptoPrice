using MenuBarCryptoPrice.Infrastructure;
using MenuBarCryptoPrice.MarketData;
using MenuBarCryptoPrice.Settings.WinUI.Pages;
using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace MenuBarCryptoPrice.Settings.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly bool _openAbout;
    private DispatcherTimer? _visibilityTimer;
    private int _visibilityChecks;
    private bool _everActivated;
    private string? _currentTag;

    public MainWindow()
    {
        _openAbout = Environment.GetCommandLineArgs()
            .Any(argument => string.Equals(argument, "--about", StringComparison.OrdinalIgnoreCase));
        AppPaths.EnsureCreated();
        var log = new FileLogger(AppPaths.LogDirectory);
        _configService = new ConfigService(AppPaths.ConfigFile, log, autoSave: false);
        ViewModel = new SettingsViewModel(
            _configService,
            new CatalogService(AppPaths.CacheDirectory, log),
            new BitgetRestClient(),
            log);

        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }

        ConfigureWindow();

        RootNavigation.Loaded += OnNavigationLoaded;
        Closed += OnClosed;
        Activated += OnActivated;
        StartVisibilityGuard();
    }

    public SettingsViewModel ViewModel { get; }

    private void ConfigureWindow()
    {
        var display = DisplayArea.Primary;
        var work = display.WorkArea;
        var width = Math.Min(1180, Math.Max(900, work.Width - 80));
        var height = Math.Min(820, Math.Max(600, work.Height - 80));

        AppWindow.Resize(new SizeInt32(width, height));
        AppWindow.Move(new PointInt32(
            work.X + ((work.Width - width) / 2),
            work.Y + ((work.Height - height) / 2)));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 900;
            presenter.PreferredMinimumHeight = 600;
        }
    }

    private async void OnNavigationLoaded(object sender, RoutedEventArgs e)
    {
        RootNavigation.Loaded -= OnNavigationLoaded;
        RootNavigation.SelectedItem = _openAbout
            ? RootNavigation.FooterMenuItems[0]
            : RootNavigation.MenuItems[0];
        ShowPage(_openAbout ? "about" : "market");
        await ViewModel.InitializeAsync();
    }

    private void OnNavigationSelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            ShowPage(tag);
        }
    }

    private void OnNavSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        var query = sender.Text.Trim();
        foreach (var item in RootNavigation.MenuItems.Concat(RootNavigation.FooterMenuItems).OfType<NavigationViewItem>())
        {
            var matches = query.Length == 0
                || (item.Content is string text && text.Contains(query, StringComparison.OrdinalIgnoreCase));
            item.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ShowPage(string tag)
    {
        if (string.Equals(_currentTag, tag, StringComparison.Ordinal))
        {
            return;
        }

        _currentTag = tag;
        PageHost.Children.Clear();
        PageHost.Children.Add(tag switch
        {
            "appearance" => new AppearancePage(ViewModel),
            "about" => new AboutPage(ViewModel),
            _ => new MarketPage(ViewModel)
        });
    }

    private void StartVisibilityGuard()
    {
        _visibilityTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _visibilityTimer.Tick += (_, _) =>
        {
            _visibilityChecks++;
            if (_everActivated || _visibilityChecks >= 20)
            {
                _visibilityTimer.Stop();
                return;
            }

            if (AppWindow.Presenter is OverlappedPresenter presenter
                && presenter.State == OverlappedPresenterState.Minimized)
            {
                presenter.Restore();
                Activate();
            }
        };
        _visibilityTimer.Start();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            return;
        }

        _everActivated = true;
        _visibilityTimer?.Stop();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _visibilityTimer?.Stop();
        ViewModel.Dispose();
        _configService.Dispose();
    }
}
