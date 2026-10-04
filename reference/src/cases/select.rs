//! `Select` (crates/component/src/select.rs) over a list of fruits.
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, ParentElement as _, SharedString, Styled as _, div,
    prelude::FluentBuilder as _, px,
    component::{
        ActiveTheme as _, Colorize as _, Icon, IconName, IndexPath, Sizable as _, Theme,
        button::{Button, ButtonVariants as _},
        combobox::{Caret, Combobox, ComboboxState},
        h_flex,
        searchable_list::{SearchableListChange, SearchableListDelegate, SearchableListItem},
        select::{SearchableVec, Select, SelectGroup, SelectItem, SelectState},
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
    // select.rs cleanable: a clear button stands in for the caret while a value is selected.
    let cleanable = param_bool(params, "cleanable");
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
            .cleanable(cleanable)
            .w(px(width))
            .into_any_element()
    }))
}

/// `Combobox` (crates/component/src/combobox.rs), single select and not searchable.
pub fn combobox(params: &Params) -> Result<Builder> {
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
            let ix: Vec<IndexPath> = if selected >= 0 { vec![IndexPath::new(selected as usize)] } else { vec![] };
            let state = cx.new(|cx| ComboboxState::new(items, ix, window, cx));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<ComboboxState<Vec<Fruit>>>().ok())
            .expect("combobox state");
        Combobox::new(&state)
            .placeholder(placeholder.clone())
            .with_size(size)
            .disabled(disabled)
            .w(px(width))
            .into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    // searchable_list/item.rs: a hovered row; select.rs: the empty view's icon
    // and a disabled frame (before the frame fades to half).
    out.push(("select.row.hover".into(), theme.accent.opacity(0.7).into()));
    out.push(("select.empty".into(), theme.muted_foreground.opacity(0.6).into()));
    out.push(("select.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8).into()));
}

// MARK: uikit:Select

/// What uikit:Select's own cases (uikit-select, uikit-combobox) add to the
/// parameters of select.toml and combobox.toml. A case without any of them is
/// built by `builder` / `combobox` unchanged, so the new ids repeat those
/// goldens exactly.
const EXTRA_PARAMS: [&str; 13] = [
    "searchable", "cleanable", "groups", "title_prefix", "menu_width", "icon", "empty", "plain",
    "search_placeholder", "multiple", "checked", "footer", "trigger",
];

fn extended(params: &Params) -> bool {
    EXTRA_PARAMS.iter().any(|key| params.contains_key(*key)) || params.contains_key("max")
}

/// The sections of the grouped cases: three fruits and three vegetables.
const GROUPS: [(&str, [&str; 3]); 2] =
    [("Fruits", ["Apple", "Banana", "Cherry"]), ("Vegetables", ["Carrot", "Leek", "Potato"])];

struct Extra {
    size: gpui_kit::component::Size,
    disabled: bool,
    width: f32,
    placeholder: String,
    count: usize,
    disabled_row: i32,
    /// Flat indices of the selected rows (`selected`, or the '1's of `checked`).
    selected: Vec<usize>,
    searchable: bool,
    cleanable: bool,
    groups: bool,
    title_prefix: Option<String>,
    menu_width: Option<f32>,
    icon: Option<IconName>,
    empty: bool,
    plain: bool,
    search_placeholder: Option<String>,
    multiple: bool,
    footer: bool,
    trigger: bool,
    max: usize,
}

impl Extra {
    fn new(params: &Params) -> Self {
        let selected = match params.get("checked").and_then(|v| v.as_str()) {
            Some(checked) => checked.chars().enumerate().filter(|(_, c)| *c == '1').map(|(i, _)| i).collect(),
            None => {
                let selected = param_f32(params, "selected", -1.) as i32;
                if selected >= 0 { vec![selected as usize] } else { vec![] }
            }
        };
        Self {
            size: size(params),
            disabled: disabled(params),
            width: param_f32(params, "width", 200.),
            placeholder: param_str(params, "placeholder", "Select a fruit").to_string(),
            count: param_f32(params, "count", 4.) as usize,
            disabled_row: param_f32(params, "disabled_row", -1.) as i32,
            selected,
            searchable: param_bool(params, "searchable"),
            cleanable: param_bool(params, "cleanable"),
            groups: param_bool(params, "groups"),
            title_prefix: params.get("title_prefix").and_then(|v| v.as_str()).map(str::to_string),
            menu_width: params.get("menu_width").and_then(|v| v.as_f64()).map(|v| v as f32),
            icon: params.get("icon").and_then(|v| v.as_str()).and_then(super::icon),
            empty: param_bool(params, "empty"),
            plain: param_bool(params, "plain"),
            search_placeholder: params.get("search_placeholder").and_then(|v| v.as_str()).map(str::to_string),
            multiple: param_bool(params, "multiple"),
            footer: param_bool(params, "footer"),
            trigger: param_bool(params, "trigger"),
            max: param_f32(params, "max", 0.) as usize,
        }
    }

    fn fruits(&self) -> SearchableVec<Fruit> {
        SearchableVec::new(
            super::list::names(self.count)
                .into_iter()
                .enumerate()
                .map(|(i, title)| Fruit { title, disabled: i as i32 == self.disabled_row })
                .collect::<Vec<_>>(),
        )
    }

    fn groups(&self) -> SearchableVec<SelectGroup<Fruit>> {
        let mut flat = 0;
        SearchableVec::new(
            GROUPS
                .iter()
                .map(|(title, items)| {
                    SelectGroup::new(*title).items(items.iter().map(|item| {
                        let fruit = Fruit { title: item.to_string(), disabled: flat == self.disabled_row };
                        flat += 1;
                        fruit
                    }))
                })
                .collect::<Vec<_>>(),
        )
    }

    /// The selection as index paths: grouped rows count through the sections.
    fn indices(&self) -> Vec<IndexPath> {
        self.selected
            .iter()
            .map(|&flat| {
                if self.groups {
                    let section = flat / 3;
                    IndexPath::new(flat % 3).section(section)
                } else {
                    IndexPath::new(flat)
                }
            })
            .collect()
    }
}

/// select.rs's empty view replaced as the Select story does: "No Data" in a 96px row.
fn no_data(cx: &gpui_kit::App) -> gpui_kit::Div {
    h_flex().h_24().justify_center().text_color(cx.theme().muted_foreground).child("No Data")
}

fn select_element<D>(state: &gpui_kit::Entity<SelectState<D>>, extra: &Extra) -> gpui_kit::AnyElement
where
    D: SearchableListDelegate + 'static,
    <D::Item as SearchableListItem>::Value: PartialEq + Clone,
{
    let mut select = Select::new(state)
        .placeholder(extra.placeholder.clone())
        .with_size(extra.size)
        .disabled(extra.disabled)
        .cleanable(extra.cleanable)
        .appearance(!extra.plain)
        .w(px(extra.width));
    if let Some(prefix) = &extra.title_prefix {
        select = select.title_prefix(prefix.clone());
    }
    if let Some(width) = extra.menu_width {
        select = select.menu_width(px(width));
    }
    if let Some(icon) = extra.icon.clone() {
        select = select.icon(icon);
    }
    if let Some(placeholder) = &extra.search_placeholder {
        select = select.search_placeholder(placeholder.clone());
    }
    if extra.empty {
        select = select.empty(|_, cx| no_data(cx));
    }
    select.into_any_element()
}

/// `Select` with search, groups, clear, a title prefix, a menu width, an icon,
/// an empty view or no appearance; select.toml's parameters otherwise.
pub fn uikit_select(params: &Params) -> Result<Builder> {
    if !extended(params) {
        return builder(params);
    }
    let extra = Rc::new(Extra::new(params));
    Ok(Rc::new(move |view, window, cx| {
        let extra = extra.clone();
        if extra.groups {
            if view.state.entity.is_none() {
                let ix = extra.indices().first().copied();
                let searchable = extra.searchable;
                let groups = extra.groups();
                let state = cx.new(|cx| SelectState::new(groups, ix, window, cx).searchable(searchable));
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<SelectState<SearchableVec<SelectGroup<Fruit>>>>().ok())
                .expect("grouped select state");
            select_element(&state, &extra)
        } else {
            if view.state.entity.is_none() {
                let ix = extra.indices().first().copied();
                let searchable = extra.searchable;
                let fruits = extra.fruits();
                let state = cx.new(|cx| SelectState::new(fruits, ix, window, cx).searchable(searchable));
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<SelectState<SearchableVec<Fruit>>>().ok())
                .expect("select state");
            select_element(&state, &extra)
        }
    }))
}

/// The fruits, keeping at most `max` selected (on_will_change, as the
/// Combobox story's "Maximum selections").
struct AtMost {
    items: SearchableVec<Fruit>,
    max: usize,
}

impl SearchableListDelegate for AtMost {
    type Item = Fruit;

    fn items_count(&self, section: usize) -> usize {
        self.items.items_count(section)
    }

    fn item(&self, ix: IndexPath) -> Option<&Fruit> {
        self.items.item(ix)
    }

    fn position<V>(&self, value: &V) -> Option<IndexPath>
    where
        Fruit: SearchableListItem<Value = V>,
        V: PartialEq,
    {
        self.items.position(value)
    }

    fn perform_search(&mut self, query: &str, window: &mut gpui_kit::Window, cx: &mut gpui_kit::App) -> gpui_kit::Task<()> {
        self.items.perform_search(query, window, cx)
    }

    fn on_will_change(&mut self, selection: &mut Vec<(IndexPath, Fruit)>, changes: &[SearchableListChange]) {
        for change in changes {
            match change {
                SearchableListChange::Deselect { index } => selection.retain(|(ix, _)| ix != index),
                SearchableListChange::Select { index } => {
                    if selection.len() < self.max
                        && let Some(item) = self.item(*index)
                        && !selection.iter().any(|(ix, _)| ix == index)
                    {
                        selection.push((*index, item.clone()));
                    }
                }
            }
        }
    }
}

fn combobox_element<D>(state: &gpui_kit::Entity<ComboboxState<D>>, extra: &Extra) -> gpui_kit::AnyElement
where
    D: SearchableListDelegate<Item = Fruit> + 'static,
{
    let mut combobox = Combobox::new(state)
        .placeholder(extra.placeholder.clone())
        .with_size(extra.size)
        .disabled(extra.disabled)
        .cleanable(extra.cleanable)
        .appearance(!extra.plain)
        .w(px(extra.width));
    if let Some(width) = extra.menu_width {
        combobox = combobox.menu_width(px(width));
    }
    if let Some(icon) = extra.icon.clone() {
        combobox = combobox.icon(icon);
    }
    if let Some(placeholder) = &extra.search_placeholder {
        combobox = combobox.search_placeholder(placeholder.clone());
    }
    if extra.empty {
        combobox = combobox.empty(|_, cx| no_data(cx));
    }
    if extra.footer {
        // The Combobox story's "Footer": a full-width ghost button below the list.
        combobox = combobox.footer(|_, cx| {
            Button::new("add")
                .ghost()
                .label("New fruit")
                .icon(Icon::new(IconName::Plus))
                .text_color(cx.theme().foreground)
                .w_full()
                .justify_start()
                .into_any_element()
        });
    }
    if extra.trigger {
        // The Combobox story's "Icons" trigger with one icon: a star, the
        // title (or the muted placeholder) and the caret.
        let placeholder = extra.placeholder.clone();
        combobox = combobox.render_trigger(move |trigger, _, cx| {
            let title = match trigger.selection() {
                [] => None,
                [(_, item)] => Some(item.title()),
                items => Some(SharedString::from(format!("{} selected", items.len()))),
            };
            h_flex()
                .w_full()
                .gap_2()
                .items_center()
                .child(Icon::new(IconName::Star).small().text_color(cx.theme().muted_foreground))
                .child(
                    div()
                        .w_full()
                        .overflow_hidden()
                        .truncate()
                        .when_some(title, |this, title| this.child(title))
                        .when(trigger.selection().is_empty(), |this| {
                            this.text_color(cx.theme().muted_foreground).child(placeholder.clone())
                        }),
                )
                .child(Caret::new(trigger.size()).text_color(cx.theme().muted_foreground))
                .into_any_element()
        });
    }
    combobox.into_any_element()
}

/// `Combobox` with search, multiple selection, a footer, a custom trigger,
/// clear, an empty view or a selection limit; combobox.toml's parameters otherwise.
pub fn uikit_combobox(params: &Params) -> Result<Builder> {
    if !extended(params) {
        return combobox(params);
    }
    let extra = Rc::new(Extra::new(params));
    Ok(Rc::new(move |view, window, cx| {
        let extra = extra.clone();
        if extra.max > 0 {
            if view.state.entity.is_none() {
                let delegate = AtMost { items: extra.fruits(), max: extra.max };
                let (indices, multiple, searchable) = (extra.indices(), extra.multiple, extra.searchable);
                let state = cx.new(|cx| {
                    ComboboxState::new(delegate, indices, window, cx).multiple(multiple).searchable(searchable)
                });
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<ComboboxState<AtMost>>().ok())
                .expect("limited combobox state");
            combobox_element(&state, &extra)
        } else if extra.groups {
            if view.state.entity.is_none() {
                let groups = extra.groups();
                let (indices, multiple, searchable) = (extra.indices(), extra.multiple, extra.searchable);
                let state = cx.new(|cx| {
                    ComboboxState::new(groups, indices, window, cx).multiple(multiple).searchable(searchable)
                });
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<ComboboxState<SearchableVec<SelectGroup<Fruit>>>>().ok())
                .expect("grouped combobox state");
            combobox_element(&state, &extra)
        } else {
            if view.state.entity.is_none() {
                let fruits = extra.fruits();
                let (indices, multiple, searchable) = (extra.indices(), extra.multiple, extra.searchable);
                let state = cx.new(|cx| {
                    ComboboxState::new(fruits, indices, window, cx).multiple(multiple).searchable(searchable)
                });
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<ComboboxState<SearchableVec<Fruit>>>().ok())
                .expect("combobox state");
            combobox_element(&state, &extra)
        }
    }))
}
