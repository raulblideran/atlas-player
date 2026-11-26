using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;

namespace AtlasPlayer.App.Controls;

/// <summary>
/// One line of Settings: what it is (<see cref="Title"/>), what it does in a sentence
/// (<see cref="Description"/>), and its control on the right; with the "stack" class the
/// control sits below instead, for wide ones. The control is named after the title for
/// screen readers. Styled in Styles/Controls.axaml.
/// </summary>
public class SettingRow : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Title));
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingRow, string?>(nameof(Description));

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == ContentProperty) NameContent();
    }

    private void NameContent()
    {
        if (Content is Control control && Title is { Length: > 0 } title
            && string.IsNullOrEmpty(AutomationProperties.GetName(control)))
            AutomationProperties.SetName(control, title);
    }
}
