#!/usr/bin/env bash
# Prepares reference/vendor/: gpui-kit at the pinned commit and the gpui-pre
# snapshot, both with the test-clock patches applied. vendor/ is gitignored.
set -euo pipefail

HERE="$(cd "$(dirname "$0")/.." && pwd)"
VENDOR="$HERE/vendor"
GPUI_KIT_REV="2c5162f8c5b0c7fcec066ed53125d304c632bfe2"
GPUI_KIT_SRC="${GPUI_KIT_SRC:-$HOME/ghq/github.com/longbridge/gpui-kit}"
GPUI_PRE_VERSION="0.3.7"
# Checksum of gpui-pre 0.3.7 as recorded in gpui-kit's Cargo.lock.
GPUI_PRE_SHA256="$(grep -A3 '^name = "gpui-pre"$' "$GPUI_KIT_SRC/Cargo.lock" 2>/dev/null | sed -n 's/^checksum = "\(.*\)"/\1/p' || true)"

rm -rf "$VENDOR"
mkdir -p "$VENDOR/gpui-kit" "$VENDOR/gpui-pre"

if [ -d "$GPUI_KIT_SRC/.git" ] && git -C "$GPUI_KIT_SRC" cat-file -e "$GPUI_KIT_REV^{commit}" 2>/dev/null; then
  git -C "$GPUI_KIT_SRC" archive "$GPUI_KIT_REV" | tar -x -C "$VENDOR/gpui-kit"
else
  tmp="$(mktemp -d)"
  git clone --quiet https://github.com/longbridge/gpui-kit "$tmp/gpui-kit"
  git -C "$tmp/gpui-kit" archive "$GPUI_KIT_REV" | tar -x -C "$VENDOR/gpui-kit"
  GPUI_PRE_SHA256="$(grep -A3 '^name = "gpui-pre"$' "$tmp/gpui-kit/Cargo.lock" | sed -n 's/^checksum = "\(.*\)"/\1/p')"
  rm -rf "$tmp"
fi

crate="$VENDOR/gpui-pre.crate"
curl -sSfL "https://static.crates.io/crates/gpui-pre/gpui-pre-$GPUI_PRE_VERSION.crate" -o "$crate"
actual="$(shasum -a 256 "$crate" | cut -d' ' -f1)"
if [ -n "$GPUI_PRE_SHA256" ] && [ "$actual" != "$GPUI_PRE_SHA256" ]; then
  echo "gpui-pre checksum mismatch: $actual != $GPUI_PRE_SHA256" >&2
  exit 1
fi
tar -xzf "$crate" -C "$VENDOR/gpui-pre" --strip-components=1
rm "$crate"

# The patches only replace wall-clock reads with the test executor's clock
# (and the calendar's "today" with a date the case pins).
for patch in "$HERE"/patches/*.patch; do
  target="$VENDOR/$(basename "$patch" | cut -d'+' -f1)"
  patch --quiet --forward --strip=1 --directory="$target" < "$patch"
done

# Guard against a patch that applied but missed a call site.
remaining="$(grep -c 'Instant::now()' "$VENDOR/gpui-pre/src/elements/animation.rs" || true)"
if [ "$remaining" != "0" ]; then
  echo "gpui-pre animation.rs still reads the wall clock ($remaining sites)" >&2
  exit 1
fi
remaining="$(grep -c 'Instant::now()' "$VENDOR/gpui-kit/crates/base/src/scrollbar.rs" || true)"
if [ "$remaining" != "0" ]; then
  echo "gpui-base scrollbar.rs still reads the wall clock ($remaining sites)" >&2
  exit 1
fi
remaining="$(grep -c 'Local::now()' "$VENDOR/gpui-kit/crates/base/src/calendar.rs" || true)"
if [ "$remaining" != "0" ]; then
  echo "gpui-base calendar.rs still reads today from the wall clock ($remaining sites)" >&2
  exit 1
fi
echo "vendor ready: gpui-kit $GPUI_KIT_REV, gpui-pre $GPUI_PRE_VERSION"
