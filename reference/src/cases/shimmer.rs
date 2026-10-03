//! `ShimmerText` (crates/component/src/shimmer.rs): text with a highlight
//! sweeping across it, painted as twelve nested bands of the glyphs.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, Styled as _,
    component::{ActiveTheme as _, shimmer::ShimmerText},
};
use std::{rc::Rc, time::Duration};

pub fn builder(params: &Params) -> Result<Builder> {
    let text = param_str(params, "text", "Thinking…").to_string();
    let muted = param_str(params, "color", "") == "muted";
    let small = param_str(params, "text_size", "base") == "sm";
    let reverse = param_bool(params, "reverse");
    let duration = param_f32(params, "duration", 2000.) as u64;
    Ok(Rc::new(move |_, _, cx| {
        // once(): a single sweep from the first frame, as repeat_synced follows
        // the app's clock, not the element's.
        let mut shimmer = ShimmerText::new(text.clone())
            .id("case")
            .once(true)
            .reverse(reverse)
            .duration(Duration::from_millis(duration));
        if small {
            shimmer = shimmer.text_sm();
        }
        if muted {
            shimmer = shimmer.text_color(cx.theme().muted_foreground);
        }
        shimmer.into_any_element()
    }))
}
