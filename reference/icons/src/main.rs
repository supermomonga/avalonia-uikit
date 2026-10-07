//! Regenerates the icons alone, without GPUI or Metal:
//!   cargo run --release -p uikit-icons
//! `reference generate` writes the same files.
use anyhow::{Context as _, Result};
use std::path::PathBuf;

fn main() -> Result<()> {
    let root = PathBuf::from(env!("CARGO_MANIFEST_DIR"))
        .ancestors()
        .nth(2)
        .context("repository root")?
        .to_path_buf();
    uikit_icons::write(&root)
}
