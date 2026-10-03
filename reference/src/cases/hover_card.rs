//! `HoverCard` (crates/component/src/hover_card.rs): a popover surface that
//! opens 600ms after the pointer enters the trigger and closes 300ms after it
//! leaves both.
use crate::{
    harness::Builder,
    manifest::{Params, param_str},
};
use anyhow::Result;
use gpui_kit::{
    Anchor, IntoElement as _, ParentElement as _, Styled as _, div,
    component::{ActiveTheme as _, StyledExt as _, hover_card::HoverCard, v_flex},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let anchor = match param_str(params, "anchor", "top-center") {
        "top-left" => Anchor::TopLeft,
        "top-right" => Anchor::TopRight,
        "bottom-center" => Anchor::BottomCenter,
        _ => Anchor::TopCenter,
    };
    let title = param_str(params, "title", "This is a hover card").to_string();
    let body = param_str(params, "body", "Rich content on hover.").to_string();
    Ok(Rc::new(move |_, _, cx| {
        let theme = cx.theme();
        HoverCard::new("case")
            .anchor(anchor)
            .trigger(div().child("Hover over me").text_color(theme.primary).text_sm())
            .child(
                v_flex()
                    .gap_2()
                    .child(div().child(title.clone()).font_semibold().text_sm())
                    .child(div().child(body.clone()).text_color(theme.muted_foreground).text_sm()),
            )
            .into_any_element()
    }))
}
