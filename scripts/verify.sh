#!/usr/bin/env bash
# Builds the theme and runs every test: tokens, looks, motions and behaviors, and
# the control catalog's.
# In Release: the pixel comparisons run 4x slower without the JIT optimizing them.
#
# A process hosts one headless Avalonia session and runs its tests one at a
# time (ADR 12), so without arguments the tests are split between processes run
# side by side: one per performance core, or AVALONIA_UIKIT_SHARDS=N.
# Arguments go to the test runner of a single process
# (e.g. --treenode-filter "/*/*/ButtonTests/*").
# AVALONIA_UIKIT_CALIBRATE=1 also writes tests/artifacts/pixel-stats.csv and
# ink-mass.csv for tuning the pixel limits, in a single process (it appends to them).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/tests/AvaloniaUIKit.Tests"
CATALOG="$ROOT/tests/AvaloniaUIKit.Demo.ControlCatalog.Tests"
dotnet build -c Release "$PROJECT"

# The control catalog's tests (docs/control-catalog.md): one process, under a minute.
# Arguments filter the theme's tests only, so they skip these.
if [[ $# -eq 0 ]]; then
  dotnet build -c Release "$CATALOG"
  dotnet run --no-build -c Release --project "$CATALOG"
fi

SHARDS="${AVALONIA_UIKIT_SHARDS:-$(sysctl -n hw.perflevel0.physicalcpu 2>/dev/null || getconf _NPROCESSORS_ONLN)}"
if [[ $# -gt 0 || -n "${AVALONIA_UIKIT_CALIBRATE:-}" || "$SHARDS" -le 1 ]]; then
  exec dotnet run --no-build -c Release --project "$PROJECT" -- "$@"
fi

LOGS="$(mktemp -d)"
PIDS=""
trap 'kill $PIDS 2>/dev/null || true; rm -rf "$LOGS"' EXIT
echo "Running the tests in $SHARDS processes"
for ((k = 0; k < SHARDS; k++)); do
  # Every process gives each test the same Shard property and runs its own (Infrastructure/Shard.cs).
  AVALONIA_UIKIT_SHARDS="$SHARDS" dotnet run --no-build -c Release --project "$PROJECT" -- \
    --treenode-filter "/*/*/*/*[Shard=$k]" --results-directory "$LOGS/$k" > "$LOGS/$k.log" 2>&1 &
  PIDS="$PIDS $!"
done

status=0
k=0
for pid in $PIDS; do
  if ! wait "$pid"; then
    status=1
    cat "$LOGS/$k.log"
  fi
  echo "shard $k: $(grep -E '^[[:space:]]*(total|failed|duration):' "$LOGS/$k.log" | xargs || true)"
  k=$((k + 1))
done
PIDS=""
[[ $status -eq 0 ]] && echo "Passed" || echo "FAILED"
exit $status
