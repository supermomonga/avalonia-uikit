//! Builders that turn a case's parameters into a GPUI Kit element, and the
//! interactions that put it into the case's state.
use crate::{
    harness::{Builder, CaseWindow, Harness},
    manifest::{Case, Params, param_bool, param_f32, param_str},
};
use anyhow::{Result, bail};
use gpui_kit::{
    MouseButton,
    component::{IconName, Size},
};
use std::time::Duration;

mod accordion;
mod alert;
mod avatar;
mod badge;
mod bubble;
mod description_list;
mod form;
mod hover_card;
mod marker;
mod message;
mod shimmer;
mod stepper;
mod breadcrumb;
mod clipboard;
mod empty;
mod kbd;
mod rating;
mod skeleton;
mod status_bar;
mod tag;
mod button;
mod carousel;
mod color_picker;
mod check;
mod display;
mod dock;
pub mod menu;
mod notification;
mod number;
mod pagination;
pub mod popover;
mod progress;
mod resizable;
mod icon;
pub mod image;
mod input;
mod label;
mod list;
mod scroll;
mod select;
mod sheet;
mod sidebar;
mod slider;
mod surface;
mod table;
mod tabs;
pub mod time;
mod title_bar;
mod toolbar;
mod toggle;
mod tree;

pub fn builder(case: &Case) -> Result<Builder> {
    let params = effective_params(case);
    match case.component.as_str() {
        "surface" => surface::builder(&params),
        "button" => button::builder(&params),
        "button-loading" => button::builder(&params),
        "dropdownbutton" => button::builder(&params),
        "buttongroup" => button::group(&params),
        "uikit-buttongroup" => button::group(&params),
        "toggle" => toggle::builder(&params),
        "togglegroup" => toggle::group(&params),
        "uikit-togglegroup" => toggle::group(&params),
        "checkbox" => check::checkbox(&params),
        "radio" => check::radio(&params),
        "switch" => check::switch(&params),
        "dropdown" => menu::dropdown(&params),
        "context" => menu::context(&params),
        "split" => menu::split(&params),
        "split-loading" => menu::split(&params),
        "menubar" => menu::menubar(&params),
        "number" => number::builder(&params),
        "groupbox" => display::group_box(&params),
        "separator" => display::separator(&params),
        "link" => display::link(&params),
        "progress" => progress::progress(&params),
        "progress-circle" => progress::circle(&params),
        "spinner" => progress::spinner(&params),
        "tooltip" => button::tooltip(&params),
        "scroll" => scroll::builder(&params),
        "icon" => icon::builder(&params),
        "uikit-icon" => icon::builder(&params),
        "label" => label::builder(&params),
        "uikit-label" => label::builder(&params),
        "input" => input::builder(&params),
        "input-menu" => input::builder(&params),
        "textarea" => input::textarea(&params),
        "input-group" => input::input_group(&params),
        "input-group-loading" => input::input_group(&params),
        "uikit-input" => input::uikit_input(&params),
        "uikit-inputgroup" => input::input_group(&params),
        "uikit-number" => number::builder(&params),
        "uikit-slider" => slider::builder(&params),
        "list" => list::builder(&params),
        "virtual" => list::virtual_list(&params),
        "select" => select::builder(&params),
        "pagination" => pagination::builder(&params),
        "uikit-pagination" => pagination::builder(&params),
        "combobox" => select::combobox(&params),
        "uikit-select" => select::uikit_select(&params),
        "uikit-combobox" => select::uikit_combobox(&params),
        "tree" => tree::builder(&params),
        "uikit-list" => list::builder(&params),
        "uikit-tree" => tree::builder(&params),
        "tabs" => tabs::builder(&params),
        "tabalonia" => tabs::builder(&params),
        "tabs-bar" => tabs::builder(&params),
        "dock" => dock::builder(&params),
        "table" => table::builder(&params),
        "uikit-table" => table::builder(&params),
        "carousel" => carousel::builder(&params),
        "uikit-carousel" => carousel::builder(&params),
        "sidebar" => sidebar::builder(&params),
        "uikit-sidebar" => sidebar::builder(&params),
        "sheet" => sheet::builder(&params),
        "uikit-sheet" => sheet::builder(&params),
        "titlebar" => title_bar::builder(&params),
        "uikit-titlebar" => title_bar::builder(&params),
        "color_picker" => color_picker::builder(&params),
        "uikit-colorselect" => color_picker::builder(&params),
        "datatable" => table::data_table(&params),
        "datagrid" => table::data_table(&params),
        "toolbar" => toolbar::builder(&params),
        "uikit-toolbar" => toolbar::builder(&params),
        "popover" => popover::popover(&params),
        "accordion" => accordion::builder(&params),
        "uikit-accordion" => accordion::builder(&params),
        "notification" => notification::notification(&params),
        "uikit-notification" => notification::notification(&params),
        "resizable" => resizable::builder(&params),
        "uikit-resizable" => resizable::builder(&params),
        "image" => image::builder(&params),
        "uikit-image" => image::builder(&params),
        "calendar" => time::calendar(&params),
        "datepicker" => time::date_picker(&params),
        "timefield" => time::time_field(&params),
        "uikit-calendar" => time::calendar(&params),
        "uikit-datepicker" => time::date_picker(&params),
        "uikit-timefield" => time::time_field(&params),
        "collapsible" => accordion::collapsible(&params),
        "slider" => slider::builder(&params),
        "badge" => badge::builder(&params),
        "tag" => tag::builder(&params),
        "alert" => alert::builder(&params),
        "skeleton" => skeleton::builder(&params),
        "statusbar" => status_bar::builder(&params),
        "breadcrumb" => breadcrumb::builder(&params),
        "kbd" => kbd::builder(&params),
        "clipboard" => clipboard::builder(&params),
        "rating" => rating::builder(&params),
        "avatar" => avatar::builder(&params),
        "avatargroup" => avatar::group(&params),
        "empty" => empty::builder(&params),
        "descriptionlist" => description_list::builder(&params),
        "stepper" => stepper::builder(&params),
        "form" => form::builder(&params),
        "hovercard" => hover_card::builder(&params),
        "shimmer" => shimmer::builder(&params),
        "marker" => marker::builder(&params),
        "bubble" => bubble::builder(&params),
        "message" => message::builder(&params),
        other => bail!("unknown component {other}"),
    }
}

/// The parameters with the `disabled` state folded in.
pub fn effective_params(case: &Case) -> Params {
    let mut params = case.params.clone();
    if case.state.split('+').any(|part| part == "disabled") {
        params.insert("disabled".into(), true.into());
    }
    params
}

fn pointer(harness: &Harness, window: &CaseWindow, params: &Params) -> Result<gpui_kit::Point<gpui_kit::Pixels>> {
    let fx = param_f32(params, "pointer_x", 0.5);
    let fy = param_f32(params, "pointer_y", 0.5);
    harness.component_point(window, (fx, fy))
}

/// Puts the component into `state` after the window's first frame.
pub fn drive(harness: &mut Harness, window: &CaseWindow, case: &Case, state: &str) -> Result<()> {
    let params = effective_params(case);
    // Where the pointer last went, for "release".
    let mut last = pointer(harness, window, &params)?;
    for part in state.split('+') {
        match part {
            "normal" | "disabled" => {}
            "hover" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
                last = at;
            }
            "pressed" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
                harness.mouse_down(window, at, MouseButton::Left)?;
                last = at;
            }
            // Releases the left button where the pointer is.
            "release" => harness.mouse_up(window, last, MouseButton::Left)?,
            "focus" => harness.tab(window)?,
            // Makes the window active: GPUI paints a caret and a selection only then.
            "activate" => harness.activate(window)?,
            "click" => {
                let at = pointer(harness, window, &params)?;
                harness.click(window, at, MouseButton::Left)?;
            }
            "right-click" => {
                let at = pointer(harness, window, &params)?;
                harness.click(window, at, MouseButton::Right)?;
            }
            "leave" => {
                let bounds = harness.component_bounds(window)?;
                let outside = gpui_kit::point(
                    bounds.origin.x + bounds.size.width + gpui_kit::px(40.),
                    bounds.origin.y + bounds.size.height + gpui_kit::px(40.),
                );
                harness.mouse_move(window, outside, None)?;
            }
            "tooltip" => {
                let at = pointer(harness, window, &params)?;
                harness.mouse_move(window, at, None)?;
                harness.advance(window, Duration::from_millis(600))?;
            }
            // Absolute window positions: "at-X-Y" moves the pointer there,
            // "click-at-X-Y" and "right-click-at-X-Y" also click.
            // "drag-at-X-Y" moves the pointer there with the left button held.
            other if other.starts_with("drag-at-") => {
                let (x, y) = other["drag-at-".len()..].split_once('-').expect("drag-at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                harness.mouse_move(window, at, Some(MouseButton::Left))?;
                last = at;
            }
            other if other.starts_with("pressed-at-") => {
                let (x, y) = other["pressed-at-".len()..].split_once('-').expect("pressed-at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                harness.mouse_move(window, at, None)?;
                harness.mouse_down(window, at, MouseButton::Left)?;
                last = at;
            }
            other if other.starts_with("at-") || other.starts_with("click-at-") || other.starts_with("right-click-at-") => {
                let (kind, coords) = other.split_once("at-").expect("prefix checked");
                let (x, y) = coords.split_once('-').expect("at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                match kind {
                    "click-" => harness.click(window, at, MouseButton::Left)?,
                    "right-click-" => harness.click(window, at, MouseButton::Right)?,
                    _ => harness.mouse_move(window, at, None)?,
                }
                last = at;
            }
            // "wheel-at-X-Y" scrolls the content under that point down by 40px.
            other if other.starts_with("wheel-at-") => {
                let (x, y) = other["wheel-at-".len()..].split_once('-').expect("wheel-at-X-Y");
                let at = gpui_kit::point(gpui_kit::px(x.parse()?), gpui_kit::px(y.parse()?));
                harness.wheel(window, at, 40.)?;
            }
            other if other.starts_with("wait-") => {
                let ms: u64 = other["wait-".len()..].trim_end_matches("ms").parse()?;
                harness.advance(window, Duration::from_millis(ms))?;
            }
            other if other.starts_with("key-") => {
                harness.key(window, &other["key-".len()..])?;
            }
            other => bail!("unknown state {other} in case {}", case.id),
        }
    }
    Ok(())
}

pub fn size(params: &Params) -> Size {
    match param_str(params, "size", "medium") {
        "xsmall" => Size::XSmall,
        "small" => Size::Small,
        "large" => Size::Large,
        _ => Size::Medium,
    }
}

pub fn icon(name: &str) -> Option<IconName> {
    Some(match name {
        "check" => IconName::Check,
        "plus" => IconName::Plus,
        "minus" => IconName::Minus,
        "chevron-down" => IconName::ChevronDown,
        "chevron-right" => IconName::ChevronRight,
        "copy" => IconName::Copy,
        "search" => IconName::Search,
        "info" => IconName::Info,
        "eye" => IconName::Eye,
        "eye-off" => IconName::EyeOff,
        "close" => IconName::Close,
        "chevron-left" => IconName::ChevronLeft,
        "chevron-up" => IconName::ChevronUp,
        "calendar" => IconName::Calendar,
        "loader" => IconName::Loader,
        "loader-circle" => IconName::LoaderCircle,
        "undo-2" => IconName::Undo2,
        "redo-2" => IconName::Redo2,
        "ellipsis" => IconName::Ellipsis,
        "bell" => IconName::Bell,
        "star" => IconName::Star,
        "star-fill" => IconName::StarFill,
        "user" => IconName::User,
        "folder" => IconName::Folder,
        "inbox" => IconName::Inbox,
        "circle-check" => IconName::CircleCheck,
        "triangle-alert" => IconName::TriangleAlert,
        "circle-x" => IconName::CircleX,
        "palette" => IconName::Palette,
        _ => return None,
    })
}

/// What a component paints with a derived color: a solid color, or a theme
/// token's background, which a theme may give as a gradient (Aurora).
pub type Paint = gpui_kit::Background;

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, Paint)>) {
    button::derived_colors(theme, out);
    check::derived_colors(theme, out);
    menu::derived_colors(theme, out);
    number::derived_colors(theme, out);
    input::derived_colors(theme, out);
    list::derived_colors(theme, out);
    select::derived_colors(theme, out);
    display::derived_colors(theme, out);
    progress::derived_colors(theme, out);
    tabs::derived_colors(theme, out);
    slider::derived_colors(theme, out);
    time::derived_colors(theme, out);
    title_bar::derived_colors(theme, out);
    tag::derived_colors(theme, out);
    alert::derived_colors(theme, out);
    avatar::derived_colors(theme, out);
    skeleton::derived_colors(theme, out);
    bubble::derived_colors(theme, out);
}

pub fn disabled(params: &Params) -> bool {
    param_bool(params, "disabled")
}
