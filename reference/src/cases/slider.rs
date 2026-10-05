//! `Slider` (crates/component/src/slider.rs) over a `SliderState` kept across
//! frames, so presses and drags move it.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::slider::{Slider, SliderScale, SliderState, SliderValue},
};
use std::rc::Rc;

/// A single `value`, or (uikit-slider) the range `start`..`end`; `scale = "log"`
/// for SliderScale::Logarithmic.
pub fn builder(params: &Params) -> Result<Builder> {
    let (value, min, max) = (param_f32(params, "value", 40.), param_f32(params, "min", 0.), param_f32(params, "max", 100.));
    let step = param_f32(params, "step", 1.);
    let (vertical, reverse, disabled) = (param_bool(params, "vertical"), param_bool(params, "reverse"), super::disabled(params));
    let width = param_f32(params, "width", 200.);
    let value = match (params.get("start"), params.get("end")) {
        (Some(_), Some(_)) => SliderValue::Range(param_f32(params, "start", 0.), param_f32(params, "end", 0.)),
        _ => SliderValue::Single(value),
    };
    let scale = if param_str(params, "scale", "linear") == "log" { SliderScale::Logarithmic } else { SliderScale::Linear };
    Ok(Rc::new(move |view, _, cx| {
        if view.state.entity.is_none() {
            let state = cx.new(|_| SliderState::new().min(min).max(max).scale(scale).step(step).default_value(value));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<SliderState>().ok())
            .expect("slider state");
        let mut slider = Slider::new(&state).disabled(disabled);
        if reverse {
            slider = slider.reverse();
        }
        if vertical {
            slider.vertical().into_any_element()
        } else {
            div().w(px(width)).child(slider).into_any_element()
        }
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    // slider.rs: the track at 20% of the bar (40% while pressed), the thumb's edge at 50%.
    let bar: gpui_kit::Background = theme.tokens.slider_bar.into();
    out.push(("slider.track".into(), bar.opacity(0.2)));
    out.push(("slider.track.active".into(), bar.opacity(0.4)));
    out.push(("slider.thumb.edge".into(), bar.opacity(0.5)));
    // The ring is ring.alpha(0.5 * s); its element opacity carries s.
    out.push(("slider.ring".into(), theme.ring.alpha(0.5).into()));
}
