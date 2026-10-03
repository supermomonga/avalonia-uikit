//! An empty window: only the theme background.
use crate::{harness::Builder, manifest::Params};
use anyhow::Result;
use gpui_kit::{IntoElement as _, Styled as _, div, px};
use std::rc::Rc;

pub fn builder(_params: &Params) -> Result<Builder> {
    Ok(Rc::new(|_, _, _| div().size(px(1.)).into_any_element()))
}
