import type { Context } from "hono"
import { Card, CardContent, CardFooter } from "@/components/ui/card"
import { type ComponentEntry, componentsByGroup, statusLabels } from "@/lib/catalog"
import { previewSize, previewUrls } from "@/lib/previews"
import { DocsPage } from "./docs-page"
import { DocsLayout } from "./docs-sidebar"

const description =
  "Every component the library covers: themes for Avalonia's own controls, and new controls for the small components Avalonia lacks."

function ComponentCard({ entry }: { entry: ComponentEntry }) {
  const id = `${entry.slug}/demo`
  const size = previewSize(id)
  const urls = previewUrls(id)
  return (
    <a href={`/docs/components/${entry.slug}`} class="block">
    <Card class="gap-0 overflow-hidden py-0 transition-colors hover:bg-muted/40">
      <CardContent class="flex aspect-[4/2.5] items-center justify-center overflow-hidden bg-surface p-4">
        {size ? (
          <>
            <img
              src={urls.light}
              alt=""
              width={size.width}
              height={size.height}
              loading="lazy"
              decoding="async"
              class="max-h-full w-auto max-w-full object-contain dark:hidden"
            />
            <img
              src={urls.dark}
              alt=""
              width={size.width}
              height={size.height}
              loading="lazy"
              decoding="async"
              class="hidden max-h-full w-auto max-w-full object-contain dark:block"
            />
          </>
        ) : (
          <span class="text-lg font-medium text-muted-foreground">
            {entry.title}
          </span>
        )}
      </CardContent>
      <CardFooter class="flex items-center justify-between border-t px-4 py-3">
        <span class="text-sm font-medium">{entry.title}</span>
        <span class="text-xs text-muted-foreground">
          {statusLabels[entry.status]}
        </span>
      </CardFooter>
    </Card>
    </a>
  )
}

export function ComponentsIndex() {
  return (
    <>
      {componentsByGroup().map(({ group, items }) => (
        <section data-not-typeset="">
          <h2 id={group.toLowerCase().replace(/\s+/g, "-")} class="mt-10 mb-4 text-xl font-semibold tracking-tight first:mt-0">
            {group}
          </h2>
          <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {items.map((entry) => (
              <ComponentCard entry={entry} />
            ))}
          </div>
        </section>
      ))}
    </>
  )
}

export function renderComponentsIndex(c: Context) {
  return c.render(
    <DocsLayout pathname="/docs/components">
      <DocsPage
        href="/docs/components"
        title="Components"
        description={description}
        toc={componentsByGroup().map(({ group }) => ({
          depth: 2,
          title: group,
          id: group.toLowerCase().replace(/\s+/g, "-"),
        }))}
      >
        <ComponentsIndex />
      </DocsPage>
    </DocsLayout>,
    { title: "Components", description }
  )
}
