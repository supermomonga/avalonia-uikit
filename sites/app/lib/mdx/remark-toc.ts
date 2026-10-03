import { valueToEstree } from "estree-util-value-to-estree"
import GithubSlugger from "github-slugger"
import type { Heading, Root } from "mdast"
import { toString as textOf } from "mdast-util-to-string"
import { visit } from "unist-util-visit"

export interface TocItem {
  depth: number
  title: string
  id: string
}

/**
 * Gives headings GitHub-style ids and exports the h2/h3 headings as `toc`
 * for the page's table of contents.
 */
export function remarkToc() {
  return (tree: Root) => {
    const slugger = new GithubSlugger()
    const toc: TocItem[] = []
    visit(tree, "heading", (node: Heading) => {
      const title = textOf(node)
      const id = slugger.slug(title)
      node.data = { ...node.data, hProperties: { id } }
      if (node.depth === 2 || node.depth === 3) {
        toc.push({ depth: node.depth, title, id })
      }
    })
    tree.children.push({
      type: "mdxjsEsm",
      value: "",
      data: {
        estree: {
          type: "Program",
          sourceType: "module",
          body: [
            {
              type: "ExportNamedDeclaration",
              specifiers: [],
              attributes: [],
              declaration: {
                type: "VariableDeclaration",
                kind: "const",
                declarations: [
                  {
                    type: "VariableDeclarator",
                    id: { type: "Identifier", name: "toc" },
                    init: valueToEstree(toc),
                  },
                ],
              },
            },
          ],
        },
      },
    } as never)
  }
}
