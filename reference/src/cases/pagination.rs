//! `Pagination` (crates/component/src/pagination.rs): previous and next
//! buttons around the page buttons, with ellipses past `visible` pages. A
//! click (on a page or an ellipsis menu's entry) moves the case to that page.
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{Disableable as _, Sizable as _, pagination::Pagination},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let disabled = disabled(params);
    let total = param_f32(params, "total", 5.) as usize;
    let current = param_f32(params, "current", 3.) as usize;
    let compact = param_bool(params, "compact");
    let visible = param_f32(params, "visible", 5.) as usize;
    Ok(Rc::new(move |view, _, cx| {
        let page = if view.state.value > 0. { view.state.value as usize } else { current };
        let pagination = Pagination::new("case")
            .with_size(size)
            .current_page(page)
            .total_pages(total)
            .visible_pages(visible)
            .disabled(disabled)
            .on_click(cx.listener(|view, page: &usize, _, cx| {
                view.state.value = *page as f32;
                cx.notify();
            }));
        if compact { pagination.compact() } else { pagination }.into_any_element()
    }))
}
