//! `Accordion` (crates/component/src/accordion.rs) and `Collapsible`
//! (crates/component/src/collapsible.rs). A click toggles an item; bit i of
//! the case value is item i's open state once clicked.
use super::{icon, size};
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, StatefulInteractiveElement as _,
    Styled as _, div, prelude::FluentBuilder as _, px,
    component::{
        ActiveTheme as _, Sizable as _, StyledExt as _, accordion::Accordion, collapsible::Collapsible,
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let size = size(params);
    let bordered = !param_bool(params, "borderless");
    let scope = param_str(params, "scope", "").to_string();
    let open_first = param_bool(params, "open_first");
    let icons = param_bool(params, "icons");
    let width = param_f32(params, "width", 280.);
    Ok(Rc::new(move |view, _, cx| {
        let open = if view.state.toggled { view.state.value as u32 } else { open_first as u32 };
        let mut accordion = Accordion::new("case")
            .w(px(width))
            .with_size(size)
            .bordered(bordered)
            .disabled(scope == "all")
            .multiple(true)
            .on_toggle_click(cx.listener(|view, open: &[usize], _, cx| {
                view.state.toggled = true;
                view.state.value = open.iter().fold(0u32, |m, ix| m | 1 << ix) as f32;
                cx.notify();
            }));
        let rows = [
            ("Is it accessible?", "Yes, it is.", "copy"),
            ("Is it styled?", "Yes, by the theme.", "plus"),
            ("Is it animated?", "Yes, with a spring.", "check"),
        ];
        for (ix, (title, body, glyph)) in rows.into_iter().enumerate() {
            let scope = scope.clone();
            accordion = accordion.item(move |item| {
                let item = item.title(title).open(open & (1 << ix) != 0).disabled(scope == "item" && ix == 1).child(body);
                if icons { item.icon(icon(glyph).expect("known icon")) } else { item }
            });
        }
        accordion.into_any_element()
    }))
}

pub fn collapsible(params: &Params) -> Result<Builder> {
    let open = param_bool(params, "open");
    let motion = param_bool(params, "motion");
    let content_first = param_bool(params, "content_first");
    let width = param_f32(params, "width", 240.);
    Ok(Rc::new(move |view, _, cx| {
        let open = open ^ view.state.toggled;
        let theme = cx.theme();
        let (muted, border, radius) = (theme.muted, theme.border, theme.radius);
        let trigger = div()
            .id("trigger")
            .text_sm()
            .font_medium()
            .child("Order details")
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        // Padding inside the content, so it is revealed with it (a parent gap would not be).
        let content = div()
            .map(|d| if content_first { d.pb_2() } else { d.pt_2() })
            .child(div().h(px(48.)).rounded(radius).border_1().border_color(border).bg(muted));
        let mut c = Collapsible::new().w(px(width)).open(open);
        c = if content_first { c.content(content).child(trigger) } else { c.child(trigger).content(content) };
        if motion {
            c = c.motion_id("case-reveal");
        }
        c.into_any_element()
    }))
}
