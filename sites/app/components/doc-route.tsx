import type { Context } from "hono"
import { docPages } from "@/lib/docs"
import { DocsPage } from "./docs-page"
import { DocsLayout } from "./docs-sidebar"
import { mdxComponents } from "./mdx-components"

/** Renders the hand-written page at `href` (sites/content/docs), or nothing. */
export function renderDoc(c: Context, href: string) {
  const page = docPages.get(href)
  if (!page) return undefined
  const Content = page.default
  return c.render(
    <DocsLayout pathname={href}>
      <DocsPage
        href={href}
        title={page.frontmatter.title}
        description={page.frontmatter.description}
        toc={page.toc}
      >
        <Content components={mdxComponents} />
      </DocsPage>
    </DocsLayout>,
    { title: page.frontmatter.title, description: page.frontmatter.description }
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
