//! `Select` (crates/component/src/select.rs) over a list of fruits.
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, SharedString, Styled as _, px,
    component::{
        Colorize as _, Disableable as _, IndexPath, Sizable as _, Theme,
        select::{Select, SelectItem, SelectState},
    },
};
use std::rc::Rc;

#[derive(Clone)]
pub struct Fruit {
    title: String,
    disabled: bool,
}

impl SelectItem for Fruit {
    type Value = String;

    fn title(&self) -> SharedString {
        self.title.clone().into()
    }

    fn value(&self) -> &Self::Value {
        &self.title
    }

    fn disabled(&self) -> bool {
        self.disabled
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let disabled = disabled(params);
    let selected = param_f32(params, "selected", -1.) as i32;
    let disabled_row = param_f32(params, "disabled_row", -1.) as i32;
    let count = param_f32(params, "count", 4.) as usize;
    let width = param_f32(params, "width", 200.);
    let placeholder = param_str(params, "placeholder", "Select a fruit").to_string();
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let items: Vec<Fruit> = super::list::names(count)
                .into_iter()
                .enumerate()
                .map(|(i, title)| Fruit { title, disabled: i as i32 == disabled_row })
                .collect();
            let ix = (selected >= 0).then(|| IndexPath::new(selected as usize));
            let state = cx.new(|cx| SelectState::new(items, ix, window, cx));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<SelectState<Vec<Fruit>>>().ok())
            .expect("select state");
        Select::new(&state)
            .placeholder(placeholder.clone())
            .with_size(size)
            .disabled(disabled)
            .w(px(width))
            .into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // searchable_list/item.rs: a hovered row; select.rs: the empty view's icon
    // and a disabled frame (before the frame fades to half).
    out.push(("select.row.hover".into(), theme.accent.opacity(0.7)));
    out.push(("select.empty".into(), theme.muted_foreground.opacity(0.6)));
    out.push(("select.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8)));
}
