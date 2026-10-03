import type { Element, ElementContent, Root, RootContent } from "hast"
import { visit } from "unist-util-visit"
import { highlight } from "../highlight.ts"

function text(node: ElementContent): string {
  if (node.type === "text") return node.value
  if (node.type === "element") return node.children.map(text).join("")
  return ""
}

function attribute(name: string, value: string) {
  return { type: "mdxJsxAttribute", name, value }
}

/** Replaces fenced code blocks with `<CodeBlock>`, highlighted with Shiki at build time. */
export function rehypeCode() {
  return async (tree: Root) => {
    const blocks: { parent: Root | Element; index: number; pre: Element }[] = []
    visit(tree, "element", (node, index, parent) => {
      if (node.tagName !== "pre" || index === undefined || !parent) return
      blocks.push({ parent: parent as Root | Element, index, pre: node })
    })
    for (const { parent, index, pre } of blocks) {
      const code = pre.children.find(
        (child): child is Element =>
          child.type === "element" && child.tagName === "code"
      )
      if (!code) continue
      const classes = (code.properties.className as string[] | undefined) ?? []
      const language =
        classes
          .find((name) => name.startsWith("language-"))
          ?.slice("language-".length) ?? "text"
      const meta = (code.data as { meta?: string } | undefined)?.meta ?? ""
      const title = meta.match(/title="([^"]+)"/)?.[1]
      const raw = code.children.map(text).join("").replace(/\n$/, "")
      const replacement: RootContent = {
        type: "mdxJsxFlowElement",
        name: "CodeBlock",
        attributes: [
          attribute("html", await highlight(raw, language)),
          attribute("raw", raw),
          attribute("language", language),
          ...(title ? [attribute("title", title)] : []),
        ],
        children: [],
      } as never
      parent.children[index] = replacement as never
    }
  }
}
