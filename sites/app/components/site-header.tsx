import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { siteConfig } from "@/lib/site"
import { GitHubIcon } from "./icon"
import { MainNav } from "./main-nav"
import { MobileNav } from "./mobile-nav"
import { ModeSwitcher } from "./mode-switcher"
import { Search } from "./search"

export function SiteHeader({ pathname }: { pathname: string }) {
  return (
    <header class="sticky top-0 z-50 w-full bg-background">
      <div class="container-wrapper px-6 3xl:fixed:px-0">
        <div class="flex h-(--header-height) items-center **:data-[slot=separator]:h-4! **:data-[slot=separator]:self-center 3xl:fixed:container">
          <MobileNav class="flex lg:hidden" />
          <a
            href="/"
            class="mr-4 hidden items-center gap-2 lg:flex"
            aria-label={siteConfig.name}
          >
            <Logo class="size-5" />
            <span class="text-sm font-semibold tracking-tight">
              {siteConfig.name}
            </span>
          </a>
          <MainNav pathname={pathname} class="hidden lg:flex" />
          <div class="ml-auto flex items-center gap-2 md:flex-1 md:justify-end">
            <div class="hidden w-full flex-1 md:flex md:w-auto md:flex-none">
              <Search />
            </div>
            <Separator orientation="vertical" class="ml-2 hidden lg:block" />
            <Button
              size="sm"
              variant="ghost"
              class="h-8 shadow-none"
              render={
                <a
                  href={siteConfig.links.github}
                  target="_blank"
                  rel="noreferrer"
                />
              }
            >
              <GitHubIcon class="size-4" />
              <span class="sr-only">GitHub</span>
            </Button>
            <Separator orientation="vertical" />
            <ModeSwitcher />
          </div>
        </div>
      </div>
    </header>
  )
}

/**
 * The site's mark (public/favicon.svg): a themed control, the blue square,
 * inside Avalonia's frame. The frame follows the text color.
 */
export function Logo({ class: className }: { class?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 32 32"
      class={className}
      aria-hidden="true"
    >
      <rect
        x="3"
        y="3"
        width="26"
        height="26"
        rx="7"
        fill="none"
        stroke="currentColor"
        stroke-width="2.5"
      />
      <rect x="12.5" y="12.5" width="13" height="13" rx="4" fill="#2563EB" />
    </svg>
  )
}
