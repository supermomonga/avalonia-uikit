//! `Icon` (crates/component/src/icon.rs) with the SVGs GPUI Kit ships.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px, radians,
    component::{ActiveTheme as _, Icon, Sizable as _},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    if param_bool(params, "grid") {
        return grid(params);
    }
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

/// Every icon of GPUI Kit's component IconName in its variant order, 12 to a
/// row with gap_2 (8px) between them.
fn grid(params: &Params) -> Result<Builder> {
    let size = super::size(params);
    let side = match param_str(params, "size", "medium") {
        "xsmall" => 12.,
        "small" => 14.,
        "large" => 24.,
        _ => 16.,
    };
    let names: Vec<String> = uikit_icons::component_icon_names().into_iter().map(|(_, stem)| stem).collect();
    Ok(Rc::new(move |_, _, _| {
        div()
            .w(px(12. * side + 11. * 8.))
            .flex()
            .flex_wrap()
            .gap_2()
            .children(names.iter().map(|stem| Icon::empty().path(format!("icons/{stem}.svg")).with_size(size)))
            .into_any_element()
    }))
}
