/**
 * Renders public/favicon.svg (the site's mark) to the raster icons the
 * renderer and the web manifest reference: favicon.ico (32px),
 * apple-touch-icon.png (180px), icon-192.png and icon-512.png.
 *
 *   bun scripts/icons.ts
 */
import { readFileSync, writeFileSync } from "node:fs"
import path from "node:path"
import sharp from "sharp"

const publicDir = path.resolve(import.meta.dirname, "../public")
const svg = readFileSync(path.join(publicDir, "favicon.svg"), "utf8")

/** The mark on a white tile; `currentColor` becomes near-black. */
function tile(size: number, padding: number): Buffer {
  const inner = size - padding * 2
  const mark = svg
    .replace(/<style>[\s\S]*?<\/style>/, "")
    .replace(/style="color:[^"]*"/, 'style="color:#0a0a0a"')
    .replace("<svg ", `<svg width="${inner}" height="${inner}" `)
  return Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">` +
      `<rect width="${size}" height="${size}" fill="#ffffff" />` +
      `<svg x="${padding}" y="${padding}">${mark}</svg>` +
      "</svg>"
  )
}

/** The mark alone, transparent (for the tab's favicon). */
function mark(size: number): Buffer {
  return Buffer.from(
    svg
      .replace(/<style>[\s\S]*?<\/style>/, "")
      .replace(/style="color:[^"]*"/, 'style="color:#0a0a0a"')
      .replace("<svg ", `<svg width="${size}" height="${size}" `)
  )
}

async function png(input: Buffer, size: number): Promise<Buffer> {
  return sharp(input, { density: 384 }).resize(size, size).png().toBuffer()
}

/** An .ico container with a single PNG-encoded image (supported since Windows Vista). */
function ico(image: Buffer, size: number): Buffer {
  const header = Buffer.alloc(6)
  header.writeUInt16LE(0, 0) // reserved
  header.writeUInt16LE(1, 2) // icon
  header.writeUInt16LE(1, 4) // one image
  const entry = Buffer.alloc(16)
  entry.writeUInt8(size === 256 ? 0 : size, 0)
  entry.writeUInt8(size === 256 ? 0 : size, 1)
  entry.writeUInt8(0, 2) // colors in palette
  entry.writeUInt8(0, 3) // reserved
  entry.writeUInt16LE(1, 4) // color planes
  entry.writeUInt16LE(32, 6) // bits per pixel
  entry.writeUInt32LE(image.length, 8)
  entry.writeUInt32LE(header.length + entry.length, 12)
  return Buffer.concat([header, entry, image])
}

const outputs: [string, Buffer][] = [
  ["favicon.ico", ico(await png(mark(32), 32), 32)],
  ["apple-touch-icon.png", await png(tile(180, 22), 180)],
  ["icon-192.png", await png(tile(192, 24), 192)],
  ["icon-512.png", await png(tile(512, 64), 512)],
]
for (const [name, data] of outputs) {
  writeFileSync(path.join(publicDir, name), data)
  console.log(`wrote public/${name} (${data.length} bytes)`)
}
