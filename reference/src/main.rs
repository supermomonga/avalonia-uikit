//! Renders GPUI Kit components with the Metal headless renderer and records
//! golden data (PNG, painted primitives, resolved theme tokens) for the
//! Avalonia theme tests.
//!
//! Usage:
//!   reference generate [--only <id-prefix>]
//!   reference verify-determinism [--only <id-prefix>]
mod cases;
mod derived;
mod fonts;
mod harness;
mod icons;
mod manifest;
mod scene_json;
mod tokens;

use anyhow::{Context as _, Result, bail};
use gpui_kit::component::{ThemeMode, scroll::ScrollbarMode};
use harness::Harness;
use manifest::{Case, param_str};
use serde_json::{Value, json};
use sha2::{Digest, Sha256};
use std::{
    path::{Path, PathBuf},
    time::Duration,
};

pub const GPUI_KIT_REV: &str = "2c5162f8c5b0c7fcec066ed53125d304c632bfe2";
/// The headless test window always renders at scale 2.
pub const SCALE: f32 = 2.0;

fn main() -> Result<()> {
    #[cfg(not(target_os = "macos"))]
    bail!("the reference harness needs GPUI's Metal renderer (macOS)");

    let args: Vec<String> = std::env::args().skip(1).collect();
    let command = args.first().map(String::as_str).unwrap_or("generate");
    let only = args
        .iter()
        .position(|a| a == "--only")
        .and_then(|i| args.get(i + 1))
        .cloned();
    let root = PathBuf::from(env!("CARGO_MANIFEST_DIR"))
        .parent()
        .context("repository root")?
        .to_path_buf();
    let out = root.join("goldens").join(format!("gpui-{}", &GPUI_KIT_REV[..7]));
    match command {
        "generate" => generate(&root, &out, only.as_deref()),
        "verify-determinism" => verify_determinism(&root, only.as_deref()),
        other => bail!("unknown command {other}"),
    }
}

fn sha256(bytes: &[u8]) -> String {
    let digest = Sha256::digest(bytes);
    digest.iter().map(|b| format!("{b:02x}")).collect()
}

fn png_bytes(image: &image::RgbaImage) -> Result<Vec<u8>> {
    let mut bytes = Vec::new();
    image.write_to(&mut std::io::Cursor::new(&mut bytes), image::ImageFormat::Png)?;
    Ok(bytes)
}

fn mode(case: &Case) -> ThemeMode {
    if case.theme == "dark" { ThemeMode::Dark } else { ThemeMode::Light }
}

fn scrollbar_mode(case: &Case) -> ScrollbarMode {
    match param_str(&case.params, "scrollbar_mode", "hover") {
        "always" => ScrollbarMode::Always,
        "scrolling" => ScrollbarMode::Scrolling,
        _ => ScrollbarMode::Hover,
    }
}

fn write(path: &Path, bytes: &[u8]) -> Result<()> {
    if let Some(parent) = path.parent() {
        std::fs::create_dir_all(parent)?;
    }
    std::fs::write(path, bytes).with_context(|| format!("writing {}", path.display()))
}

struct Captured {
    entry: Value,
    files: Vec<(String, Vec<u8>)>,
}

fn bounds_json(b: gpui_kit::Bounds<gpui_kit::Pixels>) -> Value {
    json!([
        f32::from(b.origin.x),
        f32::from(b.origin.y),
        f32::from(b.size.width),
        f32::from(b.size.height)
    ])
}

fn run_case(harness: &mut Harness, case: &Case) -> Result<Captured> {
    harness.set_theme(mode(case), scrollbar_mode(case));
    let window = harness.open(
        (case.viewport[0], case.viewport[1]),
        (case.anchor[0], case.anchor[1]),
        cases::builder(case)?,
    )?;
    let mut files = Vec::new();
    let params = cases::effective_params(case);
    let mut entry = json!({
        "id": case.id,
        "component": case.component,
        "group": case.group,
        "params": params,
        "state": case.state,
        "theme": case.theme,
        "viewport": case.viewport,
        "anchor": case.anchor,
    });
    let result = (|| -> Result<()> {
        if let Some(motion) = &case.motion {
            cases::drive(harness, &window, case, &motion.from)?;
            entry["component_bounds"] = bounds_json(harness.component_bounds(&window)?);
            cases::drive(harness, &window, case, &motion.trigger)?;
            let mut elapsed = 0u64;
            let mut frames = Vec::new();
            for &t in &motion.samples_ms {
                if t < elapsed {
                    bail!("motion samples must be increasing in {}", case.id);
                }
                harness.advance(&window, Duration::from_millis(t - elapsed))?;
                elapsed = t;
                let frame = harness.capture(&window)?;
                let png = png_bytes(&frame.image)?;
                let png_path = format!("png/{}/{t:04}ms.png", case.id);
                let scene_path = format!("scenes/{}/{t:04}ms.json", case.id);
                frames.push(json!({
                    "t_ms": t,
                    "png": png_path,
                    "scene": scene_path,
                    "png_sha256": sha256(&png),
                }));
                files.push((png_path, png));
                files.push((scene_path, serde_json::to_vec_pretty(&frame.scene)?));
            }
            entry["motion"] = json!({
                "name": motion.name,
                "trigger": motion.trigger,
                "from": motion.from,
                "frames": frames,
            });
        } else {
            cases::drive(harness, &window, case, &case.state)?;
            entry["component_bounds"] = bounds_json(harness.component_bounds(&window)?);
            let frame = harness.capture(&window)?;
            let png = png_bytes(&frame.image)?;
            let png_path = format!("png/{}.png", case.id);
            let scene_path = format!("scenes/{}.json", case.id);
            entry["png"] = json!(png_path);
            entry["scene"] = json!(scene_path);
            entry["png_sha256"] = json!(sha256(&png));
            files.push((png_path, png));
            files.push((scene_path, serde_json::to_vec_pretty(&frame.scene)?));
        }
        Ok(())
    })();
    harness.close(&window);
    result.with_context(|| format!("case {}", case.id))?;
    Ok(Captured { entry, files })
}

fn all_cases(root: &Path, only: Option<&str>) -> Result<Vec<Case>> {
    let mut cases = Vec::new();
    for spec in manifest::load_specs(&root.join("cases"))? {
        for case in manifest::expand(&spec)? {
            if only.is_none_or(|prefix| case.id.starts_with(prefix)) {
                cases.push(case);
            }
        }
    }
    Ok(cases)
}

fn generate(root: &Path, out: &Path, only: Option<&str>) -> Result<()> {
    let mut harness = Harness::new(root)?;
    let tokens = tokens::dump(&mut harness)?;
    write(&out.join("tokens/gpui-theme.json"), &serde_json::to_vec_pretty(&tokens)?)?;
    tokens::write_xaml(root, &tokens)?;
    icons::write_xaml(root)?;

    let cases = all_cases(root, only)?;
    // Every case must have a builder before anything is deleted.
    for case in &cases {
        cases::builder(case).with_context(|| format!("no builder for {}", case.id))?;
    }
    if only.is_none() {
        for dir in ["png", "scenes"] {
            let _ = std::fs::remove_dir_all(out.join(dir));
        }
    }
    let manifest_path = out.join("manifest.json");
    let mut entries: Vec<Value> = if only.is_some() && manifest_path.exists() {
        let existing: Value = serde_json::from_slice(&std::fs::read(&manifest_path)?)?;
        existing["cases"]
            .as_array()
            .cloned()
            .unwrap_or_default()
            .into_iter()
            .filter(|e| !e["id"].as_str().is_some_and(|id| only.is_some_and(|p| id.starts_with(p))))
            .collect()
    } else {
        Vec::new()
    };
    let total = cases.len();
    for (index, case) in cases.iter().enumerate() {
        let captured = run_case(&mut harness, case)?;
        for (path, bytes) in &captured.files {
            write(&out.join(path), bytes)?;
        }
        entries.push(captured.entry);
        if (index + 1) % 50 == 0 || index + 1 == total {
            eprintln!("{}/{} cases", index + 1, total);
        }
    }
    entries.sort_by(|a, b| a["id"].as_str().cmp(&b["id"].as_str()));
    let manifest = json!({
        "gpui_kit": GPUI_KIT_REV,
        "gpui_pre": "0.3.7",
        "scale": SCALE,
        "font_family": fonts::FAMILY,
        "cases": entries,
    });
    write(&manifest_path, &serde_json::to_vec_pretty(&manifest)?)?;
    eprintln!("wrote {}", out.display());
    Ok(())
}

fn verify_determinism(root: &Path, only: Option<&str>) -> Result<()> {
    let cases = all_cases(root, only)?;
    let mut first = Harness::new(root)?;
    let mut second = Harness::new(root)?;
    let mut mismatches = 0;
    for case in &cases {
        let a = run_case(&mut first, case)?;
        let b = run_case(&mut second, case)?;
        if a.files != b.files {
            eprintln!("non-deterministic: {}", case.id);
            mismatches += 1;
        }
    }
    if mismatches > 0 {
        bail!("{mismatches} of {} cases differ between runs", cases.len());
    }
    eprintln!("{} cases render identically twice", cases.len());
    Ok(())
}
