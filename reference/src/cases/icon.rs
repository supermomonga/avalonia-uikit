//! `Icon` (crates/component/src/icon.rs) with the SVGs GPUI Kit ships.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, Styled as _, radians,
    component::{ActiveTheme as _, Icon, Sizable as _},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let name = param_str(params, "icon", "check").to_string();
    let sized = params.contains_key("size");
    let size = super::size(params);
    let color = param_str(params, "color", "").to_string();
    let rotate = param_f32(params, "rotate", 0.);
    Ok(Rc::new(move |_, _, cx| {
        let mut icon = Icon::empty().path(format!("icons/{name}.svg"));
        if sized {
            icon = icon.with_size(size);
        }
        icon = match color.as_str() {
            "muted-foreground" => icon.text_color(cx.theme().muted_foreground),
            "primary" => icon.text_color(cx.theme().primary),
            "danger" => icon.text_color(cx.theme().danger),
            _ => icon,
        };
        if rotate != 0. {
            icon = icon.rotate(radians(rotate.to_radians()));
        }
        icon.into_any_element()
    }))
}
