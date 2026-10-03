//! `Clipboard` (crates/component/src/clipboard.rs): a ghost icon button that
//! copies its value and shows a check for two seconds.
use crate::{
    harness::Builder,
    manifest::{Params, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{Sizable as _, clipboard::Clipboard},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let value = param_str(params, "value", "Copied text").to_string();
    let size = super::size(params);
    Ok(Rc::new(move |_, _, _| {
        Clipboard::new("case").value(value.clone()).with_size(size).into_any_element()
    }))
}
