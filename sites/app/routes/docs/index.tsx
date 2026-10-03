import { createRoute } from "honox/factory"
import { renderDoc } from "@/components/doc-route"

export default createRoute((c) => renderDoc(c, "/docs") ?? c.notFound())
