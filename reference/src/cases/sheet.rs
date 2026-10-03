//! `Sheet` (crates/component/src/sheet.rs), opened by a click anywhere in the
//! window at the case's placement. GPUI's sheet sits below a 34px custom
//! title bar by default (sheet.margin_top); the case window has none, so the
//! offset is zeroed.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, StatefulInteractiveElement as _, Styled as _, div,
    px,
    component::{ActiveTheme as _, Placement, Theme, WindowExt as _},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let placement = match param_str(params, "placement", "right") {
        "left" => Placement::Left,
        "top" => Placement::Top,
        "bottom" => Placement::Bottom,
        _ => Placement::Right,
    };
    let title = params.get("title").and_then(|v| v.as_str()).map(str::to_string);
    let size = params.get("size").and_then(|v| v.as_f64()).map(|v| px(v as f32));
    let footer = param_bool(params, "footer");
    let overlay = !param_bool(params, "no_overlay");
    let (w, h) = (param_f32(params, "width", 560.), param_f32(params, "height", 400.));
    Ok(Rc::new(move |_, _, _| {
        let title = title.clone();
        div()
            .id("case")
            .w(px(w))
            .h(px(h))
            .on_click(move |_, window, cx| {
                Theme::global_mut(cx).sheet.margin_top = px(0.);
                let title = title.clone();
                window.open_sheet_at(placement, cx, move |sheet, _, cx| {
                    let (muted, primary) = (cx.theme().muted, cx.theme().primary);
                    let mut sheet = sheet.overlay(overlay).child(div().h(px(40.)).bg(muted));
                    if let Some(t) = title.clone() {
                        sheet = sheet.title(t);
                    }
                    if let Some(s) = size {
                        sheet = sheet.size(s);
                    }
                    if footer {
                        sheet = sheet.footer(div().w(px(80.)).h(px(24.)).bg(primary));
                    }
                    sheet
                });
            })
            .into_any_element()
    }))
}
