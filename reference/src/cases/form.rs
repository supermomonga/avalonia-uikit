//! `Form` and `Field` (crates/component/src/form): labeled inputs in a grid
//! of `columns`, labels above (vertical) or beside (horizontal) the input.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, Axis, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        Sizable as _,
        button::{Button, ButtonVariants as _},
        form::{Field, Form},
        input::{Input, InputState},
    },
};
use std::rc::Rc;

/// One field: "Label" or "Label*" (required), "Label~description", "Label^2" (span 2).
struct FieldSpec {
    label: String,
    required: bool,
    description: Option<String>,
    span: u16,
}

fn fields(spec: &str) -> Vec<FieldSpec> {
    spec.split('|')
        .map(|f| {
            let (rest, span) = match f.split_once('^') {
                Some((rest, span)) => (rest, span.parse().unwrap_or(1)),
                None => (f, 1),
            };
            let (label, description) = match rest.split_once('~') {
                Some((label, d)) => (label, Some(d.to_string())),
                None => (rest, None),
            };
            let required = label.ends_with('*');
            FieldSpec { label: label.trim_end_matches('*').to_string(), required, description, span }
        })
        .collect()
}

pub fn builder(params: &Params) -> Result<Builder> {
    let fields = Rc::new(fields(param_str(params, "fields", "Name|Email")));
    let horizontal = param_str(params, "layout", "vertical") == "horizontal";
    let columns = param_f32(params, "columns", 1.) as usize;
    let label_width = params.get("label_width").and_then(|v| v.as_f64()).map(|w| w as f32);
    let footer = params.get("footer").and_then(|v| v.as_str()).map(str::to_string);
    let size = super::size(params);
    let width = param_f32(params, "width", 360.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entities.is_empty() {
            for field in fields.iter() {
                let placeholder = format!("Enter {}", field.label.to_lowercase());
                let state = cx.new(|cx| InputState::new(window, cx).placeholder(placeholder));
                view.state.entities.push(state.into());
            }
        }
        let mut form = Form::new()
            .layout(if horizontal { Axis::Horizontal } else { Axis::Vertical })
            .columns(columns)
            .with_size(size);
        if let Some(w) = label_width {
            form = form.label_width(px(w));
        }
        for (field, entity) in fields.iter().zip(view.state.entities.iter()) {
            let state = entity.clone().downcast::<InputState>().expect("input state");
            let mut f = Field::new()
                .label(field.label.clone())
                .required(field.required)
                .col_span(field.span)
                .child(Input::new(&state).with_size(size));
            if let Some(d) = &field.description {
                f = f.description(d.clone());
            }
            form = form.child(f);
        }
        if let Some(label) = footer.clone() {
            form = form.footer(Button::new("submit").primary().label(label).with_size(size));
        }
        div().w(px(width)).child(form).into_any_element()
    }))
}
