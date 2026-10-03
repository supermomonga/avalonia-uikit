//! `Tree` (crates/component/src/tree.rs) with GPUI Kit's canonical row
//! (component-shell, docs, story): a ListItem indented 16px per level with a
//! folder, open-folder or file icon before the label.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, IntoElement as _, ParentElement as _, Styled as _, prelude::FluentBuilder as _, px,
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

pub fn builder(params: &Params) -> Result<Builder> {
    let width = param_f32(params, "width", 240.);
    let height = param_f32(params, "height", 238.);
    let selected = params.get("selected").and_then(|v| v.as_u64()).map(|v| v as usize);
    let rounded = param_bool(params, "rounded");
    Ok(Rc::new(move |view, _, cx| {
        if view.state.entity.is_none() {
            let state = cx.new(|cx| TreeState::new(cx).items(sample_items()));
            state.update(cx, |s, cx| s.set_selected_index(selected, cx));
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TreeState>().ok())
            .expect("tree state");
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
