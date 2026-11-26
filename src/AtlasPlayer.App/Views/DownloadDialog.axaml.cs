using Avalonia.Controls;
using Avalonia.Interactivity;
using AtlasPlayer.App.ViewModels;

namespace AtlasPlayer.App.Views;

public partial class DownloadDialog : Window
{
    public DownloadDialog()
    {
        InitializeComponent();
        // A link opened with (or pasted into) the box looks up its qualities by itself.
        Opened += (_, _) => Input.Focus();
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
