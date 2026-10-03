import { cn } from "cn"
import { Button } from "@/components/ui/button"
import { siteConfig } from "@/lib/site"

function active(href: string, pathname: string): boolean {
  if (href === "/") return pathname === "/"
  if (href === "/docs") return pathname.startsWith("/docs") && !pathname.startsWith("/docs/components")
  return pathname.startsWith(href)
}

export function MainNav({
  pathname,
  class: className,
}: {
  pathname: string
  class?: string
}) {
  return (
    <nav class={cn("items-center gap-0", className)}>
      {siteConfig.navItems.map((item) => (
        <Button
          variant="ghost"
          size="sm"
          class="px-2.5 data-[active=true]:text-primary"
          render={
            <a href={item.href} data-active={String(active(item.href, pathname))} />
          }
        >
          {item.label}
        </Button>
      ))}
    </nav>
  )
}
