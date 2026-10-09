using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:Settings does: the examples of GPUI Kit's own tests
/// (crates/component/src/setting/tests.rs) for the search, the selection and
/// the scroll to a group, then what the Avalonia port adds: following a
/// field's value from its DefaultValue, the reset, the fields' size and the
/// narrow layout.
/// </summary>
public class SettingsBehaviorTests
{
    /// <summary>An 832x552 window with the control at (16, 16).</summary>
    private static CaseHost Host(Control content) =>
        CaseHost.Open(GoldenManifest.Get("settings/story.normal/normal/light"), content);

    // tests.rs item_with_height: a custom item (no title) found by its keyword alone.
    private static SettingItem Item(string keyword, double height = 80) => new()
    {
        Keywords = keyword,
        Content = new Border { Height = height, Child = new TextBlock { Text = "Setting" } },
    };

    // tests.rs setup: General, Appearance (open: an untitled group, Colors and Fonts)
    // and Editor, Appearance selected, in an 800x520 box (GPUI's is 1000x700).
    private static Settings SearchFixture() => new()
    {
        Width = 800,
        Height = 520,
        SelectedIndex = 1,
        Items =
        {
            new SettingPage { Title = "General", Items = { new SettingGroup { Items = { Item("language") } } } },
            new SettingPage
            {
                Title = "Appearance",
                IsOpen = true,
                Items =
                {
                    new SettingGroup { Items = { Item("unrelated") } },
                    new SettingGroup { Title = "Colors", Items = { Item("theme colors") } },
                    new SettingGroup { Title = "Fonts", Items = { Item("unrelated"), Item("theme font") } },
                },
            },
            new SettingPage { Title = "Editor", Items = { new SettingGroup { Items = { Item("theme editor") } } } },
        },
    };

    private static SettingPage Page(Settings settings, int page) => (SettingPage)settings.Items[page]!;

    private static SettingGroup Group(Settings settings, int page, int group) => (SettingGroup)Page(settings, page).Items[group]!;

    private static SettingItem ItemAt(Settings settings, int page, int group, int item) =>
        (SettingItem)Group(settings, page, group).Items[item]!;

    // tests.rs selection: the page's index and the group's index in its page.
    private static (int Page, int? Group) Selection(Settings settings)
    {
        var group = settings.SelectedGroup;
        return (settings.SelectedIndex, group is null ? null : Page(settings, settings.SelectedIndex).Items.IndexOf(group));
    }

    // tests.rs debug_bounds(...).is_some(): the item is on the page shown.
    private static bool Shown(Control control) => control.IsEffectivelyVisible && control.Bounds.Height > 0;

    private static void Search(CaseHost host, Settings settings, string query)
    {
        settings.SearchText = query;
        host.Flush();
    }

    // The sidebar row labeled `label`, clicked in its middle (tests.rs click_nav).
    private static void ClickRow(CaseHost host, string label)
    {
        var item = host.Window.GetVisualDescendants().OfType<SidebarMenuItem>().First(i => i.Label == label && i.IsEffectivelyVisible);
        var row = item.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_Row");
        Click(host, row);
    }

    private static void Click(CaseHost host, Control control)
    {
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseMove(at);
        host.Window.MouseDown(at, MouseButton.Left);
        host.Window.MouseUp(at, MouseButton.Left);
        host.Flush();
    }

    // The item's box in the window.
    private static Rect InWindow(CaseHost host, Control control) =>
        new(control.TranslatePoint(default, host.Window)!.Value, control.Bounds.Size);

    // tests.rs search_preserves_the_page_and_clicks_use_original_indices.
    [Test]
    public async Task Search_keeps_the_page_and_clicks_use_the_original_indices()
    {
        var settings = SearchFixture();
        using var host = Host(settings);
        // The old index still points at a page with matches, but Editor would be the
        // second page with matches: the selection stays on Appearance.
        Search(host, settings, "theme");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 1, 1, 0))).IsTrue();
        await Assert.That(Shown(ItemAt(settings, 2, 0, 0))).IsFalse();

        // Only Appearance matches.
        Search(host, settings, "font");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 1, 2, 1))).IsTrue();

        // Editor chosen from the filtered menu stays selected once the search is cleared.
        Search(host, settings, "theme");
        ClickRow(host, "Editor");
        await Assert.That(Selection(settings)).IsEqualTo((2, (int?)null));
        Search(host, settings, "");
        await Assert.That(Selection(settings)).IsEqualTo((2, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 2, 0, 0))).IsTrue();

        // The page loses its matches: the first page with matches is selected.
        Search(host, settings, "font");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 1, 2, 1))).IsTrue();
        // Nothing matches: no page shows, and the selection waits for results.
        Search(host, settings, "no matching setting");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 1, 2, 1))).IsFalse();
        await Assert.That(settings.Pages.Any(p => p.IsEffectivelyVisible)).IsFalse();
        Search(host, settings, "");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
    }

    // tests.rs search_preserves_group_and_item_identity.
    [Test]
    public async Task Search_keeps_the_group_and_hides_the_items_without_matches()
    {
        var settings = SearchFixture();
        // The target group needs scrolling, with an untitled group before it.
        Group(settings, 1, 1).Items[0] = Item("theme colors", 450);
        using var host = Host(settings);
        ClickRow(host, "Fonts");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        Search(host, settings, "theme");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        await Assert.That(Shown(ItemAt(settings, 1, 2, 1))).IsTrue();
        await Assert.That(Shown(ItemAt(settings, 1, 2, 0))).IsFalse();
        // Fonts clicked once the page's first group and item are filtered out.
        ClickRow(host, "Fonts");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        var target = InWindow(host, ItemAt(settings, 1, 2, 1));
        await Assert.That(target.Top >= 0 && target.Bottom <= host.Window.Bounds.Height).IsTrue();
        Search(host, settings, "font");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        Search(host, settings, "");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        // The selected group loses its matches: the page alone stays selected.
        Search(host, settings, "colors");
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
        await Assert.That(Shown(ItemAt(settings, 1, 1, 0))).IsTrue();
    }

    // tests.rs footer_follows_group_search_visibility.
    [Test]
    public async Task A_footer_follows_its_groups_matches_and_matches_nothing_itself()
    {
        var settings = SearchFixture();
        var footer = new TextBlock { Text = "Changes apply to this device only." };
        Group(settings, 1, 2).Footer = footer;
        using var host = Host(settings);
        Search(host, settings, "font");
        await Assert.That(Shown(ItemAt(settings, 1, 2, 1))).IsTrue();
        await Assert.That(Shown(footer)).IsTrue();
        Search(host, settings, "colors");
        await Assert.That(Shown(ItemAt(settings, 1, 1, 0))).IsTrue();
        await Assert.That(Shown(footer)).IsFalse();
        Search(host, settings, "device");
        await Assert.That(Shown(footer)).IsFalse();
        Search(host, settings, "font");
        await Assert.That(Shown(footer)).IsTrue();
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)null));
    }

    // tests.rs resetting_search_results_leaves_hidden_settings_unchanged.
    [Test]
    public async Task The_reset_leaves_the_items_the_search_hides()
    {
        static SettingItem Resettable(string keyword, bool modified)
        {
            var item = Item(keyword);
            item.IsModified = modified;
            item.Reset += (_, _) => item.IsModified = false;
            return item;
        }
        var visible = Resettable("theme", false);
        var hidden = Resettable("hidden", true);
        var page = new SettingPage { Title = "General", Items = { new SettingGroup { Items = { visible, hidden } } } };
        var settings = new Settings { Width = 800, Height = 520, Items = { page } };
        using var host = Host(settings);
        var reset = page.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ResetButton");
        Search(host, settings, "theme");
        await Assert.That(reset.IsVisible).IsFalse();
        visible.IsModified = true;
        host.Flush();
        await Assert.That(reset.IsVisible).IsTrue();
        Click(host, reset);
        await Assert.That(visible.IsModified).IsFalse();
        await Assert.That(hidden.IsModified).IsTrue();
        await Assert.That(reset.IsVisible).IsFalse();
    }

    // tests.rs group_variant_overrides_the_settings_default.
    [Test]
    public async Task A_groups_variant_overrides_the_settings_variant()
    {
        static Settings Variants(bool overrideFirst, GroupBoxVariant variant)
        {
            var first = new SettingGroup { Items = { Item("plain") } };
            if (overrideFirst)
            {
                first.Variant = GroupBoxVariant.Normal;
            }
            return new Settings
            {
                Width = 800,
                Height = 520,
                GroupVariant = variant,
                Items = { new SettingPage { Title = "General", Items = { first, new SettingGroup { Items = { Item("outlined") } } } } },
            };
        }
        var baseline = Variants(false, GroupBoxVariant.Normal);
        using var host = Host(baseline);
        var overridden = Variants(true, GroupBoxVariant.Outline);
        using var other = Host(overridden);
        var (baseFirst, baseSecond) = (InWindow(host, ItemAt(baseline, 0, 0, 0)), InWindow(host, ItemAt(baseline, 0, 1, 0)));
        var (overFirst, overSecond) = (InWindow(other, ItemAt(overridden, 0, 0, 0)), InWindow(other, ItemAt(overridden, 0, 1, 0)));
        // The overridden group shows its items directly, as the baseline does.
        await Assert.That(overFirst.Top - baseFirst.Top).IsEqualTo(0);
        // The other keeps the settings' outline: a 1px border and 16px above its items.
        await Assert.That(overSecond.Top - baseSecond.Top).IsEqualTo(17);
    }

    // tests.rs selecting_a_group_from_another_page_scrolls_to_it.
    [Test]
    public async Task Selecting_a_group_on_another_page_scrolls_to_it()
    {
        var settings = SearchFixture();
        Group(settings, 1, 1).Items[0] = Item("theme colors", 900);
        using var host = Host(settings);
        settings.SelectedIndex = 0;
        host.Flush();
        settings.SelectedGroup = Group(settings, 1, 2);
        host.Flush();
        await Assert.That(Selection(settings)).IsEqualTo((1, (int?)2));
        var target = InWindow(host, ItemAt(settings, 1, 2, 1));
        await Assert.That(target.Top >= 0 && target.Bottom <= host.Window.Bounds.Height).IsTrue();
        // A page chosen from the menu starts at its top.
        ClickRow(host, "General");
        ClickRow(host, "Appearance");
        await Assert.That(InWindow(host, ItemAt(settings, 1, 0, 0)).Top).IsLessThan(host.Window.Bounds.Height);
        await Assert.That(Shown(ItemAt(settings, 1, 0, 0))).IsTrue();
    }

    // item.rs is_match: the title, the description and the keywords, ignoring case;
    // an item without a title only by its keywords, and every item by an empty search.
    [Test]
    public async Task An_item_matches_its_title_description_and_keywords()
    {
        var item = new SettingItem { Title = "Enable Two-factor auth", Description = "Ask for a code.", Keywords = "MFA, 2FA" };
        await Assert.That(item.Matches("two-FACTOR")).IsTrue();
        await Assert.That(item.Matches("code")).IsTrue();
        await Assert.That(item.Matches("mfa")).IsTrue();
        await Assert.That(item.Matches("2fa")).IsTrue();
        await Assert.That(item.Matches("password")).IsFalse();
        await Assert.That(item.Matches("")).IsTrue();
        var rich = new SettingItem { Title = "Docs", Description = new TextBlock { Text = "The crate's documentation." } };
        await Assert.That(rich.Matches("crate")).IsTrue();
        var custom = new SettingItem { Keywords = "Advanced, Network", Content = new TextBlock { Text = "Proxy" } };
        await Assert.That(custom.Matches("")).IsTrue();
        await Assert.That(custom.Matches("network")).IsTrue();
        await Assert.That(custom.Matches("proxy")).IsFalse();
    }

    // fields/mod.rs is_resettable and reset: an item follows the value of the field types
    // it knows from its DefaultValue (a value, or text that converts to one), shows the
    // page's reset button while one differs and sets the defaults back, through bindings.
    [Test]
    public async Task An_item_follows_its_fields_default_and_the_reset_sets_it_back()
    {
        var source = new CheckBox { IsChecked = false };
        var toggle = new ToggleSwitch();
        toggle[!!ToggleButton.IsCheckedProperty] = source[!!ToggleButton.IsCheckedProperty];
        var number = new NumericUpDown { Value = 14, Minimum = 8, Maximum = 72 };
        var text = new TextBox { Text = "/usr/local/bin/bash" };
        var combo = new ComboBox { ItemsSource = new[] { "Arial", "Helvetica" }, SelectedIndex = 0 };
        var inline = new ComboBox { Items = { new ComboBoxItem { Content = "Normal" }, new ComboBoxItem { Content = "Outline" } }, SelectedIndex = 1 };
        var slider = new Slider { Minimum = 0, Maximum = 1, Value = 0.5 };
        SettingItem Field(string title, object @default, Control field) => new() { Title = title, DefaultValue = @default, Content = field };
        var items = new[]
        {
            Field("Dark Mode", "False", toggle),
            Field("Font Size", "14", number),
            Field("CLI Path", "/usr/local/bin/bash", text),
            Field("Font Family", "Arial", combo),
            Field("Group Variant", "Outline", inline),
            Field("Volume", 0.5, slider),
        };
        var group = new SettingGroup();
        foreach (var item in items)
        {
            group.Items.Add(item);
        }
        var resets = 0;
        var page = new SettingPage { Title = "General", Items = { group } };
        page.AddHandler(SettingItem.ResetEvent, (_, _) => resets++);
        var settings = new Settings { Width = 800, Height = 520, Items = { page } };
        using var host = Host(settings);
        var reset = page.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ResetButton");
        await Assert.That(items.Any(i => i.IsModified)).IsFalse();
        await Assert.That(reset.IsVisible).IsFalse();

        toggle.IsChecked = true;
        number.Value = 16;
        text.Text = "/bin/zsh";
        combo.SelectedIndex = 1;
        inline.SelectedIndex = 0;
        slider.Value = 0.8;
        host.Flush();
        await Assert.That(items.All(i => i.IsModified)).IsTrue();
        await Assert.That(source.IsChecked == true).IsTrue();
        await Assert.That(reset.IsVisible).IsTrue();

        Click(host, reset);
        await Assert.That(toggle.IsChecked == false).IsTrue();
        await Assert.That(source.IsChecked == false).IsTrue();
        await Assert.That(number.Value).IsEqualTo(14m);
        await Assert.That(text.Text).IsEqualTo("/usr/local/bin/bash");
        await Assert.That(combo.SelectedItem).IsEqualTo("Arial");
        await Assert.That(inline.SelectedIndex).IsEqualTo(1);
        await Assert.That(slider.Value).IsEqualTo(0.5);
        await Assert.That(items.Any(i => i.IsModified)).IsFalse();
        await Assert.That(reset.IsVisible).IsFalse();
        await Assert.That(resets).IsEqualTo(items.Length);
    }

    // item.rs on_reset: content the item does not follow reports IsModified itself and
    // takes the reset through the Reset event or ResetCommand; resettable(false) hides
    // the button.
    [Test]
    public async Task Other_content_resets_through_its_command_and_a_page_can_refuse_the_reset()
    {
        var density = "Compact";
        var item = new SettingItem
        {
            Title = "Density",
            IsModified = true,
            Content = new Button { Content = "Compact" },
        };
        item.ResetCommand = new Command(() =>
        {
            density = "Comfortable";
            item.IsModified = false;
        });
        var page = new SettingPage { Title = "General", Items = { new SettingGroup { Items = { item } } } };
        var settings = new Settings { Width = 800, Height = 520, Items = { page } };
        using var host = Host(settings);
        var reset = page.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_ResetButton");
        await Assert.That(reset.IsVisible).IsTrue();
        page.IsResettable = false;
        host.Flush();
        await Assert.That(reset.IsVisible).IsFalse();
        page.IsResettable = true;
        host.Flush();
        Click(host, reset);
        await Assert.That(density).IsEqualTo("Comfortable");
        await Assert.That(reset.IsVisible).IsFalse();
    }

    // settings.rs with_size: the fields take the settings' size; a field's own classes stay.
    [Test]
    public async Task The_fields_take_the_settings_size()
    {
        var toggle = new ToggleSwitch();
        var button = new Button { Classes = { "outline" }, Content = "Repository..." };
        var settings = new Settings
        {
            Width = 800,
            Height = 520,
            Classes = { "small" },
            Items =
            {
                new SettingPage
                {
                    Title = "General",
                    Items = { new SettingGroup { Items = { new SettingItem { Title = "A", Content = toggle }, new SettingItem { Title = "B", Content = button } } } },
                },
            },
        };
        using var host = Host(settings);
        await Assert.That(toggle.Classes.Contains("small")).IsTrue();
        await Assert.That(button.Classes.Contains("small")).IsTrue();
        settings.Classes.Remove("small");
        settings.Classes.Add("large");
        await Assert.That(toggle.Classes.Contains("small")).IsFalse();
        await Assert.That(toggle.Classes.Contains("large")).IsTrue();
        await Assert.That(button.Classes.Contains("outline")).IsTrue();
        settings.Classes.Remove("large");
        await Assert.That(button.Classes.Contains("large")).IsFalse();
        await Assert.That(button.Classes.Contains("outline")).IsTrue();
    }

    // settings.rs STACKED_LAYOUT_MAX_WIDTH: a page 480px wide or narrower lays its items out
    // vertically; a wider one keeps each item's own orientation.
    [Test]
    public async Task A_narrow_page_stacks_its_items()
    {
        var beside = new SettingItem { Title = "Dark Mode", Content = new ToggleSwitch() };
        var under = new SettingItem { Title = "CLI Path", Orientation = Orientation.Vertical, Content = new TextBox() };
        var settings = new Settings
        {
            Width = 800,
            Height = 520,
            Items = { new SettingPage { Title = "General", Items = { new SettingGroup { Items = { beside, under } } } } },
        };
        using var host = Host(settings);
        await Assert.That(beside.Classes.Contains(":horizontal")).IsTrue();
        await Assert.That(under.Classes.Contains(":vertical")).IsTrue();
        await Assert.That(((TextBox)under.Content!).Bounds.Width).IsEqualTo(beside.Bounds.Width);
        // The group scales both panels with its width (adjust_to_container_size): 690px
        // leaves the page 474px.
        settings.Width = 690;
        host.Flush();
        await Assert.That(Page(settings, 0).Bounds.Width).IsLessThanOrEqualTo(480);
        await Assert.That(beside.Classes.Contains(":vertical")).IsTrue();
        settings.Width = 800;
        host.Flush();
        await Assert.That(beside.Classes.Contains(":horizontal")).IsTrue();
        await Assert.That(((TextBox)under.Content!).Bounds.Width).IsEqualTo(beside.Bounds.Width);
    }

    // menu.rs default_open: the page's IsOpen and its row's open submenu follow each other;
    // a click on a page's row opens it (click_to_open).
    [Test]
    public async Task A_pages_row_opens_with_the_page()
    {
        var settings = SearchFixture();
        using var host = Host(settings);
        var appearance = host.Window.GetVisualDescendants().OfType<SidebarMenuItem>().First(i => i.Label == "Appearance");
        await Assert.That(appearance.IsOpen).IsTrue();
        Page(settings, 1).IsOpen = false;
        host.Flush();
        await Assert.That(appearance.IsOpen).IsFalse();
        ClickRow(host, "Appearance");
        await Assert.That(Page(settings, 1).IsOpen).IsTrue();
        // A page with one group has no rows under it.
        var general = host.Window.GetVisualDescendants().OfType<SidebarMenuItem>().First(i => i.Label == "General");
        await Assert.That(general.ItemCount).IsEqualTo(0);
        // A titled group's row only: the untitled group has none.
        await Assert.That(string.Join(",", appearance.Items.Cast<SidebarMenuItem>().Select(i => i.Label))).IsEqualTo("Colors,Fonts");
    }

    // XAML sets SelectedIndex before it adds the pages: the selection waits for them.
    [Test]
    public async Task The_selected_page_is_kept_while_the_pages_are_added()
    {
        var settings = new Settings { Width = 800, Height = 520, SelectedIndex = 2 };
        settings.Items.Add(new SettingPage { Title = "A", Items = { new SettingGroup { Items = { Item("a") } } } });
        settings.Items.Add(new SettingPage { Title = "B", Items = { new SettingGroup { Items = { Item("b") } } } });
        settings.Items.Add(new SettingPage { Title = "C", Items = { new SettingGroup { Items = { Item("c") } } } });
        using var host = Host(settings);
        await Assert.That(settings.SelectedIndex).IsEqualTo(2);
        await Assert.That(Page(settings, 2).IsEffectivelyVisible).IsTrue();
        await Assert.That(Page(settings, 0).IsEffectivelyVisible).IsFalse();
    }

    private sealed class Command(Action run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
