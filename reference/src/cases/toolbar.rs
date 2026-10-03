//! `Toolbar` (crates/component/src/toolbar.rs): undo and redo, a separator,
//! a labelled New button, a separator and a bold toggle, after optional text.
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, SharedString, Styled as _,
    component::{
        Disableable as _, IconName, Sizable as _,
        button::{Button, Toggle},
        separator::Separator,
        toolbar::Toolbar,
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let checked = param_bool(params, "checked");
    let disabled = disabled(params);
    let content = params.get("content").and_then(|v| v.as_str()).map(SharedString::from);
    Ok(Rc::new(move |_, _, _| {
        let mut bar = Toolbar::new("case");
        if let Some(text) = content.clone() {
            bar = bar.content(text);
        }
        bar.child(Button::new("undo").icon(IconName::Undo2).disabled(disabled))
            .child(Button::new("redo").icon(IconName::Redo2).disabled(disabled))
            .content(Separator::vertical().h_5())
            .child(Button::new("new").icon(IconName::Plus).label("New").disabled(disabled))
            .content(Separator::vertical().h_5())
            .child(Toggle::new("bold").label("B").checked(checked).disabled(disabled))
            .with_size(size)
            .into_any_element()
    }))
}
