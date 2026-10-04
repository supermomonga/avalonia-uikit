import type { Context } from "hono"
import { docPages } from "@/lib/docs"
import { DocsPage } from "./docs-layout"
import { mdxComponents } from "./mdx-components"

/** Renders the hand-written page at `href` (sites/content/docs), or nothing. */
export function renderDoc(c: Context, href: string) {
  const page = docPages.get(href)
  if (!page) return undefined
  const Content = page.default
  const { title, description } = page.frontmatter
  return c.render(
    <DocsPage
      pathname={href}
      title={title}
      description={description}
      toc={page.toc}
    >
      <Content components={mdxComponents} />
    </DocsPage>,
    { title, description, section: "Docs" }
  )
}

/** Slugs of the pages directly under `prefix`, for `ssgParams`. */
export function childSlugs(prefix: string): { slug: string }[] {
  return [...docPages.keys()]
    .filter((href) => href.startsWith(`${prefix}/`))
    .map((href) => href.slice(prefix.length + 1))
    .filter((slug) => !slug.includes("/"))
    .map((slug) => ({ slug }))
}
