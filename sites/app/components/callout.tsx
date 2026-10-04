import type { Child } from "hono/jsx"

/** A note in the article, as gpui-kit.com's `:::tip` callouts. */
export function Callout({
  title,
  variant = "default",
  children,
}: {
  title?: string
  variant?: "default" | "info" | "tip" | "warning"
  children?: Child
}) {
  return (
    <aside class="callout" data-variant={variant}>
      {title && <p class="callout__title">{title}</p>}
      <div>{children}</div>
    </aside>
  )
}
