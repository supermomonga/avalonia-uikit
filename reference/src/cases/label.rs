//! `Label` (crates/component/src/label.rs): text on a fixed 1.25rem line,
//! with an optional muted secondary part.
use crate::{
    harness::Builder,
    manifest::{Params, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px, rems,
    component::{ActiveTheme as _, StyledExt as _, label::Label},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let text = param_str(params, "text", "Label").to_string();
    let secondary = params.get("secondary").and_then(|v| v.as_str()).map(str::to_string);
    let text_size = param_str(params, "text_size", "base").to_string();
    let weight = param_str(params, "weight", "normal").to_string();
    let align = param_str(params, "align", "left").to_string();
    let color = param_str(params, "color", "").to_string();
    let relaxed = param_str(params, "leading", "default") == "relaxed";
    let width = params.get("width").and_then(|v| v.as_f64()).map(|w| w as f32);
    Ok(Rc::new(move |_, _, cx| {
        let mut label = Label::new(text.clone());
        if let Some(s) = &secondary {
            label = label.secondary(s.clone());
        }
        label = match text_size.as_str() {
            "xs" => label.text_xs(),
            "sm" => label.text_sm(),
            "lg" => label.text_lg(),
            "xl" => label.text_xl(),
            "2xl" => label.text_2xl(),
            _ => label,
        };
        label = match weight.as_str() {
            "medium" => label.font_medium(),
            "semibold" => label.font_semibold(),
            "bold" => label.font_bold(),
            _ => label,
        };
        label = match align.as_str() {
            "center" => label.text_center(),
            "right" => label.text_right(),
            _ => label,
        };
        label = match color.as_str() {
            "muted" => label.text_color(cx.theme().muted_foreground),
            "danger" => label.text_color(cx.theme().danger),
            _ => label,
        };
        if relaxed {
            label = label.line_height(rems(1.8));
        }
        match width {
            Some(w) => div().w(px(w)).child(label).into_any_element(),
            None => label.into_any_element(),
        }
    }))
}
