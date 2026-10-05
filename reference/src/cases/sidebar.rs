//! `Sidebar` (crates/component/src/sidebar): the side surface beside a muted
//! content area; clicking the content toggles `collapsed`.
//!
//! uikit:Sidebar's cases (uikit-sidebar) add `menu`: the sidebar story's
//! layout (sidebar_story.rs) in a 220px sidebar, a SidebarHeader, the Platform
//! and Projects groups and a SidebarFooter, with `active` (the active item's
//! label), `closed` (Playground starts closed) and `click` ("open" or
//! "toggle": the items' click_to_open / click_to_toggle).
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    InteractiveElement as _, IntoElement as _, ParentElement as _, StatefulInteractiveElement as _, Styled as _, div,
    prelude::FluentBuilder as _, px, relative,
    component::{
        ActiveTheme as _, Icon, IconName, Side, Sizable as _, badge::Badge, h_flex,
        sidebar::{
            Sidebar, SidebarCollapsible, SidebarFooter, SidebarGroup, SidebarHeader, SidebarMenu, SidebarMenuItem,
            SidebarToggleButton,
        },
        switch::Switch,
        v_flex,
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    if param_bool(params, "menu") {
        return menu(params);
    }
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

/// The sidebar story's layout: a SidebarHeader with the company, the Platform
/// group (Playground with two sub-items, the first with a Switch; Models with
/// two; Documentation) and the Projects group (a Badge, an icon and a disabled
/// item as suffixes), and a SidebarFooter with the user.
fn menu(params: &Params) -> Result<Builder> {
    let collapsed = param_bool(params, "collapsed");
    let active = param_str(params, "active", "Playground").to_string();
    let open = !param_bool(params, "closed");
    let click = param_str(params, "click", "").to_string();
    let (w, h) = (param_f32(params, "width", 400.), param_f32(params, "height", 480.));
    let sidebar_width = param_f32(params, "sidebar_width", 220.);
    Ok(Rc::new(move |view, _, cx| {
        let theme = cx.theme();
        let is_collapsed = collapsed ^ view.state.toggled;
        let icons = is_collapsed;
        let item = |label: &'static str, icon: IconName| {
            SidebarMenuItem::new(label)
                .icon(icon)
                .active(active == label)
                .click_to_open(click == "open")
                .click_to_toggle(click == "toggle")
        };
        let header = SidebarHeader::new()
            .child(
                div()
                    .flex()
                    .items_center()
                    .justify_center()
                    .rounded(theme.radius)
                    .bg(theme.success)
                    .text_color(theme.success_foreground)
                    .size_8()
                    .flex_shrink_0()
                    .when(!icons, |this| this.child(Icon::new(IconName::GalleryVerticalEnd)))
                    .when(icons, |this| {
                        this.size_4()
                            .bg(theme.transparent)
                            .text_color(theme.foreground)
                            .child(Icon::new(IconName::GalleryVerticalEnd))
                    }),
            )
            .when(!icons, |this| {
                this.child(
                    v_flex()
                        .gap_0()
                        .text_sm()
                        .flex_1()
                        .line_height(relative(1.25))
                        .overflow_hidden()
                        .text_ellipsis()
                        .child("Company Name")
                        .child(div().child("Enterprise").text_xs()),
                )
                .child(Icon::new(IconName::ChevronsUpDown).size_4().flex_shrink_0())
            });
        let platform = SidebarGroup::new("Platform").child(SidebarMenu::new().children([
            item("Playground", IconName::SquareTerminal).default_open(open).children([
                SidebarMenuItem::new("History").suffix(|_, _| Switch::new("switch").xsmall().checked(false)),
                SidebarMenuItem::new("Starred"),
            ]),
            item("Models", IconName::Bot)
                .children([SidebarMenuItem::new("Genesis"), SidebarMenuItem::new("Explorer")]),
            item("Documentation", IconName::BookOpen),
        ]));
        let projects = SidebarGroup::new("Projects").child(SidebarMenu::new().children([
            item("Design Engineering", IconName::Frame).suffix(|_, _| {
                Badge::new().dot().count(1).child(div().p_0p5().child(Icon::new(IconName::Bell)))
            }),
            item("Sales and Marketing", IconName::ChartPie).suffix(|_, _| Icon::new(IconName::Settings2)),
            item("Travel", IconName::Map).disable(true),
        ]));
        let footer = SidebarFooter::new()
            .justify_between()
            .child(h_flex().gap_2().child(IconName::CircleUser).when(!icons, |this| this.child("Jason Lee")))
            .when(!icons, |this| this.child(Icon::new(IconName::ChevronsUpDown).size_4()));
        let sidebar = Sidebar::new("sidebar")
            .collapsible(SidebarCollapsible::Icon)
            .collapsed(is_collapsed)
            .w(px(sidebar_width))
            .gap_0()
            .header(header)
            .child(platform)
            .child(projects)
            .footer(footer);
        let content = div()
            .id("content")
            .flex_1()
            .h_full()
            .bg(theme.muted)
            .on_click(cx.listener(|view, _, _, cx| {
                view.state.toggled = !view.state.toggled;
                cx.notify();
            }));
        h_flex().id("case").w(px(w)).h(px(h)).child(sidebar).child(content).into_any_element()
    }))
}
