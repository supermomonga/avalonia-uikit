import { createRoute } from "honox/factory"
import { components } from "@/lib/catalog"
import { docPages } from "@/lib/docs"
import { siteConfig } from "@/lib/site"

export default createRoute((c) => {
  const paths = [
    "/",
    ...docPages.keys(),
    "/components",
    ...components.map((entry) => `/components/${entry.slug}`),
  ]
  const urls = paths
    .map((path) => `<url><loc>${new URL(path, siteConfig.url)}</loc></url>`)
    .join("")
  return c.body(
    `<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${urls}</urlset>`,
    200,
    { "content-type": "application/xml" }
  )
})
