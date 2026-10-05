//! `List` / `ListItem` (crates/component/src/list): rows from a delegate.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    App, AppContext as _, Context, IntoElement, ParentElement as _, ScrollStrategy, Styled as _, Task, Window, div,
    px,
    component::{
        ActiveTheme as _, IconName, IndexPath, Theme, h_flex,
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
    /// The names a search filters (perform_search), for the ListView cases.
    all: Vec<String>,
    /// The confirmed row, which shows the check icon when `check_icon` is set.
    confirmed: i32,
    check_icon: bool,
    loading: bool,
}

impl ListDelegate for Rows {
    type Item = ListItem;

    fn items_count(&self, _: usize, _: &App) -> usize {
        self.items.len()
    }

    fn render_item(&mut self, ix: IndexPath, _: &mut Window, _: &mut Context<ListState<Self>>) -> Option<ListItem> {
        let disabled = ix.row as i32 == self.disabled_row;
        let (check_icon, confirmed) = (self.check_icon, ix.row as i32 == self.confirmed);
        self.items.get(ix.row).map(|t| {
            let item = ListItem::new(ix.row).disabled(disabled).child(t.clone());
            if check_icon { item.check_icon(IconName::Check).confirmed(confirmed) } else { item }
        })
    }

    fn set_selected_index(&mut self, _: Option<IndexPath>, _: &mut Window, cx: &mut Context<ListState<Self>>) {
        cx.notify();
    }

    fn perform_search(&mut self, query: &str, _: &mut Window, _: &mut Context<ListState<Self>>) -> Task<()> {
        let query = query.to_lowercase();
        self.items = self.all.iter().filter(|t| t.to_lowercase().contains(&query)).cloned().collect();
        Task::ready(())
    }

    fn loading(&self, _: &App) -> bool {
        self.loading
    }
}

/// The sections of the ListView cases: the middle one is empty, so the list
/// skips it with its header and footer.
const SECTIONS: [(&str, &[&str]); 3] = [
    ("Fruits", &["Apple", "Banana", "Cherry"]),
    ("Berries", &[]),
    ("Citrus", &["Lemon", "Orange", "Lime"]),
];

/// A delegate with sections, and the story's header (14px muted text, px 8,
/// pb 4) and footer (12px muted text, pt 4, pb 20) for each.
pub struct SectionRows {
    sections: Vec<(String, Vec<String>)>,
    /// Headers as tall as a row (16px muted text, px 12, py 4) and no footers.
    row_headers: bool,
}

impl ListDelegate for SectionRows {
    type Item = ListItem;

    fn sections_count(&self, _: &App) -> usize {
        self.sections.len()
    }

    fn items_count(&self, section: usize, _: &App) -> usize {
        self.sections.get(section).map_or(0, |s| s.1.len())
    }

    fn render_item(&mut self, ix: IndexPath, _: &mut Window, _: &mut Context<ListState<Self>>) -> Option<ListItem> {
        let name = self.sections.get(ix.section)?.1.get(ix.row)?.clone();
        Some(ListItem::new(ix).child(name))
    }

    fn render_section_header(
        &mut self,
        section: usize,
        _: &mut Window,
        cx: &mut Context<ListState<Self>>,
    ) -> Option<impl IntoElement> {
        let title = self.sections.get(section)?.0.clone();
        let muted = cx.theme().muted_foreground;
        Some(if self.row_headers {
            h_flex().px_3().py_1().text_color(muted).child(title)
        } else {
            h_flex().pb_1().px_2().gap_2().text_sm().text_color(muted).child(title)
        })
    }

    fn render_section_footer(
        &mut self,
        section: usize,
        _: &mut Window,
        cx: &mut Context<ListState<Self>>,
    ) -> Option<impl IntoElement> {
        let count = self.sections.get(section).filter(|_| !self.row_headers)?.1.len();
        Some(div().pt_1().pb_5().px_2().text_xs().text_color(cx.theme().muted_foreground).child(format!("{count} items")))
    }

    fn set_selected_index(&mut self, _: Option<IndexPath>, _: &mut Window, cx: &mut Context<ListState<Self>>) {
        cx.notify();
    }
}

fn strategy(params: &Params) -> ScrollStrategy {
    match param_str(params, "strategy", "top") {
        "center" => ScrollStrategy::Center,
        "bottom" => ScrollStrategy::Bottom,
        _ => ScrollStrategy::Top,
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    if param_bool(params, "sections") {
        return sections(params);
    }
    let selected = param_f32(params, "selected", -1.) as i32;
    let disabled_row = param_f32(params, "disabled_row", -1.) as i32;
    let count = param_f32(params, "count", 5.) as usize;
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 200.);
    let empty = param_bool(params, "empty");
    // The ListView cases (uikit-list): searchable, selectable(false), loading, a
    // confirmed row with the check icon, and a scroll to a row.
    let searchable = param_bool(params, "searchable");
    let selectable = params.get("selectable").and_then(|v| v.as_bool()).unwrap_or(true);
    let loading = param_bool(params, "loading");
    let confirmed = param_f32(params, "confirmed", -1.) as i32;
    let check_icon = param_bool(params, "check_icon");
    let scroll_to = param_f32(params, "scroll_to", -1.) as i32;
    let strategy = strategy(params);
    let query = param_str(params, "query", "").to_string();
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let items = if empty { vec![] } else { names(count) };
            let rows = Rows { all: items.clone(), items, disabled_row, confirmed, check_icon, loading };
            let state = cx.new(|cx| ListState::new(rows, window, cx).searchable(searchable).selectable(selectable));
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
        // A query is typed on a later frame, once the list has rows: the search
        // then selects the first row (start_search). The scroll also waits for a
        // later frame: the first one is drawn before the harness pins the clock
        // the scrollbar reads.
        if !view.state.toggled && view.state.value > 0. {
            view.state.toggled = true;
            if !query.is_empty() {
                state.update(cx, |s, cx| s.set_query(&query, window, cx));
            }
            if scroll_to >= 0 {
                state.update(cx, |s, cx| s.scroll_to_item(IndexPath::new(scroll_to as usize), strategy, window, cx));
            }
        }
        view.state.value += 1.;
        List::new(&state).w(px(width)).h(px(height)).into_any_element()
    }))
}

fn sections(params: &Params) -> Result<Builder> {
    // `extra` more rows in the last section ("Item 4"...), for a long list;
    // `row_headers` for rows of one height.
    let extra = param_f32(params, "extra", 0.) as usize;
    let row_headers = param_bool(params, "row_headers");
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 220.);
    let selected = params.get("selected").and_then(|v| v.as_array()).map(|v| {
        IndexPath::new(v[1].as_u64().unwrap_or(0) as usize).section(v[0].as_u64().unwrap_or(0) as usize)
    });
    let scroll_to = params.get("scroll_to").and_then(|v| v.as_array()).map(|v| {
        IndexPath::new(v[1].as_u64().unwrap_or(0) as usize).section(v[0].as_u64().unwrap_or(0) as usize)
    });
    let strategy = strategy(params);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let mut sections: Vec<(String, Vec<String>)> = SECTIONS
                .iter()
                .map(|(title, names)| (title.to_string(), names.iter().map(|n| n.to_string()).collect()))
                .collect();
            if let Some(last) = sections.last_mut() {
                let start = last.1.len();
                last.1.extend((start..start + extra).map(|i| format!("Item {}", i + 1)));
            }
            let state = cx.new(|cx| ListState::new(SectionRows { sections, row_headers }, window, cx));
            if let Some(ix) = selected {
                state.update(cx, |s, cx| s.set_selected_index(Some(ix), window, cx));
            }
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<ListState<SectionRows>>().ok())
            .expect("list state");
        // On a later frame, as in `builder`.
        if !view.state.toggled && view.state.value > 0. {
            view.state.toggled = true;
            if let Some(ix) = scroll_to {
                state.update(cx, |s, cx| s.scroll_to_item(ix, strategy, window, cx));
            }
        }
        view.state.value += 1.;
        List::new(&state).w(px(width)).h(px(height)).into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    // delegate.rs: the empty list's Inbox icon.
    out.push(("list.empty".into(), theme.muted_foreground.opacity(0.6).into()));
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
    // VirtualListScrollHandle::scroll_to_item: Center, or the nearer edge for the rest.
    let scroll_to = params.get("scroll_to").and_then(|v| v.as_u64()).map(|ix| ix as usize);
    let strategy = strategy(params);
    let row = move |i: usize| if uniform { 34. } else { [30., 45., 60.][i % 3] };
    let sizes = Rc::new((0..count).map(|i| size(px(width), px(row(i)))).collect::<Vec<_>>());
    let handle = VirtualListScrollHandle::new();
    let applied = Rc::new(Cell::new(false));
    Ok(Rc::new(move |_, _, cx| {
        if offset > 0. && !applied.replace(true) {
            handle.set_offset(point(px(0.), px(-offset)));
        }
        if let Some(ix) = scroll_to
            && !applied.replace(true)
        {
            handle.scroll_to_item(ix, strategy);
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
