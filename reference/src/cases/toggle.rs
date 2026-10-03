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
        button::{Toggle, ToggleGroup, ToggleVariant, ToggleVariants as _},
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

/// `ToggleGroup` (toggle.rs): toggles in a row, 8px apart or joined
/// (`segmented`). `checked` marks checked toggles with '1' ("010"); a click
/// flips the clicked one, as an app following on_click would.
pub fn group(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "ghost") {
        "outline" => ToggleVariant::Outline,
        _ => ToggleVariant::Ghost,
    };
    let size = size(params);
    let disabled = disabled(params);
    let segmented = param_bool(params, "segmented");
    let split = |key: &str| -> Vec<String> {
        param_str(params, key, "").split(',').filter(|s| !s.is_empty()).map(str::to_string).collect()
    };
    let labels = split("labels");
    let icons: Vec<_> = split("icons").iter().filter_map(|n| icon(n)).collect();
    let initial: u32 = param_str(params, "checked", "")
        .bytes()
        .enumerate()
        .fold(0, |bits, (i, c)| bits | (((c == b'1') as u32) << i));
    Ok(Rc::new(move |view, _, cx| {
        let bits = initial ^ (view.state.value as u32);
        let mut group = ToggleGroup::new("case").with_variant(variant).with_size(size).disabled(disabled);
        if segmented {
            group = group.segmented();
        }
        for ix in 0..labels.len().max(icons.len()) {
            let mut toggle = Toggle::new(ix).checked(bits & (1 << ix) != 0);
            if let Some(icon) = icons.get(ix) {
                toggle = toggle.icon(icon.clone());
            }
            if let Some(label) = labels.get(ix) {
                toggle = toggle.label(label.clone());
            }
            group = group.child(toggle);
        }
        group
            .on_click(cx.listener(move |view, next: &Vec<bool>, _, cx| {
                let next_bits = next.iter().enumerate().fold(0u32, |b, (i, c)| b | ((*c as u32) << i));
                view.state.value = (next_bits ^ initial) as f32;
                cx.notify();
            }))
            .into_any_element()
    }))
}
