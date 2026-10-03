import "hono"

declare module "hono" {
  interface ContextRenderer {
    // biome-ignore lint/style/useShorthandFunctionType: HonoX's declaration merging
    (
      content: string | Promise<string>,
      props?: { title?: string; description?: string }
    ): Response | Promise<Response>
  }
}
