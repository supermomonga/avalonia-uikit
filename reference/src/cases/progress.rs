//! `Progress` (progress/progress.rs) and `Spinner` (spinner.rs).
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, StatefulInteractiveElement as _,
    Styled as _, div, px,
    component::{
        Sizable as _,
        progress::{Progress, ProgressCircle},
        spinner::Spinner,
    },
};
use std::rc::Rc;

/// A progress bar in a fixed-width box; a click on the box switches the value
/// between `value` and `value_to`, so the value transition can be recorded.
pub fn progress(params: &Params) -> Result<Builder> {
    let size = size(params);
    let value = param_f32(params, "value", 40.);
    let value_to = param_f32(params, "value_to", value);
    let loading = param_bool(params, "loading");
    let width = param_f32(params, "width", 200.);
    Ok(Rc::new(move |view, _, cx| {
        let current = if view.state.toggled { value_to } else { value };
        div()
            .id("case-box")
            .w(px(width))
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }))
            .child(
                Progress::new("case")
                    .with_size(size)
                    .value(current)
                    .loading(loading),
            )
            .into_any_element()
    }))
}

/// A progress circle; a click switches the value between `value` and `value_to`.
/// `side` > 0 gives the circle a styled box (`size_20` is 80).
pub fn circle(params: &Params) -> Result<Builder> {
    let size = size(params);
    let value = param_f32(params, "value", 40.);
    let value_to = param_f32(params, "value_to", value);
    let loading = param_bool(params, "loading");
    let side = param_f32(params, "side", 0.);
    Ok(Rc::new(move |view, _, cx| {
        let current = if view.state.toggled { value_to } else { value };
        let mut circle = ProgressCircle::new("case").with_size(size).value(current).loading(loading);
        if side > 0. {
            circle = circle.size(px(side));
        }
        div()
            .id("case-box")
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }))
            .child(circle)
            .into_any_element()
    }))
}

pub fn spinner(params: &Params) -> Result<Builder> {
    let size = size(params);
    Ok(Rc::new(move |_, _, _| Spinner::new().with_size(size).into_any_element()))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // progress.rs: the track is the bar color at 20%.
    let bar: gpui_kit::Background = theme.tokens.progress_bar.into();
    out.push(("progress.track".into(), bar.opacity(0.2).as_solid().unwrap()));
}
