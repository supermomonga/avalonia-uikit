import { ssgParams } from "hono/ssg"
import { createRoute } from "honox/factory"
import { DocsPage, PackageBadge, StatusBadge } from "@/components/docs-page"
import { DocsLayout } from "@/components/docs-sidebar"
import { mdxComponents } from "@/components/mdx-components"
import { components, findComponent } from "@/lib/catalog"
import { componentPages } from "@/lib/docs"

export default createRoute(
  ssgParams(() => components.map((entry) => ({ name: entry.slug }))),
  (c) => {
    const entry = findComponent(c.req.param("name") ?? "")
    const page = entry && componentPages.get(entry.slug)
    if (!entry || !page) return c.notFound()
    const href = `/docs/components/${entry.slug}`
    const Content = page.default
    const { title, description } = page.frontmatter
    return c.render(
      <DocsLayout pathname={href}>
        <DocsPage
          href={href}
          title={title}
          description={description}
          toc={page.toc}
          badges={
            <>
              <StatusBadge status={entry.status} />
              {entry.package && <PackageBadge>{entry.package}</PackageBadge>}
            </>
          }
        >
          <Content components={mdxComponents} />
        </DocsPage>
      </DocsLayout>,
      { title, description }
    )
  }
)
