import { siteConfig } from "@/lib/site"

const links = [
  { href: "/docs/installation", label: "Installation" },
  { href: "/docs/theming", label: "Theming" },
  { href: "/docs/icons", label: "Icons" },
  { href: "/components", label: "Components" },
  { href: siteConfig.links.github, label: "GitHub" },
  { href: siteConfig.links.issues, label: "Report Bug" },
]

function Link({ href, children }: { href: string; children?: unknown }) {
  const external = /^https?:/.test(href)
  return (
    <a
      href={href}
      class="text-muted-foreground transition-colors hover:text-foreground"
      {...(external ? { target: "_blank", rel: "noreferrer" } : {})}
    >
      {children as never}
    </a>
  )
}

/** The footer of every page, as on gpui-kit.com: credits on the left, links on the right. */
export function SiteFooter() {
  return (
    <footer class="layout-width mt-auto pt-24">
      <div class="flex flex-wrap justify-between gap-x-12 gap-y-6 border-t py-10 text-[0.8125rem] leading-relaxed">
        <div class="max-w-[30rem]">
          <p class="mb-3 text-[0.9375rem] font-[620] tracking-[-0.015em]">
            {siteConfig.name}
          </p>
          <p class="text-muted-foreground">
            Themes and controls for <Link href={siteConfig.links.avalonia}>Avalonia</Link>,
            by <Link href={siteConfig.links.author}>supermomonga</Link>.
          </p>
          <p class="mt-2 text-muted-foreground">
            Modeled on the Nova style of{" "}
            <Link href={siteConfig.links.shadcn}>shadcn/ui</Link> and on{" "}
            <Link href={siteConfig.links.gpuiKit}>GPUI Kit</Link>. Not affiliated
            with shadcn, Longbridge or AvaloniaUI.
          </p>
        </div>
        <nav
          aria-label="Footer"
          class="flex max-w-[46rem] flex-wrap content-start justify-end gap-x-6 gap-y-2 max-[640px]:justify-start"
        >
          {links.map((link) => (
            <Link href={link.href}>{link.label}</Link>
          ))}
        </nav>
      </div>
      <p class="border-t py-6 text-[0.8125rem] text-muted-foreground">
        Released under the <Link href={siteConfig.links.license}>MIT License</Link>.
        Icons by <Link href={siteConfig.links.lucide}>Lucide</Link>. The demos
        are set in <Link href={siteConfig.links.inter}>Inter</Link>.
      </p>
    </footer>
  )
}
