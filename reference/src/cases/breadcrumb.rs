//! `Breadcrumb` (crates/component/src/breadcrumb.rs): items joined by
//! chevrons, the last one in the foreground color.
use crate::{
    harness::Builder,
    manifest::{Params, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::breadcrumb::{Breadcrumb, BreadcrumbItem},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let items: Vec<String> = param_str(params, "items", "Home|Documents|Report").split('|').map(str::to_string).collect();
    let disabled: Vec<usize> = params
        .get("disabled_item")
        .and_then(|v| v.as_u64())
        .map(|i| vec![i as usize])
        .unwrap_or_default();
    Ok(Rc::new(move |_, _, _| {
        let mut breadcrumb = Breadcrumb::new();
        for (ix, label) in items.iter().enumerate() {
            breadcrumb = breadcrumb.child(
                BreadcrumbItem::new(label.clone())
                    .disabled(disabled.contains(&ix))
                    .on_click(|_, _, _| {}),
            );
        }
        breadcrumb.into_any_element()
    }))
}
