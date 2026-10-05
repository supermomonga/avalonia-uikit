//! Converts the Lucide icons GPUI Kit ships to filled outlines for Avalonia.
//!
//! GPUI renders an icon by rasterizing its SVG (24x24, 2px round strokes) with
//! resvg. Avalonia's PathIcon fills a geometry, so each stroke is expanded to
//! its outline here with the same stroker resvg uses (tiny-skia).
use anyhow::{Context as _, Result, ensure};
use std::{fmt::Write as _, path::Path};
use tiny_skia_path::PathSegment;

/// GPUI Kit's default component icons (crates/assets/default-icons.txt), the
/// variants of its component `IconName` (crates/component/src/icon.rs).
const DEFAULT_ICONS: &str = include_str!("../vendor/gpui-kit/crates/assets/default-icons.txt");

/// The icons the themes draw, by GPUI IconName file name.
pub const ICONS: [&str; 52] = [
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
];

/// The icon's opaque outline, and the outline of its translucent parts with
/// their opacity (a two-tone icon's faint half, which the theme layers at
/// that opacity).
fn outline(svg: &[u8]) -> Result<(String, Option<(String, f32)>)> {
    let tree = usvg::Tree::from_data(svg, &usvg::Options::default())?;
    // Zero-area anchors at (0,0) and (24,24): they paint nothing but make the
    // geometry's bounds the icon's 24x24 box, so a stretched Viewbox scales
    // the icon exactly as GPUI scales the SVG.
    let anchors = "F1 M0,0L0,0Z M24,24L24,24Z";
    let (mut data, mut faint) = (String::from(anchors), String::from(anchors));
    let mut opacity = None;
    collect(tree.root(), &mut data, &mut faint, &mut opacity)?;
    let faint = (faint.len() > anchors.len()).then(|| (faint, opacity.unwrap_or(1.)));
    Ok((data, faint))
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
/// parts between '-', '_' and '.', each capitalized.
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
pub fn icon_names() -> Vec<(String, String)> {
    let mut names: Vec<(String, String)> = DEFAULT_ICONS
        .lines()
        .filter_map(|line| line.trim().strip_prefix("icons/")?.strip_suffix(".svg"))
        .map(|stem| (variant(stem), stem.to_string()))
        .collect();
    names.sort();
    names
}

/// Writes the icons the themes draw (`ICONS`), then the rest of GPUI's
/// `IconName` icons, as geometry resources, and the `IconName` enum.
pub fn write_xaml(root: &Path) -> Result<()> {
    let icons = root.join("reference/vendor/gpui-kit/crates/assets/assets/icons");
    let mut xaml = String::from(
        "<!--\n  DO NOT EDIT. Generated by `reference generate` from the Lucide icons GPUI Kit\n  ships (crates/assets/assets/icons), stroked to filled outlines.\n  Lucide is ISC licensed: see Icons/LUCIDE-LICENSE.txt.\n-->\n<ResourceDictionary xmlns=\"https://github.com/avaloniaui\"\n                    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n",
    );
    let names = icon_names();
    let mut written = Vec::new();
    let mut faint_opacity = std::collections::BTreeMap::new();
    let extra = names.iter().map(|(_, stem)| stem.as_str()).filter(|stem| !ICONS.contains(stem));
    for name in ICONS.into_iter().chain(extra) {
        if written.contains(&name) {
            continue;
        }
        written.push(name);
        let svg = std::fs::read(icons.join(format!("{name}.svg")))
            .with_context(|| format!("reading icon {name}"))?;
        let key = format!("UIKit.Icon.{}", crate::tokens::pascal(name));
        let (data, faint) = outline(&svg)?;
        let _ = writeln!(xaml, "  <StreamGeometry x:Key=\"{key}\">{data}</StreamGeometry>");
        if let Some((faint, opacity)) = faint {
            let _ = writeln!(xaml, "  <StreamGeometry x:Key=\"{key}.Faint\">{faint}</StreamGeometry>");
            faint_opacity.insert(name, opacity);
        }
    }
    xaml.push_str("</ResourceDictionary>\n");
    let out = root.join("src/AvaloniaUIKit/Themes/Icons/Lucide.g.axaml");
    std::fs::create_dir_all(out.parent().unwrap())?;
    std::fs::write(out, xaml)?;
    write_icon_names(root, &names, &faint_opacity)
}

/// `IconName` for C#: GPUI's variants in GPUI's order, and each one's resource key.
fn write_icon_names(root: &Path, names: &[(String, String)], faint: &std::collections::BTreeMap<&str, f32>) -> Result<()> {
    let mut cs = String::from(
        "// <auto-generated>\n// DO NOT EDIT. Generated by `reference generate` from GPUI Kit's default\n// component icons (crates/assets/default-icons.txt).\n// </auto-generated>\nnamespace AvaloniaUIKit;\n\n\
/// <summary>\n/// GPUI Kit's IconName (crates/component/src/icon.rs): the icons GPUI Kit ships\n/// for its components. <see cref=\"Icon.Kind\"/> draws one; its geometry is the\n/// resource <see cref=\"IconNames.ResourceKey\"/> names.\n/// </summary>\npublic enum IconName\n{\n",
    );
    for (variant, stem) in names {
        // The resource key (tokens::pascal) and GPUI's variant must name the icon alike.
        ensure!(crate::tokens::pascal(stem) == *variant, "icon {stem}: key and variant differ");
        let _ = writeln!(cs, "    /// <summary>icons/{stem}.svg</summary>\n    {variant},");
    }
    cs.push_str("}\n\n/// <summary>The resources of <see cref=\"IconName\"/>s.</summary>\npublic static class IconNames\n{\n");
    cs.push_str("    /// <summary>The key of the icon's geometry resource (Themes/Icons/Lucide.g.axaml).</summary>\n");
    cs.push_str("    public static string ResourceKey(this IconName name) => name switch\n    {\n");
    for (variant, _) in names {
        let _ = writeln!(cs, "        IconName.{variant} => \"UIKit.Icon.{variant}\",");
    }
    cs.push_str("        _ => throw new ArgumentOutOfRangeException(nameof(name)),\n    };\n\n");
    cs.push_str("    /// <summary>\n    /// The opacity of the icon's faint part (the resource with \".Faint\" after\n    /// the key), or 0 when it has none.\n    /// </summary>\n");
    cs.push_str("    internal static double FaintOpacity(this IconName name) => name switch\n    {\n");
    for (variant, stem) in names {
        if let Some(opacity) = faint.get(stem.as_str()) {
            let _ = writeln!(cs, "        IconName.{variant} => {opacity},");
        }
    }
    cs.push_str("        _ => 0,\n    };\n}\n");
    let out = root.join("src/AvaloniaUIKit/Controls/IconName.g.cs");
    std::fs::write(out, cs)?;
    Ok(())
}
