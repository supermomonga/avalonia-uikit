import type { Child } from "hono/jsx"
import { ChevronDown } from "lucide"
import { type Status, statusLabels } from "@/lib/catalog"
import { type DocsSection, sectionOf } from "@/lib/docs"
import type { TocItem } from "@/lib/mdx/remark-toc"
import { Icon } from "./icon"

function SidebarLinks({
  section,
  pathname,
}: {
  section: DocsSection
  pathname: string
}) {
  return (
    <>
      <p class="docs-sidebar__title">{section.title}</p>
      <ul>
        {section.items.map((item) => (
          <li>
            <a
              href={item.href}
              class="docs-sidebar__link"
              aria-current={item.href === pathname ? "page" : undefined}
            >
              {item.title}
            </a>
          </li>
        ))}
      </ul>
    </>
  )
}

/** The section's pages; below 960px a "Browse documentation" disclosure instead. */
function DocsSidebar({ pathname }: { pathname: string }) {
  const section = sectionOf(pathname)
  return (
    <nav
      class="docs-sidebar"
      data-docs-sidebar=""
      aria-label={`${section.title} pages`}
    >
      <SidebarLinks section={section} pathname={pathname} />
    </nav>
  )
}

function MobileSectionNav({ pathname }: { pathname: string }) {
  const section = sectionOf(pathname)
  return (
    <details class="group mb-6 rounded-[var(--radius-card)] border min-[960px]:hidden">
      <summary class="flex h-10 cursor-pointer list-none items-center justify-between px-3.5 text-sm font-medium [&::-webkit-details-marker]:hidden">
        Browse documentation
        <Icon
          icon={ChevronDown}
          class="size-4 text-muted-foreground transition-transform group-open:rotate-180"
        />
      </summary>
      <nav
        class="max-h-[60vh] overflow-y-auto border-t p-2"
        aria-label={`${section.title} pages`}
      >
        <SidebarLinks section={section} pathname={pathname} />
      </nav>
    </details>
  )
}

/** "On this page": the h2 and h3 headings, the current one marked on the rail (app/client.ts). */
function DocsToc({ toc }: { toc: TocItem[] }) {
  return (
    <aside class="toc" aria-label="On this page">
      {toc.length > 1 && (
        <>
          <p class="toc__title">On this page</p>
          <ul class="toc__list" data-toc="">
            {toc.map((item) => (
              <li>
                <a
                  href={`#${item.id}`}
                  class="toc__link"
                  data-depth={item.depth}
                >
                  {item.title}
                </a>
              </li>
            ))}
          </ul>
        </>
      )}
    </aside>
  )
}

/**
 * A docs page, laid out as on gpui-kit.com: the section's sidebar, the
 * article (title, labels, standfirst, content) and the table of contents.
 */
export function DocsPage({
  pathname,
  title,
  description,
  labels,
  toc,
  children,
}: {
  pathname: string
  title: string
  description?: string
  labels?: Child
  toc: TocItem[]
  children?: Child
}) {
  return (
    <div class="docs-layout">
      <DocsSidebar pathname={pathname} />
      <main class="docs-main" id="content">
        <MobileSectionNav pathname={pathname} />
        <article class="doc-content">
          <h1>{title}</h1>
          {labels && <div class="doc-labels">{labels}</div>}
          {description && <p class="doc-standfirst">{description}</p>}
          {children}
        </article>
      </main>
      <DocsToc toc={toc} />
    </div>
  )
}

const statusDot: Record<Status, string> = {
  full: "bg-success",
  partial: "bg-warning",
  new: "bg-data-2",
}

/** How far the port of a component goes (lib/catalog.ts), linking to the compatibility table. */
export function StatusLabel({ status }: { status: Status }) {
  return (
    <a href="/docs/compatibility" class="doc-label" title="Coverage">
      <span class={`size-1.5 rounded-full ${statusDot[status]}`} />
      {statusLabels[status]}
    </a>
  )
}

/** A plain label, such as the optional package a component needs. */
export function Label({ children }: { children?: Child }) {
  return <span class="doc-label">{children}</span>
}
