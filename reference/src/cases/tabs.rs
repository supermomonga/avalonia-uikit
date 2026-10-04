//! `TabBar` / `Tab` (crates/component/src/tab): three tabs in a bar. A click
//! selects the tab, so the indicator's slide can be recorded.
use super::{icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, Styled as _, px,
    component::{
        Sizable as _,
        tab::{Tab, TabBar, TabVariant},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = match param_str(params, "variant", "tab") {
        "outline" => TabVariant::Outline,
        "pill" => TabVariant::Pill,
        "segmented" => TabVariant::Segmented,
        "underline" => TabVariant::Underline,
        _ => TabVariant::Tab,
    };
    let size = size(params);
    let selected = param_f32(params, "selected", 0.) as usize;
    let disabled_ix = params.get("disabled_index").and_then(|v| v.as_u64()).map(|v| v as usize);
    let width = param_f32(params, "width", 320.);
    let icons = param_bool(params, "icons");
    let widths: Vec<f32> = params
        .get("tab_widths")
        .and_then(|v| v.as_array())
        .map(|a| a.iter().filter_map(|v| v.as_f64()).map(|v| v as f32).collect())
        .unwrap_or_default();
    Ok(Rc::new(move |view, _, cx| {
        let current = if view.state.toggled { view.state.value as usize } else { selected };
        let mut bar = TabBar::new("case")
            .with_variant(variant)
            .with_size(size)
            .w(px(width))
            .selected_index(current)
            .on_click(cx.listener(|view, ix: &usize, _, cx| {
                view.state.toggled = true;
                view.state.value = *ix as f32;
                cx.notify();
            }));
        for (ix, (label, glyph)) in [("Account", "copy"), ("Profile", "plus"), ("Settings", "check")].into_iter().enumerate() {
            let mut tab = if icons { Tab::new().icon(icon(glyph).expect("known icon")) } else { Tab::new().label(label) };
            if let Some(w) = widths.get(ix) {
                tab = tab.w(px(*w));
            }
            bar = bar.child(tab.disabled(Some(ix) == disabled_ix));
        }
        bar.into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    // tab.rs disabled(): a disabled selected pill at half strength.
    out.push(("tab.pill.disabled.selected-background".into(), theme.primary.opacity(0.5).into()));
    out.push(("tab.pill.disabled.selected-foreground".into(), theme.primary_foreground.opacity(0.5).into()));
}
