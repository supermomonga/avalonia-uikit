import type { FC } from "hono/jsx"
import { components } from "./catalog"
import type { TocItem } from "./mdx/remark-toc"

export interface DocModule {
  default: FC<{ components?: Record<string, unknown> }>
  frontmatter: { title: string; description?: string }
  toc: TocItem[]
}

const modules = import.meta.glob<DocModule>("/content/docs/**/*.mdx", {
  eager: true,
})

/** Hand-written pages by URL path, e.g. `/docs/installation`. */
export const docPages = new Map<string, DocModule>(
  Object.entries(modules).map(([file, mod]) => [
    `/docs/${file.replace(/^\/content\/docs\//, "").replace(/\.mdx$/, "")}`
      .replace(/\/index$/, "")
      .replace(/^\/docs\/$/, "/docs"),
    mod,
  ])
)

const writtenComponentPages = import.meta.glob<DocModule>(
  "/content/components/*.mdx",
  { eager: true }
)

/** Component pages by slug (content/components/). */
export const componentPages = new Map<string, DocModule>(
  Object.entries(writtenComponentPages).map(([file, mod]) => [
    file.replace(/^.*\//, "").replace(/\.mdx$/, ""),
    mod,
  ])
)

export interface NavItem {
  title: string
  href: string
}

/** A section of the site with its own sidebar, as on gpui-kit.com. */
export interface DocsSection {
  /** The sidebar's heading. */
  title: string
  /** The section's top-level path. */
  href: string
  items: NavItem[]
}

function page(href: string): NavItem {
  const doc = docPages.get(href)
  if (!doc) throw new Error(`No page for ${href} in sites/content/docs`)
  return { title: doc.frontmatter.title, href }
}

export const docsSections: DocsSection[] = [
  {
    title: "Avalonia UIKit",
    href: "/docs",
    items: [
      page("/docs"),
      page("/docs/installation"),
      page("/docs/theming"),
      page("/docs/icons"),
    ],
  },
  {
    title: "Components",
    href: "/components",
    items: [
      { title: "Components", href: "/components" },
      ...[...components]
        .sort((a, b) => a.title.localeCompare(b.title, "en"))
        .map((entry) => ({
          title: entry.title,
          href: `/components/${entry.slug}`,
        })),
    ],
  },
]

/** The section a path belongs to. */
export function sectionOf(pathname: string): DocsSection {
  return (
    docsSections.find(
      (section) =>
        pathname === section.href || pathname.startsWith(`${section.href}/`)
    ) ?? docsSections[0]
  )
}
