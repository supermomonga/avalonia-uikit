import type { Child } from "hono/jsx"
import { Kbd } from "@/components/ui/kbd"
import { Callout } from "./callout"
import { CodeBlock } from "./code-block"
import { Demo } from "./demo"
import { ThemeList } from "./theme-list"

type HeadingProps = { id?: string; children?: Child }

/** A heading with a `#` link in the margin, shown on hover. */
function heading(Tag: "h2" | "h3" | "h4") {
  return ({ id, children }: HeadingProps) => (
    <Tag id={id}>
      {id && (
        <a href={`#${id}`} class="heading-anchor" aria-hidden="true" tabindex={-1}>
          #
        </a>
      )}
      {children}
    </Tag>
  )
}

function Table(props: Record<string, unknown>) {
  return (
    <div class="doc-table">
      <table {...props} />
    </div>
  )
}

/** Components MDX pages can use, and the elements they replace. */
export const mdxComponents = {
  h2: heading("h2"),
  h3: heading("h3"),
  h4: heading("h4"),
  table: Table,
  Callout,
  CodeBlock,
  Demo,
  Kbd,
  ThemeList,
}
