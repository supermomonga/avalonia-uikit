//! `ColorPicker` / `ColorSelect` (crates/component/src/color_picker.rs): the
//! swatch or the field, closed or opened by the case's click; for
//! uikit-colorselect also without a color (`value = "none"`), with a
//! placeholder or with an icon in place of the swatch.
use super::{icon, size};
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
    let value = param_str(params, "value", "2563EB").to_string();
    let label = params.get("label").and_then(|v| v.as_str()).map(str::to_string);
    let placeholder = params.get("placeholder").and_then(|v| v.as_str()).map(str::to_string);
    let icon_name = params.get("icon").and_then(|v| v.as_str()).map(|name| icon(name).expect("known icon"));
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let state = if value == "none" {
                cx.new(|cx| ColorPickerState::new(window, cx))
            } else {
                let hex = format!("#{value}");
                let color = <Hsla as gpui_kit::component::Colorize>::parse_hex(&hex).expect("hex value");
                // GPUI floors channel * 255 when it formats: only values that survive the round trip.
                assert_eq!(color.to_hex(), hex, "the value must survive GPUI's hex round trip");
                cx.new(|cx| ColorPickerState::new(window, cx).default_value(color))
            };
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<ColorPickerState>().ok())
            .expect("color picker state");
        if field {
            let mut select = ColorSelect::new(&state).with_size(size);
            if let Some(placeholder) = placeholder.clone() {
                select = select.placeholder(placeholder);
            }
            div().w(px(width)).child(select).into_any_element()
        } else {
            let mut picker = ColorPicker::new(&state).with_size(size);
            if let Some(label) = label.clone() {
                picker = picker.label(label);
            }
            if let Some(name) = icon_name.clone() {
                picker = picker.icon(name);
            }
            picker.into_any_element()
        }
    }))
}
