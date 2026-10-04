import { Search as SearchIcon } from "lucide"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { Kbd } from "@/components/ui/kbd"
import { Icon } from "./icon"

/** Classes the site's dialogs share: high on the page, the site's surface radius and shadow. */
export const dialogClass =
  "site-dialog top-[8vh] translate-y-0 gap-0 overflow-hidden rounded-[var(--radius-surface)] border border-border bg-popover p-0 shadow-dialog ring-0"

/** The top bar's search button: `/` (or ⌘K) opens the dialog too (app/client.ts). */
export function SearchButton() {
  return (
    <Button
      variant="outline"
      class="h-8 w-52 justify-start gap-2 bg-background px-2.5 font-normal text-muted-foreground shadow-none hover:text-foreground max-[1080px]:w-[9.5rem] max-[900px]:w-8 max-[900px]:justify-center max-[900px]:px-0 dark:bg-background"
      {...{ command: "show-modal", commandfor: "search" }}
      aria-label="Search"
    >
      <Icon icon={SearchIcon} class="size-4" />
      <span class="max-[900px]:hidden">Search</span>
      <Kbd class="ml-auto h-[1.15rem] min-w-[1.15rem] border bg-secondary font-mono text-[0.7rem] max-[900px]:hidden">
        /
      </Kbd>
    </Button>
  )
}

/**
 * The search dialog: it filters the pages and components of /search.json as
 * you type (app/client.ts).
 */
export function SearchDialog() {
  return (
    <Dialog id="search">
      <DialogContent
        showCloseButton={false}
        class={`${dialogClass} w-[42rem] max-w-[calc(100%-2rem)] sm:max-w-[42rem]`}
      >
        <DialogTitle class="sr-only">Search documentation</DialogTitle>
        <DialogDescription class="sr-only">
          Find components, guides and examples.
        </DialogDescription>
        <div class="flex h-12 items-center gap-2.5 border-b px-4">
          <Icon icon={SearchIcon} class="size-4 text-muted-foreground" />
          <input
            type="search"
            data-search-input=""
            placeholder="Search documentation"
            autocomplete="off"
            spellcheck={false}
            aria-label="Search documentation"
            class="h-full flex-1 bg-transparent text-[0.9375rem] outline-none placeholder:text-muted-foreground [&::-webkit-search-cancel-button]:hidden"
          />
          <Kbd class="border bg-secondary font-mono">Esc</Kbd>
        </div>
        <div
          data-search-results=""
          role="listbox"
          aria-label="Results"
          class="max-h-[min(24rem,60vh)] min-h-[14rem] overflow-y-auto p-2"
        />
        <div class="flex h-10 items-center gap-4 border-t bg-sidebar px-4 text-xs text-muted-foreground">
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">↑</Kbd>
            <Kbd class="border bg-background">↓</Kbd>
            Navigate
          </span>
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">↵</Kbd>
            Open
          </span>
          <span class="flex items-center gap-1.5">
            <Kbd class="border bg-background">Esc</Kbd>
            Close
          </span>
        </div>
      </DialogContent>
    </Dialog>
  )
}
