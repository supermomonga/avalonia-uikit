import { ssgParams } from "hono/ssg"
import { createRoute } from "honox/factory"
import { DocsPage, Label } from "@/components/docs-layout"
import { mdxComponents } from "@/components/mdx-components"
import { components, findComponent } from "@/lib/catalog"
import { componentPages } from "@/lib/docs"

export default createRoute(
  ssgParams(() => components.map((entry) => ({ name: entry.slug }))),
  (c) => {
    const entry = findComponent(c.req.param("name") ?? "")
    const page = entry && componentPages.get(entry.slug)
    if (!entry || !page) return c.notFound()
    const Content = page.default
    const { title, description } = page.frontmatter
    return c.render(
      <DocsPage
        pathname={`/components/${entry.slug}`}
        title={title}
        description={description}
        toc={page.toc}
        labels={entry.package && <Label>{entry.package}</Label>}
      >
        <Content components={mdxComponents} />
      </DocsPage>,
      { title, description, section: "Components" }
    )
  }
)
