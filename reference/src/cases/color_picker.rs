//! `ColorPicker` / `ColorSelect` (crates/component/src/color_picker.rs), closed.
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, Hsla, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        Colorize as _, Sizable as _,
        color_picker::{ColorPicker, ColorPickerState, ColorSelect},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let field = param_bool(params, "field");
    let width = param_f32(params, "width", 200.);
    let hex = format!("#{}", param_str(params, "value", "2563EB"));
    let label = params.get("label").and_then(|v| v.as_str()).map(str::to_string);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let color = <Hsla as gpui_kit::component::Colorize>::parse_hex(&hex).expect("hex value");
            // GPUI floors channel * 255 when it formats: only values that survive the round trip.
            assert_eq!(color.to_hex(), hex, "the value must survive GPUI's hex round trip");
            let state = cx.new(|cx| ColorPickerState::new(window, cx).default_value(color));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<ColorPickerState>().ok())
            .expect("color picker state");
        if field {
            div().w(px(width)).child(ColorSelect::new(&state).with_size(size)).into_any_element()
        } else {
            let mut picker = ColorPicker::new(&state).with_size(size);
            if let Some(label) = label.clone() {
                picker = picker.label(label);
            }
            picker.into_any_element()
        }
    }))
}
