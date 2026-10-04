//! `Bubble` (crates/component/src/bubble.rs): a message surface in seven
//! variants, at most 80% of the row, with an optional reaction pill.
use crate::{
    harness::Builder,
    manifest::{Params, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    IntoElement as _, ParentElement as _, Styled as _, div, px,
    component::{
        bubble::{Bubble, BubbleReactionSide, BubbleReactions, BubbleVariant},
        message::MessageAlignment,
        v_flex,
    },
};
use std::rc::Rc;

pub fn variant(name: &str) -> BubbleVariant {
    match name {
        "secondary" => BubbleVariant::Secondary,
        "muted" => BubbleVariant::Muted,
        "tinted" => BubbleVariant::Tinted,
        "outline" => BubbleVariant::Outline,
        "ghost" => BubbleVariant::Ghost,
        "destructive" => BubbleVariant::Destructive,
        _ => BubbleVariant::Filled,
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    let variant = variant(param_str(params, "variant", "filled"));
    let alignment = match param_str(params, "align", "") {
        "start" => Some(MessageAlignment::Start),
        "end" => Some(MessageAlignment::End),
        _ => None,
    };
    let text = param_str(params, "text", "Can you review this draft?").to_string();
    let reaction = params.get("reaction").and_then(|v| v.as_str()).map(str::to_string);
    let top = param_str(params, "reaction_side", "bottom") == "top";
    let reaction_start = param_str(params, "reaction_align", "end") == "start";
    let width = param_f32(params, "width", 360.);
    Ok(Rc::new(move |_, _, _| {
        let mut bubble = Bubble::new().with_variant(variant).child(text.clone());
        if let Some(alignment) = alignment {
            bubble = bubble.alignment(alignment);
        }
        if let Some(reaction) = reaction.clone() {
            bubble = bubble.reactions(
                BubbleReactions::new()
                    .side(if top { BubbleReactionSide::Top } else { BubbleReactionSide::Bottom })
                    .alignment(if reaction_start { MessageAlignment::Start } else { MessageAlignment::End })
                    .child(reaction),
            );
        }
        div().w(px(width)).child(v_flex().w_full().child(bubble)).into_any_element()
    }))
}

pub fn derived_colors(theme: &gpui_kit::component::Theme, out: &mut Vec<(String, super::Paint)>) {
    use gpui_kit::component::Colorize as _;
    // bubble.rs: Tinted mixes the primary into the background (12%, dark 24%);
    // Destructive is the destructive color at 10% (dark 20%).
    let tokens = theme.semantic_tokens();
    let dark = theme.is_dark();
    out.push(("bubble.tinted".into(), tokens.colors.primary.mix_oklab(tokens.colors.background, if dark { 0.24 } else { 0.12 }).into()));
    out.push(("bubble.destructive".into(), tokens.colors.destructive.opacity(if dark { 0.2 } else { 0.1 }).into()));
    out.push(("bubble.destructive-foreground".into(), tokens.colors.destructive.into()));
}
