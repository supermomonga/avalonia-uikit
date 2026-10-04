import { cn } from "cn"
import { ArrowUpRight, ChevronDown } from "lucide"
import { Button } from "@/components/ui/button"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { navSection, siteConfig } from "@/lib/site"
import { GitHubIcon, Icon } from "./icon"
import { SearchButton, SearchDialog } from "./search"
import { ThemeButton, ThemePalette } from "./theme-palette"

const external = (href: string) => /^https?:/.test(href)

/**
 * The top bar, as on gpui-kit.com: the mark and the name, the sections and a
 * Resources menu on the left; search, GitHub and the theme on the right. Below
 * 900px the links move into a menu.
 */
export function SiteHeader({ pathname }: { pathname: string }) {
  const section = navSection(pathname)
  return (
    <header class="site-nav">
      <div class="layout-width flex h-full items-center gap-[1.9rem]">
        <a
          href="/"
          class="flex shrink-0 items-center gap-2"
          aria-label={`${siteConfig.name} home`}
        >
          <Logo class="h-[1.4rem] w-[1.4rem]" />
          <span class="text-[0.9rem] font-[620] tracking-[-0.022em]">
            {siteConfig.name}
          </span>
        </a>
        <nav
          class="hidden items-center gap-[1.6rem] min-[901px]:flex"
          aria-label="Main"
        >
          {siteConfig.navItems.map((item) => (
            <a
              href={item.href}
              class="site-nav__link"
              aria-current={item.href === section ? "page" : undefined}
            >
              {item.label}
            </a>
          ))}
          <ResourcesMenu />
        </nav>
        <div class="ml-auto flex items-center gap-1.5">
          <SearchButton />
          <Button
            variant="ghost"
            class="h-8 gap-1.5 px-2 font-medium max-[900px]:w-8 max-[900px]:px-0"
            render={
              <a
                href={siteConfig.links.github}
                target="_blank"
                rel="noreferrer"
                aria-label="GitHub"
              />
            }
          >
            <GitHubIcon class="size-4" />
            <span class="max-[900px]:hidden">GitHub</span>
          </Button>
          <ThemeButton />
          <MobileMenu section={section} />
        </div>
      </div>
      <SearchDialog />
      <ThemePalette />
    </header>
  )
}

function ResourcesMenu() {
  return (
    <Popover>
      <PopoverTrigger class="site-nav__link cursor-pointer">
        Resources
        <Icon icon={ChevronDown} class="size-3.5 opacity-70" />
      </PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={10}
        class="w-52 gap-0 rounded-[var(--radius-card)] p-1.5 shadow-panel ring-border"
      >
        {siteConfig.resources.map((item) => (
          <a
            href={item.href}
            class="flex h-8 items-center justify-between rounded-[var(--radius-control)] px-2.5 text-[0.8125rem] hover:bg-secondary"
            {...(external(item.href)
              ? { target: "_blank", rel: "noreferrer" }
              : {})}
          >
            {item.label}
            {external(item.href) && (
              <Icon
                icon={ArrowUpRight}
                class="size-3.5 text-muted-foreground"
              />
            )}
          </a>
        ))}
      </PopoverContent>
    </Popover>
  )
}

function MobileMenu({ section }: { section?: string }) {
  return (
    <Popover>
      <PopoverTrigger
        render={
          <Button
            variant="ghost"
            size="icon"
            class="min-[901px]:hidden"
            aria-label="Menu"
          />
        }
      >
        <span class="relative block h-3 w-4">
          <span class="absolute inset-x-0 top-0 h-[1.5px] rounded bg-foreground" />
          <span class="absolute inset-x-0 bottom-0 h-[1.5px] rounded bg-foreground" />
        </span>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        sideOffset={12}
        class="w-[min(20rem,calc(100vw-2rem))] gap-0 rounded-[var(--radius-card)] p-2 shadow-panel ring-border"
      >
        {siteConfig.navItems.map((item) => (
          <a
            href={item.href}
            class={cn(
              "flex h-10 items-center rounded-[var(--radius-control)] px-3 text-[0.9375rem] hover:bg-secondary",
              item.href === section && "font-semibold"
            )}
          >
            {item.label}
          </a>
        ))}
        <div class="my-2 h-px bg-border" />
        <p class="px-3 pt-1 pb-2 kicker">Resources</p>
        {siteConfig.resources.map((item) => (
          <a
            href={item.href}
            class="flex h-9 items-center justify-between rounded-[var(--radius-control)] px-3 text-sm text-muted-foreground hover:bg-secondary hover:text-foreground"
            {...(external(item.href)
              ? { target: "_blank", rel: "noreferrer" }
              : {})}
          >
            {item.label}
            {external(item.href) && (
              <Icon icon={ArrowUpRight} class="size-3.5" />
            )}
          </a>
        ))}
      </PopoverContent>
    </Popover>
  )
}

/**
 * The site's mark (public/logo.svg): a square-cut A whose crossbar is the
 * theme's blue. The letter follows the text color.
 */
export function Logo({ class: className }: { class?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 32 32"
      class={className}
      aria-hidden="true"
    >
      <path fill="currentColor" d="M4 28V4h24v24h-6V9H10v19H4Z" />
      <path fill="var(--data-2)" d="M10 15h12v5H10Z" />
    </svg>
  )
}
