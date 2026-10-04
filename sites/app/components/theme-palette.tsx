import { Check, Palette, Search as SearchIcon } from "lucide"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { Kbd } from "@/components/ui/kbd"
import { bundledThemes } from "@/lib/themes"
import { Icon } from "./icon"
import { dialogClass } from "./search"

const bundledModes = Object.fromEntries(bundledThemes.map((theme) => [theme.id, theme.mode]))

/**
 * Applies the stored theme before the first paint: `localStorage.theme` is
 * `light`, `dark` or a bundled theme's id (its mode sets `dark`, its id
 * `<html data-theme>`), or absent to follow the system.
 */
export const THEME_SCRIPT = `try{var t=localStorage.theme,m=${JSON.stringify(bundledModes)}[t],r=document.documentElement,d=m?m==="dark":t==="dark"||(t!=="light"&&matchMedia("(prefers-color-scheme: dark)").matches);r.classList.toggle("dark",d);if(m)r.dataset.theme=t}catch(e){}`

interface Option {
  value: string
  /** GPUI Kit's name, which the live demos take (app/avalonia-demo.ts). */
  label: string
  mode: "system" | "light" | "dark"
  family: string
  swatch: readonly [string, string]
}

const groups: { heading: string; options: Option[] }[] = [
  {
    heading: "System",
    options: [
      { value: "system", label: "Follow System", mode: "system", family: "Default", swatch: ["#ffffff", "#0a0a0a"] },
    ],
  },
  ...(["light", "dark"] as const).map((mode) => ({
    heading: mode === "light" ? "Light" : "Dark",
    options: [
      {
        value: mode,
        label: mode === "light" ? "Default Light" : "Default Dark",
        mode,
        family: "Default",
        swatch: mode === "light" ? (["#ffffff", "#171717"] as const) : (["#0a0a0a", "#fafafa"] as const),
      },
      ...bundledThemes
        .filter((theme) => theme.mode === mode)
        .map((theme) => ({ value: theme.id, label: theme.name, mode, family: theme.family, swatch: theme.swatch })),
    ],
  })),
]

/** The top bar's theme button; T opens the palette too (app/client.ts). */
export function ThemeButton() {
  return (
    <Button
      variant="ghost"
      size="icon"
      {...{ command: "show-modal", commandfor: "theme-palette" }}
      aria-label="Theme"
      title="Theme (T)"
    >
      <Icon icon={Palette} class="size-4" />
    </Button>
  )
}

/**
 * The theme palette, a command palette as on gpui-kit.com: Default Light and
 * Default Dark and every theme GPUI Kit bundles. Typing filters it, the arrow
 * keys preview a theme, Enter or a click keeps it, Escape restores the
 * previous one (app/client.ts).
 */
export function ThemePalette() {
  return (
    <Dialog id="theme-palette">
      <DialogContent
        showCloseButton={false}
        class={`${dialogClass} w-[30rem] max-w-[calc(100%-2rem)] sm:max-w-[30rem]`}
      >
        <DialogTitle class="sr-only">Theme</DialogTitle>
        <DialogDescription class="sr-only">
          Choose the color theme of the site and its live demos.
        </DialogDescription>
        <div class="flex h-12 items-center gap-2.5 border-b px-4">
          <Icon icon={SearchIcon} class="size-4 text-muted-foreground" />
          <input
            type="search"
            data-theme-search=""
            placeholder="Search themes"
            autocomplete="off"
            spellcheck={false}
            aria-label="Search themes"
            aria-controls="theme-options"
            class="h-full flex-1 bg-transparent text-[0.9375rem] outline-none placeholder:text-muted-foreground [&::-webkit-search-cancel-button]:hidden"
          />
          <Kbd class="border bg-secondary font-mono">T</Kbd>
        </div>
        <div
          id="theme-options"
          role="listbox"
          aria-label="Themes"
          class="max-h-[min(24rem,60vh)] overflow-y-auto p-2"
          data-theme-options=""
        >
          {groups.map((group) => (
            <div role="group" aria-label={group.heading} data-theme-group="">
              <p class="px-2.5 pt-2 pb-1.5 text-[0.7rem] font-medium text-muted-foreground">
                {group.heading}
              </p>
              {group.options.map((option) => (
                <button
                  type="button"
                  role="option"
                  data-theme-option={option.value}
                  data-theme-name={option.label}
                  data-mode={option.mode}
                  data-search={`${option.label} ${option.family} ${option.mode}`.toLowerCase()}
                  class="group flex h-9 w-full items-center gap-3 rounded-[var(--radius-control)] px-2.5 text-left text-sm outline-none data-[highlighted]:bg-secondary"
                >
                  <span
                    class="size-4 shrink-0 rounded-full shadow-hairline"
                    style={{
                      background: `linear-gradient(135deg, ${option.swatch[0]} 50%, ${option.swatch[1]} 50%)`,
                    }}
                  />
                  {option.label}
                  <Icon
                    icon={Check}
                    class="ml-auto size-4 opacity-0 group-aria-selected:opacity-100"
                  />
                </button>
              ))}
            </div>
          ))}
          <p data-theme-empty="" hidden class="py-10 text-center text-sm text-muted-foreground">
            No matching themes
          </p>
        </div>
        <div class="flex h-10 items-center gap-4 border-t bg-sidebar px-4 text-xs text-muted-foreground">
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">↑</Kbd>
            <Kbd class="border bg-background">↓</Kbd>
            Preview
          </span>
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">↵</Kbd>
            Apply
          </span>
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">Esc</Kbd>
            Cancel
          </span>
        </div>
      </DialogContent>
    </Dialog>
  )
}
