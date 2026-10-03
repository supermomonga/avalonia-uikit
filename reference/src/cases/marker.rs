//! `Marker` (crates/component/src/marker.rs): a muted status row in a
//! conversation or timeline, plain, between separator lines or over a border.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        Icon,
        marker::{Marker, MarkerAlignment, MarkerContent, MarkerIcon, MarkerLoadingStyle, MarkerVariant},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "plain") {
        "separator" => MarkerVariant::Separator,
        "border" => MarkerVariant::Border,
        _ => MarkerVariant::Plain,
    };
    let alignment = match param_str(params, "align", "") {
        "start" => Some(MarkerAlignment::Start),
        "center" => Some(MarkerAlignment::Center),
        "end" => Some(MarkerAlignment::End),
        _ => None,
    };
    let text = param_str(params, "text", "Conversation archived").to_string();
    let icon = super::icon(param_str(params, "icon", ""));
    let loading = param_bool(params, "loading");
    let shimmer = param_str(params, "loading_style", "spinner") == "shimmer";
    let width = param_f32(params, "width", 320.);
    Ok(Rc::new(move |_, _, _| {
        let mut marker = Marker::new().with_variant(variant).loading(loading);
        if let Some(alignment) = alignment {
            marker = marker.alignment(alignment);
        }
        if shimmer {
            marker = marker
                .with_loading_style(MarkerLoadingStyle::Shimmer)
                .with_shimmer_style(gpui_kit::component::shimmer::ShimmerStyle::new().once(true));
        }
        if let Some(icon) = icon.clone() {
            marker = marker.icon(MarkerIcon::new().child(Icon::new(icon)));
        }
        marker = marker.content(MarkerContent::new().text(text.clone()));
        div().w(px(width)).child(marker).into_any_element()
    }))
}
