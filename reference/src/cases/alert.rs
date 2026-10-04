//! `Alert` (crates/component/src/alert.rs): a message in a bordered box, or a
//! full-width banner, with an icon, an optional title and a close button.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::{Context as _, Result};
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{Sizable as _, alert::{Alert, AlertVariant}},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "default") {
        "info" => AlertVariant::Info,
        "success" => AlertVariant::Success,
        "warning" => AlertVariant::Warning,
        "error" => AlertVariant::Error,
        _ => AlertVariant::Default,
    };
    let message = param_str(params, "message", "This is an alert message.").to_string();
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let icon = match params.get("icon").and_then(|v| v.as_str()) {
        Some(name) => Some(super::icon(name).context("unknown alert icon")?),
        None => None,
    };
    let banner = param_bool(params, "banner");
    let closable = param_bool(params, "closable");
    let size = super::size(params);
    let width = param_f32(params, "width", 360.);
    Ok(Rc::new(move |_, _, _| {
        let mut alert = Alert::new("case", message.clone()).with_variant(variant).with_size(size);
        if let Some(title) = title.clone() {
            alert = alert.title(title);
        }
        if let Some(icon) = icon.clone() {
            alert = alert.icon(icon);
        }
        if banner {
            alert = alert.banner();
        }
        if closable {
            alert = alert.on_close(|_, _, _| {});
        }
        div().w(px(width)).child(alert).into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    use gpui_kit::{component::Colorize as _, transparent_white};
    // alert.rs AlertVariant::bg / border_color: the variant color mixed into
    // transparent white at 4% and 30%; the close button's hover and press take
    // the fill at 80% and 90%.
    let default_bg = theme.background;
    out.push(("alert.close.hover".into(), default_bg.opacity(0.8).into()));
    out.push(("alert.close.pressed".into(), default_bg.opacity(0.9).into()));
    for (name, color) in [("info", theme.info), ("success", theme.success), ("warning", theme.warning), ("error", theme.danger)] {
        let bg = color.mix_oklab(transparent_white(), 0.04);
        out.push((format!("alert.{name}.background"), bg.into()));
        out.push((format!("alert.{name}.border"), color.mix_oklab(transparent_white(), 0.3).into()));
        out.push((format!("alert.{name}.close.hover"), bg.opacity(0.8).into()));
        out.push((format!("alert.{name}.close.pressed"), bg.opacity(0.9).into()));
    }
}
