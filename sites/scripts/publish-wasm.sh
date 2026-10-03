#!/usr/bin/env bash
# Publishes the browser app (samples/AvaloniaUIKit.Browser) and places its
# _framework under sites/public/wasm/<hash>/, where <hash> fingerprints the
# bundle so the whole directory can be cached as immutable (docs/site.md).
# sites/public/wasm/index.json names the current directory for the site build.
# Needs the wasm-tools workload: `dotnet workload install wasm-tools`.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/sites/public/wasm"
DOTNET="${DOTNET:-dotnet}"

PUB_ROOT="$ROOT/samples/AvaloniaUIKit.Browser/bin/Release/net10.0-browser/publish"
# publish does not clean its output: fingerprinted files of earlier builds would pile up.
rm -rf "$PUB_ROOT"
"$DOTNET" publish "$ROOT/samples/AvaloniaUIKit.Browser" -c Release -nologo
PUB="$PUB_ROOT/wwwroot/_framework"
[ -d "$PUB" ] || { echo "publish output not found: $PUB" >&2; exit 1; }

if command -v sha256sum >/dev/null; then SUM=sha256sum; else SUM="shasum -a 256"; fi
HASH="$(cd "$PUB" && find . -type f ! -name '*.br' ! -name '*.gz' | LC_ALL=C sort | xargs $SUM | $SUM | cut -c1-10)"

rm -rf "$OUT"
mkdir -p "$OUT/$HASH"
cp -R "$PUB" "$OUT/$HASH/_framework"
# Precompressed twins (Cloudflare compresses itself) and source maps are not served.
find "$OUT" \( -name '*.br' -o -name '*.gz' -o -name '*.map' \) -delete
printf '{"base":"/wasm/%s"}\n' "$HASH" > "$OUT/index.json"
echo "wasm: $OUT/$HASH ($(du -sh "$OUT/$HASH" | cut -f1))"
