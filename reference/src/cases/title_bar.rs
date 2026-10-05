//! `TitleBar` (crates/component/src/title_bar.rs). The reference renders on
//! macOS: an 80px left inset for the traffic lights and no window controls
//! (title_bar.rs TITLE_BAR_LEFT_PADDING, WindowControls).
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    FontWeight, Hsla, IntoElement as _, ParentElement as _, Rgba, Styled as _, div, hsla, px,
    component::{
        IconName, Sizable as _, Theme, TitleBar,
        button::{Button, ButtonVariants as _},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let width = param_f32(params, "width", 480.);
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let content = param_bool(params, "content");
    Ok(Rc::new(move |_, _, _| {
        let mut bar = TitleBar::new();
        if let Some(title) = title.clone() {
            // The story's title (crates/story/src/title_bar.rs): text_sm, medium.
            bar = bar.child(
                div()
                    .flex()
                    .items_center()
                    .child(div().text_sm().font_weight(FontWeight::MEDIUM).child(title)),
            );
        }
        if content {
            // The story's right side (crates/story/src/title_bar.rs): small ghost icon
            // buttons, 8px apart and 8px in from both ends.
            bar = bar.child(
                div()
                    .flex()
                    .items_center()
                    .justify_end()
                    .px_2()
                    .gap_2()
                    .child(Button::new("github").icon(IconName::Github).small().ghost())
                    .child(Button::new("bell").small().ghost().compact().icon(IconName::Bell)),
            );
        }
        div().w(px(width)).child(bar).into_any_element()
    }))
}

pub fn derived_colors(theme: &Theme, out: &mut Vec<(String, super::Paint)>) {
    // default_title_bar_background: the gradient's top, 55% title_bar and 45%
    // background mixed per sRGB channel; it ends in title_bar.
    let (tb, bg) = (theme.title_bar.to_rgb(), theme.background.to_rgb());
    let mix = |a: f32, b: f32| a * 0.55 + b * 0.45;
    out.push((
        "title-bar.gradient-top".into(),
        Hsla::from(Rgba { r: mix(tb.r, bg.r), g: mix(tb.g, bg.g), b: mix(tb.b, bg.b), a: mix(tb.a, bg.a) }).into(),
    ));
    // window_border.rs: the client-decorated frame, a fixed gray by mode.
    let frame = if theme.mode.is_dark() { hsla(0., 0., 0.2, 1.) } else { hsla(0., 0., 0.8, 1.) };
    out.push(("window-frame".into(), frame.into()));
}
