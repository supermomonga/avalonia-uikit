import { cn } from "cn"
import { Button } from "@/components/ui/button"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { docsNav } from "@/lib/docs"
import { siteConfig } from "@/lib/site"

export function MobileNav({ class: className }: { class?: string }) {
  return (
    <Popover>
      <PopoverTrigger
        render={
          <Button
            variant="ghost"
            class={cn(
              "extend-touch-target h-8 touch-manipulation items-center justify-start gap-2.5 p-0! hover:bg-transparent focus-visible:bg-transparent focus-visible:ring-0 active:bg-transparent dark:hover:bg-transparent",
              className
            )}
          />
        }
      >
        <div class="relative flex h-8 w-4 items-center justify-center">
          <div class="relative size-4">
            <span class="absolute top-1 left-0 block h-0.5 w-4 bg-foreground" />
            <span class="absolute top-2.5 left-0 block h-0.5 w-4 bg-foreground" />
          </div>
          <span class="sr-only">Toggle Menu</span>
        </div>
        <span class="flex h-8 items-center text-lg leading-none font-medium">
          Menu
        </span>
      </PopoverTrigger>
      <PopoverContent
        class="no-scrollbar h-(--available-height) max-h-[80svh] w-(--available-width) overflow-y-auto rounded-none border-none bg-background/90 p-0 shadow-none backdrop-blur duration-100"
        align="start"
        side="bottom"
        sideOffset={14}
      >
        <div class="flex flex-col gap-12 overflow-auto px-6 py-6">
          <div class="flex flex-col gap-4">
            <div class="text-sm font-medium text-muted-foreground">Menu</div>
            <div class="flex flex-col gap-3">
              {siteConfig.navItems.map((item) => (
                <a href={item.href} class="text-2xl font-medium">
                  {item.label}
                </a>
              ))}
            </div>
          </div>
          {docsNav.map((group) => (
            <div class="flex flex-col gap-4">
              <div class="text-sm font-medium text-muted-foreground">
                {group.title}
              </div>
              <div class="flex flex-col gap-3">
                {group.items.map((item) => (
                  <a href={item.href} class="text-2xl font-medium">
                    {item.title}
                  </a>
                ))}
              </div>
            </div>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  )
}
