//! `NumberInput` (crates/component/src/input/number_input.rs).
use super::{disabled, size};
use crate::{harness::Builder, manifest::{Params, param_f32, param_str}};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, Styled as _, px,
    component::{
        Colorize as _, Disableable as _, Sizable as _, Theme,
        input::{InputState, NumberInput},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let value = param_str(params, "value", "42").to_string();
    let width = param_f32(params, "width", 160.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let value = value.clone();
            let state = cx.new(|cx| InputState::new(window, cx).default_value(value));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<InputState>().ok())
            .expect("input state");
        NumberInput::new(&state)
            .with_size(size)
            .disabled(disabled)
            .w(px(width))
            .into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // number_input.rs: the step buttons tint to the frame on hover and press;
    // a disabled frame fills with `input` at 80% and borders with it at 50%.
    out.push(("number.button.hover".into(), theme.input.opacity(0.4)));
    out.push(("number.button.pressed".into(), theme.input.opacity(0.6)));
    out.push(("number.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8)));
    out.push(("number.disabled.border".into(), theme.input.opacity(0.5)));
    // A disabled input draws its text at half strength (inside the frame's own 50% fade).
    out.push(("number.disabled.text".into(), theme.foreground.opacity(0.5)));
}
