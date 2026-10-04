#!/usr/bin/env bash
# Publishes the gallery with NativeAOT (trim and AOT warnings are errors) and
# runs it once: it renders every control in light, dark and two bundled
# themes, then exits.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RID="${RID:-$(dotnet --info | sed -n 's/^ *RID: *//p' | head -1)}"
OUT="${OUT:-$ROOT/samples/AvaloniaUIKit.AotSmoke/bin/aot/$RID}"
dotnet publish "$ROOT/samples/AvaloniaUIKit.AotSmoke" -c Release -r "$RID" -p:PublishAot=true -o "$OUT"
"$OUT/AvaloniaUIKit.AotSmoke" --smoke
