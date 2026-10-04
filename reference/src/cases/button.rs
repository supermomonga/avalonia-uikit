//! `Button` (crates/component/src/button/button.rs).
use super::{Paint, disabled, icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::{Result, bail};
use gpui_kit::{
    Hsla, IntoElement as _, transparent_white,
    component::{
        Colorize as _, Theme,
        Disableable as _, Selectable as _, Sizable as _,
        button::{Button, ButtonGroup, ButtonRounded, ButtonVariant, ButtonVariants as _},
    },
};
use std::rc::Rc;

pub fn variant(name: &str) -> Result<ButtonVariant> {
    Ok(match name {
        "default" => ButtonVariant::Default,
        "primary" => ButtonVariant::Primary,
        "secondary" => ButtonVariant::Secondary,
        "danger" => ButtonVariant::Danger,
        "warning" => ButtonVariant::Warning,
        "success" => ButtonVariant::Success,
        "info" => ButtonVariant::Info,
        "ghost" => ButtonVariant::Ghost,
        "link" => ButtonVariant::Link,
        "text" => ButtonVariant::Text,
        other => bail!("unknown button variant {other}"),
    })
}

fn rounded(params: &Params) -> ButtonRounded {
    match param_str(params, "rounded", "medium") {
        "none" => ButtonRounded::None,
        "small" => ButtonRounded::Small,
        "large" => ButtonRounded::Large,
        _ => ButtonRounded::Medium,
    }
}

/// `ButtonGroup` (crates/component/src/button/button_group.rs): buttons that
/// share their inner edges. `selected` marks buttons with '1' ("010").
pub fn group(params: &Params) -> Result<Builder> {
    let name = param_str(params, "variant", "default");
    let variant = if name == "default" { None } else { Some(variant(name)?) };
    let outline = param_bool(params, "outline");
    let compact = param_bool(params, "compact");
    let vertical = param_str(params, "layout", "horizontal") == "vertical";
    let size = size(params);
    let disabled = disabled(params);
    let rounded = rounded(params);
    let labels: Vec<String> = param_str(params, "labels", "One,Two,Three").split(',').map(str::to_string).collect();
    let initial: u32 = param_str(params, "selected", "")
        .bytes()
        .enumerate()
        .fold(0, |bits, (i, c)| bits | (((c == b'1') as u32) << i));
    // `selectable`: an app that applies the indices on_click reports;
    // `multiple` toggles the clicked button instead of selecting it alone.
    let selectable = param_bool(params, "selectable");
    let multiple = param_bool(params, "multiple");
    Ok(Rc::new(move |view, _, cx| {
        let bits = if view.state.toggled { view.state.value as u32 } else { initial };
        // `disabled` before `child`: ButtonGroup::child copies it.
        let mut group = ButtonGroup::new("case")
            .with_size(size)
            .disabled(disabled)
            .multiple(multiple)
            .layout(if vertical { gpui_kit::Axis::Vertical } else { gpui_kit::Axis::Horizontal });
        if let Some(variant) = variant {
            group = group.with_variant(variant);
        }
        if outline {
            group = group.outline();
        }
        if compact {
            group = group.compact();
        }
        for (ix, label) in labels.iter().enumerate() {
            group = group.child(
                Button::new(ix)
                    .label(label.clone())
                    .rounded(rounded)
                    .selected(bits & (1 << ix) != 0),
            );
        }
        if selectable {
            group = group.on_click(cx.listener(|view, selected: &Vec<usize>, _, cx| {
                view.state.toggled = true;
                view.state.value = selected.iter().fold(0u32, |bits, ix| bits | 1 << ix) as f32;
                cx.notify();
            }));
        }
        group.into_any_element()
    }))
}

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = variant(param_str(params, "variant", "default"))?;
    let outline = param_bool(params, "outline");
    let compact = param_bool(params, "compact");
    let selected = param_bool(params, "selected");
    let disabled = disabled(params);
    let size = size(params);
    let label = params
        .get("label")
        .and_then(|v| v.as_str())
        .map(|s| s.to_string());
    let icon = icon(param_str(params, "icon", ""));
    let rounded = rounded(params);
    // Button::loading and loading_icon (button.rs, button_icon.rs).
    let loading = param_bool(params, "loading");
    let loading_icon = super::icon(param_str(params, "loading_icon", ""));
    Ok(Rc::new(move |_, _, _| {
        let mut button = Button::new("case")
            .with_variant(variant)
            .with_size(size)
            .rounded(rounded)
            .disabled(disabled)
            .selected(selected)
            .loading(loading);
        if let Some(name) = loading_icon.clone() {
            button = button.loading_icon(name);
        }
        if outline {
            button = button.outline();
        }
        if compact {
            button = button.compact();
        }
        if let Some(icon) = icon.clone() {
            button = button.icon(icon);
        }
        if let Some(label) = label.clone() {
            button = button.label(label);
        }
        button.into_any_element()
    }))
}

#[derive(Clone, Copy, PartialEq)]
enum State {
    Normal,
    Hover,
    Active,
    Selected,
    Disabled,
}

/// Mirrors `ButtonVariant::{normal, hovered, active, selected, disabled}`.
/// Returns (background, foreground, border). The background is the theme
/// token's own, a gradient where the theme gives one (Aurora).
fn style(theme: &Theme, variant: ButtonVariant, outline: bool, state: State) -> (Paint, Hsla, Hsla) {
    use ButtonVariant as V;
    let t = &theme.tokens;
    let outline_bg = |state: State| -> Paint {
        match (variant, state) {
            (V::Default, State::Normal) => theme.input_background().into(),
            (V::Default, State::Hover) => theme.input.mix_oklab(theme.transparent, 0.5).into(),
            (V::Default, _) => theme.input.mix_oklab(theme.transparent, 0.7).into(),
            (V::Primary, State::Normal) => t.primary.background.opacity(0.1),
            (V::Primary, State::Hover) => t.primary_hover.background.opacity(0.2),
            (V::Primary, _) => t.primary_active.background.opacity(0.4),
            (V::Secondary, State::Normal) => t.secondary.background.opacity(0.1),
            (V::Secondary, State::Hover) => t.secondary_hover.background.opacity(0.2),
            (V::Secondary, _) => t.secondary_active.background.opacity(0.4),
            (V::Danger, State::Normal) => t.danger.background.opacity(0.1),
            (V::Danger, State::Hover) => t.danger_hover.background.opacity(0.2),
            (V::Danger, _) => t.danger_active.background.opacity(0.4),
            (V::Warning, State::Normal) => t.warning.background.opacity(0.1),
            (V::Warning, State::Hover) => t.warning_hover.background.opacity(0.2),
            (V::Warning, _) => t.warning_active.background.opacity(0.4),
            (V::Success, State::Normal) => t.success.background.opacity(0.1),
            (V::Success, State::Hover) => t.success_hover.background.opacity(0.2),
            (V::Success, _) => t.success_active.background.opacity(0.4),
            (V::Info, State::Normal) => t.info.background.opacity(0.1),
            (V::Info, State::Hover) => t.info_hover.background.opacity(0.2),
            (V::Info, _) => t.info_active.background.opacity(0.4),
            (V::Ghost | V::Link | V::Text, _) => theme.transparent.into(),
            (V::Custom(_), _) => unreachable!(),
        }
    };
    let bg_color = || -> Paint {
        if outline {
            return outline_bg(State::Normal);
        }
        match variant {
            V::Default => t.button.into(),
            V::Primary => t.button_primary.into(),
            V::Secondary => t.button_secondary.into(),
            V::Danger => t.button_danger.into(),
            V::Warning => t.button_warning.into(),
            V::Success => t.button_success.into(),
            V::Info => t.button_info.into(),
            V::Ghost | V::Link | V::Text => theme.transparent.into(),
            V::Custom(_) => unreachable!(),
        }
    };
    let text_color = |outline: bool| -> Hsla {
        match variant {
            V::Default => theme.button_foreground,
            V::Primary => if outline { theme.primary } else { theme.button_primary_foreground },
            V::Secondary => if outline { theme.secondary_foreground } else { theme.button_secondary_foreground },
            V::Ghost => theme.secondary_foreground,
            V::Danger => if outline { theme.danger } else { theme.button_danger_foreground },
            V::Warning => if outline { theme.warning } else { theme.button_warning_foreground },
            V::Success => if outline { theme.success } else { theme.button_success_foreground },
            V::Info => if outline { theme.info } else { theme.button_info_foreground },
            V::Link => theme.link,
            V::Text => theme.foreground.opacity(0.9),
            V::Custom(_) => unreachable!(),
        }
    };
    let border_color = |outline: bool| -> Hsla {
        match variant {
            V::Default => theme.input,
            V::Secondary => theme.border,
            V::Primary => theme.primary,
            V::Danger => if outline { theme.danger.mix_oklab(transparent_white(), 0.4) } else { theme.button_danger },
            V::Info => if outline { theme.info.mix_oklab(transparent_white(), 0.4) } else { theme.button_info },
            V::Warning => if outline { theme.warning.mix_oklab(transparent_white(), 0.4) } else { theme.button_warning },
            V::Success => if outline { theme.success.mix_oklab(transparent_white(), 0.4) } else { theme.button_success },
            V::Ghost | V::Link | V::Text => theme.transparent,
            V::Custom(_) => unreachable!(),
        }
    };
    let hovered = || -> (Paint, Hsla, Hsla) {
        let bg = match variant {
            V::Default if !outline => t.button_hover.into(),
            V::Primary if !outline => t.button_primary_hover.into(),
            V::Secondary if !outline => t.button_secondary_hover.into(),
            V::Danger if !outline => t.button_danger_hover.into(),
            V::Warning if !outline => t.button_warning_hover.into(),
            V::Success if !outline => t.button_success_hover.into(),
            V::Info if !outline => t.button_info_hover.into(),
            V::Default | V::Primary | V::Secondary | V::Danger | V::Warning | V::Success | V::Info => outline_bg(State::Hover),
            V::Ghost => {
                let accent: Paint = t.accent.into();
                if theme.mode.is_dark() { accent.opacity(0.5) } else { accent }
            }
            V::Link | V::Text => theme.transparent.into(),
            V::Custom(_) => unreachable!(),
        };
        let fg = match variant {
            V::Link => theme.link_hover,
            V::Text => theme.foreground,
            V::Ghost => theme.accent_foreground,
            _ => text_color(outline),
        };
        (bg, fg, border_color(outline))
    };
    let active = || -> (Paint, Hsla, Hsla) {
        let bg = match variant {
            V::Default if !outline => t.button_active.into(),
            V::Primary if !outline => t.button_primary_active.into(),
            V::Secondary if !outline => t.button_secondary_active.into(),
            V::Ghost => t.button_active.into(),
            V::Danger if !outline => t.button_danger_active.into(),
            V::Warning if !outline => t.button_warning_active.into(),
            V::Success if !outline => t.button_success_active.into(),
            V::Info if !outline => t.button_info_active.into(),
            V::Default | V::Primary | V::Secondary | V::Danger | V::Warning | V::Success | V::Info => outline_bg(State::Active),
            V::Link | V::Text => theme.transparent.into(),
            V::Custom(_) => unreachable!(),
        };
        let fg = match variant {
            V::Link => theme.link_active,
            V::Text => theme.foreground.opacity(0.7),
            _ => text_color(outline),
        };
        (bg, fg, border_color(outline))
    };
    match state {
        State::Normal => (bg_color(), text_color(outline), border_color(outline)),
        State::Hover => hovered(),
        State::Active => active(),
        State::Selected => {
            if outline {
                let (bg, _, border) = active();
                return (bg, text_color(outline), border);
            }
            let bg = match variant {
                V::Default => t.button_active.into(),
                V::Primary => t.button_primary_active.into(),
                V::Secondary => t.button_secondary_active.into(),
                V::Ghost => t.secondary_active.into(),
                V::Danger => t.button_danger_active.into(),
                V::Warning => t.button_warning_active.into(),
                V::Success => t.button_success_active.into(),
                V::Info => t.button_info_active.into(),
                V::Link | V::Text => theme.transparent.into(),
                V::Custom(_) => unreachable!(),
            };
            let fg = match variant {
                V::Link => theme.link_active,
                V::Text => theme.foreground.opacity(0.7),
                _ => text_color(false),
            };
            (bg, fg, border_color(outline))
        }
        State::Disabled => {
            let bg = match variant {
                V::Default | V::Link | V::Ghost | V::Text => theme.transparent.into(),
                V::Primary => t.button_primary.background.opacity(0.15),
                V::Danger => t.button_danger.background.opacity(0.15),
                V::Warning => t.button_warning.background.opacity(0.15),
                V::Success => t.button_success.background.opacity(0.15),
                V::Info => t.button_info.background.opacity(0.15),
                V::Secondary => t.button_secondary.background.opacity(1.5),
                V::Custom(_) => unreachable!(),
            };
            let fg = theme.muted_foreground.opacity(0.5);
            let (bg, border) = if outline {
                (outline_bg(State::Normal).opacity(0.5), border_color(true).opacity(0.5))
            } else if let V::Default = variant {
                (theme.input_background().opacity(0.5).into(), theme.input.opacity(0.5))
            } else {
                let border = match variant {
                    V::Primary => theme.button_primary.opacity(0.15),
                    V::Secondary => theme.button_secondary.opacity(1.5),
                    V::Danger => theme.button_danger.opacity(0.15),
                    V::Warning => theme.button_warning.opacity(0.15),
                    V::Success => theme.button_success.opacity(0.15),
                    V::Info => theme.button_info.opacity(0.15),
                    V::Default | V::Link | V::Ghost | V::Text => theme.transparent,
                    V::Custom(_) => unreachable!(),
                };
                (bg, border)
            };
            (bg, fg, border)
        }
    }
}

/// The foreground of a variant's normal look (ButtonVariant::normal).
pub fn normal_foreground(theme: &Theme, variant: ButtonVariant, outline: bool) -> Hsla {
    style(theme, variant, outline, State::Normal).1
}

pub const VARIANTS: [&str; 10] = [
    "default", "primary", "secondary", "danger", "warning", "success", "info", "ghost", "link", "text",
];

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, Paint)>) {
    for name in VARIANTS {
        let v = variant(name).expect("known variant");
        for outline in [false, true] {
            for (state, state_name) in [
                (State::Normal, "normal"),
                (State::Hover, "hover"),
                (State::Active, "pressed"),
                (State::Selected, "selected"),
                (State::Disabled, "disabled"),
            ] {
                let (bg, fg, border) = style(theme, v, outline, state);
                let prefix = format!("button.{name}{}.{state_name}", if outline { ".outline" } else { "" });
                out.push((format!("{prefix}.background"), bg));
                out.push((format!("{prefix}.foreground"), fg.into()));
                out.push((format!("{prefix}.border"), border.into()));
            }
        }
    }
    // Hover background at half strength for an idle member of a hover group (SplitButton).
    for name in VARIANTS {
        let v = variant(name).expect("known variant");
        for outline in [false, true] {
            let (bg, _, _) = style(theme, v, outline, State::Hover);
            out.push((format!("button.{name}{}.group-hover.background", if outline { ".outline" } else { "" }), bg.opacity(0.5)));
        }
    }
    // The focus ring band and the dropdown caret.
    out.push(("focus-ring".into(), theme.ring.alpha(0.5).into()));
}

/// A button with a tooltip (tooltip.rs), placed so the tooltip fits above it.
pub fn tooltip(params: &Params) -> Result<Builder> {
    let label = param_str(params, "label", "Hover me").to_string();
    let tip = param_str(params, "tip", "Tooltip text").to_string();
    let size = super::size(params);
    Ok(Rc::new(move |_, _, _| {
        Button::new("case")
            .with_size(size)
            .label(label.clone())
            .tooltip(tip.clone())
            .into_any_element()
    }))
}
