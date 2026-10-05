//! `GroupBox` (group_box.rs) with its footer, `Separator` (separator.rs) with
//! its label, and `Link` (link.rs).
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        group_box::{GroupBox, GroupBoxVariant, GroupBoxVariants as _},
        link::Link,
        separator::Separator,
    },
};
use std::rc::Rc;

pub fn group_box(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "normal") {
        "fill" => GroupBoxVariant::Fill,
        "outline" => GroupBoxVariant::Outline,
        _ => GroupBoxVariant::Normal,
    };
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let content = param_str(params, "content", "Content").to_string();
    let footer = params.get("footer").and_then(|v| v.as_str()).map(str::to_string);
    let width = param_f32(params, "width", 240.);
    Ok(Rc::new(move |_, _, _| {
        let mut group = GroupBox::new().with_variant(variant).child(content.clone());
        if let Some(title) = title.clone() {
            group = group.title(title);
        }
        if let Some(footer) = footer.clone() {
            group = group.footer(footer);
        }
        div().w(px(width)).child(group).into_any_element()
    }))
}

pub fn separator(params: &Params) -> Result<Builder> {
    let vertical = param_bool(params, "vertical");
    let dashed = param_bool(params, "dashed");
    let length = param_f32(params, "length", 160.);
    let label = params.get("label").and_then(|v| v.as_str()).map(str::to_string);
    Ok(Rc::new(move |_, _, _| {
        let mut separator = match (vertical, dashed) {
            (false, false) => Separator::horizontal(),
            (false, true) => Separator::horizontal_dashed(),
            (true, false) => Separator::vertical(),
            (true, true) => Separator::vertical_dashed(),
        };
        if let Some(label) = label.clone() {
            separator = separator.label(label);
        }
        if vertical {
            div().h(px(length)).flex().child(separator).into_any_element()
        } else {
            div().w(px(length)).flex().flex_col().child(separator).into_any_element()
        }
    }))
}

pub fn link(params: &Params) -> Result<Builder> {
    let label = param_str(params, "label", "Documentation").to_string();
    Ok(Rc::new(move |_, _, _| {
        Link::new("case").child(label.clone()).into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    // link.rs: the text fades on hover and press; its underline is at half until then.
    out.push(("link.underline".into(), theme.link.opacity(0.5).into()));
    out.push(("link.hover".into(), theme.link.opacity(0.8).into()));
    out.push(("link.pressed".into(), theme.link.opacity(0.6).into()));
}
