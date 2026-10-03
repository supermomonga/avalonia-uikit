//! Builders that turn a case's parameters into a GPUI Kit element, and the
//! interactions that put it into the case's state.
use crate::{
    harness::{Builder, CaseWindow, Harness},
    manifest::{Case, Params, param_bool, param_f32, param_str},
};
use anyhow::{Result, bail};
use gpui_kit::{
    MouseButton,
    component::{IconName, Size},
};
use std::time::Duration;

mod button;
mod check;
mod display;
pub mod menu;
mod number;
mod progress;
mod scroll;
mod surface;
mod toggle;

pub fn builder(case: &Case) -> Result<Builder> {
    let params = effective_params(case);
    match case.component.as_str() {
        "surface" => surface::builder(&params),
        "button" => button::builder(&params),
        "toggle" => toggle::builder(&params),
        "checkbox" => check::checkbox(&params),
        "radio" => check::radio(&params),
        "switch" => check::switch(&params),
        "dropdown" => menu::dropdown(&params),
        "context" => menu::context(&params),
        "split" => menu::split(&params),
        "menubar" => menu::menubar(&params),
        "number" => number::builder(&params),
        "groupbox" => display::group_box(&params),
        "separator" => display::separator(&params),
        "link" => display::link(&params),
        "progress" => progress::progress(&params),
        "spinner" => progress::spinner(&params),
        "tooltip" => button::tooltip(&params),
        "scroll" => scroll::builder(&params),
        other => bail!("unknown component {other}"),
    }
}

/// The parameters with the `disabled` state folded in.
pub fn effective_params(case: &Case) -> Params {
    let mut params = case.params.clone();
    if case.state == "disabled" {
        params.insert("disabled".into(), true.into());
    }
    params
}

fn pointer(harness: &Harness, window: &CaseWindow, params: &Params) -> Result<gpui_kit::Point<gpui_kit::Pixels>> {
    let fx = param_f32(params, "pointer_x", 0.5);
    let fy = param_f32(params, "pointer_y", 0.5);
    harness.component_point(window, (fx, fy))
}

/// Puts the component into `state` after the window's first frame.
pub fn drive(harness: &mut Harness, window: &CaseWindow, case: &Case, state: &str) -> Result<()> {
    let params = effective_params(case);
    for part in state.split('+') {
        match part {
            "normal" | "disabled" => {}
            "hover" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
            }
            "pressed" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
                harness.mouse_down(window, at, MouseButton::Left)?;
            }
            "focus" => harness.tab(window)?,
            "click" => {
                let at = pointer(harness, window, &params)?;
                harness.click(window, at, MouseButton::Left)?;
            }
            "right-click" => {
                let at = pointer(harness, window, &params)?;
                harness.click(window, at, MouseButton::Right)?;
            }
            "leave" => {
                let bounds = harness.component_bounds(window)?;
                let outside = gpui_kit::point(
                    bounds.origin.x + bounds.size.width + gpui_kit::px(40.),
                    bounds.origin.y + bounds.size.height + gpui_kit::px(40.),
                );
                harness.mouse_move(window, outside, None)?;
            }
            "tooltip" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
                harness.advance(window, Duration::from_millis(600))?;
            }
            // Absolute window positions: "at-X-Y" moves the pointer there,
            // "click-at-X-Y" and "right-click-at-X-Y" also click.
            other if other.starts_with("pressed-at-") => {
                let (x, y) = other["pressed-at-".len()..].split_once('-').expect("pressed-at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                harness.mouse_move(window, at, None)?;
                harness.mouse_down(window, at, MouseButton::Left)?;
            }
            other if other.starts_with("at-") || other.starts_with("click-at-") || other.starts_with("right-click-at-") => {
                let (kind, coords) = other.split_once("at-").expect("prefix checked");
                let (x, y) = coords.split_once('-').expect("at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                match kind {
                    "click-" => harness.click(window, at, MouseButton::Left)?,
                    "right-click-" => harness.click(window, at, MouseButton::Right)?,
                    _ => harness.mouse_move(window, at, None)?,
                }
            }
            // "wheel-at-X-Y" scrolls the content under that point down by 40px.
            other if other.starts_with("wheel-at-") => {
                let (x, y) = other["wheel-at-".len()..].split_once('-').expect("wheel-at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                harness.wheel(window, at, 40.)?;
            }
            other if other.starts_with("wait-") => {
                let ms: u64 = other["wait-".len()..].trim_end_matches("ms").parse()?;
                harness.advance(window, Duration::from_millis(ms))?;
            }
            other if other.starts_with("key-") => {
                harness.key(window, &other["key-".len()..])?;
            }
            other => bail!("unknown state {other} in case {}", case.id),
        }
    }
    Ok(())
}

pub fn size(params: &Params) -> Size {
    match param_str(params, "size", "medium") {
        "xsmall" => Size::XSmall,
        "small" => Size::Small,
        "large" => Size::Large,
        _ => Size::Medium,
    }
}

pub fn icon(name: &str) -> Option<IconName> {
    Some(match name {
        "check" => IconName::Check,
        "plus" => IconName::Plus,
        "minus" => IconName::Minus,
        "chevron-down" => IconName::ChevronDown,
        "chevron-right" => IconName::ChevronRight,
        "copy" => IconName::Copy,
        "loader" => IconName::Loader,
        _ => return None,
    })
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    button::derived_colors(theme, out);
    check::derived_colors(theme, out);
    menu::derived_colors(theme, out);
    number::derived_colors(theme, out);
    display::derived_colors(theme, out);
    progress::derived_colors(theme, out);
}

pub fn disabled(params: &Params) -> bool {
    param_bool(params, "disabled")
}
