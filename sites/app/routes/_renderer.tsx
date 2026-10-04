import { raw } from "hono/html"
import { jsxRenderer } from "hono/jsx-renderer"
import { Link } from "honox/server"
import { SiteFooter } from "@/components/site-footer"
import { SiteHeader } from "@/components/site-header"
import { SiteScript } from "@/components/site-script"
import { THEME_SCRIPT } from "@/components/theme-palette"
import type { RenderProps } from "@/global"
import { siteConfig } from "@/lib/site"

const sectionHome = { Docs: "/docs", Components: "/components" } as const

/** The `<title>`, as gpui-kit.com writes it: "Button — Components · Avalonia UIKit". */
function documentTitle({ title, section }: RenderProps): string {
  if (!title) return `${siteConfig.name} — ${siteConfig.tagline.replace(/\.$/, "")}`
  if (section === "Components" && title !== "Components") {
    return `${title} — Components · ${siteConfig.name}`
  }
  return `${title} · ${siteConfig.name}`
}

/** Structured data: the site on the home page; the page and its breadcrumbs elsewhere. */
function structuredData(props: RenderProps, url: string, title: string) {
  const site = {
    "@type": "WebSite",
    name: siteConfig.name,
    url: siteConfig.url,
    description: siteConfig.description,
  }
  if (!props.title) return { "@context": "https://schema.org", ...site }
  const crumbs: { name: string; url: string }[] = [
    { name: siteConfig.name, url: `${siteConfig.url}/` },
  ]
  if (props.section) {
    const href = new URL(sectionHome[props.section], siteConfig.url).toString()
    if (href !== url) crumbs.push({ name: props.section, url: href })
  }
  crumbs.push({ name: props.title, url })
  return {
    "@context": "https://schema.org",
    "@graph": [
      {
        "@type": "WebPage",
        name: title,
        url,
        description: props.description ?? siteConfig.description,
        isPartOf: site,
      },
      {
        "@type": "BreadcrumbList",
        itemListElement: crumbs.map((crumb, index) => ({
          "@type": "ListItem",
          position: index + 1,
          name: crumb.name,
          item: crumb.url,
        })),
      },
    ],
  }
}

export default jsxRenderer((props, c) => {
  const { children, description, bare } = props
  const pageTitle = documentTitle(props)
  const pageDescription = description ?? siteConfig.description
  const url = new URL(c.req.path, siteConfig.url).toString()
  const { ogImage, themeColor } = siteConfig
  const image = new URL(ogImage.url, siteConfig.url).toString()
  const json = JSON.stringify(structuredData(props, url, pageTitle)).replace(
    /</g,
    "\\u003c"
  )
  return (
    <html lang="en">
      <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>{pageTitle}</title>
        <meta name="description" content={pageDescription} />
        {bare && <meta name="robots" content="noindex" />}
        <link rel="canonical" href={url} />
        <meta
          name="theme-color"
          media="(prefers-color-scheme: light)"
          content={themeColor.light}
        />
        <meta
          name="theme-color"
          media="(prefers-color-scheme: dark)"
          content={themeColor.dark}
        />
        {/* Link previews (Open Graph, X). */}
        <meta property="og:type" content="website" />
        <meta property="og:site_name" content={siteConfig.name} />
        <meta property="og:locale" content="en_US" />
        <meta property="og:url" content={url} />
        <meta property="og:title" content={pageTitle} />
        <meta property="og:description" content={pageDescription} />
        <meta property="og:image" content={image} />
        <meta property="og:image:type" content="image/png" />
        <meta property="og:image:width" content={String(ogImage.width)} />
        <meta property="og:image:height" content={String(ogImage.height)} />
        <meta property="og:image:alt" content={ogImage.alt} />
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content={pageTitle} />
        <meta name="twitter:description" content={pageDescription} />
        <meta name="twitter:image" content={image} />
        <meta name="twitter:image:alt" content={ogImage.alt} />
        <link rel="icon" href="/favicon.ico" sizes="32x32" />
        <link rel="icon" href="/favicon.svg" type="image/svg+xml" />
        <link rel="apple-touch-icon" href="/apple-touch-icon.png" />
        <link rel="manifest" href="/manifest.webmanifest" />
        <script type="application/ld+json">{raw(json)}</script>
        <script>{raw(THEME_SCRIPT)}</script>
        <Link href="/app/style.css" rel="stylesheet" />
        <SiteScript src="/app/client.ts" />
        {/* The live demos' loader; small, so it is on every page. */}
        <SiteScript src="/app/avalonia-demo.ts" />
      </head>
      <body class="bg-background font-sans text-foreground">
        {bare ? (
          children
        ) : (
          <div class="flex min-h-svh flex-col">
            <a
              href="#content"
              class="sr-only z-[60] rounded-[var(--radius-control)] bg-background px-3 py-2 text-sm shadow-panel focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
            >
              Skip to content
            </a>
            <SiteHeader pathname={c.req.path} />
            {children}
            <SiteFooter />
          </div>
        )}
      </body>
    </html>
  )
})
