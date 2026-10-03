//! `Avatar` and `AvatarGroup` (crates/component/src/avatar): initials in a
//! color picked from them, an image, or a placeholder icon, in a circle.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _,
    component::{Sizable as _, avatar::{Avatar, AvatarGroup}},
};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let name = params.get("name").and_then(|v| v.as_str()).map(str::to_string);
    let image = match params.get("image").and_then(|v| v.as_str()) {
        Some(image) => Some(super::image::decode(image)?),
        None => None,
    };
    let size = super::size(params);
    Ok(Rc::new(move |_, _, _| {
        let mut avatar = Avatar::new().with_size(size);
        if let Some(name) = name.clone() {
            avatar = avatar.name(name);
        }
        if let Some(image) = image.clone() {
            avatar = avatar.src(image);
        }
        avatar.into_any_element()
    }))
}

pub fn group(params: &Params) -> Result<Builder> {
    let names: Vec<String> = param_str(params, "names", "Alice|Bob|Carol").split('|').map(str::to_string).collect();
    let limit = param_f32(params, "limit", 3.) as usize;
    let ellipsis = param_bool(params, "ellipsis");
    let size = super::size(params);
    Ok(Rc::new(move |_, _, _| {
        let mut group = AvatarGroup::new().with_size(size).limit(limit);
        if ellipsis {
            group = group.ellipsis();
        }
        group
            .children(names.iter().map(|name| Avatar::new().name(name.clone())))
            .into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, gpui_kit::Hsla)>) {
    use gpui_kit::component::oklch;
    // avatar.rs IdentityColor::from_hue: twelve OkLCH hues, 30 degrees apart,
    // at a fixed lightness and chroma per theme (background, foreground, border).
    for step in 0..12 {
        let hue = step as f32 * 30.;
        let (background, foreground, border) = if theme.is_dark() {
            (oklch(0.30, 0.05, hue), oklch(0.82, 0.11, hue), oklch(0.36, 0.06, hue))
        } else {
            (oklch(0.97, 0.032, hue), oklch(0.50, 0.145, hue), oklch(0.89, 0.05, hue))
        };
        out.push((format!("avatar.hue-{step}.background"), background));
        out.push((format!("avatar.hue-{step}.foreground"), foreground));
        out.push((format!("avatar.hue-{step}.border"), border));
    }
}
