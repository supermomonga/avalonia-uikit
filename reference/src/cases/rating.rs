//! `Rating` (crates/component/src/rating.rs): a row of stars; the pointer
//! previews a value and a click sets it.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{ActiveTheme as _, rating::Rating},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let value = param_f32(params, "value", 3.) as usize;
    let max = param_f32(params, "max", 5.) as usize;
    let size = super::size(params);
    let disabled = super::disabled(params);
    let color = param_str(params, "color", "").to_string();
    Ok(Rc::new(move |_, _, cx| {
        let mut rating = Rating::new("case").value(value).max(max).with_size(size).disabled(disabled);
        if color == "red" {
            rating = rating.color(cx.theme().red);
        }
        rating.into_any_element()
    }))
}
