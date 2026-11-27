using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CommunityToolkit.Mvvm.Input;

using RailworksForge.Views.Components;

namespace RailworksForge.Tests.Components;

[Collection(ComponentSession.CollectionName)]
public class ComponentTests
{
    [Fact]
    public Task SelectionToolbar_IsOnlyVisibleWithASelection()
    {
        return ComponentSession.Run(() =>
        {
            var toolbar = new SelectionToolbar();

            Assert.False(toolbar.IsVisible);

            toolbar.SelectedCount = 2;

            Assert.True(toolbar.IsVisible);

            toolbar.SelectedCount = 0;

            Assert.False(toolbar.IsVisible);
        });
    }

    [Fact]
    public Task SelectionToolbar_ClearButtonRunsTheClearCommand()
    {
        return ComponentSession.Run(() =>
        {
            var cleared = 0;
            var toolbar = new SelectionToolbar
            {
                SelectedCount = 1,
                ClearText = "Clear selection",
                ClearCommand = new RelayCommand(() => cleared++),
            };

            Show(toolbar);

            var clearButton = FindButton(toolbar, "Clear selection");

            clearButton.Command!.Execute(null);

            Assert.Equal(1, cleared);
        });
    }

    [Fact]
    public Task PendingChangesBar_ButtonsRunTheirCommands()
    {
        return ComponentSession.Run(() =>
        {
            var applied = 0;
            var discarded = 0;
            var bar = new PendingChangesBar
            {
                ApplyText = "Save",
                DiscardText = "Discard",
                ApplyCommand = new RelayCommand(() => applied++),
                DiscardCommand = new RelayCommand(() => discarded++),
            };

            Show(bar);

            FindButton(bar, "Save").Command!.Execute(null);
            FindButton(bar, "Discard").Command!.Execute(null);

            Assert.Equal(1, applied);
            Assert.Equal(1, discarded);
        });
    }

    [Fact]
    public Task BreadcrumbItem_MarksTheCurrentAndDirtyCrumbs()
    {
        return ComponentSession.Run(() =>
        {
            var crumb = new BreadcrumbItem { IsCurrent = true, IsDirty = true };

            Assert.Contains(":current", crumb.Classes);
            Assert.Contains(":dirty", crumb.Classes);

            crumb.IsDirty = false;

            Assert.DoesNotContain(":dirty", crumb.Classes);
        });
    }

    [Fact]
    public Task BreadcrumbItem_NavigatesThroughItsCommand()
    {
        return ComponentSession.Run(() =>
        {
            var navigated = 0;
            var crumb = new BreadcrumbItem { Text = "Routes", Command = new RelayCommand(() => navigated++) };

            Show(crumb);

            var button = crumb.GetVisualDescendants().OfType<Button>().Single();

            button.Command!.Execute(null);

            Assert.Equal(1, navigated);
        });
    }

    [Fact]
    public Task SectionHeader_ShowsTheCountOnlyWhenGiven()
    {
        return ComponentSession.Run(() =>
        {
            var header = new SectionHeader { Title = "Services" };

            Show(header);

            var badge = header.GetVisualDescendants().OfType<Badge>().Single();

            Assert.False(badge.IsVisible);

            header.Count = 12;
            Dispatcher.UIThread.RunJobs();

            Assert.True(badge.IsVisible);
        });
    }

    [Fact]
    public Task UniformWrapPanel_StretchesColumnsToFillTheWidth()
    {
        return ComponentSession.Run(() =>
        {
            var panel = new UniformWrapPanel { MinItemWidth = 240, ColumnSpacing = 16, RowSpacing = 16 };

            for (var index = 0; index < 5; index++)
            {
                panel.Children.Add(new Border { Height = 100 });
            }

            panel.Measure(new Size(1000, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, 1000, panel.DesiredSize.Height));

            var firstRowEnd = panel.Children[2].Bounds.Right;
            var secondRow = panel.Children[3].Bounds;

            Assert.Equal(1000, firstRowEnd, 3);
            Assert.Equal(0, secondRow.X);
            Assert.Equal(116, secondRow.Y);
            Assert.Equal(216, panel.DesiredSize.Height);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task SearchBox_TakesCtrlFOnlyWhenItIsTheShortcutTarget(bool isShortcutTarget)
    {
        return ComponentSession.Run(() =>
        {
            var searchBox = new SearchBox { IsShortcutTarget = isShortcutTarget };
            var other = new Button { Content = "Other" };
            var window = Show(new StackPanel { Children = { other, searchBox } });

            other.Focus();
            window.KeyPress(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f");

            var textBox = searchBox.GetVisualDescendants().OfType<TextBox>().Single();

            Assert.Equal(isShortcutTarget, textBox.IsFocused);
        });
    }

    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public Task EveryComponent_RendersInBothThemes(string theme)
    {
        return ComponentSession.Run(() =>
        {
            var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
            var components = new Control[]
            {
                new Badge { Content = "Standard" },
                new IconLabel { Text = "Open folder" },
                new MarkerLabel { Text = "English", IsMarked = true },
                new PageHeader { Title = "Scenario", Description = "Description" },
                new SectionHeader { Title = "Services", Count = 3 },
                new SearchBox { Placeholder = "Search", ShortcutHint = "Ctrl F" },
                new SelectionToolbar { SelectedCount = 1, SelectionText = "1 selected" },
                new PendingChangesBar { Summary = "1 unsaved change", ApplyText = "Save" },
                new EmptyState { Title = "Nothing here", Hint = "Pick a folder" },
                new Thumbnail { Width = 92, Height = 52 },
                new RouteCard { Title = "Route", Width = 248 },
                new RouteCard { Title = "Route", Classes = { "compact" } },
                new BreadcrumbItem { Text = "Routes", IsCurrent = true },
                new SettingsSection { Title = "Appearance", Content = new SettingsRow { Title = "Theme" } },
                new TaskIndicator { Label = "Loading" },
                new UniformWrapPanel { Children = { new Border { Height = 20 } } },
                new BlueprintSummary { Provider = "DTG", Product = "WCML", BlueprintId = @"RailVehicles\Class390.xml" },
                new AcquisitionStateIcon(),
                new LoadingSpinner(),
            };
            var panel = new StackPanel();

            panel.Children.AddRange(components);

            var window = Show(panel);

            window.RequestedThemeVariant = variant;
            Dispatcher.UIThread.RunJobs();

            var frame = window.CaptureRenderedFrame();
            var unstyled = components.Where(component => component is TemplatedControl { Template: null }).ToList();

            Assert.NotNull(frame);
            Assert.Empty(unstyled);
        });
    }

    private static Window Show(Control content)
    {
        var window = new Window { Width = 1200, Height = 900, Content = content };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        return window;
    }

    private static Button FindButton(Control root, string content)
    {
        return root.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, content));
    }
}
