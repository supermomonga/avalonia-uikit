//! Serializes the primitives GPUI painted for a frame.
//!
//! Every coordinate is converted from scaled (device) pixels to logical pixels
//! by dividing by the window scale, and every color is converted with GPUI's own
//! `Rgba::from(Hsla)`, so the values are exactly what the Metal shaders receive.
use gpui_kit::{Background, Bounds, Corners, Edges, Hsla, LinearColorStop, Rgba, Scene, ScaledPixels};
use serde_json::{Value, json};

fn rgba(color: Hsla) -> Value {
    let c = Rgba::from(color);
    json!([round6(c.r), round6(c.g), round6(c.b), round6(c.a)])
}

fn round6(value: f32) -> f64 {
    (value as f64 * 1_000_000.0).round() / 1_000_000.0
}

fn px(value: ScaledPixels, scale: f32) -> f64 {
    round6(value.as_f32() / scale)
}

fn bounds(bounds: &Bounds<ScaledPixels>, scale: f32) -> Value {
    json!([
        px(bounds.origin.x, scale),
        px(bounds.origin.y, scale),
        px(bounds.size.width, scale),
        px(bounds.size.height, scale)
    ])
}

fn corners(corners: &Corners<ScaledPixels>, scale: f32) -> Value {
    json!([
        px(corners.top_left, scale),
        px(corners.top_right, scale),
        px(corners.bottom_right, scale),
        px(corners.bottom_left, scale)
    ])
}

fn edges(edges: &Edges<ScaledPixels>, scale: f32) -> Value {
    json!([
        px(edges.top, scale),
        px(edges.right, scale),
        px(edges.bottom, scale),
        px(edges.left, scale)
    ])
}

fn background(background: &Background) -> Value {
    if let Some(color) = background.as_solid() {
        return json!({ "kind": "solid", "rgba": rgba(color) });
    }
    // Background keeps its fields crate-private but serializes them all.
    let raw = serde_json::to_value(background).unwrap_or(Value::Null);
    if raw["tag"] == "LinearGradient" {
        let stops: Vec<Value> = raw["colors"]
            .as_array()
            .into_iter()
            .flatten()
            .filter_map(|stop| serde_json::from_value::<LinearColorStop>(stop.clone()).ok())
            .map(|stop| json!({ "rgba": rgba(stop.color), "percentage": stop.percentage }))
            .collect();
        return json!({
            "kind": "linear",
            "angle": raw["gradient_angle_or_pattern_height"],
            "color_space": raw["color_space"],
            "stops": stops,
        });
    }
    json!({ "kind": "other", "debug": format!("{background:?}") })
}

/// Converts a painted scene to JSON. `scale` is the window scale factor.
pub fn scene_to_json(scene: &Scene, scale: f32) -> Value {
    let quads: Vec<Value> = scene
        .quads
        .iter()
        .map(|quad| {
            json!({
                "order": quad.order,
                "bounds": bounds(&quad.bounds, scale),
                "clip": bounds(&quad.content_mask.bounds, scale),
                "background": background(&quad.background),
                "border_color": rgba(quad.border_color),
                "border_widths": edges(&quad.border_widths, scale),
                "corner_radii": corners(&quad.corner_radii, scale),
                "border_style": format!("{:?}", quad.border_style),
            })
        })
        .collect();
    let shadows: Vec<Value> = scene
        .shadows
        .iter()
        .map(|shadow| {
            json!({
                "order": shadow.order,
                "bounds": bounds(&shadow.bounds, scale),
                "clip": bounds(&shadow.content_mask.bounds, scale),
                "corner_radii": corners(&shadow.corner_radii, scale),
                "sigma": px(shadow.blur_radius, scale),
                "color": rgba(shadow.color),
                "element_bounds": bounds(&shadow.element_bounds, scale),
                "element_corner_radii": corners(&shadow.element_corner_radii, scale),
                "inset": shadow.inset != 0,
            })
        })
        .collect();
    let underlines: Vec<Value> = scene
        .underlines
        .iter()
        .map(|underline| {
            json!({
                "order": underline.order,
                "bounds": bounds(&underline.bounds, scale),
                "clip": bounds(&underline.content_mask.bounds, scale),
                "thickness": px(underline.thickness, scale),
                "color": rgba(underline.color),
                "wavy": underline.wavy == gpui_kit::PaddedBool32::from(true),
            })
        })
        .collect();
    let mono_sprites: Vec<Value> = scene
        .monochrome_sprites
        .iter()
        .map(|sprite| {
            json!({
                "order": sprite.order,
                "bounds": bounds(&sprite.bounds, scale),
                "clip": bounds(&sprite.content_mask.bounds, scale),
                "color": rgba(sprite.color),
                "transform": transform(&sprite.transformation),
            })
        })
        .collect();
    let subpixel_sprites: Vec<Value> = scene
        .subpixel_sprites
        .iter()
        .map(|sprite| {
            json!({
                "order": sprite.order,
                "bounds": bounds(&sprite.bounds, scale),
                "clip": bounds(&sprite.content_mask.bounds, scale),
                "color": rgba(sprite.color),
                "transform": transform(&sprite.transformation),
            })
        })
        .collect();
    let poly_sprites: Vec<Value> = scene
        .polychrome_sprites
        .iter()
        .map(|sprite| {
            json!({
                "order": sprite.order,
                "bounds": bounds(&sprite.bounds, scale),
                "clip": bounds(&sprite.content_mask.bounds, scale),
                "opacity": round6(sprite.opacity),
                "corner_radii": corners(&sprite.corner_radii, scale),
            })
        })
        .collect();
    let paths: Vec<Value> = scene
        .paths
        .iter()
        .map(|path| {
            json!({
                "order": path.order,
                "bounds": bounds(&path.bounds, scale),
                "clip": bounds(&path.content_mask.bounds, scale),
                "color": background(&path.color),
                "vertices": path.vertices.len(),
            })
        })
        .collect();
    json!({
        "quads": quads,
        "shadows": shadows,
        "underlines": underlines,
        "mono_sprites": mono_sprites,
        "subpixel_sprites": subpixel_sprites,
        "poly_sprites": poly_sprites,
        "paths": paths,
    })
}

fn transform(matrix: &gpui_kit::TransformationMatrix) -> Value {
    json!({
        "rotation_scale": matrix.rotation_scale.iter().map(|row| row.iter().map(|v| round6(*v)).collect::<Vec<_>>()).collect::<Vec<_>>(),
        "translation": matrix.translation.iter().map(|v| round6(*v)).collect::<Vec<_>>(),
    })
}
