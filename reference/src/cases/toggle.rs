//! `Toggle` (crates/component/src/button/toggle.rs).
use super::{disabled, icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{
        Disableable as _, Sizable as _,
        button::{Toggle, ToggleVariant, ToggleVariants as _},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "ghost") {
        "outline" => ToggleVariant::Outline,
        _ => ToggleVariant::Ghost,
    };
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let size = size(params);
    let label = params.get("label").and_then(|v| v.as_str()).map(str::to_string);
    let icon = icon(param_str(params, "icon", ""));
    Ok(Rc::new(move |view, _, cx| {
        let mut toggle = Toggle::new("case")
            .with_variant(variant)
            .with_size(size)
            .checked(checked ^ view.state.toggled)
            .disabled(disabled)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        if let Some(icon) = icon.clone() {
            toggle = toggle.icon(icon);
        }
        if let Some(label) = label.clone() {
            toggle = toggle.label(label);
        }
        toggle.into_any_element()
    }))
}
