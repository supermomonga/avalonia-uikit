//! `Settings` (crates/component/src/setting): a searchable sidebar of pages
//! beside the selected page's groups of items, after the settings story
//! (settings_story.rs) in a `width` x `height` box.
//!
//! Pages: General (Appearance: a switch and a checkbox; Font: a dropdown and
//! a number input, with a footer; Other: a vertical input), Software Update
//! (a switch) and About (a custom item, and a render field in Links).
//! `variant` is the settings' group variant, `size` the fields' size, `page`
//! the page selected at first, `dirty` starts Dark Mode and Font Size away from
//! their defaults (the page shows its reset button), `resettable = false`
//! hides it, `disabled` disables the Appearance items, and `group_variant`
//! overrides the Font group's variant. The values live in the case, so the
//! fields and the reset button change them.
use crate::{
    harness::Builder,
    manifest::{Params, param_bool, param_f32, param_str},
};
use anyhow::Result;
use gpui_kit::{
    App, Axis, IntoElement as _, ParentElement as _, SharedString, Styled as _, div, px,
    component::{
        ActiveTheme as _, IconName, Sizable as _,
        button::Button,
        group_box::GroupBoxVariant,
        label::Label,
        setting::{NumberFieldOptions, SelectIndex, SettingField, SettingGroup, SettingItem, SettingPage, Settings},
        v_flex,
    },
};
use std::{cell::RefCell, rc::Rc};

#[derive(Clone)]
struct Values {
    dark: bool,
    auto_switch: bool,
    font_family: SharedString,
    font_size: f64,
    cli_path: SharedString,
    auto_update: bool,
}

impl Default for Values {
    fn default() -> Self {
        Self {
            dark: false,
            auto_switch: false,
            font_family: "Arial".into(),
            font_size: 14.,
            cli_path: "/usr/local/bin/bash".into(),
            auto_update: true,
        }
    }
}

fn variant(name: &str) -> GroupBoxVariant {
    match name {
        "fill" => GroupBoxVariant::Fill,
        "outline" => GroupBoxVariant::Outline,
        _ => GroupBoxVariant::Normal,
    }
}

pub fn builder(params: &Params) -> Result<Builder> {
    let group_variant = variant(param_str(params, "variant", "normal"));
    let font_variant = params.get("group_variant").and_then(|v| v.as_str()).map(variant);
    let size = super::size(params);
    let (w, h) = (param_f32(params, "width", 800.), param_f32(params, "height", 520.));
    let page = param_f32(params, "page", 0.) as usize;
    let resettable = params.get("resettable").and_then(|v| v.as_bool()).unwrap_or(true);
    let disabled = param_bool(params, "disabled");
    let mut start = Values::default();
    if param_bool(params, "dirty") {
        start.dark = true;
        start.font_size = 16.;
    }
    let values = Rc::new(RefCell::new(start));
    Ok(Rc::new(move |_, _, cx| {
        let view = cx.entity().downgrade();
        let defaults = Values::default();
        // A field's getter and setter over one of the values; the setter redraws the case.
        macro_rules! field {
            ($name:ident) => {
                (
                    {
                        let values = values.clone();
                        move |_: &App| values.borrow().$name.clone()
                    },
                    {
                        let (values, view) = (values.clone(), view.clone());
                        move |value, cx: &mut App| {
                            values.borrow_mut().$name = value;
                            view.update(cx, |_, cx| cx.notify()).ok();
                        }
                    },
                )
            };
        }
        let (dark, set_dark) = field!(dark);
        let (auto_switch, set_auto_switch) = field!(auto_switch);
        let (font_family, set_font_family) = field!(font_family);
        let (font_size, set_font_size) = field!(font_size);
        let (cli_path, set_cli_path) = field!(cli_path);
        let (auto_update, set_auto_update) = field!(auto_update);

        let mut font = SettingGroup::new()
            .title("Font")
            .footer(|_, _| "Font preferences apply to this window only.")
            .items([
                SettingItem::new(
                    "Font Family",
                    SettingField::dropdown(
                        vec![
                            ("Arial".into(), "Arial".into()),
                            ("Helvetica".into(), "Helvetica".into()),
                            ("Times New Roman".into(), "Times New Roman".into()),
                        ],
                        font_family,
                        set_font_family,
                    )
                    .default_value(defaults.font_family.clone()),
                )
                .description("Select the font family."),
                SettingItem::new(
                    "Font Size",
                    SettingField::number_input(
                        NumberFieldOptions { min: 8., max: 72., ..Default::default() },
                        font_size,
                        set_font_size,
                    )
                    .default_value(defaults.font_size),
                )
                .description("Adjust the font size between 8 and 72."),
            ]);
        if let Some(v) = font_variant {
            font = font.variant(v);
        }
        let general = SettingPage::new("General")
            .icon(IconName::Settings2)
            .default_open(true)
            .resettable(resettable)
            .groups([
                SettingGroup::new().title("Appearance").items([
                    SettingItem::new("Dark Mode", SettingField::switch(dark, set_dark).default_value(defaults.dark))
                        .description("Switch between light and dark themes.")
                        .disabled(disabled),
                    SettingItem::new(
                        "Auto Switch Theme",
                        SettingField::checkbox(auto_switch, set_auto_switch).default_value(defaults.auto_switch),
                    )
                    .description("Automatically switch theme based on system settings.")
                    .disabled(disabled),
                ]),
                font,
                SettingGroup::new().title("Other").item(
                    SettingItem::new(
                        "CLI Path",
                        SettingField::input(cli_path, set_cli_path).default_value(defaults.cli_path.clone()),
                    )
                    .layout(Axis::Vertical)
                    .description("Path to the CLI executable.")
                    .keywords(["shell", "terminal"]),
                ),
            ]);
        let update = SettingPage::new("Software Update")
            .icon(IconName::Cpu)
            .resettable(resettable)
            .group(
                SettingGroup::new().title("Updates").item(
                    SettingItem::new(
                        "Auto Update",
                        SettingField::switch(auto_update, set_auto_update).default_value(defaults.auto_update),
                    )
                    .description("Download and install new versions as they come out."),
                ),
            );
        let about = SettingPage::new("About")
            .icon(IconName::Info)
            .description("The application and where to find out more.")
            .groups([
                SettingGroup::new().item(SettingItem::render(|_, _, cx| {
                    v_flex()
                        .gap_1()
                        .child("GPUI Kit")
                        .child(Label::new("Version 0.1.0").text_sm().text_color(cx.theme().muted_foreground))
                })),
                SettingGroup::new().title("Links").item(
                    SettingItem::new(
                        "GitHub Repository",
                        SettingField::render(|options, _, _| {
                            Button::new("repository").outline().label("Repository...").with_size(options.size())
                        }),
                    )
                    .description("Open the GitHub repository."),
                ),
            ]);
        let settings = Settings::new("settings")
            .with_group_variant(group_variant)
            .with_size(size)
            .default_selected_index(SelectIndex { page_ix: page, group_ix: None })
            .pages([general, update, about]);
        div().w(px(w)).h(px(h)).child(settings).into_any_element()
    }))
}
