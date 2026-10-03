//! `Message` (crates/component/src/message.rs): avatar, header, bubbles and
//! footer of one conversation turn, at the start or the end of the row.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        avatar::Avatar,
        bubble::Bubble,
        message::{Message, MessageAlignment, MessageAvatar, MessageContent, MessageFooter, MessageHeader},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let end = param_str(params, "align", "start") == "end";
    let avatar = params.get("avatar").and_then(|v| v.as_str()).map(str::to_string);
    let header = params.get("header").and_then(|v| v.as_str()).map(str::to_string);
    let footer = params.get("footer").and_then(|v| v.as_str()).map(str::to_string);
    let texts: Vec<String> = param_str(params, "texts", "Can you review this draft?").split('|').map(str::to_string).collect();
    let variant = super::bubble::variant(param_str(params, "variant", "secondary"));
    let plain = param_bool(params, "plain");
    let width = param_f32(params, "width", 400.);
    Ok(Rc::new(move |_, _, _| {
        let mut message = Message::new().alignment(if end { MessageAlignment::End } else { MessageAlignment::Start });
        if let Some(name) = avatar.clone() {
            message = message.avatar_slot(MessageAvatar::new().child(Avatar::new().name(name).size_8()));
        }
        if let Some(header) = header.clone() {
            message = message.header(header.split('·').fold(MessageHeader::new(), |h, part| h.child(part.trim().to_string())));
        }
        let mut content = MessageContent::new();
        for text in &texts {
            content = if plain {
                gpui_kit::ParentElement::child(content, text.clone())
            } else {
                content.bubble(Bubble::new().with_variant(variant).child(text.clone()))
            };
        }
        message = message.content(content);
        if let Some(footer) = footer.clone() {
            message = message.footer(MessageFooter::new().child(footer));
        }
        div().w(px(width)).child(message).into_any_element()
    }))
}
