import { siteConfig } from "@/lib/site"

export function SiteFooter() {
  return (
    <footer class="group-has-[.docs-nav]/body:pb-20 group-has-[.section-soft]/body:bg-surface/40 group-has-[[data-slot=docs]]/body:hidden group-has-[.docs-nav]/body:sm:pb-0 dark:bg-transparent dark:group-has-[.section-soft]/body:bg-surface/40 3xl:fixed:bg-transparent">
      <div class="container-wrapper px-4 xl:px-6">
        <div class="flex h-(--footer-height) items-center justify-between">
          <div class="w-full px-1 text-center text-xs leading-loose text-muted-foreground sm:text-sm">
            Built by{" "}
            <a
              href="https://github.com/supermomonga"
              target="_blank"
              rel="noreferrer"
              class="font-medium underline underline-offset-4"
            >
              supermomonga
            </a>
            . Based on{" "}
            <a
              href={siteConfig.links.shadcn}
              target="_blank"
              rel="noreferrer"
              class="font-medium underline underline-offset-4"
            >
              shadcn/ui
            </a>{" "}
            and{" "}
            <a
              href={siteConfig.links.gpuiKit}
              target="_blank"
              rel="noreferrer"
              class="font-medium underline underline-offset-4"
            >
              GPUI Kit
            </a>
            . Not affiliated with shadcn, Longbridge or AvaloniaUI.
            The source code is available on{" "}
            <a
              href={siteConfig.links.github}
              target="_blank"
              rel="noreferrer"
              class="font-medium underline underline-offset-4"
            >
              GitHub
            </a>
            .
          </div>
        </div>
      </div>
    </footer>
  )
}
