//! `Input` (crates/component/src/input/input.rs) on an `InputState`.
use super::{disabled, icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, Styled as _, px,
    component::{
        Colorize as _, Disableable as _, Icon, Sizable as _, Theme,
        button::{Button, ButtonVariants as _},
        input::{Input, InputState, Textarea, TextareaState},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let masked = param_bool(params, "masked");
    let mask_toggle = param_bool(params, "mask_toggle");
    let select_all = param_bool(params, "select_all");
    let prefix = icon(param_str(params, "prefix", ""));
    let suffix = icon(param_str(params, "suffix", ""));
    let width = param_f32(params, "width", 200.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            let state = cx.new(|cx| {
                let mut state = InputState::new(window, cx)
                    .placeholder(placeholder)
                    .default_value(value)
                    .masked(masked);
                if select_all {
                    state.select_all(window, cx);
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
        let mut input = Input::new(&state)
            .with_size(size)
            .disabled(disabled)
            .readonly(readonly)
            .w(px(width));
        if let Some(name) = prefix.clone() {
            input = input.prefix(Icon::new(name).small());
        }
        if let Some(name) = suffix.clone() {
            input = input.suffix(Button::new("suffix").ghost().icon(name).xsmall());
        }
        if mask_toggle {
            input = input.mask_toggle();
        }
        input.into_any_element()
    }))
}

/// `Textarea` (crates/component/src/input/textarea.rs): `rows` lines, or
/// `auto_grow(min_rows, max_rows)`, or a fixed `height`.
pub fn textarea(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let rows = param_f32(params, "rows", 1.) as usize;
    let grow = params
        .get("max_rows")
        .map(|_| (param_f32(params, "min_rows", 1.) as usize, param_f32(params, "max_rows", 1.) as usize));
    let height = params.get("height").and_then(serde_json::Value::as_f64).map(|h| h as f32);
    let width = param_f32(params, "width", 220.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            let state = cx.new(|cx| {
                let state = TextareaState::new(window, cx).rows(rows).placeholder(placeholder).default_value(value);
                match grow {
                    Some((min, max)) => state.auto_grow(min, max),
                    None => state,
                }
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TextareaState>().ok())
            .expect("textarea state");
        let mut textarea = Textarea::new(&state).with_size(size).disabled(disabled).readonly(readonly).w(px(width));
        if let Some(h) = height {
            textarea = textarea.h(px(h));
        }
        textarea.into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // input.rs: a disabled frame fills with `input` mixed 80% toward transparent,
    // then at half strength; its text and placeholder are at half strength.
    out.push(("input.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8).opacity(0.5)));
    out.push(("input.disabled.text".into(), theme.foreground.opacity(0.5)));
    out.push(("input.disabled.placeholder".into(), theme.muted_foreground.opacity(0.5)));
}
