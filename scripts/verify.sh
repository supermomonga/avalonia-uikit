#!/usr/bin/env bash
# Builds the theme and runs every test: tokens, looks, motions and behaviors.
# Arguments go to the test runner (e.g. --treenode-filter "/*/*/ButtonTests/*").
# AVALONIA_UIKIT_CALIBRATE=1 also writes tests/artifacts/pixel-stats.csv and
# ink-mass.csv for tuning the pixel limits.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
dotnet build "$ROOT/tests/AvaloniaUIKit.Tests"
dotnet run --no-build --project "$ROOT/tests/AvaloniaUIKit.Tests" -- "$@"
