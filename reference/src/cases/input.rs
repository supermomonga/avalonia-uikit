//! `Input` (crates/component/src/input/input.rs) on an `InputState`.
use super::{disabled, icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, ParentElement as _, Styled as _, px,
    component::{
        Colorize as _, Disableable as _, Icon, Sizable as _, Size, Theme,
        button::{Button, ButtonVariants as _},
        input::{
            Input, InputGroup, InputGroupAddon, InputGroupAddonAlignment, InputGroupButton, InputState, Textarea,
            TextareaState,
        },
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

/// `InputGroup` (crates/component/src/input/group.rs): an input with addons.
/// `start` is an icon name or text before the input; `end` text, `end_button`
/// a labelled button or `end_icon` an icon button after it.
pub fn input_group(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let invalid = param_bool(params, "invalid");
    let multiline = param_bool(params, "multiline");
    let rows = param_f32(params, "rows", 3.) as usize;
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let start = param_str(params, "start", "").to_string();
    let end = param_str(params, "end", "").to_string();
    let end_button = param_str(params, "end_button", "").to_string();
    let end_icon = param_str(params, "end_icon", "").to_string();
    let button_size = if param_str(params, "button_size", "xsmall") == "small" { Size::Small } else { Size::XSmall };
    let width = param_f32(params, "width", 240.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            view.state.entity = Some(if multiline {
                cx.new(|cx| TextareaState::new(window, cx).rows(rows).placeholder(placeholder).default_value(value)).into()
            } else {
                cx.new(|cx| InputState::new(window, cx).placeholder(placeholder).default_value(value)).into()
            });
        }
        let mut group = InputGroup::new("case")
            .with_size(size)
            .disabled(disabled)
            .readonly(readonly)
            .invalid(invalid)
            .w(px(width));
        let entity = view.state.entity.clone().expect("state");
        group = match entity.clone().downcast::<TextareaState>() {
            Ok(state) => group.input(Textarea::new(&state)),
            Err(_) => group.input(Input::new(&entity.downcast::<InputState>().ok().expect("input state"))),
        };
        if let Some(name) = icon(&start) {
            group = group.addon(InputGroupAddon::new("start").child(Icon::new(name).size_4()));
        } else if !start.is_empty() {
            group = group.addon(InputGroupAddon::new("start").child(start.clone()));
        }
        let mut tail = InputGroupAddon::new("end").align(InputGroupAddonAlignment::InlineEnd);
        let mut has_tail = false;
        if !end.is_empty() {
            tail = tail.child(end.clone());
            has_tail = true;
        }
        if !end_button.is_empty() {
            tail = tail.child(InputGroupButton::new("btn").label(end_button.clone()).with_size(button_size));
            has_tail = true;
        }
        if let Some(name) = icon(&end_icon) {
            tail = tail.child(InputGroupButton::new("btn").icon(name).with_size(button_size));
            has_tail = true;
        }
        if has_tail {
            group = group.addon(tail);
        }
        group.into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // input.rs: a disabled frame fills with `input` mixed 80% toward transparent,
    // then at half strength; its text and placeholder are at half strength.
    out.push(("input.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8).opacity(0.5)));
    out.push(("input.disabled.text".into(), theme.foreground.opacity(0.5)));
    out.push(("input.disabled.placeholder".into(), theme.muted_foreground.opacity(0.5)));
    // group.rs GroupAppearance: the frame and its invalid ring differ by mode; group buttons tint with `muted`.
    let dark = theme.is_dark();
    out.push(("input-group.background".into(), if dark { theme.input.opacity(0.3) } else { theme.transparent }));
    out.push(("input-group.disabled.background".into(), theme.input.opacity(if dark { 0.8 } else { 0.5 })));
    out.push(("input-group.invalid.ring".into(), theme.danger.opacity(if dark { 0.4 } else { 0.2 })));
    out.push(("input-group.button.hover".into(), theme.muted.opacity(if dark { 0.5 } else { 1.0 })));
}
