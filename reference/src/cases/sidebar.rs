//! `Sidebar` (crates/component/src/sidebar): the side surface beside a muted
//! content area; clicking the content toggles `collapsed`.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, StatefulInteractiveElement as _, Styled as _, div, px,
    component::{
        ActiveTheme as _, Side, h_flex,
        sidebar::{Sidebar, SidebarCollapsible, SidebarMenu, SidebarToggleButton},
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let side = if param_str(params, "side", "left") == "right" { Side::Right } else { Side::Left };
    let collapsible = match param_str(params, "collapsible", "icon") {
        "offcanvas" => SidebarCollapsible::Offcanvas,
        "none" => SidebarCollapsible::None,
        _ => SidebarCollapsible::Icon,
    };
    let collapsed = param_bool(params, "collapsed");
    let sidebar_width = params.get("sidebar_width").and_then(|v| v.as_f64()).map(|v| v as f32);
    let (blocks, toggle) = (param_bool(params, "blocks"), param_bool(params, "toggle"));
    let (w, h) = (param_f32(params, "width", 400.), param_f32(params, "height", 160.));
    Ok(Rc::new(move |view, _, cx| {
        let theme = cx.theme();
        let (primary, muted) = (theme.sidebar_primary, theme.muted);
        let is_collapsed = collapsed ^ view.state.toggled;
        let mut sidebar = Sidebar::<SidebarMenu>::new("sidebar")
            .side(side)
            .collapsible(collapsible)
            .collapsed(is_collapsed);
        if let Some(sw) = sidebar_width {
            sidebar = sidebar.w(px(sw));
        }
        // 24px blocks show the header and footer paddings.
        if blocks {
            sidebar = sidebar
                .header(div().size(px(24.)).bg(primary))
                .footer(div().size(px(24.)).bg(primary));
        }
        if toggle {
            sidebar = sidebar.header(SidebarToggleButton::new().side(side).collapsed(is_collapsed));
        }
        let content = div()
            .id("content")
            .flex_1()
            .h_full()
            .bg(muted)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        let row = h_flex().id("case").w(px(w)).h(px(h));
        match side {
            Side::Left => row.child(sidebar).child(content),
            Side::Right => row.child(content).child(sidebar),
        }
        .into_any_element()
    }))
}
