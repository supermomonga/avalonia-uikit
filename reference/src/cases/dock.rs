//! `DockArea` (crates/component/src/dock) with DockSkin, laid out as the dock
//! story: Explorer and Search in one group, the Editor alone, and Terminal and
//! Problems under them. Everything is a split of the centre, as Dock.Avalonia
//! lays out a ProportionalDock (GPUI's edge docks have no counterpart there).
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    App, AppContext as _, Context, Entity, EventEmitter, FocusHandle, Focusable, IntoElement, ParentElement as _,
    Render, SharedString, Styled as _, Window, div, px,
    component::{
        ActiveTheme as _,
        dock::{BasePanel, DockArea, DockLayout, DockSkin, Panel, PanelEvent, panel_handle},
    },
};
use std::rc::Rc;

pub struct CasePanel {
    name: &'static str,
    title: SharedString,
    focus_handle: FocusHandle,
}

impl EventEmitter<PanelEvent> for CasePanel {}

impl Focusable for CasePanel {
    fn focus_handle(&self, _: &App) -> FocusHandle {
        self.focus_handle.clone()
    }
}

impl BasePanel for CasePanel {
    fn panel_name(&self) -> &'static str {
        self.name
    }
}

impl Panel for CasePanel {
    fn title(&mut self, _: &mut Window, _: &mut Context<Self>) -> impl IntoElement {
        // In Inter, as everything the cases draw: the drag preview is drawn outside the
        // case root, where the window's font is the system UI font (R13).
        div().font_family(crate::fonts::FAMILY).child(self.title.clone())
    }
}

impl Render for CasePanel {
    fn render(&mut self, _: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        // The story's panel body: p_4 and the foreground.
        div().size_full().p_4().text_color(cx.theme().foreground).child(self.title.clone())
    }
}

fn panel(name: &'static str, title: &'static str, cx: &mut App) -> Entity<CasePanel> {
    cx.new(|cx| CasePanel { name, title: title.into(), focus_handle: cx.focus_handle() })
}

pub fn builder(params: &Params) -> Result<Builder> {
    let width = param_f32(params, "width", 640.);
    let height = param_f32(params, "height", 360.);
    let left = param_f32(params, "left", 240.);
    let bottom = param_f32(params, "bottom", 160.);
    let close_buttons = param_bool(params, "close_buttons");
    Ok(Rc::new(move |view, window, cx| {
        if view.state.entity.is_none() {
            let (area, skin) = DockSkin::dock_area("case-dock", Some(1), window, cx);
            skin.set_toggle_button_visible(false, cx);
            skin.set_close_button_visible(close_buttons, cx);
            let explorer = panel("CaseExplorer", "Explorer", cx);
            let search = panel("CaseSearch", "Search", cx);
            let editor = panel("CaseEditor", "Editor", cx);
            let terminal = panel("CaseTerminal", "Terminal", cx);
            let problems = panel("CaseProblems", "Problems", cx);
            area.update(cx, |area, cx| {
                area.set_center(
                    DockLayout::v_split()
                        .child(
                            DockLayout::h_split()
                                .child(
                                    DockLayout::tabs()
                                        .panel_view(panel_handle(explorer), cx)
                                        .panel_view(panel_handle(search), cx),
                                    Some(px(left)),
                                )
                                .child(DockLayout::tabs().panel_view(panel_handle(editor), cx), None),
                            None,
                        )
                        .child(
                            DockLayout::tabs()
                                .panel_view(panel_handle(terminal), cx)
                                .panel_view(panel_handle(problems), cx),
                            Some(px(bottom)),
                        ),
                    window,
                    cx,
                );
            });
            // The area keeps its own clone of the skin.
            view.state.entity = Some(area.into());
        }
        let area = view.state.entity.clone().and_then(|e| e.downcast::<DockArea>().ok()).expect("dock area");
        div().w(px(width)).h(px(height)).child(area).into_any_element()
    }))
}
