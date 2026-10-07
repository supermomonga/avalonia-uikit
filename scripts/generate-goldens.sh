#!/usr/bin/env bash
# Regenerates the GPUI Kit reference data under goldens/ and the generated
# theme resources (Palettes.g.cs, the icons, the site's themes). macOS with
# Metal only. `reference tokens` rewrites only the colors, and
# `cargo run --release -p uikit-icons` (in reference/) only the icons.
# Arguments go to `reference generate` (e.g. --only button/).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
"$ROOT/reference/scripts/vendor.sh"
cd "$ROOT/reference"
cargo build --release
./target/release/reference generate "$@"
./target/release/reference verify-determinism "$@"
python3 "$ROOT/scripts/prune_goldens.py"
