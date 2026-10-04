import "hono"

/** What a page passes to the renderer (routes/_renderer.tsx). */
export interface RenderProps {
  /** The page's own title; the renderer adds the section and the site's name. */
  title?: string
  description?: string
  /** The site section the page belongs to, for the title and the breadcrumbs. */
  section?: "Docs" | "Components"
  /** Only the page itself: no header or footer, and not indexed (the social image's page). */
  bare?: boolean
}

declare module "hono" {
  interface ContextRenderer {
    // biome-ignore lint/style/useShorthandFunctionType: HonoX's declaration merging
    (
      content: string | Promise<string>,
      props?: RenderProps
    ): Response | Promise<Response>
  }
}
