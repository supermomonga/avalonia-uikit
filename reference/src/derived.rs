//! Colors the components compute while painting, evaluated with GPUI's own
//! color functions so the Avalonia resources carry exactly the same values.
use crate::cases::Paint;
use gpui_kit::component::Theme;

pub fn derived(theme: &Theme) -> Vec<(String, Paint)> {
    let mut out: Vec<(String, Paint)> = Vec::new();
    crate::cases::derived_colors(theme, &mut out);
    out
}
