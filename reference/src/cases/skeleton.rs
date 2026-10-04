//! `Skeleton` (crates/component/src/skeleton.rs): a placeholder bar that
//! pulses between full and half opacity every two seconds.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, Styled as _, px,
    component::skeleton::Skeleton,
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let secondary = param_bool(params, "secondary");
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 0.);
    let radius = param_f32(params, "radius", 0.);
    Ok(Rc::new(move |_, _, _| {
        let mut skeleton = Skeleton::new().w(px(width)).rounded(px(radius));
        if secondary {
            skeleton = skeleton.secondary();
        }
        if height > 0. {
            skeleton = skeleton.h(px(height));
        }
        skeleton.into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    // skeleton.rs: the secondary skeleton is the skeleton color at 50%.
    out.push(("skeleton.secondary".into(), theme.skeleton.opacity(0.5).into()));
}
