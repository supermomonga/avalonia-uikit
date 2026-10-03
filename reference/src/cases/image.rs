//! GPUI's `img()` (GPUI Kit has no Image component of its own), drawing a
//! decoded test image so the first frame already shows it.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{IntoElement as _, ObjectFit, RenderImage, Styled as _, StyledImage as _, img, px};
use std::{rc::Rc, sync::Arc};

fn decode(name: &str) -> Result<Arc<RenderImage>> {
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
    Ok(Rc::new(move |_, _, _| {
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
