//! `Tag` (crates/component/src/tag.rs): a small bordered label in a theme
//! color or a palette color.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::{Result, bail};
use gpui_kit::{
    IntoElement as _, ParentElement as _,
    component::{ColorName, Sizable as _, tag::{Tag, TagVariant}},
};
use std::rc::Rc;

pub fn color_name(name: &str) -> Option<ColorName> {
    ColorName::all().into_iter().find(|c| c.to_string().eq_ignore_ascii_case(name))
}

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "primary") {
        "primary" => TagVariant::Primary,
        "secondary" => TagVariant::Secondary,
        "danger" => TagVariant::Danger,
        "success" => TagVariant::Success,
        "warning" => TagVariant::Warning,
        "info" => TagVariant::Info,
        other => match color_name(other) {
            Some(color) => TagVariant::Color(color),
            None => bail!("unknown tag variant {other}"),
        },
    };
    let outline = param_bool(params, "outline");
    let rounded_full = param_bool(params, "rounded_full");
    let size = super::size(params);
    let label = param_str(params, "label", "Tag").to_string();
    Ok(Rc::new(move |_, _, _| {
        let mut tag = Tag::new().with_variant(variant.clone()).with_size(size).child(label.clone());
        if outline {
            tag = tag.outline();
        }
        if rounded_full {
            tag = tag.rounded_full();
        }
        tag.into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // tag.rs TagVariant::Color: the palette color's 50 / 200 / 600 scales, and
    // 950 at half / 800 at half / 300 on a dark theme.
    for color in ColorName::all() {
        let name = color.to_string().to_lowercase();
        let (bg, border, fg) = if theme.is_dark() {
            (color.scale(950).opacity(0.5), color.scale(800).opacity(0.5), color.scale(300))
        } else {
            (color.scale(50), color.scale(200), color.scale(600))
        };
        out.push((format!("tag.{name}.background"), bg));
        out.push((format!("tag.{name}.border"), border));
        out.push((format!("tag.{name}.foreground"), fg));
    }
}
