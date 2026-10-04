import { createRoute } from "honox/factory"
import { NotFound } from "@/components/not-found"

/** dist/404.html, which Cloudflare serves for missing paths (wrangler.jsonc). */
export default createRoute((c) => c.render(<NotFound />, { title: "Not Found" }))
