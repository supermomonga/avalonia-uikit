import { ssgParams } from "hono/ssg"
import { createRoute } from "honox/factory"
import { renderComponentsIndex } from "@/components/components-index"
import { childSlugs, renderDoc } from "@/components/doc-route"

// `/docs/components` is here too: a static route under docs/components/ would
// come after this one, which would answer it with a 404.
export default createRoute(
  ssgParams(() => [...childSlugs("/docs"), { slug: "components" }]),
  (c) => {
    const slug = c.req.param("slug")
    if (slug === "components") return renderComponentsIndex(c)
    return renderDoc(c, `/docs/${slug}`) ?? c.notFound()
  }
)
