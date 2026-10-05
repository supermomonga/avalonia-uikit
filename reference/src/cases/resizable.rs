//! `h_resizable` / `v_resizable` (crates/component/src/resizable.rs): two
//! empty panels and the handle between them.
//!
//! uikit:ResizablePanelGroup's cases (uikit-resizable) add `sizes` (each
//! panel's size, 0 for none), `maxs` (the end of a panel's size_range, 0 for
//! none), `hidden` (the index of a panel built with visible(false)) and
//! `nested` (a vertical group whose first panel holds the horizontal pair).
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::resizable::{ResizablePanelGroup, h_resizable, resizable_panel, v_resizable},
};
use std::rc::Rc;

fn numbers(params: &Params, key: &str) -> Option<Vec<f32>> {
    params.get(key)?.as_array().map(|values| values.iter().map(|v| v.as_f64().unwrap_or(0.) as f32).collect())
}

pub fn builder(params: &Params) -> Result<Builder> {
    let vertical = param_bool(params, "vertical");
    let nested = param_bool(params, "nested");
    let width = param_f32(params, "width", 320.);
    let height = param_f32(params, "height", 120.);
    let first = param_f32(params, "first", 120.);
    let sizes = numbers(params, "sizes").unwrap_or_else(|| vec![first, 0.]);
    let maxs = numbers(params, "maxs").unwrap_or_default();
    let hidden = params.get("hidden").and_then(|v| v.as_u64()).map(|v| v as usize);
    Ok(Rc::new(move |_, _, _| {
        let panels = |group: ResizablePanelGroup| {
            sizes.iter().enumerate().fold(group, |group, (ix, &size)| {
                let mut panel = resizable_panel().child(div().size_full());
                if size > 0. {
                    panel = panel.size(px(size));
                }
                if let Some(&max) = maxs.get(ix).filter(|&&max| max > 0.) {
                    panel = panel.size_range(px(100.)..px(max));
                }
                if hidden == Some(ix) {
                    panel = panel.visible(false);
                }
                group.child(panel)
            })
        };
        let group = if nested {
            v_resizable("case")
                .child(resizable_panel().size(px(first)).child(panels(h_resizable("inner"))))
                .child(resizable_panel().child(div().size_full()))
        } else if vertical {
            panels(v_resizable("case"))
        } else {
            panels(h_resizable("case"))
        };
        div().w(px(width)).h(px(height)).child(group).into_any_element()
    }))
}
