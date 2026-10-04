import type { NotFoundHandler } from "hono"
import { NotFound } from "@/components/not-found"

const handler: NotFoundHandler = (c) => {
  c.status(404)
  return c.render(<NotFound />, { title: "Not Found" })
}

export default handler
