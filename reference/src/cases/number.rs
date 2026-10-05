//! `NumberInput` (crates/component/src/input/number_input.rs).
use super::{disabled, icon, size};
use crate::{harness::Builder, manifest::{Params, param_f32, param_str}};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, SharedString, Styled as _, px,
    component::{
        Colorize as _, Disableable as _, Icon, Sizable as _, Theme,
        button::{Button, ButtonVariants as _},
        input::{InputState, MaskPattern, NumberInput},
    },
};
use std::rc::Rc;

/// `value` in a NumberInput; uikit-number adds the state's `step`, `min`,
/// `max`, and a MaskPattern::Number with `separator` and `fraction`.
/// `prefix` / `suffix` are text; `prefix_icon` a small icon and `suffix_icon` a
/// text xsmall icon button (the story's info action).
pub fn builder(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let value = param_str(params, "value", "42").to_string();
    let width = param_f32(params, "width", 160.);
    let prefix = param_str(params, "prefix", "").to_string();
    let suffix = param_str(params, "suffix", "").to_string();
    let prefix_icon = icon(param_str(params, "prefix_icon", ""));
    let suffix_icon = icon(param_str(params, "suffix_icon", ""));
    let number = |key: &str| params.get(key).and_then(serde_json::Value::as_f64);
    let (step, min, max) = (number("step"), number("min"), number("max"));
    let separator = param_str(params, "separator", "").chars().next();
    let fraction = params.get("fraction").and_then(serde_json::Value::as_u64).map(|f| f as usize);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let value = value.clone();
            let state = cx.new(|cx| {
                let mut state = InputState::new(window, cx).default_value(value);
                if let Some(step) = step {
                    state = state.step(step);
                }
                if let Some(min) = min {
                    state = state.min(min);
                }
                if let Some(max) = max {
                    state = state.max(max);
                }
                if separator.is_some() {
                    state = state.mask_pattern(MaskPattern::Number { separator, fraction });
                }
                state
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<InputState>().ok())
            .expect("input state");
        let mut number = NumberInput::new(&state).with_size(size).disabled(disabled).w(px(width));
        if let Some(name) = prefix_icon.clone() {
            number = number.prefix(Icon::new(name).small());
        } else if !prefix.is_empty() {
            number = number.prefix(SharedString::from(prefix.clone()));
        }
        if let Some(name) = suffix_icon.clone() {
            number = number.suffix(Button::new("suffix").text().icon(name).xsmall());
        } else if !suffix.is_empty() {
            number = number.suffix(SharedString::from(suffix.clone()));
        }
        number.into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    // number_input.rs: the step buttons tint to the frame on hover and press;
    // a disabled frame fills with `input` at 80% and borders with it at 50%.
    out.push(("number.button.hover".into(), theme.input.opacity(0.4).into()));
    out.push(("number.button.pressed".into(), theme.input.opacity(0.6).into()));
    out.push(("number.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8).into()));
    out.push(("number.disabled.border".into(), theme.input.opacity(0.5).into()));
    // A disabled input draws its text at half strength (inside the frame's own 50% fade).
    out.push(("number.disabled.text".into(), theme.foreground.opacity(0.5).into()));
}
