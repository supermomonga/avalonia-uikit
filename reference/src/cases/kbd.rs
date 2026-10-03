//! `Kbd` (crates/component/src/kbd.rs): a keystroke in GPUI's platform
//! notation on a small muted key cap.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_str},
};
use anyhow::Result;
use gpui_kit::{IntoElement as _, Keystroke, component::kbd::Kbd};
use std::rc::Rc;

pub fn builder(params: &Params) -> Result<Builder> {
    let stroke = Keystroke::parse(param_str(params, "keys", "a"))?;
    let outline = param_bool(params, "outline");
    Ok(Rc::new(move |_, _, _| {
        let mut kbd = Kbd::new(stroke.clone());
        if outline {
            kbd = kbd.outline();
        }
        kbd.into_any_element()
    }))
}
