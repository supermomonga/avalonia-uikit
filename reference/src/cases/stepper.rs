//! `Stepper` (crates/component/src/stepper): numbered or icon indicators
//! joined by lines; steps up to the selected one are checked.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        Sizable as _,
        stepper::{Stepper, StepperItem},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let labels: Vec<String> = param_str(params, "labels", "Step 1|Step 2|Step 3").split('|').map(str::to_string).collect();
    let icons: Vec<Option<gpui_kit::component::IconName>> = param_str(params, "icons", "")
        .split('|')
        .map(super::icon)
        .collect();
    let selected = param_f32(params, "selected", 0.) as usize;
    let vertical = param_str(params, "layout", "horizontal") == "vertical";
    let text_center = param_bool(params, "text_center");
    let disabled = super::disabled(params);
    let size = super::size(params);
    let width = param_f32(params, "width", 480.);
    // A vertical stepper's items share the height, which the lines then span.
    let height = param_f32(params, "height", 200.);
    Ok(Rc::new(move |_, _, _| {
        let stepper = Stepper::new("case")
            .selected_index(selected)
            .text_center(text_center)
            .disabled(disabled)
            .with_size(size)
            .items(labels.iter().enumerate().map(|(ix, label)| {
                let mut item = StepperItem::new().child(label.clone());
                if let Some(Some(icon)) = icons.get(ix) {
                    item = item.icon(icon.clone());
                }
                item
            }));
        if vertical {
            div().w(px(width)).h(px(height)).flex().child(stepper.vertical()).into_any_element()
        } else {
            div().w(px(width)).child(stepper).into_any_element()
        }
    }))
}
