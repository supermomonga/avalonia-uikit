//! The themes GPUI Kit ships in `themes/*.json` besides its default one, and
//! what a case's `theme` names: `light` and `dark` for Default Light and
//! Default Dark, or a bundled theme by its slug (`aurora-light`).
use anyhow::{Context as _, Result};
use gpui_kit::component::{ThemeConfig, ThemeSet};
use serde_json::Value;
use std::{path::Path, rc::Rc};

pub struct Bundled {
    /// The file's stem (`ayu`).
    pub source: String,
    /// The theme set's name (`Ayu`).
    pub family: String,
    /// The theme as GPUI Kit's registry loads it.
    pub config: Rc<ThemeConfig>,
    /// The theme's entry in the file, for what the registry does not resolve (`highlight`).
    pub raw: Value,
}

impl Bundled {
    pub fn name(&self) -> &str {
        &self.config.name
    }

    pub fn slug(&self) -> String {
        slug(&self.config.name)
    }

    pub fn dark(&self) -> bool {
        self.config.mode.is_dark()
    }
}

/// `Ayu Dark` -> `ayu-dark`; `macOS Classic Light` -> `macos-classic-light`.
pub fn slug(name: &str) -> String {
    name.split(|c: char| !c.is_ascii_alphanumeric())
        .filter(|part| !part.is_empty())
        .map(str::to_ascii_lowercase)
        .collect::<Vec<_>>()
        .join("-")
}

/// Every theme in `reference/vendor/gpui-kit/themes`, ordered by name.
pub fn load(root: &Path) -> Result<Vec<Bundled>> {
    let dir = root.join("reference/vendor/gpui-kit/themes");
    let mut files: Vec<_> = std::fs::read_dir(&dir)
        .with_context(|| format!("reading {}", dir.display()))?
        .filter_map(|entry| entry.ok().map(|e| e.path()))
        .filter(|path| path.extension().is_some_and(|ext| ext == "json"))
        .collect();
    files.sort();
    let mut themes = Vec::new();
    for path in files {
        let text = std::fs::read_to_string(&path)?;
        let set: ThemeSet = serde_json::from_str(&text).with_context(|| format!("parsing {}", path.display()))?;
        let raw: Value = serde_json::from_str(&text)?;
        let source = path.file_stem().unwrap_or_default().to_string_lossy().to_string();
        for (index, config) in set.themes.into_iter().enumerate() {
            themes.push(Bundled {
                source: source.clone(),
                family: set.name.to_string(),
                config: Rc::new(config),
                raw: raw["themes"][index].clone(),
            });
        }
    }
    themes.sort_by_key(|theme| theme.name().to_lowercase());
    Ok(themes)
}
