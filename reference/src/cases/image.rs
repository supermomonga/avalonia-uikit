//! GPUI's `img()` (GPUI Kit has no Image component of its own), drawing a
//! decoded test image so the first frame already shows it; or a load that
//! never finishes (the loading content after 200ms) or fails (the fallback).
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    AnyElement, App, ImageCacheError, InteractiveElement as _, IntoElement as _, ObjectFit,
    ParentElement as _, RenderImage, Styled as _, StyledImage as _, Window, div, img, px,
    component::{ActiveTheme as _, Icon, Sizable as _, Size},
};
use std::{rc::Rc, sync::Arc};

pub fn decode(name: &str) -> Result<Arc<RenderImage>> {
    let bytes: &[u8] = match name {
        "wide" => include_bytes!("../../../assets/images/wide-192x96.png"),
        "tall" => include_bytes!("../../../assets/images/tall-96x192.png"),
        _ => include_bytes!("../../../assets/images/small-48x48.png"),
    };
    let mut rgba = image::load_from_memory(bytes)?.into_rgba8();
    // BGRA, as GPUI's own decoder hands frames to the renderer (platform.rs).
    for p in rgba.chunks_exact_mut(4) {
        p.swap(0, 2);
    }
    Ok(Arc::new(RenderImage::new(vec![image::Frame::new(rgba)])))
}

fn fit(name: &str) -> ObjectFit {
    match name {
        "fill" => ObjectFit::Fill,
        "cover" => ObjectFit::Cover,
        "scale-down" => ObjectFit::ScaleDown,
        "none" => ObjectFit::None,
        _ => ObjectFit::Contain,
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    let fit_name = param_str(params, "fit", "contain").to_string();
    let (w, h) = (param_f32(params, "width", 96.), param_f32(params, "height", 96.));
    let radius = param_f32(params, "radius", 0.);
    let image = decode(param_str(params, "image", "wide"))?;
    let source = param_str(params, "source", "loaded").to_string();
    Ok(Rc::new(move |_, _, cx| {
        if source != "loaded" {
            return pending(&source, w, h, cx.theme().muted, cx.theme().muted_foreground);
        }
        let mut el = img(image.clone()).object_fit(fit(&fit_name)).rounded(px(radius));
        if w > 0. {
            el = el.w(px(w));
        }
        if h > 0. {
            el = el.h(px(h));
        }
        el.into_any_element()
    }))
}

/// An image whose custom loader never finishes ("loading") or fails at once
/// ("failed"), with an id so it keeps the loading state, and app content for
/// both: the muted box with the loader or the circle-x icon.
fn pending(source: &str, w: f32, h: f32, muted: gpui_kit::Hsla, muted_foreground: gpui_kit::Hsla) -> AnyElement {
    let content = move |icon: &'static str| {
        div()
            .size_full()
            .flex()
            .items_center()
            .justify_center()
            .bg(muted)
            .child(Icon::new(super::icon(icon).unwrap()).with_size(Size::Medium).text_color(muted_foreground))
            .into_any_element()
    };
    let el = if source == "failed" {
        img(|_: &mut Window, _: &mut App| -> Option<Result<Arc<RenderImage>, ImageCacheError>> {
            Some(Err(ImageCacheError::Asset("missing".into())))
        })
    } else {
        img(|_: &mut Window, _: &mut App| -> Option<Result<Arc<RenderImage>, ImageCacheError>> { None })
    };
    el.id("image")
        .w(px(w))
        .h(px(h))
        .with_loading(move || content("loader"))
        .with_fallback(move || content("circle-x"))
        .into_any_element()
}
