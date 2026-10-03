#!/usr/bin/env bash
# Regenerates the GPUI Kit reference data under goldens/ and the generated
# theme resources (Colors.g.axaml, Lucide.g.axaml). macOS with Metal only.
# Arguments go to `reference generate` (e.g. --only button/).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
"$ROOT/reference/scripts/vendor.sh"
cd "$ROOT/reference"
cargo build --release
./target/release/reference generate "$@"
./target/release/reference verify-determinism "$@"
python3 "$ROOT/scripts/prune_goldens.py"
