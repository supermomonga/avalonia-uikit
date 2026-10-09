using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>The adapter for uikit:Settings and its pages, groups and items.</summary>
public static partial class Adapters
{
    /// <summary>
    /// reference/src/cases/settings.rs as a uikit:Settings: the same pages,
    /// groups, items and values, the fields as the standard controls the theme
    /// gives GPUI's look (ToggleSwitch, CheckBox, an outline DropDownButton, a
    /// NumericUpDown, a TextBox, an outline Button).
    /// </summary>
    private static Settings SettingsCase(GoldenCase c)
    {
        var dirty = c.Bool("dirty");
        var disabled = c.Bool("disabled");
        var resettable = !c.Has("resettable") || c.Bool("resettable");
        static Avalonia.Media.Geometry Lucide(string name) =>
            (Avalonia.Media.Geometry)Avalonia.Application.Current!.FindResource("UIKit.Icon." + name)!;

        var font = new SettingGroup
        {
            Title = "Font",
            Footer = "Font preferences apply to this window only.",
            Items =
            {
                new SettingItem
                {
                    Title = "Font Family",
                    Description = "Select the font family.",
                    Content = new DropDownButton
                    {
                        Classes = { "outline" },
                        Content = "Arial",
                        Flyout = new MenuFlyout
                        {
                            Placement = PlacementMode.BottomEdgeAlignedRight,
                            ItemsSource = new[] { "Arial", "Helvetica", "Times New Roman" },
                        },
                    },
                },
                new SettingItem
                {
                    Title = "Font Size",
                    Description = "Adjust the font size between 8 and 72.",
                    DefaultValue = 14m,
                    Content = new NumericUpDown { Value = dirty ? 16 : 14, Minimum = 8, Maximum = 72 },
                },
            },
        };
        if (c.Has("group_variant"))
        {
            font.Variant = Enum.Parse<GroupBoxVariant>(c.Str("group_variant"), ignoreCase: true);
        }

        var general = new SettingPage
        {
            Title = "General",
            Icon = Lucide("Settings2"),
            IsOpen = true,
            IsResettable = resettable,
            Items =
            {
                new SettingGroup
                {
                    Title = "Appearance",
                    Items =
                    {
                        new SettingItem
                        {
                            Title = "Dark Mode",
                            Description = "Switch between light and dark themes.",
                            DefaultValue = false,
                            IsEnabled = !disabled,
                            Content = new ToggleSwitch { IsChecked = dirty },
                        },
                        new SettingItem
                        {
                            Title = "Auto Switch Theme",
                            Description = "Automatically switch theme based on system settings.",
                            DefaultValue = false,
                            IsEnabled = !disabled,
                            Content = new CheckBox { IsChecked = false },
                        },
                    },
                },
                font,
                new SettingGroup
                {
                    Title = "Other",
                    Items =
                    {
                        new SettingItem
                        {
                            Title = "CLI Path",
                            Description = "Path to the CLI executable.",
                            Keywords = "shell, terminal",
                            Orientation = Orientation.Vertical,
                            DefaultValue = "/usr/local/bin/bash",
                            Content = new TextBox { Text = "/usr/local/bin/bash" },
                        },
                    },
                },
            },
        };
        var update = new SettingPage
        {
            Title = "Software Update",
            Icon = Lucide("Cpu"),
            IsResettable = resettable,
            Items =
            {
                new SettingGroup
                {
                    Title = "Updates",
                    Items =
                    {
                        new SettingItem
                        {
                            Title = "Auto Update",
                            Description = "Download and install new versions as they come out.",
                            DefaultValue = true,
                            Content = new ToggleSwitch { IsChecked = true },
                        },
                    },
                },
            },
        };
        var version = new TextBlock { Text = "Version 0.1.0", FontSize = 14, LineHeight = 20 };
        version[!TextBlock.ForegroundProperty] = new DynamicResourceExtension("UIKit.MutedForeground");
        var about = new SettingPage
        {
            Title = "About",
            Icon = Lucide("Info"),
            Description = "The application and where to find out more.",
            Items =
            {
                new SettingGroup
                {
                    Items =
                    {
                        new SettingItem
                        {
                            Content = new StackPanel
                            {
                                Spacing = 4,
                                Children = { new TextBlock { Text = "GPUI Kit", FontSize = 16, LineHeight = 26 }, version },
                            },
                        },
                    },
                },
                new SettingGroup
                {
                    Title = "Links",
                    Items =
                    {
                        new SettingItem
                        {
                            Title = "GitHub Repository",
                            Description = "Open the GitHub repository.",
                            Content = new Button { Classes = { "outline" }, Content = "Repository..." },
                        },
                    },
                },
            },
        };

        var settings = new Settings
        {
            Width = c.Num("width", 800),
            Height = c.Num("height", 520),
            GroupVariant = Enum.Parse<GroupBoxVariant>(c.Str("variant", "normal"), ignoreCase: true),
            SelectedIndex = (int)c.Num("page", 0),
            Items = { general, update, about },
        };
        ClassFrom(settings, c, "size", "medium");
        return settings;
    }
}
