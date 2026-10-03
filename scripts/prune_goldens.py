#!/usr/bin/env python3
"""Deletes golden PNGs and scenes no manifest entry refers to (cases renamed or removed)."""
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent / "goldens" / "gpui-2c5162f"
manifest = json.loads((ROOT / "manifest.json").read_text())
keep = set()
for case in manifest["cases"]:
    for key in ("png", "scene"):
        if case.get(key):
            keep.add((ROOT / case[key]).resolve())
    for frame in (case.get("motion") or {}).get("frames", []):
        keep.add((ROOT / frame["png"]).resolve())
        keep.add((ROOT / frame["scene"]).resolve())
removed = 0
for kind in ("png", "scenes"):
    for path in sorted((ROOT / kind).rglob("*"), key=lambda p: -len(p.parts)):
        if path.is_file() and path.resolve() not in keep:
            path.unlink()
            removed += 1
        elif path.is_dir() and not any(path.iterdir()):
            path.rmdir()
print(f"pruned {removed} files")
