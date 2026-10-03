import { cn } from "cn"
import { raw } from "hono/html"
import { Check, Copy } from "lucide"
import { Button } from "@/components/ui/button"
import { Icon } from "./icon"

/** Copies `value` (see app/client.ts). */
export function CopyButton({
  value,
  class: className,
}: {
  value: string
  class?: string
}) {
  return (
    <Button
      data-slot="copy-button"
      data-copy={value}
      size="icon"
      variant="ghost"
      class={cn(
        "group/copy absolute top-3 right-2 z-10 size-7 bg-code hover:opacity-100 focus-visible:opacity-100",
        className
      )}
    >
      <span class="sr-only">Copy</span>
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
    <figure
      data-code-figure=""
      data-not-typeset=""
      data-language={language}
      class={className}
    >
      {title && (
        <figcaption
          data-code-title=""
          class="flex items-center gap-2 text-code-foreground"
        >
          {title}
        </figcaption>
      )}
      <CopyButton value={source} class={title ? "top-1.5" : undefined} />
      {raw(html)}
    </figure>
  )
}
