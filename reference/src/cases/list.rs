//! `List` / `ListItem` (crates/component/src/list): rows from a delegate.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    App, AppContext as _, Context, IntoElement as _, ParentElement as _, Styled as _, Window, px,
    component::{
        Colorize as _, Disableable as _, IndexPath, Theme,
        list::{List, ListDelegate, ListItem, ListState},
    },
};
use std::rc::Rc;

const NAMES: [&str; 12] = [
    "Apple", "Banana", "Cherry", "Grape", "Lemon", "Mango", "Orange", "Peach", "Pear", "Plum", "Kiwi", "Lime",
];

/// The row names a case shows: the fruit names, then "Item N".
pub fn names(count: usize) -> Vec<String> {
    (0..count)
        .map(|i| NAMES.get(i).map(|s| s.to_string()).unwrap_or_else(|| format!("Item {}", i + 1)))
        .collect()
}

pub struct Rows {
    items: Vec<String>,
    disabled_row: i32,
}

impl ListDelegate for Rows {
    type Item = ListItem;

    fn items_count(&self, _: usize, _: &App) -> usize {
        self.items.len()
    }

    fn render_item(&mut self, ix: IndexPath, _: &mut Window, _: &mut Context<ListState<Self>>) -> Option<ListItem> {
        let disabled = ix.row as i32 == self.disabled_row;
        self.items.get(ix.row).map(|t| ListItem::new(ix.row).disabled(disabled).child(t.clone()))
    }

    fn set_selected_index(&mut self, _: Option<IndexPath>, _: &mut Window, cx: &mut Context<ListState<Self>>) {
        cx.notify();
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    let selected = param_f32(params, "selected", -1.) as i32;
    let disabled_row = param_f32(params, "disabled_row", -1.) as i32;
    let count = param_f32(params, "count", 5.) as usize;
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 200.);
    let empty = param_bool(params, "empty");
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let items = if empty { vec![] } else { names(count) };
            let state = cx.new(|cx| ListState::new(Rows { items, disabled_row }, window, cx));
            if selected >= 0 {
                state.update(cx, |s, cx| s.set_selected_index(Some(IndexPath::new(selected as usize)), window, cx));
            }
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<ListState<Rows>>().ok())
            .expect("list state");
        List::new(&state).w(px(width)).h(px(height)).into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // delegate.rs: the empty list's Inbox icon.
    out.push(("list.empty".into(), theme.muted_foreground.opacity(0.6)));
}

/// `v_virtual_list` (crates/base/src/virtual_list.rs) with its overlay
/// scrollbar: unstyled "Row N" rows, 30/45/60px tall in turn (34px when
/// `uniform`), every other one on `secondary`, scrolled to `offset`.
pub fn virtual_list(params: &Params) -> Result<Builder> {
    use gpui_kit::{
        InteractiveElement as _, div, point, prelude::FluentBuilder as _, size,
        component::{ActiveTheme as _, VirtualListScrollHandle, scroll::{Scrollbar, ScrollbarHandle as _}, v_virtual_list},
    };
    use std::cell::Cell;
    let count = param_f32(params, "count", 1000.) as usize;
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 200.);
    let offset = param_f32(params, "offset", 0.);
    let uniform = param_bool(params, "uniform");
    let row = move |i: usize| if uniform { 34. } else { [30., 45., 60.][i % 3] };
    let sizes = Rc::new((0..count).map(|i| size(px(width), px(row(i)))).collect::<Vec<_>>());
    let handle = VirtualListScrollHandle::new();
    let applied = Rc::new(Cell::new(false));
    Ok(Rc::new(move |_, _, cx| {
        if offset > 0. && !applied.replace(true) {
            handle.set_offset(point(px(0.), px(-offset)));
        }
        let heights = sizes.clone();
        div()
            .id("case")
            .relative()
            .w(px(width))
            .h(px(height))
            .child(
                v_virtual_list(cx.entity(), "vlist", sizes.clone(), move |_, range, _, cx| {
                    range
                        .map(|ix| {
                            div()
                                .w_full()
                                .h(heights[ix].height)
                                .px_3()
                                .when(ix % 2 == 1, |this| this.bg(cx.theme().secondary))
                                .child(format!("Row {ix}"))
                        })
                        .collect()
                })
                .track_scroll(&handle),
            )
            .child(Scrollbar::vertical(&handle))
            .into_any_element()
    }))
}
