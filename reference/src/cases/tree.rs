//! `Tree` (crates/component/src/tree.rs) with GPUI Kit's canonical row
//! (component-shell, docs, story): a ListItem indented 16px per level with a
//! folder, open-folder or file icon before the label.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, ParentElement as _, ScrollStrategy, SharedString, Styled as _,
    prelude::FluentBuilder as _, px,
    component::{
        ActiveTheme as _, Icon, IconName, h_flex,
        list::ListItem,
        tree::{TreeItem, TreeState, tree},
    },
};
use std::rc::Rc;

fn sample_items() -> Vec<TreeItem> {
    vec![
        TreeItem::new("src", "src").expanded(true).children([
            TreeItem::new("src/components", "components").expanded(true).children([
                TreeItem::new("src/components/button.rs", "button.rs"),
                TreeItem::new("src/components/tree.rs", "tree.rs"),
            ]),
            TreeItem::new("src/lib.rs", "lib.rs"),
        ]),
        TreeItem::new("assets", "assets")
            .disabled(true)
            .child(TreeItem::new("assets/logo.svg", "logo.svg")),
        TreeItem::new("Cargo.toml", "Cargo.toml"),
    ]
}

/// A deeper tree for the Tree cases (uikit-tree): only `src` is expanded, so
/// `ui/widgets` holds a hidden third level, with `extra` more files.
fn deep_items(extra: usize) -> Vec<TreeItem> {
    vec![
        TreeItem::new("src", "src").expanded(true).children([
            TreeItem::new("src/components", "components").children([
                TreeItem::new("src/components/button.rs", "button.rs"),
                TreeItem::new("src/components/list.rs", "list.rs"),
            ]),
            TreeItem::new("src/ui", "ui").children([
                TreeItem::new("src/ui/widgets", "widgets")
                    .children([
                        TreeItem::new("src/ui/widgets/input.rs", "input.rs"),
                        TreeItem::new("src/ui/widgets/select.rs", "select.rs"),
                    ])
                    .children((0..extra).map(|i| {
                        TreeItem::new(format!("src/ui/widgets/widget_{i:02}.rs"), format!("widget_{i:02}.rs"))
                    })),
                TreeItem::new("src/ui/theme.rs", "theme.rs"),
            ]),
            TreeItem::new("src/lib.rs", "lib.rs"),
        ]),
        TreeItem::new("docs", "docs").child(TreeItem::new("docs/guide.md", "guide.md")),
        TreeItem::new("Cargo.toml", "Cargo.toml"),
    ]
}

/// A big tree for the virtualized cases: `src` holds 200 files.
fn big_items() -> Vec<TreeItem> {
    vec![
        TreeItem::new("src", "src")
            .expanded(true)
            .children((0..200).map(|i| TreeItem::new(format!("src/file_{i:03}.rs"), format!("file_{i:03}.rs")))),
        TreeItem::new("Cargo.toml", "Cargo.toml"),
    ]
}

pub fn builder(params: &Params) -> Result<Builder> {
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 238.);
    let selected = params.get("selected").and_then(|v| v.as_u64()).map(|v| v as usize);
    let rounded = param_bool(params, "rounded");
    let items = param_str(params, "items", "sample").to_string();
    let extra = param_f32(params, "extra", 0.) as usize;
    // reveal_item: expands the item's ancestors and scrolls it into view; the
    // case also selects it.
    let reveal = params.get("reveal").and_then(|v| v.as_str()).map(|v| SharedString::from(v.to_string()));
    let strategy = match param_str(params, "strategy", "top") {
        "center" => ScrollStrategy::Center,
        "bottom" => ScrollStrategy::Bottom,
        _ => ScrollStrategy::Top,
    };
    Ok(Rc::new(move |view, _, cx| {
        if view.state.entity.is_none() {
            let items = match items.as_str() {
                "deep" => deep_items(extra),
                "big" => big_items(),
                _ => sample_items(),
            };
            let state = cx.new(|cx| TreeState::new(cx).items(items));
            state.update(cx, |s, cx| s.set_selected_index(selected, cx));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TreeState>().ok())
            .expect("tree state");
        // On a later frame: the first one is drawn before the harness pins the
        // clock the scrollbar reads.
        if let Some(id) = reveal.clone()
            && !view.state.toggled
            && view.state.value > 0.
        {
            view.state.toggled = true;
            state.update(cx, |s, cx| {
                s.reveal_item(&id, strategy, cx);
                let ix = s.index_of(&id);
                s.set_selected_index(ix, cx);
            });
        }
        view.state.value += 1.;
        let (radius, border) = (cx.theme().radius, cx.theme().border);
        tree(&state, move |ix, entry, selected, _, _| {
            let icon = if !entry.is_folder() {
                IconName::File
            } else if entry.is_expanded() {
                IconName::FolderOpen
            } else {
                IconName::Folder
            };
            ListItem::new(ix)
                .selected(selected)
                .w_full()
                .when(rounded, |this| this.rounded(radius))
                .px_3()
                .pl(px(16.) * entry.depth() + px(12.))
                .child(h_flex().gap_2().child(Icon::new(icon)).child(entry.item().label.clone()))
        })
        .when(rounded, |this| this.p_1().border_1().border_color(border).rounded(radius))
        .w(px(width))
        .h(px(height))
        .into_any_element()
    }))
}
