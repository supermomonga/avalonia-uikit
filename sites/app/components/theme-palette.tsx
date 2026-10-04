import { Check, Palette } from "lucide"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { Kbd } from "@/components/ui/kbd"
import { Icon } from "./icon"
import { dialogClass } from "./search"

/**
 * Applies the stored theme before the first paint: `localStorage.theme` is
 * `light` or `dark`, or absent to follow the system.
 */
export const THEME_SCRIPT = `try{var t=localStorage.theme,d=t==="dark"||(t!=="light"&&matchMedia("(prefers-color-scheme: dark)").matches);document.documentElement.classList.toggle("dark",d)}catch(e){}`

const options = [
  {
    group: "System",
    value: "system",
    label: "Follow System",
    swatch: ["#ffffff", "#0a0a0a"],
  },
  { group: "Light", value: "light", label: "Default Light", swatch: ["#ffffff", "#ffffff"] },
  { group: "Dark", value: "dark", label: "Default Dark", swatch: ["#0a0a0a", "#0a0a0a"] },
] as const

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
 * The theme palette, a command palette as on gpui-kit.com: the arrow keys
 * preview a theme, Enter or a click keeps it, Escape restores the previous
 * one (app/client.ts).
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
          Choose the site's color theme.
        </DialogDescription>
        <div class="flex h-11 items-center justify-between border-b px-4">
          <span class="kicker">Theme</span>
          <Kbd class="border bg-secondary font-mono">T</Kbd>
        </div>
        <div role="listbox" aria-label="Themes" class="p-2" data-theme-options="">
          {options.map((option) => (
            <div>
              <p class="px-2.5 pt-2 pb-1.5 text-[0.7rem] font-medium text-muted-foreground">
                {option.group}
              </p>
              <button
                type="button"
                role="option"
                data-theme-option={option.value}
                class="group flex h-9 w-full items-center gap-3 rounded-[var(--radius-control)] px-2.5 text-left text-sm outline-none data-[highlighted]:bg-secondary"
              >
                <span
                  class="size-4 rounded-full shadow-hairline"
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
            </div>
          ))}
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
