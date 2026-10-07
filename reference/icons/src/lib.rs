//! Converts the icons GPUI Kit ships (Lucide's and its own) to filled outlines
//! for Avalonia.
//!
//! GPUI renders an icon by rasterizing its SVG (24x24, 2px round strokes) with
//! resvg. Avalonia's PathIcon fills a geometry, so each stroke is expanded to
//! its outline here with the same stroker resvg uses (tiny-skia).
//!
//! Three files come out (ADR 38):
//! - `Themes/Icons/Lucide.g.axaml`: the icons the theme carries, `ICONS` and
//!   GPUI Kit's component icons.
//! - `Controls/IconName.g.cs`: `IconName`, every icon GPUI Kit ships.
//! - `AvaloniaUIKit.Generators/Icons.g.tsv`: every icon, with the geometry of
//!   the ones the theme does not carry, which the generator adds to the apps
//!   that use them.
use anyhow::{Context as _, Result, ensure};
use std::{collections::BTreeMap, fmt::Write as _, path::Path};
use tiny_skia_path::PathSegment;

/// GPUI Kit's component icons (crates/assets/default-icons.txt), the
/// variants of its component `IconName` (crates/component/src/icon.rs).
const DEFAULT_ICONS: &str = include_str!("../../vendor/gpui-kit/crates/assets/default-icons.txt");

/// The Lucide release GPUI Kit bundles (crates/assets/lucide.json).
const LUCIDE: &str = include_str!("../../vendor/gpui-kit/crates/assets/lucide.json");

/// The icons the themes draw, by GPUI IconName file name.
pub const ICONS: [&str; 61] = [
    "check",
    "minus",
    "plus",
    "chevron-down",
    "chevron-right",
    "chevron-up",
    "chevron-left",
    "chevrons-up-down",
    "sort-ascending",
    "sort-descending",
    "loader",
    "loader-circle",
    "copy",
    "external-link",
    "close",
    "eye",
    "eye-off",
    "calendar",
    "inbox",
    "search",
    "folder",
    "folder-open",
    "file",
    "info",
    "circle-check",
    "triangle-alert",
    "circle-x",
    "panel-left-open",
    "panel-left-close",
    "panel-right-open",
    "panel-right-close",
    "window-minimize",
    "window-maximize",
    "window-restore",
    "window-close",
    "undo-2",
    "redo-2",
    "ellipsis",
    "bell",
    "star",
    "star-fill",
    "user",
    // ColorSelect: an icon in place of the swatch.
    "palette",
    // Dock (AvaloniaUIKit.Dock): the dock toggles, zoom and pinning.
    "panel-left",
    "panel-right",
    "panel-top",
    "panel-bottom",
    "panel-bottom-open",
    "maximize",
    "minimize",
    "pin",
    "pin-off",
    // uikit:Sidebar: the sidebar story's items, header and footer.
    "square-terminal",
    "bot",
    "book-open",
    "frame",
    "chart-pie",
    "map",
    "settings-2",
    "gallery-vertical-end",
    "circle-user",
];

/// An icon's opaque outline, and the outline of its translucent parts with
/// their opacity (a two-tone icon's faint half, which the theme layers at
/// that opacity).
struct Outline {
    data: String,
    faint: Option<(String, f32)>,
}

fn outline(svg: &[u8]) -> Result<Outline> {
    let tree = usvg::Tree::from_data(svg, &usvg::Options::default())?;
    // Zero-area anchors at (0,0) and (24,24): they paint nothing but make the
    // geometry's bounds the icon's 24x24 box, so a stretched Viewbox scales
    // the icon exactly as GPUI scales the SVG.
    let anchors = "F1 M0,0L0,0Z M24,24L24,24Z";
    let (mut data, mut faint) = (String::from(anchors), String::from(anchors));
    let mut opacity = None;
    collect(tree.root(), &mut data, &mut faint, &mut opacity)?;
    let faint = (faint.len() > anchors.len()).then(|| (faint, opacity.unwrap_or(1.)));
    Ok(Outline { data, faint })
}

fn collect(group: &usvg::Group, data: &mut String, faint: &mut String, opacity: &mut Option<f32>) -> Result<()> {
    for node in group.children() {
        match node {
            // usvg wraps a path with an opacity in a group of that opacity.
            usvg::Node::Group(inner) if inner.opacity().get() < 1.0 => {
                let o = inner.opacity().get();
                ensure!(opacity.is_none_or(|known| known == o), "an icon's faint parts differ in opacity");
                *opacity = Some(o);
                collect(inner, faint, &mut String::new(), &mut None)?
            }
            usvg::Node::Group(inner) => collect(inner, data, faint, opacity)?,
            usvg::Node::Path(path) => {
                let transform = path.abs_transform();
                if let Some(stroke) = path.stroke() {
                    let outline = path
                        .data()
                        .stroke(&stroke.to_tiny_skia(), 1.0)
                        .context("stroking an icon path")?
                        .transform(transform)
                        .context("transforming an icon path")?;
                    append(&outline, data);
                }
                if path.fill().is_some() {
                    let fill = path.data().clone().transform(transform).context("transforming")?;
                    append(&fill, data);
                }
            }
            _ => {}
        }
    }
    Ok(())
}

fn n(v: f32) -> String {
    let s = format!("{:.4}", v);
    let s = s.trim_end_matches('0').trim_end_matches('.');
    if s == "-0" { "0".into() } else { s.into() }
}

fn append(path: &tiny_skia_path::Path, data: &mut String) {
    for segment in path.segments() {
        let _ = match segment {
            PathSegment::MoveTo(p) => write!(data, " M{},{}", n(p.x), n(p.y)),
            PathSegment::LineTo(p) => write!(data, " L{},{}", n(p.x), n(p.y)),
            PathSegment::QuadTo(c, p) => write!(data, " Q{},{} {},{}", n(c.x), n(c.y), n(p.x), n(p.y)),
            PathSegment::CubicTo(c1, c2, p) => write!(
                data,
                " C{},{} {},{} {},{}",
                n(c1.x), n(c1.y), n(c2.x), n(c2.y), n(p.x), n(p.y)
            ),
            PathSegment::Close => write!(data, " Z"),
        };
    }
}

/// GPUI's IconName variant for an icon file (crates/assets/build.rs): the
/// parts between '-', '_' and '.', each capitalized. The resource key is
/// `UIKit.Icon.<variant>`.
fn variant(stem: &str) -> String {
    stem.split(['-', '_', '.'])
        .filter(|part| !part.is_empty())
        .map(|part| {
            let mut chars = part.chars();
            let first = chars.next().unwrap();
            format!("{}{}", first.to_ascii_uppercase(), chars.as_str().to_ascii_lowercase())
        })
        .collect()
}

/// GPUI Kit's component `IconName`, in its variant order (GPUI generates the
/// enum from a map keyed by variant): (variant, icon file stem).
pub fn component_icon_names() -> Vec<(String, String)> {
    let mut names: Vec<(String, String)> = DEFAULT_ICONS
        .lines()
        .filter_map(|line| line.trim().strip_prefix("icons/")?.strip_suffix(".svg"))
        .map(|stem| (variant(stem), stem.to_string()))
        .collect();
    names.sort();
    names
}

/// Every icon GPUI Kit ships, the shared `IconName` (crates/assets/build.rs),
/// in its variant order: (variant, icon file stem).
fn all_icon_names(icons: &Path) -> Result<Vec<(String, String)>> {
    let mut names = Vec::new();
    for entry in std::fs::read_dir(icons).with_context(|| format!("reading {}", icons.display()))? {
        let path = entry?.path();
        if path.extension().and_then(|ext| ext.to_str()) != Some("svg") {
            continue;
        }
        let stem = path.file_stem().and_then(|stem| stem.to_str()).context("icon file name")?;
        names.push((variant(stem), stem.to_string()));
    }
    names.sort();
    ensure!(names.windows(2).all(|pair| pair[0].0 != pair[1].0), "two icons share a variant");
    // Icon.Kind's default; GPUI has no such icon.
    ensure!(names.iter().all(|(variant, _)| variant != "None"), "an icon is named None");
    Ok(names)
}

fn lucide_version() -> Result<&'static str> {
    let rest = LUCIDE.split_once("\"version\": \"").context("lucide.json version")?.1;
    Ok(rest.split_once('"').context("lucide.json version")?.0)
}

/// Writes the theme's icons (`Lucide.g.axaml`), `IconName` and the
/// generator's icons from GPUI Kit's SVGs.
pub fn write(root: &Path) -> Result<()> {
    let dir = root.join("reference/vendor/gpui-kit/crates/assets/assets/icons");
    let all = all_icon_names(&dir)?;
    let component = component_icon_names();
    let mut outlines = BTreeMap::new();
    for (_, stem) in &all {
        let svg = std::fs::read(dir.join(format!("{stem}.svg"))).with_context(|| format!("reading icon {stem}"))?;
        outlines.insert(stem.as_str(), outline(&svg).with_context(|| format!("outlining icon {stem}"))?);
    }
    // The theme carries the icons it draws, then GPUI Kit's component icons.
    let mut bundled: Vec<&str> = Vec::new();
    for stem in ICONS.into_iter().chain(component.iter().map(|(_, stem)| stem.as_str())) {
        ensure!(outlines.contains_key(stem), "no icon file {stem}.svg");
        if !bundled.contains(&stem) {
            bundled.push(stem);
        }
    }
    write_xaml(root, &bundled, &outlines)?;
    write_generator_icons(root, &all, &bundled, &outlines)?;
    write_icon_names(root, &all, &bundled, &component, &outlines)
}

/// The icons the theme carries, as geometry resources.
fn write_xaml(root: &Path, bundled: &[&str], outlines: &BTreeMap<&str, Outline>) -> Result<()> {
    let mut xaml = String::from(
        "<!--\n  DO NOT EDIT. Generated by `uikit-icons` (reference/icons) from the icons GPUI\n  Kit ships (crates/assets/assets/icons), stroked to filled outlines: the ones\n  the theme carries. The generator (AvaloniaUIKit.Generators) adds the others.\n  Lucide is ISC licensed: see Icons/LUCIDE-LICENSE.txt.\n-->\n<ResourceDictionary xmlns=\"https://github.com/avaloniaui\"\n                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n",
    );
    for stem in bundled {
        let key = format!("UIKit.Icon.{}", variant(stem));
        let outline = &outlines[stem];
        let _ = writeln!(xaml, "  <StreamGeometry x:Key=\"{key}\">{}</StreamGeometry>", outline.data);
        if let Some((faint, _)) = &outline.faint {
            let _ = writeln!(xaml, "  <StreamGeometry x:Key=\"{key}.Faint\">{faint}</StreamGeometry>");
        }
    }
    xaml.push_str("</ResourceDictionary>\n");
    let out = root.join("src/AvaloniaUIKit/Themes/Icons/Lucide.g.axaml");
    std::fs::create_dir_all(out.parent().unwrap())?;
    std::fs::write(out, xaml)?;
    Ok(())
}

/// Every icon, one a line, for the generator to add the ones the theme does
/// not carry to the apps that use them.
fn write_generator_icons(
    root: &Path,
    all: &[(String, String)],
    bundled: &[&str],
    outlines: &BTreeMap<&str, Outline>,
) -> Result<()> {
    let mut tsv = format!(
        "# DO NOT EDIT. Generated by `uikit-icons` (reference/icons) from the icons GPUI\n# Kit ships (crates/assets/assets/icons, Lucide {}), stroked to filled outlines.\n# Lucide is ISC licensed: see THIRD-PARTY-NOTICES.md.\n# Every IconName, one a line: the ones the theme carries (Themes/Icons/Lucide.g.axaml)\n# by name alone, the others with their geometry:\n# <IconName> [TAB <geometry> [TAB <faint opacity> TAB <faint geometry>]]\n",
        lucide_version()?
    );
    for (variant, stem) in all {
        if bundled.contains(&stem.as_str()) {
            let _ = writeln!(tsv, "{variant}");
            continue;
        }
        let outline = &outlines[stem.as_str()];
        let _ = write!(tsv, "{variant}\t{}", outline.data);
        if let Some((faint, opacity)) = &outline.faint {
            let _ = write!(tsv, "\t{opacity}\t{faint}");
        }
        tsv.push('\n');
    }
    let out = root.join("src/AvaloniaUIKit.Generators/Icons.g.tsv");
    std::fs::create_dir_all(out.parent().unwrap())?;
    std::fs::write(out, tsv)?;
    Ok(())
}

/// `IconName` for C#: `None`, then GPUI's variants in GPUI's order, each
/// one's resource key and faint opacity, and the icons the theme carries.
fn write_icon_names(
    root: &Path,
    all: &[(String, String)],
    bundled: &[&str],
    component: &[(String, String)],
    outlines: &BTreeMap<&str, Outline>,
) -> Result<()> {
    let mut cs = format!(
        "// <auto-generated>\n// DO NOT EDIT. Generated by `uikit-icons` (reference/icons) from the icons GPUI\n// Kit ships (crates/assets/assets/icons, Lucide {}).\n// </auto-generated>\nnamespace AvaloniaUIKit;\n\n\
/// <summary>\n/// GPUI Kit's IconName (crates/assets): every icon GPUI Kit ships, Lucide's and\n/// its own. <see cref=\"Icon.Kind\"/> draws one; its geometry is the resource\n/// <see cref=\"IconNames.ResourceKey\"/> names. The theme carries the icons in\n/// <see cref=\"IconNames.Bundled\"/>; the generator that comes with the package\n/// adds the others an app uses to the app.\n/// </summary>\npublic enum IconName\n{{\n    /// <summary>No icon: an <see cref=\"Icon\"/> leaves its Data to the app.</summary>\n    None,\n",
        lucide_version()?
    );
    for (variant, stem) in all {
        let _ = writeln!(cs, "    /// <summary>icons/{stem}.svg</summary>\n    {variant},");
    }
    cs.push_str("}\n\n/// <summary>The resources of <see cref=\"IconName\"/>s.</summary>\npublic static class IconNames\n{\n");
    cs.push_str("    /// <summary>The key of the icon's geometry resource, <c>UIKit.Icon.&lt;name&gt;</c>.</summary>\n");
    cs.push_str("    public static string ResourceKey(this IconName name) => name switch\n    {\n");
    for (variant, _) in all {
        let _ = writeln!(cs, "        IconName.{variant} => \"UIKit.Icon.{variant}\",");
    }
    cs.push_str("        _ => throw new ArgumentOutOfRangeException(nameof(name)),\n    };\n\n");
    cs.push_str("    /// <summary>\n    /// The opacity of the icon's faint part (the resource with \".Faint\" after\n    /// the key), or 0 when it has none.\n    /// </summary>\n");
    cs.push_str("    internal static double FaintOpacity(this IconName name) => name switch\n    {\n");
    for (variant, stem) in all {
        if let Some((_, opacity)) = &outlines[stem.as_str()].faint {
            let _ = writeln!(cs, "        IconName.{variant} => {opacity},");
        }
    }
    cs.push_str("        _ => 0,\n    };\n\n");
    cs.push_str("    /// <summary>\n    /// The icons the theme carries (Themes/Icons/Lucide.g.axaml): GPUI Kit's\n    /// component icons and the few more its controls draw. They draw in any app;\n    /// the generator adds the others to the apps that use them.\n    /// </summary>\n");
    cs.push_str("    public static IReadOnlyList<IconName> Bundled { get; } =\n    [\n");
    for (variant, stem) in all {
        if bundled.contains(&stem.as_str()) {
            let _ = writeln!(cs, "        IconName.{variant},");
        }
    }
    cs.push_str("    ];\n\n");
    cs.push_str("    /// <summary>GPUI Kit's component icons (crates/assets/default-icons.txt), in its order.</summary>\n");
    cs.push_str("    internal static IReadOnlyList<IconName> Component { get; } =\n    [\n");
    for (variant, _) in component {
        let _ = writeln!(cs, "        IconName.{variant},");
    }
    cs.push_str("    ];\n}\n");
    let out = root.join("src/AvaloniaUIKit/Controls/IconName.g.cs");
    std::fs::write(out, cs)?;
    Ok(())
}
