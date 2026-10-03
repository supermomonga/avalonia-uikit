/**
 * Checks the built site (dist/): every page exists, internal links, the
 * head's links and scripts, and the demos' preview images resolve, no React
 * prop leaks into the HTML, and the output stays within Cloudflare's static
 * asset limits (20,000 files, 25 MiB per file on the Free plan).
 */
import { readdirSync, readFileSync, statSync } from "node:fs"
import path from "node:path"
import { components } from "../app/lib/catalog"
import { siteConfig } from "../app/lib/site"

const dist = path.resolve(import.meta.dirname, "../dist")
const failures: string[] = []
const warnings: string[] = []

function walk(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name)
    return entry.isDirectory() ? walk(full) : [full]
  })
}

const files = walk(dist)
const relative = new Set(files.map((file) => `/${path.relative(dist, file)}`))

function resolves(pathname: string): boolean {
  const clean = pathname.replace(/\/$/, "") || "/"
  return (
    clean === "/" ||
    relative.has(clean) ||
    relative.has(`${clean}.html`) ||
    relative.has(`${clean}/index.html`)
  )
}

if (files.length >= 20_000)
  failures.push(`${files.length} files (limit 20,000)`)
for (const file of files) {
  if (statSync(file).size >= 25 * 1024 * 1024) {
    failures.push(`${path.relative(dist, file)} is 25 MiB or larger`)
  }
}

const pages = [
  "/index.html",
  "/404.html",
  "/docs.html",
  "/docs/installation.html",
  "/docs/theming.html",
  "/docs/icons.html",
  "/docs/compatibility.html",
  "/docs/components.html",
  "/sitemap.xml",
  "/search.json",
  "/favicon.ico",
  "/favicon.svg",
  "/apple-touch-icon.png",
  "/icon-192.png",
  "/icon-512.png",
  "/manifest.webmanifest",
  "/robots.txt",
  "/_headers",
  ...components.map((entry) => `/docs/components/${entry.slug}.html`),
]
for (const page of pages) {
  if (!relative.has(page)) failures.push(`missing ${page}`)
}
// The social image is rendered by samples/AvaloniaUIKit.Previews, separately.
if (!relative.has(siteConfig.ogImage.url)) {
  warnings.push(`${siteConfig.ogImage.url} is not built yet`)
}

/** The path of a URL on the site, or undefined for other sites. */
function sitePath(url: string): string | undefined {
  const parsed = new URL(url, siteConfig.url)
  return parsed.origin === siteConfig.url ? parsed.pathname : undefined
}

/** Internal links, the head's links and scripts, the demos' images, and React's className. */
async function inspect(html: string) {
  const links: string[] = []
  const head: string[] = []
  const images: string[] = []
  let demos = 0
  let className = false
  await new HTMLRewriter()
    .on("head link[href]", {
      element(element) {
        head.push(element.getAttribute("href") ?? "")
      },
    })
    .on("head script[src]", {
      element(element) {
        head.push(element.getAttribute("src") ?? "")
      },
    })
    .on("meta[property='og:image']", {
      element(element) {
        head.push(element.getAttribute("content") ?? "")
      },
    })
    .on("*", {
      element(element) {
        if (element.hasAttribute("classname")) className = true
      },
    })
    .on("a[href^='/']", {
      element(element) {
        links.push(element.getAttribute("href") ?? "")
      },
    })
    .on("avalonia-demo", {
      element() {
        demos++
      },
    })
    .on("avalonia-demo img[src], img[src^='/previews/']", {
      element(element) {
        images.push(element.getAttribute("src") ?? "")
      },
    })
    .transform(new Response(html))
    .text()
  return { links, head, images, demos, className }
}

let demoCount = 0
for (const file of files.filter((f) => f.endsWith(".html"))) {
  const html = readFileSync(file, "utf8")
  const name = path.relative(dist, file)
  const { links, head, images, demos, className } = await inspect(html)
  demoCount += demos
  if (className) failures.push(`${name} renders className`)
  for (const url of head) {
    const href = sitePath(url)
    if (href === siteConfig.ogImage.url && !resolves(href)) continue
    if (href && !resolves(href))
      failures.push(`${name} links to missing ${url}`)
  }
  for (const link of links) {
    const href = link.replace(/[#?].*$/, "")
    if (href && !resolves(href)) {
      failures.push(`${name} links to missing ${href}`)
    }
  }
  for (const src of images) {
    if (!resolves(src)) failures.push(`${name} shows missing ${src}`)
  }
}

// The sitemap and the search index point at pages that exist.
const sitemap = readFileSync(path.join(dist, "sitemap.xml"), "utf8")
for (const [, loc] of sitemap.matchAll(/<loc>([^<]+)<\/loc>/g)) {
  const href = sitePath(loc)
  if (!href || !resolves(href)) failures.push(`sitemap.xml lists missing ${loc}`)
}
const search = JSON.parse(
  readFileSync(path.join(dist, "search.json"), "utf8")
) as { pages: { href: string }[]; components: { href: string }[] }
for (const entry of [...search.pages, ...search.components]) {
  if (!resolves(entry.href)) failures.push(`search.json lists missing ${entry.href}`)
}

for (const warning of warnings) console.warn(`- ${warning}`)
if (failures.length > 0) {
  console.error(failures.map((failure) => `- ${failure}`).join("\n"))
  process.exit(1)
}
console.log(
  `Checked ${files.length} files, ${pages.length} pages and ${demoCount} demos in dist/.`
)
