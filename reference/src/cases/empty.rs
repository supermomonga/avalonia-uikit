//! `Empty` (crates/component/src/empty.rs): media, title and description
//! over the actions, centered.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        Icon, IconName,
        button::{Button, ButtonVariants as _},
        empty::{Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyMediaVariant, EmptyTitle},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let media = param_str(params, "media", "icon").to_string();
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let description = params.get("description").and_then(|v| v.as_str()).map(str::to_string);
    let actions: Vec<String> = param_str(params, "actions", "").split('|').filter(|t| !t.is_empty()).map(str::to_string).collect();
    let width = param_f32(params, "width", 360.);
    Ok(Rc::new(move |_, _, _| {
        let mut header = EmptyHeader::new();
        match media.as_str() {
            "icon" => {
                header = header.media(
                    EmptyMedia::new().with_variant(EmptyMediaVariant::Icon).child(Icon::new(IconName::Folder)),
                )
            }
            "plain" => header = header.media(EmptyMedia::new().child(Icon::new(IconName::Inbox).size(px(40.)))),
            _ => {}
        }
        if let Some(title) = title.clone() {
            header = header.title(EmptyTitle::new().child(title));
        }
        if let Some(description) = description.clone() {
            header = header.description(EmptyDescription::new().child(description));
        }
        let mut empty = Empty::new().header(header);
        if !actions.is_empty() {
            let mut content = EmptyContent::new().flex_row().justify_center().gap_2();
            for (ix, label) in actions.iter().enumerate() {
                let button = Button::new(ix).label(label.clone());
                content = content.child(if ix == 0 { button.primary() } else { button.outline() });
            }
            empty = empty.content(content);
        }
        div().w(px(width)).child(empty).into_any_element()
    }))
}
