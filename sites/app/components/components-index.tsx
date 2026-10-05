import type { Context } from "hono"
import { componentsByGroup } from "@/lib/catalog"
import { componentPages } from "@/lib/docs"
import { DocsPage } from "./docs-layout"

const description =
  "Every component the library covers: themes for Avalonia's own controls with the attached properties that add what they lack, and new controls in the uikit: namespace."

const groupId = (group: string) =>
  `${group.toLowerCase().replace(/\s+/g, "-")}-components`

/** The components index, as gpui-kit.com's: a list per group, each with its one-line description. */
export function renderComponentsIndex(c: Context) {
  const groups = componentsByGroup()
  return c.render(
    <DocsPage
      pathname="/components"
      title="Components"
      description={description}
      toc={groups.map(({ group }) => ({
        depth: 3,
        title: `${group} Components`,
        id: groupId(group),
      }))}
    >
      {groups.map(({ group, items }) => (
        <>
          <h3 id={groupId(group)}>{group} Components</h3>
          <ul>
            {items.map((entry) => (
              <li>
                <a href={`/components/${entry.slug}`}>{entry.title}</a>
                {" – "}
                {componentPages
                  .get(entry.slug)
                  ?.frontmatter.description?.replace(/\.$/, "")}
              </li>
            ))}
          </ul>
        </>
      ))}
    </DocsPage>,
    { title: "Components", description, section: "Components" }
  )
}
