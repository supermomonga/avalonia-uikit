import { createRoute } from "honox/factory"
import { components } from "@/lib/catalog"
import { componentPages, docsSections } from "@/lib/docs"

/** The search dialog's index (components/search.tsx, app/client.ts). */
export default createRoute((c) =>
  c.json({
    pages: [
      ...docsSections[0].groups[0].items.map((item) => ({
        title: item.title,
        href: item.href,
      })),
      { title: "Components", href: "/components" },
    ],
    components: components.map((entry) => ({
      title: entry.title,
      href: `/components/${entry.slug}`,
      description: [
        componentPages.get(entry.slug)?.frontmatter.description,
        entry.aliases,
        ...entry.avalonia,
      ]
        .filter(Boolean)
        .join(" "),
    })),
  })
)
