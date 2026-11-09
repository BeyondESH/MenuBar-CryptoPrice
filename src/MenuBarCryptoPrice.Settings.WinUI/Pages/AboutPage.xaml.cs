using MenuBarCryptoPrice.Settings.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MenuBarCryptoPrice.Settings.WinUI.Pages;

public sealed partial class AboutPage : Page
{
    public AboutPage(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    public SettingsViewModel ViewModel { get; }

    private async void OnTipClick(object sender, RoutedEventArgs e)
    {
        var panel = new StackPanel
        {
            Spacing = 12,
            Width = 340
        };

        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "dashang.jpg");
        if (File.Exists(imagePath))
        {
            panel.Children.Add(new Image
            {
                Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute)),
                Stretch = Stretch.Uniform,
                Width = 320,
                MaxHeight = 480,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = "打赏图片未找到，请确认 Assets/dashang.jpg 已随客户端发布。",
                TextWrapping = TextWrapping.Wrap
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = "感谢支持！",
            Opacity = 0.7,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var dialog = new ContentDialog
        {
            Title = "打赏作者",
            Content = panel,
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
