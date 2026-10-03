//! `Badge` (crates/component/src/badge.rs): a count, dot or icon over its
//! child, here a plain secondary square the size of a matching avatar.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::{Context as _, Result};
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{ActiveTheme as _, Sizable as _, Size, badge::Badge},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = param_str(params, "variant", "count").to_string();
    let count = param_f32(params, "count", 3.) as usize;
    let max = params.get("max").and_then(|v| v.as_f64()).map(|m| m as usize);
    let size = super::size(params);
    let color = param_str(params, "color", "").to_string();
    let icon = super::icon(param_str(params, "icon", "check")).context("unknown badge icon")?;
    Ok(Rc::new(move |_, _, cx| {
        let theme = cx.theme();
        let mut badge = Badge::new().with_size(size);
        badge = match variant.as_str() {
            "dot" => badge.dot(),
            "icon" => badge.icon(icon.clone()),
            _ => badge.count(count),
        };
        if let Some(max) = max {
            badge = badge.max(max);
        }
        badge = match color.as_str() {
            "blue" => badge.color(theme.blue),
            "green" => badge.color(theme.green),
            "yellow" => badge.color(theme.yellow),
            _ => badge,
        };
        let side = match size {
            Size::Large => 48.,
            Size::Small | Size::XSmall => 24.,
            _ => 32.,
        };
        badge
            .child(div().size(px(side)).rounded(theme.radius).bg(theme.secondary))
            .into_any_element()
    }))
}
