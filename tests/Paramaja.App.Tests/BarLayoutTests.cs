using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Paramaja.App.Services;
using Paramaja.App.ViewModels;
using Paramaja.App.Views;
using Paramaja.Core.Settings;

namespace Paramaja.App.Tests;

/// <summary>Moves pieces of the bar as the editor does.</summary>
public static class Bar
{
    public static BarSlot Slot(MainViewModel vm, string id) =>
        vm.BarZones.Append(vm.BarOffZone).SelectMany(z => z.Slots).First(s => s.Id == id);

    public static void Off(MainViewModel vm, string id) => vm.MoveBarSlot(Slot(vm, id), BarZoneKind.Off, 0);

    public static void Move(MainViewModel vm, string id, BarZoneKind to, int index) => vm.MoveBarSlot(Slot(vm, id), to, index);

    /// <summary>A new space or divider, from off the bar.</summary>
    public static void Add(MainViewModel vm, string id, BarZoneKind to, int index) =>
        vm.MoveBarSlot(vm.BarOffZone.Slots.First(s => s.Id == id), to, index);
}

/// <summary>The modular bar: placed, ordered and taken off piece by piece (2.0.0-beta.5).</summary>
public class BarLayoutTests
{
    private static void Playing(Rig r)
    {
        r.Vm.IsIdle = false;
        r.Vm.Duration = 5400;
        r.Vm.Position = 1880;
        r.Vm.MediaTitle = "Dangerous Tour, Bucharest (1992)";
    }

    private static ControlsBar ShowBar(Rig r, double width = 1280)
    {
        var bar = new ControlsBar { DataContext = r.Vm, Width = width };
        bar.Show();
        Dispatcher.UIThread.RunJobs();
        return bar;
    }

    private static StackPanel Group(ControlsBar bar, string name) => bar.FindControl<StackPanel>(name)!;

    [AvaloniaFact]
    public void The_default_bar_is_the_simple_bar_in_its_order()
    {
        using var r = new Rig();
        Playing(r);
        var bar = ShowBar(r);
        Assert.Equal(BarItems.Simple.Left, bar.IdsIn(Group(bar, "LeftGroup")));
        Assert.Equal(BarItems.Simple.Centre, bar.IdsIn(Group(bar, "Transport")));
        Assert.Equal(BarItems.Simple.Right, bar.IdsIn(Group(bar, "RightGroup")));
        Assert.Null(bar.GroupOf("stop"));
        Assert.Equal("right", bar.GroupOf("fullscreen"));
        bar.Close();
    }

    [AvaloniaFact]
    public void Every_piece_can_go_on_the_bar_and_moves_there_at_once()
    {
        using var r = new Rig();
        Playing(r);
        var bar = ShowBar(r, 1920);
        foreach (var slot in r.Vm.BarOffZone.Slots.Where(s => !BarItems.IsRepeatable(s.Id)).ToList())
            r.Vm.MoveBarSlot(slot, BarZoneKind.Left, 0);
        Dispatcher.UIThread.RunJobs();
        foreach (var item in BarItems.All.Where(i => !i.Repeatable))
            Assert.NotNull(bar.GroupOf(item.Id));
        Assert.True(r.Vm.BarOffZone.Slots.All(s => BarItems.IsRepeatable(s.Id))); // a space and a divider are always there to take

        Bar.Move(r.Vm, "fullscreen", BarZoneKind.Left, 0);
        Bar.Move(r.Vm, "play", BarZoneKind.Right, 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("fullscreen", bar.IdsIn(Group(bar, "LeftGroup"))[0]);
        Assert.Equal("play", bar.IdsIn(Group(bar, "RightGroup"))[0]);
        Assert.True(bar.FindControl<Button>("PlayButton")!.IsEffectivelyVisible);
        Assert.Equal(r.Settings.BarRight, bar.IdsIn(Group(bar, "RightGroup")));
        bar.Close();
    }

    [Fact]
    public void Spaces_and_dividers_repeat_and_play_more_and_settings_stay_on()
    {
        using var r = new Rig();
        Bar.Add(r.Vm, "separator", BarZoneKind.Right, 0);
        Bar.Add(r.Vm, "separator", BarZoneKind.Right, 0);
        Bar.Add(r.Vm, "spacer", BarZoneKind.Centre, 0);
        Assert.Equal(2, r.Settings.BarRight.Count(i => i == "separator"));
        Assert.Equal("spacer", r.Settings.BarCentre[0]);
        Assert.Contains(r.Vm.BarOffZone.Slots, s => s.Id == "separator"); // still there for the next one
        foreach (var id in new[] { "play", "more", "settings" })
        {
            Bar.Off(r.Vm, id);
            Assert.True(r.Vm.OnBar(id), id);
            r.Vm.SelectedBarSlot = Bar.Slot(r.Vm, id);
            Assert.False(r.Vm.TakeSelectedOffCommand.CanExecute(null));
        }
        // A divider taken off the bar is gone, not kept twice off it.
        r.Vm.MoveBarSlot(r.Vm.BarZones[2].Slots.First(s => s.Id == "separator"), BarZoneKind.Off, 0);
        Assert.Single(r.Settings.BarRight, i => i == "separator");
        Assert.Single(r.Vm.BarOffZone.Slots, s => s.Id == "separator");
        // Saved, and the same after a restart.
        var again = r.Store.Load();
        Assert.Equal(r.Settings.BarRight, again.BarRight);
        Assert.Equal(r.Settings.BarCentre, again.BarCentre);
    }

    [Fact]
    public void Moving_within_a_group_counts_the_piece_where_it_is()
    {
        using var r = new Rig();
        var right = r.Settings.BarRight.ToList();
        Bar.Move(r.Vm, right[0], BarZoneKind.Right, 2); // before the third: one step on
        Assert.Equal([right[1], right[0], right[2]], r.Settings.BarRight.Take(3));
        Bar.Move(r.Vm, right[0], BarZoneKind.Right, 1); // before itself: no change
        Assert.Equal([right[1], right[0], right[2]], r.Settings.BarRight.Take(3));
        Bar.Move(r.Vm, right[0], BarZoneKind.Right, 99);
        Assert.Equal(right[0], r.Settings.BarRight[^1]);
    }

    [Fact]
    public void The_editors_buttons_move_the_selected_piece()
    {
        using var r = new Rig();
        Assert.False(r.Vm.MoveSelectedEarlierCommand.CanExecute(null));
        r.Vm.SelectedBarSlot = Bar.Slot(r.Vm, "fullscreen");
        Assert.True(r.Vm.SelectedBarSlot.IsSelected);
        Assert.Contains("Fullscreen", r.Vm.SelectedBarSlotText);
        Assert.False(r.Vm.MoveSelectedLaterCommand.CanExecute(null)); // already last
        r.Vm.MoveSelectedEarlierCommand.Execute(null);
        Assert.Equal("fullscreen", r.Settings.BarRight[^2]);
        r.Vm.MoveSelectedLaterCommand.Execute(null);
        Assert.Equal("fullscreen", r.Settings.BarRight[^1]);
        r.Vm.MoveSelectedToCommand.Execute("Left");
        Assert.Equal("fullscreen", r.Settings.BarLeft[^1]);
        Assert.True(r.Vm.TakeSelectedOffCommand.CanExecute(null));
        r.Vm.TakeSelectedOffCommand.Execute(null);
        Assert.False(r.Vm.OnBar("fullscreen"));
        Assert.True(r.Vm.FullscreenInMore);
        Assert.Null(r.Vm.SelectedBarSlot);
        // Off the bar it can be placed again.
        r.Vm.SelectedBarSlot = Bar.Slot(r.Vm, "fullscreen");
        r.Vm.MoveSelectedToCommand.Execute("Centre");
        Assert.Equal("fullscreen", r.Settings.BarCentre[^1]);
    }

    [Fact]
    public void The_readouts_tell_the_time_left_the_time_and_the_title()
    {
        using var r = new Rig();
        Playing(r);
        Assert.Equal("−0:58:40", r.Vm.RemainingText);
        Assert.True(r.Vm.HasDuration);
        var changed = new List<string?>();
        r.Vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        r.Vm.Position = 5000;
        Assert.Contains(nameof(MainViewModel.RemainingText), changed);
        Assert.Equal("−0:06:40", r.Vm.RemainingText);
        r.Vm.Position = 6000; // past the end (a live stream's estimate): never negative
        Assert.Equal("−0:00:00", r.Vm.RemainingText);
        var now = new[] { DateTime.Now.AddMinutes(-1), DateTime.Now, DateTime.Now.AddMinutes(1) }
            .Select(t => t.ToString("t", System.Globalization.CultureInfo.CurrentCulture));
        Bar.Move(r.Vm, "clock", BarZoneKind.Right, 0);
        r.Vm.TickAutoHide(DateTime.UtcNow);
        Assert.Contains(r.Vm.ClockText, now);
    }

    [AvaloniaFact]
    public void The_readouts_on_the_bar_show_what_plays()
    {
        using var r = new Rig();
        Playing(r);
        foreach (var id in new[] { "title", "elapsed", "remaining", "total" }) Bar.Move(r.Vm, id, BarZoneKind.Left, 99);
        var bar = ShowBar(r);
        Assert.Equal("Dangerous Tour, Bucharest (1992)", bar.FindControl<TextBlock>("TitleReadout")!.Text);
        Assert.Equal(r.Vm.RemainingText, bar.FindControl<TextBlock>("RemainingReadout")!.Text);
        Assert.True(bar.FindControl<TextBlock>("TotalReadout")!.IsEffectivelyVisible);
        r.Vm.MediaTitle = "";
        r.Vm.Duration = 0; // a live stream: no length, no time left
        Dispatcher.UIThread.RunJobs();
        Assert.False(bar.FindControl<TextBlock>("TitleReadout")!.IsEffectivelyVisible);
        Assert.False(bar.FindControl<TextBlock>("TotalReadout")!.IsEffectivelyVisible);
        Assert.False(bar.FindControl<TextBlock>("RemainingReadout")!.IsEffectivelyVisible);
        Assert.True(bar.FindControl<TextBlock>("ElapsedReadout")!.IsEffectivelyVisible);
        bar.Close();
    }

    [AvaloniaFact]
    public void Play_off_the_centre_leaves_the_centre_group_centred()
    {
        using var r = new Rig();
        Playing(r);
        Bar.Move(r.Vm, "play", BarZoneKind.Left, 0);
        var bar = ShowBar(r);
        var transport = Group(bar, "Transport");
        var row = (Panel)transport.Parent!;
        Assert.InRange(transport.Bounds.Center.X, row.Bounds.Width / 2 - 1, row.Bounds.Width / 2 + 1);
        Assert.Equal("left", bar.GroupOf("play"));
        bar.Close();
    }

    [AvaloniaFact]
    public void A_menu_moved_to_another_group_still_keeps_the_bar_open()
    {
        using var r = new Rig();
        Playing(r);
        var bar = ShowBar(r);
        Bar.Move(r.Vm, "subtitles", BarZoneKind.Left, 0);
        Dispatcher.UIThread.RunJobs();
        var subs = bar.FindControl<Button>("SubtitlesButton")!;
        Assert.Equal("left", bar.GroupOf("subtitles"));
        subs.Flyout!.ShowAt(subs);
        Dispatcher.UIThread.RunJobs();
        Assert.True(r.Vm.IsMenuOpen);
        subs.Flyout.Hide();
        Dispatcher.UIThread.RunJobs();
        Assert.False(r.Vm.IsMenuOpen);
        bar.Close();
    }

    [AvaloniaFact]
    public void Right_clicking_the_bar_offers_to_customize_it()
    {
        using var r = new Rig();
        Playing(r);
        var bar = ShowBar(r);
        var root = bar.FindControl<Border>("Root")!;
        var menu = Assert.IsType<MenuFlyout>(root.ContextFlyout);
        menu.ShowAt(root); // its item is bound once it opens
        Dispatcher.UIThread.RunJobs();
        Assert.True(r.Vm.IsMenuOpen); // the bar stays while it's open
        var item = menu.Items.OfType<MenuItem>().Single();
        Assert.Equal("Customize bar…", item.Header);
        item.Command!.Execute(null);
        Assert.Equal(1, r.Dialogs.SettingsShown);
        Assert.Equal(SettingsSection.ControlsBar, r.Dialogs.LastSettingsSection);
        bar.Close();
    }

    // ---- The editor, dragged with the mouse ------------------------------------------------------

    private static (SettingsWindow Window, BarEditor Editor) ShowEditor(Rig r)
    {
        var w = new SettingsWindow(SettingsSection.ControlsBar) { DataContext = r.Vm, Height = 1400 };
        w.Show();
        Dispatcher.UIThread.RunJobs();
        return (w, w.FindControl<BarEditor>("BarEditor")!);
    }

    private static Border Chip(BarEditor editor, BarZoneKind zone, string id) =>
        editor.GetVisualDescendants().OfType<Border>().First(b => b.Classes.Contains("bar-chip") && b.DataContext is BarSlot s
            && s.Id == id && b.FindAncestorOfType<Border>() is { DataContext: BarZone z } && z.Kind == zone);

    private static Point At(Window w, Visual v, double fx = 0.5, double fy = 0.5) =>
        v.TranslatePoint(new Point(v.Bounds.Width * fx, v.Bounds.Height * fy), w)!.Value;

    private static void Drag(Window w, Point from, Point to)
    {
        w.MouseDown(from, MouseButton.Left);
        w.MouseMove(new Point(from.X + 10, from.Y + 10));
        w.MouseMove(to);
        Dispatcher.UIThread.RunJobs();
        w.MouseUp(to, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Customize_opens_settings_at_the_editor()
    {
        using var r = new Rig();
        var (w, editor) = ShowEditor(r);
        Assert.True(w.FindControl<TabItem>("InterfaceTab")!.IsSelected);
        Assert.True(editor.IsEffectivelyVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void A_piece_dragged_from_off_the_bar_goes_before_the_chip_it_is_dropped_on()
    {
        using var r = new Rig();
        var (w, editor) = ShowEditor(r);
        var clock = Chip(editor, BarZoneKind.Off, "clock");
        var play = Chip(editor, BarZoneKind.Centre, "play");
        Drag(w, At(w, clock), At(w, play, 0.2));
        var before = r.Settings.BarCentre.IndexOf("play") - 1;
        Assert.Equal("clock", r.Settings.BarCentre[before]);
        Assert.DoesNotContain(r.Vm.BarOffZone.Slots, s => s.Id == "clock");
        Assert.Equal("clock", r.Vm.SelectedBarSlot?.Id); // the moved piece is selected for the buttons
        w.Close();
    }

    [AvaloniaFact]
    public void Chips_reorder_within_a_group_and_go_off_the_bar_by_drag()
    {
        using var r = new Rig();
        var (w, editor) = ShowEditor(r);
        Drag(w, At(w, Chip(editor, BarZoneKind.Right, "fullscreen")), At(w, Chip(editor, BarZoneKind.Right, "subtitles"), 0.1));
        Assert.Equal(r.Settings.BarRight.IndexOf("subtitles") - 1, r.Settings.BarRight.IndexOf("fullscreen"));

        var off = editor.FindControl<ContentControl>("OffZone")!;
        Drag(w, At(w, Chip(editor, BarZoneKind.Right, "fullscreen")), At(w, off, 0.9, 0.5));
        Assert.False(r.Vm.OnBar("fullscreen"));

        // Play can't leave: dropped off the bar, it stays where it was.
        var centre = r.Settings.BarCentre.ToList();
        Drag(w, At(w, Chip(editor, BarZoneKind.Centre, "play")), At(w, off, 0.9, 0.5));
        Assert.Equal(centre, r.Settings.BarCentre);
        w.Close();
    }

    [AvaloniaFact]
    public void A_click_selects_a_chip_and_escape_gives_up_a_drag()
    {
        using var r = new Rig();
        var (w, editor) = ShowEditor(r);
        var audio = At(w, Chip(editor, BarZoneKind.Right, "audio"));
        w.MouseDown(audio, MouseButton.Left);
        w.MouseUp(audio, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("audio", r.Vm.SelectedBarSlot?.Id);
        var takeOff = editor.FindControl<Button>("TakeOffButton")!;
        Assert.True(r.Vm.TakeSelectedOffCommand.CanExecute(null));
        Assert.True(takeOff.IsEffectivelyEnabled);

        var right = r.Settings.BarRight.ToList();
        w.MouseDown(audio, MouseButton.Left);
        w.MouseMove(new Point(audio.X + 30, audio.Y + 200));
        w.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        w.MouseUp(new Point(audio.X + 30, audio.Y + 200), MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(right, r.Settings.BarRight);
        Assert.True(w.IsVisible); // Escape gave up the drag; it didn't close Settings
        w.Close();
    }
}
