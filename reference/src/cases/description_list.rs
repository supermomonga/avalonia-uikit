//! `DescriptionList` (crates/component/src/description_list.rs): labels and
//! values in rows of `columns`, items spanning columns, bordered by default.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    Axis, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{Sizable as _, description_list::DescriptionList},
};
use std::rc::Rc;

/// "Label:Value:span|..." with "-" for a separator.
fn items(spec: &str) -> Vec<Option<(String, String, usize)>> {
    spec.split('|')
        .map(|item| {
            if item == "-" {
                return None;
            }
            let mut parts = item.splitn(3, ':');
            let label = parts.next().unwrap_or_default().to_string();
            let value = parts.next().unwrap_or_default().to_string();
            let span = parts.next().and_then(|s| s.parse().ok()).unwrap_or(1);
            Some((label, value, span))
        })
        .collect()
}

pub fn builder(params: &Params) -> Result<Builder> {
    let items = items(param_str(params, "items", "Name:GPUI Kit|Version:0.1.0|License:Apache-2.0"));
    let vertical = param_str(params, "layout", "horizontal") == "vertical";
    let bordered = !param_bool(params, "borderless");
    let columns = param_f32(params, "columns", 3.) as usize;
    let label_width = params.get("label_width").and_then(|v| v.as_f64()).map(|w| w as f32);
    let size = super::size(params);
    let width = param_f32(params, "width", 520.);
    Ok(Rc::new(move |_, _, _| {
        let mut list = DescriptionList::new()
            .layout(if vertical { Axis::Vertical } else { Axis::Horizontal })
            .bordered(bordered)
            .columns(columns)
            .with_size(size);
        if let Some(w) = label_width {
            list = list.label_width(px(w));
        }
        for item in &items {
            list = match item {
                Some((label, value, span)) => list.item(label.clone(), value.clone(), *span),
                None => list.separator(),
            };
        }
        div().w(px(width)).child(list).into_any_element()
    }))
}
