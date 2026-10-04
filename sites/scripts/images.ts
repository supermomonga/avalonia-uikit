/**
 * Renders the site's social image and icons into public/, where they are
 * committed:
 * - og.png: the dev server's /og-image page (app/routes/og-image.tsx) at
 *   1200×630, twice the pixels. The page shows the demos' previews, so render
 *   them first (docs/site.md).
 * - apple-touch-icon.png, icon-192.png, icon-512.png and favicon.ico, from
 *   logo.svg.
 *
 *   bun scripts/images.ts
 *
 * Needs Playwright's Chromium (`bunx playwright install chromium`).
 */
import { existsSync, readFileSync, writeFileSync } from "node:fs"
import path from "node:path"
import { chromium } from "playwright-core"
import { createServer } from "vite"
import { siteConfig } from "../app/lib/site"

const SITE_DIR = path.resolve(import.meta.dirname, "..")
const PUBLIC = path.join(SITE_DIR, "public")

if (!existsSync(path.join(PUBLIC, "previews/manifest.json"))) {
  throw new Error(
    "public/previews is empty: render the previews first (docs/site.md)"
  )
}

/** The icons: the mark on white, where platforms would fill transparency. */
const ICONS = [
  { file: "apple-touch-icon.png", size: 180, padding: 24 },
  { file: "icon-192.png", size: 192, padding: 26 },
  { file: "icon-512.png", size: 512, padding: 68 },
]

/** An ICO file holding one PNG image, which every browser reads. */
function ico(png: Buffer, size: number): Buffer {
  const header = Buffer.alloc(22)
  header.writeUInt16LE(0, 0) // reserved
  header.writeUInt16LE(1, 2) // type: icon
  header.writeUInt16LE(1, 4) // number of images
  header.writeUInt8(size, 6) // width
  header.writeUInt8(size, 7) // height
  header.writeUInt8(0, 8) // no palette
  header.writeUInt8(0, 9) // reserved
  header.writeUInt16LE(1, 10) // color planes
  header.writeUInt16LE(32, 12) // bits per pixel
  header.writeUInt32LE(png.length, 14)
  header.writeUInt32LE(header.length, 18) // offset of the image
  return Buffer.concat([header, png])
}

// HonoX's dev server resolves its entry (app/server.ts) from the working directory.
process.chdir(SITE_DIR)
const server = await createServer({
  root: SITE_DIR,
  configFile: path.join(SITE_DIR, "vite.config.ts"),
  logLevel: "warn",
  server: { port: 0 },
})
await server.listen()
const browser = await chromium.launch()
try {
  const base = server.resolvedUrls?.local[0]
  if (!base) throw new Error("The dev server has no local URL")
  const { width, height } = siteConfig.ogImage
  const page = await browser.newPage({
    viewport: { width: width / 2, height: height / 2 },
    deviceScaleFactor: 2,
    colorScheme: "light",
  })
  const response = await page.goto(new URL("/og-image", base).toString(), {
    waitUntil: "networkidle",
  })
  if (!response?.ok()) {
    throw new Error(`/og-image responded with ${response?.status()}`)
  }
  await page.evaluate(() => document.fonts.ready)
  await page.screenshot({ path: path.join(PUBLIC, "og.png") })
  console.log(`wrote public/og.png (${width}×${height})`)

  const svg = readFileSync(path.join(PUBLIC, "logo.svg"))
  const icon = async (size: number, padding: number, background?: string) => {
    const page = await browser.newPage({
      viewport: { width: size, height: size },
      deviceScaleFactor: 1,
    })
    await page.setContent(
      `<body style="margin:0;background:${background ?? "transparent"}"><img src="data:image/svg+xml;base64,${svg.toString("base64")}" width="${size - padding * 2}" height="${size - padding * 2}" style="display:block;margin:${padding}px"></body>`
    )
    const png = await page.screenshot({ omitBackground: !background })
    await page.close()
    return png
  }
  for (const { file, size, padding } of ICONS) {
    writeFileSync(path.join(PUBLIC, file), await icon(size, padding, "#ffffff"))
    console.log(`wrote public/${file}`)
  }
  writeFileSync(path.join(PUBLIC, "favicon.ico"), ico(await icon(32, 0), 32))
  console.log("wrote public/favicon.ico")
} finally {
  await browser.close()
  await server.close()
}
