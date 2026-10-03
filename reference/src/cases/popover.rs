//! `Popover` (crates/component/src/popover.rs) on an outline Button.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::{Result, bail};
use gpui_kit::{
    Anchor, IntoElement as _, ParentElement as _, SharedString, Styled as _, px,
    component::{
        button::Button,
        popover::Popover,
    },
};
use std::rc::Rc;

pub fn anchor(name: &str) -> Result<Anchor> {
    Ok(match name {
        "top-left" => Anchor::TopLeft,
        "top-center" => Anchor::TopCenter,
        "top-right" => Anchor::TopRight,
        "bottom-left" => Anchor::BottomLeft,
        "bottom-center" => Anchor::BottomCenter,
        "bottom-right" => Anchor::BottomRight,
        "left-center" => Anchor::LeftCenter,
        "right-center" => Anchor::RightCenter,
        other => bail!("unknown anchor {other}"),
    })
}

pub fn popover(params: &Params) -> Result<Builder> {
    let anchor = anchor(param_str(params, "anchor", "top-left"))?;
    let label = param_str(params, "label", "Open").to_string();
    // "a|b" renders two children (two lines of the column).
    let lines: Vec<SharedString> = param_str(params, "content", "Popover content")
        .split('|')
        .map(|s| SharedString::from(s.to_string()))
        .collect();
    let offset = params.get("offset").and_then(|v| v.as_f64()).map(|v| px(v as f32));
    let width = params.get("width").and_then(|v| v.as_f64()).map(|v| px(v as f32));
    let (arrow, plain) = (param_bool(params, "arrow"), param_bool(params, "plain"));
    Ok(Rc::new(move |_, _, _| {
        let mut popover = Popover::new("case")
            .anchor(anchor)
            .arrow(arrow)
            .appearance(!plain)
            .trigger(Button::new("trigger").label(label.clone()).outline());
        if let Some(offset) = offset {
            popover = popover.offset(offset);
        }
        if let Some(width) = width {
            popover = popover.w(width);
        }
        popover.children(lines.clone()).into_any_element()
    }))
}
