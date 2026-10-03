import type { FC } from "hono/jsx"
import { components } from "./catalog"
import type { TocItem } from "./mdx/remark-toc"

export interface DocModule {
  default: FC<{ components?: Record<string, unknown> }>
  frontmatter: { title: string; description?: string }
  toc: TocItem[]
}

const modules = import.meta.glob<DocModule>(
  ["/content/docs/**/*.mdx", "!/content/docs/components/*.mdx"],
  { eager: true }
)

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
  "/content/docs/components/*.mdx",
  { eager: true }
)

/** Component pages by slug (content/docs/components/). */
export const componentPages = new Map<string, DocModule>(
  Object.entries(writtenComponentPages).map(([file, mod]) => [
    file.replace(/^.*\//, "").replace(/\.mdx$/, ""),
    mod,
  ])
)

export interface NavItem {
  title: string
  href: string
  /** Shown as a dot in the sidebar. */
  badge?: string
}

export interface NavGroup {
  title: string
  items: NavItem[]
}

function page(href: string): NavItem {
  const doc = docPages.get(href)
  if (!doc) throw new Error(`No page for ${href} in sites/content/docs`)
  return { title: doc.frontmatter.title, href }
}

export const docsNav: NavGroup[] = [
  {
    title: "Get Started",
    items: [
      page("/docs"),
      page("/docs/installation"),
      page("/docs/theming"),
      page("/docs/icons"),
      page("/docs/compatibility"),
    ],
  },
  {
    title: "Components",
    items: [...components]
      .sort((a, b) => a.title.localeCompare(b.title, "en"))
      .map((entry) => ({
        title: entry.title,
        href: `/docs/components/${entry.slug}`,
        badge: entry.status === "new" ? "New control" : undefined,
      })),
  },
]

/** Every page in reading order, for the previous and next links. */
const readingOrder: NavItem[] = [
  ...docsNav[0].items,
  { title: "Components", href: "/docs/components" },
  ...docsNav[1].items,
]

export function neighbours(href: string): {
  previous?: NavItem
  next?: NavItem
} {
  const index = readingOrder.findIndex((item) => item.href === href)
  if (index === -1) return {}
  return { previous: readingOrder[index - 1], next: readingOrder[index + 1] }
}

export function isActive(href: string, pathname: string): boolean {
  return href === "/docs" ? pathname === href : pathname.startsWith(href)
}
