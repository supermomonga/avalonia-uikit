//! `Input` (crates/component/src/input/input.rs) on an `InputState`.
use super::{disabled, icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, DismissEvent, Entity, Focusable as _, InteractiveElement as _, IntoElement as _, MouseButton,
    ParentElement as _, Pixels, Point, Styled as _, Subscription, anchored, deferred, div, px,
    component::{
        Colorize as _, Icon, Sizable as _, Size, Theme,
        button::{Button, ButtonVariants as _},
        input::{
            Copy, Cut, Input, InputGroup, InputGroupAddon, InputGroupAddonAlignment, InputGroupButton, InputState,
            MaskPattern, Paste, SelectAll, Textarea, TextareaState,
        },
        menu::PopupMenu,
    },
};
use std::{cell::RefCell, rc::Rc};

/// The open right-click menu: where it opened, the menu, and its dismissal.
type OpenMenu = Rc<RefCell<Option<(Point<Pixels>, Entity<PopupMenu>, Subscription)>>>;

pub fn builder(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let masked = param_bool(params, "masked");
    let mask_toggle = param_bool(params, "mask_toggle");
    let select_all = param_bool(params, "select_all");
    let prefix = icon(param_str(params, "prefix", ""));
    let suffix = icon(param_str(params, "suffix", ""));
    let width = param_f32(params, "width", 200.);
    // input.rs cleanable: a clear button after the text while it is editable and not empty.
    let cleanable = param_bool(params, "cleanable");
    // The right-click menu as GPUI Kit draws it where the OS has no native one.
    let context_menu = param_bool(params, "context_menu");
    let open_menu: OpenMenu = Rc::new(RefCell::new(None));
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            let state = cx.new(|cx| {
                let mut state = InputState::new(window, cx)
                    .placeholder(placeholder)
                    .default_value(value)
                    .masked(masked);
                if select_all {
                    state.select_all(window, cx);
                }
                state
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<InputState>().ok())
            .expect("input state");
        let mut input = Input::new(&state)
            .with_size(size)
            .disabled(disabled)
            .readonly(readonly)
            .w(px(width));
        if let Some(name) = prefix.clone() {
            input = input.prefix(Icon::new(name).small());
        }
        if let Some(name) = suffix.clone() {
            input = input.suffix(Button::new("suffix").ghost().icon(name).xsmall());
        }
        if mask_toggle {
            input = input.mask_toggle();
        }
        if cleanable {
            input = input.cleanable(true);
        }
        if !context_menu {
            return input.into_any_element();
        }
        // input.rs shows its context menu as a NativeMenu, which the macOS backend
        // hands to AppKit (nothing to capture here) and native_menu/fallback.rs
        // draws as a PopupMenu at the pointer: built here the same way, after the
        // input has handled the click (and moved its caret).
        let view_entity = cx.entity().downgrade();
        let slot = open_menu.clone();
        let mut root = div().child(input).capture_any_mouse_up(move |event, window, cx| {
            if event.button != MouseButton::Right {
                return;
            }
            let (position, slot, state, view) = (event.position, slot.clone(), state.clone(), view_entity.clone());
            window.defer(cx, move |window, cx| {
                let capabilities = state.read(cx).context_menu_capabilities();
                let enabled = !capabilities.is_disabled();
                let editable = enabled && !capabilities.is_readonly();
                let copyable = capabilities.is_copyable();
                let focus = state.read(cx).focus_handle(cx);
                let menu = PopupMenu::build(window, cx, move |menu, _, _| {
                    menu.action_context(focus.clone())
                        .menu_with_check_and_disabled("Cut", false, Box::new(Cut), !(editable && copyable))
                        .menu_with_check_and_disabled("Copy", false, Box::new(Copy), !copyable)
                        .menu_with_check_and_disabled("Paste", false, Box::new(Paste), !editable)
                        .separator()
                        .menu_with_check_and_disabled("Select All", false, Box::new(SelectAll), false)
                });
                // Weak: the slot holds the subscription.
                let dismissed = Rc::downgrade(&slot);
                let subscription = cx.subscribe(&menu, move |_, _: &DismissEvent, _| {
                    if let Some(slot) = dismissed.upgrade() {
                        slot.borrow_mut().take();
                    }
                });
                menu.focus_handle(cx).focus(window, cx);
                *slot.borrow_mut() = Some((position, menu, subscription));
                let _ = view.update(cx, |_, cx| cx.notify());
            });
        });
        if let Some((position, menu, _)) = open_menu.borrow().as_ref() {
            root = root.child(
                deferred(anchored().position(*position).snap_to_window_with_margin(px(8.)).child(menu.clone()))
                    .with_priority(gpui_kit::base::POPUP_PRIORITY),
            );
        }
        root.into_any_element()
    }))
}

/// The InputState rules uikit:Inputs ports (crates/base/src/input/base/state.rs):
/// `mask` ("number" for MaskPattern::Number with `separator` and `fraction`, or
/// a MaskPattern::new pattern), `digits` (a validate closure that keeps digits
/// only; GPUI's pattern goes through the same is_valid_input) and
/// `clean_on_escape`. With `textarea`, a Textarea of `rows` lines with its
/// default TabSize (2 spaces), its lines selected when `select_lines`.
pub fn uikit_input(params: &Params) -> Result<Builder> {
    if param_bool(params, "textarea") {
        let value = param_str(params, "value", "").to_string();
        let rows = param_f32(params, "rows", 3.) as usize;
        let width = param_f32(params, "width", 220.);
        let select_lines = param_bool(params, "select_lines");
        return Ok(Rc::new(move |view, window, cx| {
            if view.state.entity.is_none() {
                let value = value.clone();
                let state = cx.new(|cx| {
                    let mut state = TextareaState::new(window, cx).rows(rows).default_value(value);
                    if select_lines {
                        state.select_all(window, cx);
                    }
                    state
                });
                view.state.entity = Some(state.into());
            }
            let state = view
                .state
                .entity
                .clone()
                .and_then(|e| e.downcast::<TextareaState>().ok())
                .expect("textarea state");
            Textarea::new(&state).w(px(width)).into_any_element()
        }));
    }
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let mask = param_str(params, "mask", "").to_string();
    let separator = param_str(params, "separator", "").chars().next();
    let fraction = params.get("fraction").and_then(serde_json::Value::as_u64).map(|f| f as usize);
    let digits = param_bool(params, "digits");
    let clean_on_escape = param_bool(params, "clean_on_escape");
    let width = param_f32(params, "width", 200.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder, mask) = (value.clone(), placeholder.clone(), mask.clone());
            let state = cx.new(|cx| {
                let mut state = InputState::new(window, cx).placeholder(placeholder).default_value(value);
                if mask == "number" {
                    state = state.mask_pattern(MaskPattern::Number { separator, fraction });
                } else if !mask.is_empty() {
                    state = state.mask_pattern(mask.as_str());
                }
                if digits {
                    state = state.validate(|text, _| text.chars().all(|c| c.is_ascii_digit()));
                }
                if clean_on_escape {
                    state = state.clean_on_escape();
                }
                state
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<InputState>().ok())
            .expect("input state");
        Input::new(&state).w(px(width)).into_any_element()
    }))
}

/// `Textarea` (crates/component/src/input/textarea.rs): `rows` lines, or
/// `auto_grow(min_rows, max_rows)`, or a fixed `height`.
pub fn textarea(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let rows = param_f32(params, "rows", 1.) as usize;
    let grow = params
        .get("max_rows")
        .map(|_| (param_f32(params, "min_rows", 1.) as usize, param_f32(params, "max_rows", 1.) as usize));
    let height = params.get("height").and_then(serde_json::Value::as_f64).map(|h| h as f32);
    let width = param_f32(params, "width", 220.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            let state = cx.new(|cx| {
                let state = TextareaState::new(window, cx).rows(rows).placeholder(placeholder).default_value(value);
                match grow {
                    Some((min, max)) => state.auto_grow(min, max),
                    None => state,
                }
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<TextareaState>().ok())
            .expect("textarea state");
        let mut textarea = Textarea::new(&state).with_size(size).disabled(disabled).readonly(readonly).w(px(width));
        if let Some(h) = height {
            textarea = textarea.h(px(h));
        }
        textarea.into_any_element()
    }))
}

/// `InputGroup` (crates/component/src/input/group.rs): an input with addons.
/// `start` is an icon name or text before the input; `end` text, `end_button`
/// a labelled button or `end_icon` an icon button after it.
pub fn input_group(params: &Params) -> Result<Builder> {
    let disabled = disabled(params);
    let size = size(params);
    let readonly = param_bool(params, "readonly");
    let invalid = param_bool(params, "invalid");
    let multiline = param_bool(params, "multiline");
    let rows = param_f32(params, "rows", 3.) as usize;
    let value = param_str(params, "value", "").to_string();
    let placeholder = param_str(params, "placeholder", "").to_string();
    let start = param_str(params, "start", "").to_string();
    let end = param_str(params, "end", "").to_string();
    let end_button = param_str(params, "end_button", "").to_string();
    let end_icon = param_str(params, "end_icon", "").to_string();
    let button_size = if param_str(params, "button_size", "xsmall") == "small" { Size::Small } else { Size::XSmall };
    // InputGroupButton::loading (group.rs).
    let loading = param_bool(params, "loading");
    // uikit-inputgroup: text in a row above the input, text and a button (at the row's end) below it.
    let block_start = param_str(params, "block_start", "").to_string();
    let block_end = param_str(params, "block_end", "").to_string();
    let block_button = param_str(params, "block_button", "").to_string();
    let width = param_f32(params, "width", 240.);
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (value, placeholder) = (value.clone(), placeholder.clone());
            view.state.entity = Some(if multiline {
                cx.new(|cx| TextareaState::new(window, cx).rows(rows).placeholder(placeholder).default_value(value)).into()
            } else {
                cx.new(|cx| InputState::new(window, cx).placeholder(placeholder).default_value(value)).into()
            });
        }
        let mut group = InputGroup::new("case")
            .with_size(size)
            .disabled(disabled)
            .readonly(readonly)
            .invalid(invalid)
            .w(px(width));
        let entity = view.state.entity.clone().expect("state");
        group = match entity.clone().downcast::<TextareaState>() {
            Ok(state) => group.input(Textarea::new(&state)),
            Err(_) => group.input(Input::new(&entity.downcast::<InputState>().ok().expect("input state"))),
        };
        if let Some(name) = icon(&start) {
            group = group.addon(InputGroupAddon::new("start").child(Icon::new(name).size_4()));
        } else if !start.is_empty() {
            group = group.addon(InputGroupAddon::new("start").child(start.clone()));
        }
        let mut tail = InputGroupAddon::new("end").align(InputGroupAddonAlignment::InlineEnd);
        let mut has_tail = false;
        if !end.is_empty() {
            tail = tail.child(end.clone());
            has_tail = true;
        }
        if !end_button.is_empty() {
            tail = tail.child(InputGroupButton::new("btn").label(end_button.clone()).with_size(button_size).loading(loading));
            has_tail = true;
        }
        if let Some(name) = icon(&end_icon) {
            tail = tail.child(InputGroupButton::new("btn").icon(name).with_size(button_size).loading(loading));
            has_tail = true;
        }
        if has_tail {
            group = group.addon(tail);
        }
        if !block_start.is_empty() {
            group = group.addon(
                InputGroupAddon::new("top").align(InputGroupAddonAlignment::BlockStart).child(block_start.clone()),
            );
        }
        if !block_end.is_empty() || !block_button.is_empty() {
            let mut bottom = InputGroupAddon::new("bottom").align(InputGroupAddonAlignment::BlockEnd);
            if !block_end.is_empty() {
                bottom = bottom.child(block_end.clone());
            }
            if !block_button.is_empty() {
                bottom = bottom.child(InputGroupButton::new("send").label(block_button.clone()).ml_auto());
            }
            group = group.addon(bottom);
        }
        group.into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    // input.rs: a disabled frame fills with `input` mixed 80% toward transparent,
    // then at half strength; its text and placeholder are at half strength.
    out.push(("input.disabled.background".into(), theme.input.mix_oklab(theme.transparent, 0.8).opacity(0.5).into()));
    out.push(("input.disabled.text".into(), theme.foreground.opacity(0.5).into()));
    out.push(("input.disabled.placeholder".into(), theme.muted_foreground.opacity(0.5).into()));
    // group.rs GroupAppearance: the frame and its invalid ring differ by mode; group buttons tint with `muted`.
    let dark = theme.is_dark();
    out.push(("input-group.background".into(), (if dark { theme.input.opacity(0.3) } else { theme.transparent }).into()));
    out.push(("input-group.disabled.background".into(), theme.input.opacity(if dark { 0.8 } else { 0.5 }).into()));
    out.push(("input-group.invalid.ring".into(), theme.danger.opacity(if dark { 0.4 } else { 0.2 }).into()));
    out.push(("input-group.button.hover".into(), theme.muted.opacity(if dark { 0.5 } else { 1.0 }).into()));
}
