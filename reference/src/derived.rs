//! Colors the components compute while painting, evaluated with GPUI's own
//! color functions so the Avalonia resources carry exactly the same values.
use gpui_kit::{Hsla, component::Theme};

pub fn derived(theme: &Theme) -> Vec<(String, Hsla)> {
    let mut out: Vec<(String, Hsla)> = Vec::new();
    crate::cases::derived_colors(theme, &mut out);
    out
}
