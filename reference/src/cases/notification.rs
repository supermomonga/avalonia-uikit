//! `Notification` (crates/component/src/notification.rs) in a NotificationList
//! filling the case area, as Root's notification layer holds it. The card is
//! pushed on the first frame, so its enter motion starts there.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, InteractiveElement as _, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::notification::{Notification, NotificationList, NotificationType},
};
use std::rc::Rc;

pub fn notification(params: &Params) -> Result<Builder> {
    let (width, height) = (param_f32(params, "width", 430.), param_f32(params, "height", 170.));
    let kind = param_str(params, "type", "info").to_string();
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let message = param_str(params, "message", "Your changes have been saved.").to_string();
    let placement = super::popover::anchor(param_str(params, "placement", "top-right"))?;
    let autohide = !param_bool(params, "persistent");
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let list = cx.new(|cx| NotificationList::new(window, cx));
            let mut note = Notification::new().message(message.clone()).placement(placement).autohide(autohide);
            if let Some(title) = title.clone() {
                note = note.title(title);
            }
            note = match kind.as_str() {
                "success" => note.with_type(NotificationType::Success),
                "warning" => note.with_type(NotificationType::Warning),
                "error" => note.with_type(NotificationType::Error),
                "none" => note,
                _ => note.with_type(NotificationType::Info),
            };
            list.update(cx, |list, cx| list.push(note, window, cx));
            view.state.entity = Some(list.into());
        }
        let list = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<NotificationList>().ok())
            .expect("notification list");
        div()
            .id("case")
            .relative()
            .w(px(width))
            .h(px(height))
            .child(div().absolute().inset_0().child(list))
            .into_any_element()
    }))
}
