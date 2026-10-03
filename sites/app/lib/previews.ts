/**
 * Build-time outputs of the .NET side (docs/site.md), both optional:
 * - public/previews/manifest.json: the logical size of each demo's PNGs
 *   (samples/AvaloniaUIKit.Previews).
 * - public/wasm/index.json: the hashed directory of the current WASM bundle
 *   (scripts/publish-wasm.sh), which the live demos load from.
 */
import { existsSync, readFileSync } from "node:fs"
import path from "node:path"
import { fileURLToPath } from "node:url"

const publicDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../public"
)

function readJson<T>(file: string): T | undefined {
  const full = path.join(publicDir, file)
  if (!existsSync(full)) return undefined
  return JSON.parse(readFileSync(full, "utf8")) as T
}

export interface PreviewSize {
  width: number
  height: number
}

const manifest =
  readJson<Record<string, PreviewSize>>("previews/manifest.json") ?? {}

/** The logical size of the demo's preview, or undefined when it is not rendered. */
export function previewSize(id: string): PreviewSize | undefined {
  return manifest[id]
}

/** `/previews/<id>.light.png` and `.dark.png`. */
export function previewUrls(id: string): { light: string; dark: string } {
  return { light: `/previews/${id}.light.png`, dark: `/previews/${id}.dark.png` }
}

/** `/wasm/<hash>`, or undefined when no bundle is published. */
export const wasmBase = readJson<{ base: string }>("wasm/index.json")?.base
