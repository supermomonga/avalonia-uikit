//! `TabBar` / `Tab` (crates/component/src/tab): three tabs in a bar. A click
//! selects the tab, so the indicator's slide can be recorded. The Tabalonia
//! cases add what its TabsControl draws: a close button on each tab (the tabs
//! story's closable tab), the bar's prefix and suffix, and the menu.
use super::{icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, prelude::FluentBuilder as _, px,
    component::{
        IconName, Sizable as _, h_flex,
        button::{Button, ButtonVariants as _},
        tab::{Tab, TabBar, TabVariant},
    },
};
use std::rc::Rc;

/// Ghost xsmall icon buttons in a row with mx_1, as the tabs story's prefix and suffix.
fn bar_buttons(id: &'static str, icons: [IconName; 2]) -> gpui_kit::AnyElement {
    let [first, second] = icons;
    h_flex()
        .mx_1()
        .child(Button::new((id, 0usize)).ghost().xsmall().icon(first))
        .child(Button::new((id, 1usize)).ghost().xsmall().icon(second))
        .into_any_element()
}

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
    let closable = param_bool(params, "closable");
    let bar_prefix = param_bool(params, "bar_prefix");
    let bar_suffix = param_bool(params, "bar_suffix");
    let menu = param_bool(params, "menu");
    let max_width = params.get("max_width").and_then(|v| v.as_f64()).map(|v| px(v as f32));
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
            .menu(menu)
            .when_some(max_width, |bar, max_width| bar.max_width(max_width))
            .on_click(cx.listener(|view, ix: &usize, _, cx| {
                view.state.toggled = true;
                view.state.value = *ix as f32;
                cx.notify();
            }));
        if bar_prefix {
            bar = bar.prefix(bar_buttons("prefix", [IconName::ChevronLeft, IconName::ChevronRight]));
        }
        if bar_suffix {
            bar = bar.suffix(bar_buttons("suffix", [IconName::Inbox, IconName::Ellipsis]));
        }
        for (ix, (label, glyph)) in [("Account", "copy"), ("Profile", "plus"), ("Settings", "check")].into_iter().enumerate() {
            let mut tab = if icons { Tab::new().icon(icon(glyph).expect("known icon")) } else { Tab::new().label(label) };
            if let Some(w) = widths.get(ix) {
                tab = tab.w(px(*w));
            }
            if closable {
                tab = tab.px_2().suffix(Button::new(("close", ix)).ghost().xsmall().icon(IconName::Close));
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
