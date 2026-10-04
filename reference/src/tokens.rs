//! Dumps the resolved GPUI Kit themes, Default Light / Default Dark and every
//! theme GPUI Kit bundles (`themes.rs`): every ThemeColor field, the token
//! backgrounds the components paint (`fills`, a gradient where the theme gives
//! one), the scalars the components read, the motion tokens, and the derived
//! colors the components compute while painting (see `derived.rs`). Writes them
//! for the tests, for the Avalonia theme (`Palettes.g.cs`) and for the site.
use crate::{cases::Paint, harness::Harness, themes::Bundled};
use anyhow::{Result, bail};
use gpui_kit::{
    Hsla, Rgba,
    component::{ActiveTheme as _, Theme, scroll::ScrollbarMode},
};
use serde_json::{Map, Value, json};
use std::fmt::Write as _;

/// The ThemeColor fields GPUI Kit's theme schema reads as backgrounds, which a
/// theme may give as a gradient (`apply_background_color!` in theme/schema.rs).
/// Components paint them through `theme.tokens.<field>`.
pub const FILLS: [&str; 78] = [
    "background", "muted", "button", "button_hover", "button_active", "primary", "primary_hover",
    "primary_active", "button_primary", "button_primary_hover", "button_primary_active", "secondary",
    "secondary_hover", "secondary_active", "button_secondary", "button_secondary_hover",
    "button_secondary_active", "success", "success_hover", "success_active", "button_success",
    "button_success_hover", "button_success_active", "info", "info_hover", "info_active", "button_info",
    "button_info_hover", "button_info_active", "warning", "warning_hover", "warning_active",
    "button_warning", "button_warning_hover", "button_warning_active", "accent", "accordion", "group_box",
    "danger", "danger_active", "danger_hover", "button_danger", "button_danger_hover",
    "button_danger_active", "description_list_label", "drop_target", "list", "list_active", "list_even",
    "list_head", "list_hover", "popover", "progress_bar", "scrollbar", "scrollbar_thumb",
    "scrollbar_thumb_hover", "selection", "sidebar", "sidebar_accent", "sidebar_primary", "skeleton",
    "slider_bar", "slider_thumb", "switch", "switch_thumb", "tab", "tab_active", "tab_bar",
    "tab_bar_segmented", "table", "table_active", "table_even", "table_head", "table_foot", "table_hover",
    "title_bar", "status_bar", "overlay",
];

pub fn hex(color: Hsla) -> String {
    let c = Rgba::from(color);
    let to8 = |v: f32| (v * 255.0).round() as u8;
    format!(
        "#{:02x}{:02x}{:02x}{:02x}",
        to8(c.r),
        to8(c.g),
        to8(c.b),
        to8(c.a)
    )
}

fn float_rgba(color: Hsla) -> Value {
    let c = Rgba::from(color);
    json!([c.r, c.g, c.b, c.a])
}

/// A gradient as GPUI paints it: two stops along an angle (CSS degrees).
struct Gradient {
    angle: f32,
    stops: [(Hsla, f32); 2],
}

/// The gradient of a paint, or None when it is solid. GPUI keeps the fields
/// private, so they are read back through its serde form.
fn gradient(paint: &Paint) -> Result<Option<Gradient>> {
    if paint.as_solid().is_some() {
        return Ok(None);
    }
    let value = serde_json::to_value(paint)?;
    if value["tag"] != "LinearGradient" {
        bail!("unsupported background {value}");
    }
    if value["color_space"] != "Srgb" {
        bail!("gradients interpolate in sRGB in Avalonia, not in {}", value["color_space"]);
    }
    let stop = |i: usize| -> Result<(Hsla, f32)> {
        let color: Hsla = serde_json::from_value(value["colors"][i]["color"].clone())?;
        let percentage = value["colors"][i]["percentage"].as_f64().unwrap_or_default() as f32;
        Ok((color, percentage))
    };
    Ok(Some(Gradient {
        angle: value["gradient_angle_or_pattern_height"].as_f64().unwrap_or_default() as f32,
        stops: [stop(0)?, stop(1)?],
    }))
}

/// `#rrggbbaa`, or `linear-gradient(180deg, #rrggbbaa 0%, #rrggbbaa 100%)`.
pub fn paint(paint: &Paint) -> Result<String> {
    Ok(match gradient(paint)? {
        None => hex(paint.as_solid().unwrap_or_default()),
        Some(g) => format!(
            "linear-gradient({}deg, {} {}%, {} {}%)",
            g.angle,
            hex(g.stops[0].0),
            g.stops[0].1 * 100.,
            hex(g.stops[1].0),
            g.stops[1].1 * 100.
        ),
    })
}

/// The solid color that stands for a paint: itself, or a gradient's first
/// stop, as GPUI Kit takes for a theme color given as a gradient.
fn representative(paint: &Paint) -> Result<Hsla> {
    Ok(match gradient(paint)? {
        None => paint.as_solid().unwrap_or_default(),
        Some(g) => g.stops[0].0,
    })
}

fn theme_dump(harness: &mut Harness, theme_name: &str) -> Result<Value> {
    harness.set_theme(theme_name, ScrollbarMode::Hover)?;
    harness.cx.update(|cx| -> Result<Value> {
        let theme: &Theme = cx.theme();
        let mut hex_colors = Map::new();
        if let Value::Object(map) = serde_json::to_value(theme.colors)? {
            for (key, value) in map {
                if let Value::String(s) = value {
                    hex_colors.insert(key, Value::String(s));
                }
            }
        }
        let tokens = serde_json::to_value(theme.tokens)?;
        let mut fills = Map::new();
        for field in FILLS {
            let background: Paint = serde_json::from_value(tokens[field]["background"].clone())?;
            fills.insert(field.to_string(), Value::String(paint(&background)?));
        }
        let mut derived_hex = Map::new();
        let mut float_colors = Map::new();
        for (name, color) in &crate::derived::derived(theme) {
            derived_hex.insert(name.clone(), Value::String(paint(color)?));
            float_colors.insert(name.clone(), float_rgba(representative(color)?));
        }
        let motion = &theme.motion;
        Ok(json!({
            "mode": format!("{:?}", theme.mode),
            "colors": hex_colors,
            "derived": derived_hex,
            "derived_float": float_colors,
            "fills": fills,
            "scalars": {
                "font_size": f32::from(theme.font_size),
                "radius": f32::from(theme.radius),
                "radius_lg": f32::from(theme.radius_lg),
                "shadow": theme.shadow,
                "focus_ring": theme.focus_ring,
            },
            "motion": format!("{motion:?}"),
        }))
    })
}

/// Default Light and Default Dark (`tokens/gpui-theme.json`).
pub fn dump(harness: &mut Harness) -> Result<Value> {
    let light = theme_dump(harness, "light")?;
    let dark = theme_dump(harness, "dark")?;
    Ok(json!({
        "gpui_kit": crate::GPUI_KIT_REV,
        "light": light,
        "dark": dark,
    }))
}

/// Every bundled theme (`tokens/gpui-themes.json`), ordered by name.
pub fn dump_bundled(harness: &mut Harness) -> Result<Value> {
    let mut themes = Vec::new();
    let names: Vec<(String, String, String, String, bool)> = harness
        .themes
        .iter()
        .map(|t| (t.name().to_string(), t.slug(), t.family.clone(), t.source.clone(), t.dark()))
        .collect();
    for (name, slug, family, source, dark) in names {
        let mut dump = theme_dump(harness, &slug)?;
        let object = dump.as_object_mut().expect("a theme dump is an object");
        object.remove("derived_float");
        object.remove("motion");
        themes.push(json!({
            "name": name,
            "slug": slug,
            "family": family,
            "source": source,
            "dark": dark,
            "theme": dump,
        }));
    }
    // Leave the harness in Default Light, as before the bundled themes.
    harness.set_theme("light", ScrollbarMode::Hover)?;
    Ok(json!({
        "gpui_kit": crate::GPUI_KIT_REV,
        "themes": themes,
    }))
}

/// `button_primary_hover` -> `ButtonPrimaryHover`; `group-hover` -> `GroupHover`.
pub fn pascal(segment: &str) -> String {
    segment
        .split(['_', '-'])
        .filter(|part| !part.is_empty())
        .map(|part| {
            let mut chars = part.chars();
            match chars.next() {
                Some(first) => first.to_uppercase().collect::<String>() + chars.as_str(),
                None => String::new(),
            }
        })
        .collect()
}

/// The resource key without `UIKit.`: `button.primary.normal.background` -> `Button.Primary.Normal.Background`.
fn resource_name(name: &str) -> String {
    name.split('.').map(pascal).collect::<Vec<_>>().join(".")
}

/// `Ayu Dark` -> `AyuDark`; `macOS Classic Light` -> `MacOSClassicLight`.
fn identifier(name: &str) -> String {
    name.split(|c: char| !c.is_ascii_alphanumeric())
        .filter(|part| !part.is_empty())
        .map(|part| {
            let mut chars = part.chars();
            match chars.next() {
                Some(first) => first.to_ascii_uppercase().to_string() + chars.as_str(),
                None => String::new(),
            }
        })
        .collect()
}

/// `#rrggbbaa` -> `0xAARRGGBB`.
fn argb(hex: &str) -> Result<String> {
    let h = hex.trim_start_matches('#');
    if h.len() != 8 {
        bail!("not a #rrggbbaa color: {hex}");
    }
    Ok(format!("0x{}{}", &h[6..8], &h[0..6]).to_uppercase().replace("0X", "0x"))
}

/// Parses what `paint` wrote back into (representative, gradient).
fn parse_paint(value: &str) -> Result<(String, Option<String>)> {
    let Some(inner) = value.strip_prefix("linear-gradient(").and_then(|v| v.strip_suffix(')')) else {
        return Ok((argb(value)?, None));
    };
    let parts: Vec<&str> = inner.split(", ").collect();
    let [angle, from, to] = parts.as_slice() else {
        bail!("unexpected gradient {value}");
    };
    let stop = |stop: &str| -> Result<(String, String)> {
        let (color, percent) = stop.split_once(' ').unwrap_or((stop, "0%"));
        let percent: f32 = percent.trim_end_matches('%').parse()?;
        Ok((argb(color)?, format!("{}f", percent / 100.)))
    };
    let angle = angle.trim_end_matches("deg");
    let (from_color, from_offset) = stop(from)?;
    let (to_color, to_offset) = stop(to)?;
    Ok((
        from_color.clone(),
        Some(format!("{angle}f, {from_color}, {from_offset}, {to_color}, {to_offset}")),
    ))
}

/// The C# of one palette: its colors in name order, its fills, and its gradients.
fn palette(out: &mut String, field: &str, theme: &Value, color_names: &[String], dark: bool, name: &str) -> Result<()> {
    let mut colors = Vec::new();
    let mut gradients = Vec::new();
    for (index, key) in color_names.iter().enumerate() {
        let value = theme["colors"][key].as_str().or_else(|| theme["derived"][key].as_str());
        let Some(value) = value else { bail!("{name} has no color {key}") };
        let (color, gradient) = parse_paint(value)?;
        colors.push(color);
        if let Some(gradient) = gradient {
            gradients.push(format!("new({index}, {gradient})"));
        }
    }
    let mut fills = Vec::new();
    for (index, field) in FILLS.iter().enumerate() {
        let Some(value) = theme["fills"][field].as_str() else { bail!("{name} has no fill {field}") };
        let (color, gradient) = parse_paint(value)?;
        fills.push(color);
        if let Some(gradient) = gradient {
            gradients.push(format!("new({}, {gradient})", color_names.len() + index));
        }
    }
    let list = |values: &[String]| -> String {
        values.chunks(8).map(|chunk| format!("            {},\n", chunk.join(", "))).collect()
    };
    writeln!(out, "    /// <summary>GPUI Kit's {name}.</summary>")?;
    writeln!(out, "    internal static readonly Palette {field} = new(")?;
    writeln!(out, "        \"{name}\",")?;
    writeln!(out, "        dark: {dark},")?;
    writeln!(out, "        colors:\n        [\n{}        ],", list(&colors))?;
    writeln!(out, "        fills:\n        [\n{}        ],", list(&fills))?;
    if gradients.is_empty() {
        writeln!(out, "        gradients: []);\n")?;
    } else {
        writeln!(out, "        gradients:\n        [\n{}        ]);\n", gradients.iter().map(|g| format!("            {g},\n")).collect::<String>())?;
    }
    Ok(())
}

/// Writes `Palettes.g.cs`: the colors of every theme for the Avalonia theme,
/// and a theme variant for each bundled theme.
pub fn write_csharp(root: &std::path::Path, tokens: &Value, bundled: &Value) -> Result<()> {
    let light = &tokens["light"];
    let mut color_names: Vec<String> = Vec::new();
    for section in ["colors", "derived"] {
        for (name, _) in light[section].as_object().into_iter().flatten() {
            color_names.push(name.clone());
        }
    }
    let mut out = String::new();
    writeln!(
        out,
        "// <auto-generated>\n\
         // DO NOT EDIT. Generated by `reference generate` from GPUI Kit {rev}.\n\
         // Every color is the value GPUI Kit resolves for the theme, including the\n\
         // colors its components derive while painting (reference/src/tokens.rs).\n\
         // </auto-generated>\n\
         using Avalonia.Styling;\n\n\
         namespace AvaloniaUIKit;\n\n\
         internal sealed partial class Palette\n{{",
        rev = crate::GPUI_KIT_REV
    )?;
    writeln!(out, "    /// <summary>The colors' resource keys, in the order of every palette's colors.</summary>")?;
    writeln!(out, "    internal static readonly string[] ColorKeys =\n    [")?;
    for name in &color_names {
        writeln!(out, "        \"UIKit.{}\",", resource_name(name))?;
    }
    writeln!(out, "    ];\n")?;
    writeln!(out, "    /// <summary>The token backgrounds' resource keys, in the order of every palette's fills.</summary>")?;
    writeln!(out, "    internal static readonly string[] FillKeys =\n    [")?;
    for field in FILLS {
        writeln!(out, "        \"UIKit.{}.Fill\",", pascal(field))?;
    }
    writeln!(out, "    ];\n")?;
    palette(&mut out, "DefaultLight", light, &color_names, false, "Default Light")?;
    palette(&mut out, "DefaultDark", &tokens["dark"], &color_names, true, "Default Dark")?;
    let themes = bundled["themes"].as_array().cloned().unwrap_or_default();
    for theme in &themes {
        let name = theme["name"].as_str().unwrap_or_default();
        let dark = theme["dark"].as_bool().unwrap_or_default();
        palette(&mut out, &identifier(name), &theme["theme"], &color_names, dark, name)?;
    }
    writeln!(out, "}}\n")?;
    writeln!(out, "public static partial class UIKitThemeVariants\n{{")?;
    for theme in &themes {
        let name = theme["name"].as_str().unwrap_or_default();
        let dark = theme["dark"].as_bool().unwrap_or_default();
        let (mode, inherit) = if dark { ("dark", "Dark") } else { ("light", "Light") };
        writeln!(out, "    /// <summary>GPUI Kit's {name} theme, a {mode} theme.</summary>")?;
        writeln!(out, "    public static ThemeVariant {} {{ get; }} = new(\"{name}\", ThemeVariant.{inherit});\n", identifier(name))?;
    }
    writeln!(out, "    private static readonly (ThemeVariant Variant, Palette Palette)[] Bundled =\n    [")?;
    for theme in &themes {
        let id = identifier(theme["name"].as_str().unwrap_or_default());
        writeln!(out, "        ({id}, Palette.{id}),")?;
    }
    writeln!(out, "    ];\n}}")?;
    let path = root.join("src/AvaloniaUIKit/Themes/Tokens/Palettes.g.cs");
    std::fs::create_dir_all(path.parent().unwrap())?;
    std::fs::write(&path, out)?;
    Ok(())
}

/// The site's CSS variables (sites/app/style.css) and the theme color each
/// takes, as on gpui-kit.com (website/src/lib/theme-catalog.ts).
const SITE_VARS: [(&str, &str); 31] = [
    ("background", "background"),
    ("foreground", "foreground"),
    ("card", "background"),
    ("card-foreground", "foreground"),
    ("popover", "popover"),
    ("popover-foreground", "popover_foreground"),
    ("primary", "primary"),
    ("primary-foreground", "primary_foreground"),
    ("secondary", "secondary"),
    ("secondary-foreground", "secondary_foreground"),
    ("muted", "muted"),
    ("muted-foreground", "muted_foreground"),
    ("accent", "accent"),
    ("accent-foreground", "accent_foreground"),
    ("destructive", "danger"),
    ("border", "border"),
    ("input", "input"),
    ("ring", "ring"),
    ("sidebar", "sidebar"),
    ("sidebar-foreground", "sidebar_foreground"),
    ("sidebar-accent", "sidebar_accent"),
    ("sidebar-accent-foreground", "sidebar_accent_foreground"),
    ("sidebar-border", "sidebar_border"),
    ("brand", "primary"),
    ("brand-contrast", "primary_foreground"),
    ("selection", "selection"),
    ("success", "success"),
    ("warning", "warning"),
    ("data-1", "cyan"),
    ("data-2", "blue"),
    ("titlebar", "title_bar"),
];

/// The code colors, from the theme file's `highlight` (which GPUI Kit's theme
/// does not resolve into colors).
const SITE_CODE_VARS: [(&str, &str); 7] = [
    ("code-bg", "editor.background"),
    ("code-fg", "editor.foreground"),
    ("code-keyword", "syntax.keyword"),
    ("code-string", "syntax.string"),
    ("code-comment", "syntax.comment"),
    ("code-fn", "syntax.function"),
    ("code-type", "syntax.type"),
];

fn highlight_color(highlight: &Value, key: &str) -> Option<String> {
    let value = highlight.get(key).cloned().or_else(|| {
        key.split('.').try_fold(highlight.clone(), |part, segment| part.get(segment).cloned())
    })?;
    match value {
        Value::String(s) => Some(s),
        Value::Object(o) => o.get("color").and_then(Value::as_str).map(str::to_string),
        _ => None,
    }
}

/// `#rrggbbff` -> `#rrggbb`; other colors as they are.
fn css_color(color: &str) -> String {
    match color.strip_suffix("ff") {
        Some(opaque) if color.len() == 9 => opaque.to_string(),
        _ => color.to_string(),
    }
}

/// Writes the site's themes: `sites/app/lib/themes.g.json`, the bundled themes
/// for the theme palette, and `sites/app/styles/themes.g.css`, the site's colors
/// in each of them under `html[data-theme="<slug>"]`.
pub fn write_site(root: &std::path::Path, bundled: &Value, themes: &[Bundled]) -> Result<()> {
    let mut list = Vec::new();
    let mut css = format!(
        "/*\n * DO NOT EDIT. Generated by `reference generate` from GPUI Kit {rev}.\n \
         * The site's colors (app/style.css) in each theme GPUI Kit bundles, chosen\n \
         * by <html data-theme> (components/theme-palette.tsx), as on gpui-kit.com.\n */\n",
        rev = crate::GPUI_KIT_REV
    );
    for (theme, source) in bundled["themes"].as_array().into_iter().flatten().zip(themes) {
        let colors = &theme["theme"]["colors"];
        let slug = theme["slug"].as_str().unwrap_or_default();
        writeln!(css, "\nhtml[data-theme=\"{slug}\"] {{")?;
        for (var, field) in SITE_VARS {
            if let Some(color) = colors[field].as_str() {
                // GPUI Kit keeps the selection translucent; the site mixes its own (::selection).
                let color = if var == "selection" { &color[..7] } else { color };
                writeln!(css, "  --{var}: {};", css_color(color))?;
            }
        }
        let highlight = source.raw.get("highlight").cloned().unwrap_or(Value::Null);
        for (var, key) in SITE_CODE_VARS {
            if let Some(color) = highlight_color(&highlight, key) {
                writeln!(css, "  --{var}: {};", css_color(&color.to_lowercase()))?;
            }
        }
        writeln!(css, "}}")?;
        let swatch = |field: &str| css_color(colors[field].as_str().unwrap_or_default());
        list.push(json!({
            "id": slug,
            "name": theme["name"],
            "family": theme["family"],
            "mode": if theme["dark"].as_bool().unwrap_or_default() { "dark" } else { "light" },
            "swatch": [swatch("background"), swatch("primary")],
        }));
    }
    let mut text = serde_json::to_string_pretty(&json!({
        "gpui_kit": crate::GPUI_KIT_REV,
        "themes": list,
    }))?;
    text.push('\n');
    std::fs::write(root.join("sites/app/lib/themes.g.json"), text)?;
    std::fs::write(root.join("sites/app/styles/themes.g.css"), css)?;
    Ok(())
}
