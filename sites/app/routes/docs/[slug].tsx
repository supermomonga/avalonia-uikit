import { ssgParams } from "hono/ssg"
import { createRoute } from "honox/factory"
import { childSlugs, renderDoc } from "@/components/doc-route"

export default createRoute(
  ssgParams(() => childSlugs("/docs")),
  (c) => renderDoc(c, `/docs/${c.req.param("slug")}`) ?? c.notFound()
)
