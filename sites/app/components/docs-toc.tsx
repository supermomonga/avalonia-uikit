import { cn } from "cn"
import type { TocItem } from "@/lib/mdx/remark-toc"

export function DocsTableOfContents({
  toc,
  class: className,
}: {
  toc: TocItem[]
  class?: string
}) {
  if (toc.length === 0) return null
  return (
    <div
      class={cn("flex flex-col gap-2 p-4 pt-0 text-sm", className)}
      data-toc=""
    >
      <p class="sticky top-0 h-6 bg-background text-xs font-medium text-muted-foreground">
        On This Page
      </p>
      {toc.map((item) => (
        <a
          href={`#${item.id}`}
          class="text-[0.8rem] text-muted-foreground no-underline transition-colors hover:text-foreground data-[active=true]:font-medium data-[active=true]:text-foreground data-[depth=3]:pl-4"
          data-depth={item.depth}
        >
          {item.title}
        </a>
      ))}
    </div>
  )
}
