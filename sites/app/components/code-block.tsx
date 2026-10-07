import { cn } from "cn"
import { raw } from "hono/html"
import { Check, Copy, File } from "lucide"
import { Button } from "@/components/ui/button"
import { Icon } from "./icon"

/** Copies `value` (see app/client.ts). */
export function CopyButton({
  value,
  label = "Copy",
  class: className,
  ...data
}: {
  value: string
  label?: string
  class?: string
} & Record<`data-${string}`, string>) {
  return (
    <Button
      data-copy={value}
      size="icon-xs"
      variant="ghost"
      class={cn(
        "group/copy text-muted-foreground hover:bg-secondary hover:text-foreground",
        className
      )}
      aria-label={label}
      {...data}
    >
      <Icon icon={Copy} class="group-data-copied/copy:hidden" />
      <Icon icon={Check} class="hidden group-data-copied/copy:block" />
    </Button>
  )
}

/** A code block highlighted at build time (see lib/mdx/rehype-code.ts). */
export function CodeBlock({
  html,
  raw: source,
  title,
  language,
  class: className,
}: {
  html: string
  raw: string
  title?: string
  language?: string
  class?: string
}) {
  return (
    <figure class={cn("code-block", className)} data-language={language}>
      {title && (
        <figcaption class="code-block__title">
          <Icon icon={File} class="size-3.5" />
          {title}
        </figcaption>
      )}
      <CopyButton
        value={source}
        class="code-block__copy bg-code-bg data-copied:opacity-100"
      />
      {raw(html)}
    </figure>
  )
}
