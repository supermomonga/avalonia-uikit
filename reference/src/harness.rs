//! Headless GPUI context, case windows and frame capture.
use crate::scene_json::scene_to_json;
use anyhow::{Context as _, Result};
use gpui_kit::{
    AnyElement, AnyWindowHandle, AppContext, InputEvent as _, Bounds, Context, DevicePixels, HeadlessAppContext,
    InteractiveElement as _, IntoElement, KeyDownEvent, KeyUpEvent, Keystroke, MouseButton,
    MouseDownEvent, MouseMoveEvent, MouseUpEvent, ParentElement as _, PlatformAtlas, ScrollDelta, ScrollWheelEvent,
    PlatformHeadlessRenderer, Pixels, Point, Render, Scene, Size, Styled as _, Window, div,
    point, px, size,
    assets::Assets,
    component::{ActiveTheme as _, Theme, ThemeMode, scroll::ScrollbarMode},
    test::TestWindowExt as _,
};
use image::RgbaImage;
use serde_json::Value;
use std::{
    cell::{Cell, RefCell},
    path::Path,
    rc::Rc,
    sync::Arc,
    time::Duration,
};

/// Forwards to the Metal headless renderer and keeps the scene it was handed.
struct RecordingRenderer {
    inner: Box<dyn PlatformHeadlessRenderer>,
    sink: Rc<RefCell<Option<Value>>>,
}

impl PlatformHeadlessRenderer for RecordingRenderer {
    fn render_scene_to_image(&mut self, scene: &Scene, size: Size<DevicePixels>) -> Result<RgbaImage> {
        *self.sink.borrow_mut() = Some(scene_to_json(scene, crate::SCALE));
        self.inner.render_scene_to_image(scene, size)
    }

    fn render_scene(&mut self, scene: &Scene, size: Size<DevicePixels>) -> Result<()> {
        self.inner.render_scene(scene, size)
    }

    fn sprite_atlas(&self) -> Arc<dyn PlatformAtlas> {
        self.inner.sprite_atlas()
    }
}

/// Mutable state that cases read while building their element. Event handlers
/// installed by cases update it, so interactive motion can be recorded.
#[derive(Default)]
pub struct CaseState {
    pub toggled: bool,
    pub value: f32,
    pub entity: Option<gpui_kit::AnyEntity>,
}

pub type Builder = Rc<dyn Fn(&mut CaseView, &mut Window, &mut Context<CaseView>) -> AnyElement>;

/// The root view of a case window: the theme background, and the component
/// absolutely positioned at the anchor.
pub struct CaseView {
    pub build: Builder,
    pub anchor: (f32, f32),
    pub state: CaseState,
    /// Bounds of the component's outermost element, updated every prepaint.
    pub bounds: Rc<Cell<Option<Bounds<Pixels>>>>,
}

impl Render for CaseView {
    fn render(&mut self, window: &mut Window, cx: &mut Context<Self>) -> impl IntoElement {
        let build = self.build.clone();
        let child = build(self, window, cx);
        let slot = self.bounds.clone();
        div()
            .id("case-root")
            .size_full()
            .relative()
            .bg(cx.theme().background)
            .text_color(cx.theme().foreground)
            .child(
                div()
                    .absolute()
                    .left(px(self.anchor.0))
                    .top(px(self.anchor.1))
                    .on_children_prepainted(move |children, _, _| {
                        slot.set(children.first().copied());
                    })
                    .child(child),
            )
    }
}

pub struct Harness {
    pub cx: HeadlessAppContext,
    sink: Rc<RefCell<Option<Value>>>,
}

pub struct CaseWindow {
    pub handle: AnyWindowHandle,
    pub bounds: Rc<Cell<Option<Bounds<Pixels>>>>,
}

/// One captured frame.
pub struct Frame {
    pub image: RgbaImage,
    pub scene: Value,
}

impl Harness {
    pub fn new(root: &Path) -> Result<Self> {
        let sink: Rc<RefCell<Option<Value>>> = Rc::new(RefCell::new(None));
        let factory_sink = sink.clone();
        let mut cx = HeadlessAppContext::with_platform(
            gpui_kit::platform::current_platform(true).text_system(),
            Arc::new(Assets),
            move || {
                let inner = gpui_kit::platform::current_headless_renderer()?
                    .context("no headless renderer on this platform")?;
                Ok(Some(Box::new(RecordingRenderer {
                    inner,
                    sink: factory_sink.clone(),
                }) as Box<dyn PlatformHeadlessRenderer>))
            },
        );
        cx.update(gpui_kit::init);
        cx.update(crate::cases::menu::init);
        let fonts = crate::fonts::load(root)?;
        cx.update(|cx| cx.text_system().add_fonts(fonts))?;
        Ok(Self { cx, sink })
    }

    /// Applies the theme mode and the harness-wide theme settings.
    pub fn set_theme(&mut self, mode: ThemeMode, scrollbar: ScrollbarMode) {
        self.cx.update(|cx| {
            cx.set_reduce_motion(false);
            Theme::change(mode, None, cx);
            Theme::update(cx, |theme| {
                theme.font_family = crate::fonts::FAMILY.into();
                theme.focus_ring = true;
                theme.scrollbar_mode = scrollbar;
            });
        });
    }

    pub fn open(&mut self, viewport: (f32, f32), anchor: (f32, f32), build: Builder) -> Result<CaseWindow> {
        let bounds: Rc<Cell<Option<Bounds<Pixels>>>> = Rc::new(Cell::new(None));
        let view_bounds = bounds.clone();
        let (handle, _) = self.cx.update(|cx| {
            gpui_kit::open_window(
                gpui_kit::WindowOptions {
                    window_bounds: Some(gpui_kit::WindowBounds::Windowed(Bounds {
                        origin: Default::default(),
                        size: size(px(viewport.0), px(viewport.1)),
                    })),
                    focus: true,
                    show: false,
                    ..Default::default()
                },
                cx,
                move |_, cx| {
                    cx.new(|_| CaseView {
                        build,
                        anchor,
                        state: CaseState::default(),
                        bounds: view_bounds,
                    })
                },
            )
        })?;
        self.sync_clock();
        self.cx.update_window(handle, |_, window, cx| window.render_frame(cx))?;
        Ok(CaseWindow { handle, bounds })
    }

    pub fn close(&mut self, window: &CaseWindow) {
        let _ = self
            .cx
            .update_window(window.handle, |_, window, _| window.remove_window());
        self.cx.run_until_parked();
    }

    /// Pins the scrollbar's sampled time to the test executor's clock.
    pub fn sync_clock(&self) {
        gpui_base::test_clock::set(Some(self.cx.background_executor().now()));
    }

    pub fn render(&mut self, window: &CaseWindow) -> Result<()> {
        self.cx.run_until_parked();
        self.sync_clock();
        self.cx
            .update_window(window.handle, |_, window, cx| window.render_frame(cx))?;
        Ok(())
    }

    /// Advances the executor clock, runs due timers and renders a frame.
    pub fn advance(&mut self, window: &CaseWindow, by: Duration) -> Result<()> {
        self.cx.advance_clock(by);
        self.render(window)
    }

    pub fn capture(&mut self, window: &CaseWindow) -> Result<Frame> {
        self.render(window)?;
        *self.sink.borrow_mut() = None;
        let image = self.cx.capture_screenshot(window.handle)?;
        let scene = self
            .sink
            .borrow_mut()
            .take()
            .context("the renderer did not receive a scene")?;
        Ok(Frame { image, scene })
    }

    pub fn component_bounds(&self, window: &CaseWindow) -> Result<Bounds<Pixels>> {
        window.bounds.get().context("the component was not prepainted")
    }

    /// The point at `fraction` of the component's bounds (0.5, 0.5 is the center).
    pub fn component_point(&self, window: &CaseWindow, fraction: (f32, f32)) -> Result<Point<Pixels>> {
        let bounds = self.component_bounds(window)?;
        Ok(point(
            bounds.origin.x + bounds.size.width * fraction.0,
            bounds.origin.y + bounds.size.height * fraction.1,
        ))
    }

    pub fn mouse_move(&mut self, window: &CaseWindow, position: Point<Pixels>, pressed: Option<MouseButton>) -> Result<()> {
        self.sync_clock();
        self.cx.update_window(window.handle, |_, window, cx| {
            window.dispatch_event(
                MouseMoveEvent {
                    position,
                    pressed_button: pressed,
                    modifiers: Default::default(),
                }
                .to_platform_input(),
                cx,
            );
        })?;
        self.render(window)
    }

    pub fn mouse_down(&mut self, window: &CaseWindow, position: Point<Pixels>, button: MouseButton) -> Result<()> {
        self.sync_clock();
        self.cx.update_window(window.handle, |_, window, cx| {
            window.dispatch_event(
                MouseDownEvent {
                    button,
                    position,
                    modifiers: Default::default(),
                    click_count: 1,
                    first_mouse: false,
                }
                .to_platform_input(),
                cx,
            );
        })?;
        self.render(window)
    }

    pub fn mouse_up(&mut self, window: &CaseWindow, position: Point<Pixels>, button: MouseButton) -> Result<()> {
        self.sync_clock();
        self.cx.update_window(window.handle, |_, window, cx| {
            window.dispatch_event(
                MouseUpEvent {
                    button,
                    position,
                    modifiers: Default::default(),
                    click_count: 1,
                }
                .to_platform_input(),
                cx,
            );
        })?;
        self.render(window)
    }

    /// Scrolls the content under `position` down by `by` pixels, as a trackpad does.
    pub fn wheel(&mut self, window: &CaseWindow, position: Point<Pixels>, by: f32) -> Result<()> {
        self.mouse_move(window, position, None)?;
        self.sync_clock();
        self.cx.update_window(window.handle, |_, window, cx| {
            window.dispatch_event(
                ScrollWheelEvent {
                    position,
                    delta: ScrollDelta::Pixels(point(px(0.), px(-by))),
                    ..Default::default()
                }
                .to_platform_input(),
                cx,
            );
        })?;
        self.render(window)
    }

    pub fn click(&mut self, window: &CaseWindow, position: Point<Pixels>, button: MouseButton) -> Result<()> {
        self.mouse_move(window, position, None)?;
        self.mouse_down(window, position, button)?;
        self.mouse_up(window, position, button)
    }

    /// Moves keyboard focus forward, as Tab does. Tab only reaches Root's
    /// binding once something inside it holds focus, so the first stop calls
    /// `focus_next` directly (as gpui-kit's own rendering tests do).
    pub fn tab(&mut self, window: &CaseWindow) -> Result<()> {
        let focused = self
            .cx
            .update_window(window.handle, |_, window, cx| window.focused(cx).is_some())?;
        if focused {
            return self.key(window, "tab");
        }
        self.sync_clock();
        self.cx
            .update_window(window.handle, |_, window, cx| window.focus_next(cx))?;
        self.render(window)
    }

    pub fn key(&mut self, window: &CaseWindow, key: &str) -> Result<()> {
        let keystroke = Keystroke::parse(key)?;
        self.sync_clock();
        self.cx.update_window(window.handle, |_, window, cx| {
            window.dispatch_event(
                KeyDownEvent {
                    keystroke: keystroke.clone(),
                    is_held: false,
                    prefer_character_input: false,
                }
                .to_platform_input(),
                cx,
            );
            window.dispatch_event(KeyUpEvent { keystroke }.to_platform_input(), cx);
        })?;
        self.render(window)
    }
}
