//! `Carousel` (crates/component/src/carousel): slides with Previous/Next
//! controls and numbered pagination, as the story composes them. `vertical`
//! turns the track (its content then takes the slides' height) and `looping`
//! wraps it.
use super::size;
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    AppContext as _, Axis, IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        ActiveTheme as _, Sizable as _, StyledExt as _,
        carousel::{
            Carousel, CarouselContent, CarouselItem, CarouselNext, CarouselPagination, CarouselPaginationItem,
            CarouselPrevious, CarouselState,
        },
    },
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let count = param_f32(params, "count", 3.) as usize;
    let selected = param_f32(params, "selected", 0.) as usize;
    let (width, height) = (param_f32(params, "width", 240.), param_f32(params, "height", 120.));
    let controls = !param_bool(params, "no_controls");
    let pagination = !param_bool(params, "no_pagination");
    let size = size(params);
    let vertical = param_bool(params, "vertical");
    let looping = param_bool(params, "looping");
    Ok(Rc::new(move |view, _, cx| {
        if view.state.entity.is_none() {
            let axis = if vertical { Axis::Vertical } else { Axis::Horizontal };
            let state = cx.new(|_| {
                CarouselState::new(count).with_selected_index(selected).with_axis(axis).with_looping(looping)
            });
            view.state.entity = Some(state.into());
        }
        let state = view
            .state
            .entity
            .clone()
            .and_then(|e| e.downcast::<CarouselState>().ok())
            .expect("carousel state");
        let theme = cx.theme();
        let (border, muted, radius_lg) = (theme.border, theme.muted, theme.radius_lg);
        // The slides are app content: a muted card with its number.
        let track = CarouselContent::new(&state);
        let track = if vertical { track.h(px(height)) } else { track };
        let content = (0..count).fold(track, |content, ix| {
            content.child(
                CarouselItem::new(("slide", ix), ix, &state).child(
                    div()
                        .h(px(height))
                        .flex()
                        .items_center()
                        .justify_center()
                        .rounded(radius_lg)
                        .border_1()
                        .border_color(border)
                        .bg(muted)
                        .text_2xl()
                        .font_semibold()
                        .child((ix + 1).to_string()),
                ),
            )
        });
        let mut carousel = Carousel::new("case", &state).w(px(width)).child(content);
        if controls {
            carousel = carousel
                .child(CarouselPrevious::new(&state).with_size(size))
                .child(CarouselNext::new(&state).with_size(size));
        }
        if pagination {
            carousel = carousel.child(CarouselPagination::new().children((0..count).map(|ix| {
                CarouselPaginationItem::new(("page", ix), ix, &state).child((ix + 1).to_string())
            })));
        }
        carousel.into_any_element()
    }))
}
