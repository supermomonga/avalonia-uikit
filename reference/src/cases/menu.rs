//! Menus: `DropdownButton` (button/dropdown_button.rs), the dropdown and
//! context menus (menu/popup_menu.rs, dropdown_menu.rs, context_menu.rs) and
//! `AppMenuBar` (menu/app_menu_bar.rs).
use super::{disabled, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::Result;
use gpui_kit::{
    App, Context, InteractiveElement as _, IntoElement as _, KeyBinding, Menu,
    MenuItem, ParentElement as _, Styled as _, Window, div, px,
    component::{
        Disableable as _, GlobalState, Sizable as _,
        button::{Button, ButtonVariants as _, DropdownButton},
        menu::{AppMenuBar, ContextMenuExt as _, DropdownMenu as _, PopupMenu},
    },
};
use std::rc::Rc;

gpui_kit::actions!(
    case,
    [NewFile, Refresh, Wrap, Rename, Copy, Paste, Undo, Redo, About, Quit, Zoom]
);

/// Key bindings so the menus show a shortcut. A gesture without modifiers is
/// formatted the same on every platform (R22).
pub fn init(cx: &mut App) {
    cx.bind_keys([KeyBinding::new("f5", Refresh, None)]);
}

/// The menu every popup case shows.
fn standard_menu(menu: PopupMenu, window: &mut Window, cx: &mut Context<PopupMenu>) -> PopupMenu {
    menu.menu("New File", Box::new(NewFile))
        .menu("Refresh", Box::new(Refresh))
        .menu_with_check("Word Wrap", true, Box::new(Wrap))
        .separator()
        .menu_with_disabled("Rename", Box::new(Rename), true)
        .submenu("Edit", window, cx, |menu, _, _| {
            menu.menu("Copy", Box::new(Copy)).menu("Paste", Box::new(Paste))
        })
}

/// A Default button that opens the standard menu 4px below it.
pub fn dropdown(params: &Params) -> Result<Builder> {
    let label = param_str(params, "label", "Open").to_string();
    Ok(Rc::new(move |_, _, _| {
        Button::new("case")
            .label(label.clone())
            .dropdown_menu(standard_menu)
            .into_any_element()
    }))
}

/// A bordered area with the standard menu on right click.
pub fn context(_params: &Params) -> Result<Builder> {
    Ok(Rc::new(move |_, _, cx| {
        let border = gpui_kit::component::ActiveTheme::theme(&**cx).border;
        div()
            .id("case")
            .w(px(120.))
            .h(px(48.))
            .border_1()
            .border_color(border)
            .context_menu(standard_menu)
            .into_any_element()
    }))
}

pub fn split(params: &Params) -> Result<Builder> {
    let variant = super::button::variant(param_str(params, "variant", "default"))?;
    let outline = param_bool(params, "outline");
    let selected = param_bool(params, "selected");
    let disabled = disabled(params);
    let size = size(params);
    let label = param_str(params, "label", "Save").to_string();
    Ok(Rc::new(move |_, _, _| {
        let mut button = DropdownButton::new("case")
            .button(Button::new("action").label(label.clone()))
            .dropdown_menu(standard_menu)
            .with_variant(variant)
            .with_size(size)
            .disabled(disabled);
        if outline {
            button = button.outline();
        }
        if selected {
            button = gpui_kit::component::Selectable::selected(button, true);
        }
        button.into_any_element()
    }))
}

pub fn menubar(_params: &Params) -> Result<Builder> {
    Ok(Rc::new(move |view, _, cx| {
        if view.state.entity.is_none() {
            let menus = vec![
                Menu {
                    name: "File".into(),
                    items: vec![
                        MenuItem::action("New File", NewFile),
                        MenuItem::action("Refresh", Refresh),
                        MenuItem::Separator,
                        MenuItem::action("Quit", Quit),
                    ],
                    disabled: false,
                },
                Menu {
                    name: "Edit".into(),
                    items: vec![
                        MenuItem::action("Undo", Undo),
                        MenuItem::action("Redo", Redo),
                    ],
                    disabled: false,
                },
                Menu {
                    name: "View".into(),
                    items: vec![MenuItem::action("Zoom", Zoom)],
                    disabled: false,
                },
            ];
            let owned = menus.into_iter().map(|menu| menu.owned()).collect();
            GlobalState::global_mut(cx).set_app_menus(owned);
            view.state.entity = Some(AppMenuBar::new(cx).into());
        }
        let bar = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<AppMenuBar>().ok())
            .expect("menu bar entity");
        div().id("case").child(bar).into_any_element()
    }))
}


pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    // styled.rs popover_ring: the hairline ring standing in for a popover's border.
    out.push(("popover-ring".into(), theme.foreground.alpha(0.1)));
    // button.rs: the dropdown caret is the normal foreground at 75%.
    for name in super::button::VARIANTS {
        for outline in [false, true] {
            let variant = super::button::variant(name).expect("known variant");
            let fg = super::button::normal_foreground(theme, variant, outline);
            out.push((
                format!("button.{name}{}.caret", if outline { ".outline" } else { "" }),
                fg.opacity(0.75),
            ));
        }
    }
}
