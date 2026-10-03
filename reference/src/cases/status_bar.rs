//! `StatusBar` (crates/component/src/status_bar.rs): left, center and right
//! regions on a bar with a top border.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::status_bar::StatusBar,
};
use std::rc::Rc;

fn texts(params: &Params, key: &str) -> Vec<String> {
    params
        .get(key)
        .and_then(|v| v.as_str())
        .map(|s| s.split('|').filter(|t| !t.is_empty()).map(str::to_string).collect())
        .unwrap_or_default()
}

pub fn builder(params: &Params) -> Result<Builder> {
    let (left, center, right) = (texts(params, "left"), texts(params, "center"), texts(params, "right"));
    let width = param_f32(params, "width", 400.);
    Ok(Rc::new(move |_, _, _| {
        let mut bar = StatusBar::new();
        for text in &left {
            bar = bar.left(text.clone());
        }
        for text in &right {
            bar = bar.right(text.clone());
        }
        bar = bar.children(center.clone());
        div().w(px(width)).child(bar).into_any_element()
    }))
}
