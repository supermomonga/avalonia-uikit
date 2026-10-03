import { cn } from "cn"
import { Kbd } from "@/components/ui/kbd"
import { Callout, Step, Steps } from "./callout"
import { CodeBlock } from "./code-block"
import { CompatibilityTable, UncoveredList } from "./compatibility-table"
import { Demo } from "./demo"

function LinkedCard({
  href,
  class: className,
  children,
}: {
  href: string
  class?: string
  children?: unknown
}) {
  return (
    <a
      href={href}
      data-not-typeset=""
      class={cn(
        "flex w-full flex-col items-center rounded-2xl bg-surface p-6 text-surface-foreground transition-colors hover:bg-surface/80 sm:p-10",
        className
      )}
    >
      {children as never}
    </a>
  )
}

function Table(props: Record<string, unknown>) {
  return (
    <div class="typeset-scroll scroll-fade-x scrollbar-none *:[table]:w-full">
      <table {...props} />
    </div>
  )
}

/** Components MDX pages can use, and the elements they replace. */
export const mdxComponents = {
  table: Table,
  Callout,
  CodeBlock,
  CompatibilityTable,
  Demo,
  Kbd,
  LinkedCard,
  Step,
  Steps,
  UncoveredList,
}
