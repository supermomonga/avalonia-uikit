import { CornerDownLeft } from "lucide"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Kbd, KbdGroup } from "@/components/ui/kbd"
import { Icon } from "./icon"

/**
 * The header's search, like ui.shadcn.com's command menu: ⌘K or the button
 * opens a dialog that filters the pages and components of /search.json
 * (app/client.ts).
 */
export function Search() {
  return (
    <>
      <Button
        variant="outline"
        class="relative h-8 w-full justify-start rounded-lg border-none bg-muted pl-3 text-foreground shadow-none transition-colors hover:bg-muted/50 md:w-48 lg:w-40 xl:w-64 dark:bg-card"
        {...{ command: "show-modal", commandfor: "search" }}
      >
        <span class="hidden xl:inline-flex">Search documentation...</span>
        <span class="inline-flex xl:hidden">Search...</span>
        <KbdGroup class="absolute top-1.5 right-1.5 hidden sm:flex">
          <Kbd class="border">⌘</Kbd>
          <Kbd class="border">K</Kbd>
        </KbdGroup>
      </Button>
      <Dialog id="search">
        <DialogContent
          showCloseButton={false}
          class="top-[20%] translate-y-0 gap-0 rounded-xl border-none bg-clip-padding p-2 pb-11 shadow-2xl ring-4 ring-neutral-200/80 dark:bg-neutral-900 dark:ring-neutral-800"
        >
          <DialogHeader class="sr-only">
            <DialogTitle>Search documentation...</DialogTitle>
            <DialogDescription>
              Search for a page or a component.
            </DialogDescription>
          </DialogHeader>
          <input
            type="search"
            data-search-input=""
            placeholder="Search documentation..."
            autocomplete="off"
            aria-label="Search documentation"
            class="h-9 w-full rounded-md border border-input bg-input/50 px-3 text-sm outline-none placeholder:text-muted-foreground"
          />
          <div
            data-search-results=""
            role="listbox"
            class="no-scrollbar mt-2 flex max-h-80 min-h-80 scroll-pt-2 scroll-pb-1.5 flex-col overflow-y-auto"
          />
          <div class="absolute inset-x-0 bottom-0 z-20 flex h-10 items-center gap-2 rounded-b-xl border-t border-t-neutral-100 bg-neutral-50 px-4 text-xs font-medium text-muted-foreground dark:border-t-neutral-700 dark:bg-neutral-800">
            <Kbd class="border">
              <Icon icon={CornerDownLeft} />
            </Kbd>
            Go to Page
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
