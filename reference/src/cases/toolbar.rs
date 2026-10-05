//! `Toolbar` (crates/component/src/toolbar.rs): undo and redo, a separator,
//! a labelled New button, a separator and a bold toggle, after optional text.
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, SharedString, Styled as _, div, px,
    component::{
        Disableable as _, IconName, Sizable as _,
        button::{Button, Toggle},
        separator::Separator,
        toolbar::{Toolbar, ToolbarGroup},
    },
};
use std::rc::Rc;

/// `group` puts undo and redo in a ToolbarGroup with gap_1; `spacer` puts a
/// flexible space (`content(div().flex_1())`) before the toggle, in a bar
/// `width` wide.
pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let content = params.get("content").and_then(|v| v.as_str()).map(SharedString::from);
    let group = param_bool(params, "group");
    let spacer = param_bool(params, "spacer");
    let width = params.get("width").map(|_| param_f32(params, "width", 0.));
    Ok(Rc::new(move |_, _, _| {
        let mut bar = Toolbar::new("case");
        if let Some(width) = width {
            bar = bar.w(px(width));
        }
        if let Some(text) = content.clone() {
            bar = bar.content(text);
        }
        let undo = Button::new("undo").icon(IconName::Undo2).disabled(disabled);
        let redo = Button::new("redo").icon(IconName::Redo2).disabled(disabled);
        bar = if group {
            bar.child(ToolbarGroup::new("history").label("History").gap_1().child(undo).child(redo))
        } else {
            bar.child(undo).child(redo)
        };
        bar = bar
            .content(Separator::vertical().h_5())
            .child(Button::new("new").icon(IconName::Plus).label("New").disabled(disabled))
            .content(Separator::vertical().h_5());
        if spacer {
            bar = bar.content(div().flex_1());
        }
        bar.child(Toggle::new("bold").label("B").checked(checked).disabled(disabled))
            .with_size(size)
            .into_any_element()
    }))
}
