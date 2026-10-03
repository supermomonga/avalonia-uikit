//! `h_resizable` / `v_resizable` (crates/component/src/resizable.rs): two
//! empty panels and the handle between them.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::resizable::{h_resizable, resizable_panel, v_resizable},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let vertical = param_bool(params, "vertical");
    let width = param_f32(params, "width", 320.);
    let height = param_f32(params, "height", 120.);
    let first = param_f32(params, "first", 120.);
    Ok(Rc::new(move |_, _, _| {
        let group = if vertical { v_resizable("case") } else { h_resizable("case") };
        div()
            .w(px(width))
            .h(px(height))
            .child(
                group
                    .child(resizable_panel().size(px(first)).child(div().size_full()))
                    .child(resizable_panel().child(div().size_full())),
            )
            .into_any_element()
    }))
}
