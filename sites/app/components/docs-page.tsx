import type { Child } from "hono/jsx"
import { ArrowLeft, ArrowRight } from "lucide"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { type Status, statusLabels } from "@/lib/catalog"
import { neighbours } from "@/lib/docs"
import type { TocItem } from "@/lib/mdx/remark-toc"
import { DocsTableOfContents } from "./docs-toc"
import { Icon } from "./icon"

/** A docs page: title, description, content, previous/next links and the table of contents. */
export function DocsPage({
  href,
  title,
  description,
  badges,
  toc,
  children,
}: {
  href: string
  title: string
  description?: string
  badges?: Child
  toc: TocItem[]
  children?: Child
}) {
  const { previous, next } = neighbours(href)
  return (
    <div
      data-slot="docs"
      class="flex scroll-mt-24 items-stretch pb-8 text-[1.05rem] sm:text-[15px] xl:w-full"
    >
      <div class="flex min-w-0 flex-1 flex-col">
        <div class="h-(--top-spacing) shrink-0" />
        <div class="mx-auto flex w-full max-w-180 min-w-0 flex-1 flex-col gap-6 px-4 py-6 text-foreground md:px-0 lg:py-8">
          <div class="flex flex-col gap-2">
            <div class="flex items-center justify-between md:items-start">
              <h1 class="scroll-m-24 text-3xl font-semibold tracking-tight sm:text-3xl">
                {title}
              </h1>
              <div class="docs-nav flex items-center gap-2">
                <div class="ml-auto flex gap-2">
                  {previous && (
                    <Button
                      variant="secondary"
                      size="icon"
                      class="extend-touch-target size-8 shadow-none md:size-7"
                      render={<a href={previous.href} />}
                    >
                      <Icon icon={ArrowLeft} />
                      <span class="sr-only">Previous</span>
                    </Button>
                  )}
                  {next && (
                    <Button
                      variant="secondary"
                      size="icon"
                      class="extend-touch-target size-8 shadow-none md:size-7"
                      render={<a href={next.href} />}
                    >
                      <span class="sr-only">Next</span>
                      <Icon icon={ArrowRight} />
                    </Button>
                  )}
                </div>
              </div>
            </div>
            {description && (
              <p class="text-[1.05rem] text-muted-foreground sm:text-base sm:text-balance md:max-w-[80%]">
                {description}
              </p>
            )}
            {badges && <div class="flex flex-wrap gap-2 pt-1">{badges}</div>}
          </div>
          <div class="typeset w-full flex-1 pb-16 sm:pb-0">{children}</div>
          <div class="hidden h-16 w-full items-center gap-2 px-4 sm:flex sm:px-0">
            {previous && (
              <Button
                variant="secondary"
                size="sm"
                class="shadow-none"
                render={<a href={previous.href} />}
              >
                <Icon icon={ArrowLeft} /> {previous.title}
              </Button>
            )}
            {next && (
              <Button
                variant="secondary"
                size="sm"
                class="ml-auto shadow-none"
                render={<a href={next.href} />}
              >
                {next.title} <Icon icon={ArrowRight} />
              </Button>
            )}
          </div>
        </div>
      </div>
      <div class="sticky top-[calc(var(--header-height)+1px)] z-30 ml-auto hidden h-[90svh] w-(--sidebar-width) flex-col gap-4 overflow-hidden overscroll-none pb-8 xl:flex">
        <div class="h-(--top-spacing) shrink-0" />
        {toc.length > 0 && (
          <div class="flex scroll-fade scrollbar-none flex-col gap-8 overflow-y-auto px-8">
            <DocsTableOfContents toc={toc} />
          </div>
        )}
      </div>
    </div>
  )
}

const statusClass: Record<Status, string> = {
  full: "border-transparent bg-emerald-500/15 text-emerald-700 dark:text-emerald-300",
  partial: "border-transparent bg-amber-500/15 text-amber-700 dark:text-amber-300",
  new: "border-transparent bg-blue-500/15 text-blue-700 dark:text-blue-300",
}

/** How far the port of a component goes (lib/catalog.ts). */
export function StatusBadge({ status }: { status: Status }) {
  return (
    <Badge variant="outline" class={`rounded-md ${statusClass[status]}`}>
      {statusLabels[status]}
    </Badge>
  )
}

export function PackageBadge({ children }: { children?: Child }) {
  return (
    <Badge variant="secondary" class="rounded-md font-mono font-normal">
      {children}
    </Badge>
  )
}
