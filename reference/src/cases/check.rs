//! `Checkbox` (checkbox.rs), `Radio` (radio.rs) and `Switch` (switch.rs).
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{
        Colorize as _, Disableable as _, Sizable as _, Theme, checkbox::Checkbox, radio::Radio,
        switch::Switch,
    },
};
use std::rc::Rc;

fn label(params: &Params) -> Option<String> {
    params.get("label").and_then(|v| v.as_str()).map(str::to_string)
}

pub fn checkbox(params: &Params) -> Result<Builder> {
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let size = size(params);
    let label = label(params);
    Ok(Rc::new(move |view, _, cx| {
        let mut checkbox = Checkbox::new("case")
            .checked(checked ^ view.state.toggled)
            .disabled(disabled)
            .with_size(size)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        if let Some(label) = label.clone() {
            checkbox = checkbox.label(label);
        }
        checkbox.into_any_element()
    }))
}

pub fn radio(params: &Params) -> Result<Builder> {
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let size = size(params);
    let label = label(params);
    Ok(Rc::new(move |view, _, cx| {
        let mut radio = Radio::new("case")
            .checked(checked ^ view.state.toggled)
            .disabled(disabled)
            .with_size(size)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        if let Some(label) = label.clone() {
            radio = radio.label(label);
        }
        radio.into_any_element()
    }))
}

pub fn switch(params: &Params) -> Result<Builder> {
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let size = size(params);
    let label = label(params);
    Ok(Rc::new(move |view, _, cx| {
        let mut switch = Switch::new("case")
            .checked(checked ^ view.state.toggled)
            .disabled(disabled)
            .with_size(size)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        if let Some(label) = label.clone() {
            switch = switch.label(label);
        }
        switch.into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    let input_background = theme.input_background();
    out.push(("input-background".into(), input_background.into()));
    // checkbox.rs / radio.rs: disabled indicators fade their border (and a
    // checked fill) to half; the mark fades to half too.
    out.push(("check.disabled.border".into(), theme.input.opacity(0.5).into()));
    out.push(("check.disabled.checked".into(), theme.primary.opacity(0.5).into()));
    out.push(("check.disabled.mark".into(), theme.primary_foreground.opacity(0.5).into()));
    // radio.rs computes the checked disabled fill from `input` at half, then halves it again
    // only when unchecked; a checked disabled radio uses primary at half.
    // switch.rs: a disabled track fades to half; the thumb does not.
    let unchecked: gpui_kit::Background = theme.tokens.switch.into();
    let checked: gpui_kit::Background = theme.tokens.primary.into();
    out.push(("switch.disabled.track".into(), unchecked.opacity(0.5)));
    out.push(("switch.disabled.checked-track".into(), checked.opacity(0.5)));
    let _ = input_background.mix_oklab(theme.transparent, 1.0);
}
