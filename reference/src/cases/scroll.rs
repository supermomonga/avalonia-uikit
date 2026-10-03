//! Scrollbars (crates/base/src/scrollbar.rs) on `overflow_y_scrollbar`
//! (crates/component/src/scroll/scrollable.rs).
use crate::{
    harness::Builder,
    manifest::{Params, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::scroll::ScrollableElement as _,
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let width = param_f32(params, "width", 160.);
    let height = param_f32(params, "height", 100.);
    let content = param_f32(params, "content", 400.);
    Ok(Rc::new(move |_, _, _| {
        div()
            .id("case")
            .w(px(width))
            .h(px(height))
            .overflow_y_scrollbar()
            .child(div().w(px(width)).h(px(content)))
            .into_any_element()
    }))
}
