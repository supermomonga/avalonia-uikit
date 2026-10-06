using System.Globalization;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>
/// What the catalog remembers between runs: the component, the theme, the panels.
/// A file of <c>key=value</c> lines in the user's application data.
/// </summary>
public sealed class CatalogSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AvaloniaUIKit", "ControlCatalog", "settings.txt");

    /// <summary>The slug of the component shown last.</summary>
    public string? Component { get; set; }

    /// <summary>The GPUI Kit theme (<c>Default Dark</c>, <c>Tokyo Night</c>), or null to follow the system.</summary>
    public string? Theme { get; set; }

    /// <summary>Whether the sidebar is collapsed.</summary>
    public bool IsSidebarCollapsed { get; set; }

    /// <summary>Whether the property grid is shown.</summary>
    public bool IsInspectorOpen { get; set; } = true;

    /// <summary>The property grid's width.</summary>
    public double InspectorWidth { get; set; } = 360;

    public static CatalogSettings Load()
    {
        var settings = new CatalogSettings();
        try
        {
            if (!File.Exists(FilePath))
            {
                return settings;
            }
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }
                var value = line[(equals + 1)..];
                switch (line[..equals])
                {
                    case "component":
                        settings.Component = value;
                        break;
                    case "theme":
                        settings.Theme = value.Length == 0 ? null : value;
                        break;
                    case "sidebar-collapsed":
                        settings.IsSidebarCollapsed = value == "true";
                        break;
                    case "inspector-open":
                        settings.IsInspectorOpen = value != "false";
                        break;
                    case "inspector-width" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var width):
                        settings.InspectorWidth = width;
                        break;
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        return settings;
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath,
            [
                $"component={Component}",
                $"theme={Theme}",
                $"sidebar-collapsed={(IsSidebarCollapsed ? "true" : "false")}",
                $"inspector-open={(IsInspectorOpen ? "true" : "false")}",
                $"inspector-width={InspectorWidth.ToString(CultureInfo.InvariantCulture)}",
            ]);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
