using Avalonia;
using AtlasPlayer.App.Controls;
using AtlasPlayer.App.ViewModels;

namespace AtlasPlayer.App.Tests;

/// <summary>
/// Brushes kept in static state (MainViewModel's colour swatches, SeekBar's default colours) are
/// built by whichever thread first touches their class. A SolidColorBrush belongs to the thread
/// that made it: made off the UI thread, MainViewModel's type initializer threw "Call from invalid
/// thread" and every UI test after it failed (Windows, .NET 10). They must be immutable brushes.
/// </summary>
public class StaticBrushTests
{
    [Fact]
    public async Task Static_brushes_belong_to_no_thread()
    {
        // A plain [Fact] runs off the UI thread, as the failing first touch did.
        var brushes = await Task.Run(() =>
            MainViewModel.SubtitleColors.Concat(MainViewModel.SubtitleBorderColors).Select(c => (object?)c.Brush)
                .Concat([
                    SeekBar.TrackBrushProperty.GetDefaultValue(typeof(SeekBar)),
                    SeekBar.BufferedBrushProperty.GetDefaultValue(typeof(SeekBar)),
                    SeekBar.ProgressBrushProperty.GetDefaultValue(typeof(SeekBar)),
                    SeekBar.ThumbBrushProperty.GetDefaultValue(typeof(SeekBar)),
                    SeekBar.LabelBackgroundProperty.GetDefaultValue(typeof(SeekBar)),
                ]).ToList());

        Assert.All(brushes, b =>
        {
            Assert.NotNull(b);
            Assert.IsNotAssignableFrom<AvaloniaObject>(b);
        });
    }
}
