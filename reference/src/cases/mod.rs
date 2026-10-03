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
mod surface;

pub fn builder(case: &Case) -> Result<Builder> {
    let params = effective_params(case);
    match case.component.as_str() {
        "surface" => surface::builder(&params),
        "button" => button::builder(&params),
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
}

pub fn disabled(params: &Params) -> bool {
    param_bool(params, "disabled")
}
