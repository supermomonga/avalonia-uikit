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

/** A headed list of the sidebar's links. */
export interface NavGroup {
  title: string
  items: NavItem[]
}

/** A section of the site with its own sidebar, as on gpui-kit.com. */
export interface DocsSection {
  /** The section's name, which also heads its first group. */
  title: string
  /** The section's top-level path. */
  href: string
  groups: NavGroup[]
}

function page(href: string): NavItem {
  const doc = docPages.get(href)
  if (!doc) throw new Error(`No page for ${href} in sites/content/docs`)
  return { title: doc.frontmatter.title, href }
}

type Kind = "avalonia" | "new" | "third-party"

/** A component's sidebar group: Avalonia's controls, the new `uikit:` controls, or a third-party library's. */
function kindOf(entry: (typeof components)[number]): Kind {
  if (entry.library) return "third-party"
  return entry.status === "new" ? "new" : "avalonia"
}

/** The components of one sidebar group, in alphabetical order. */
function componentLinks(kind: Kind): NavItem[] {
  return components
    .filter((entry) => kindOf(entry) === kind)
    .sort((a, b) => a.title.localeCompare(b.title, "en"))
    .map((entry) => ({
      title: entry.title,
      href: `/components/${entry.slug}`,
    }))
}

export const docsSections: DocsSection[] = [
  {
    title: "Avalonia UIKit",
    href: "/docs",
    groups: [
      {
        title: "Avalonia UIKit",
        items: [
          page("/docs"),
          page("/docs/installation"),
          page("/docs/theming"),
          page("/docs/icons"),
        ],
      },
    ],
  },
  {
    title: "Components",
    href: "/components",
    groups: [
      {
        title: "Components",
        items: [{ title: "Overview", href: "/components" }],
      },
      // Themes for Avalonia's own controls, the new `uikit:` controls, then third-party libraries' controls.
      { title: "Avalonia Controls", items: componentLinks("avalonia") },
      { title: "UIKit Controls", items: componentLinks("new") },
      { title: "Third-party Controls", items: componentLinks("third-party") },
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
