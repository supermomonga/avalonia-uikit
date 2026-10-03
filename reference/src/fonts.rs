//! The bundled test fonts. The Avalonia tests load the same files.
use anyhow::{Context as _, Result};
use std::{borrow::Cow, path::Path};

pub const FAMILY: &str = "Inter";
pub const FILES: [&str; 4] = [
    "Inter-Regular.ttf",
    "Inter-Medium.ttf",
    "Inter-SemiBold.ttf",
    "Inter-Bold.ttf",
];

pub fn load(root: &Path) -> Result<Vec<Cow<'static, [u8]>>> {
    FILES
        .iter()
        .map(|file| {
            let path = root.join("assets/fonts/inter").join(file);
            std::fs::read(&path)
                .with_context(|| format!("reading {}", path.display()))
                .map(Cow::Owned)
        })
        .collect()
}
