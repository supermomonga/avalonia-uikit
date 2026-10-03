import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
} from "@/components/ui/sidebar"
import { docsNav, isActive } from "@/lib/docs"

const itemClass =
  "relative h-[30px] w-fit overflow-visible border border-transparent text-[0.8rem] font-medium after:absolute after:inset-x-0 after:-inset-y-1 after:z-0 after:rounded-md data-active:border-accent data-active:bg-accent 3xl:fixed:w-full 3xl:fixed:max-w-48"

export function DocsSidebar({ pathname }: { pathname: string }) {
  return (
    <Sidebar
      class="sticky top-[calc(var(--header-height)+0.6rem)] z-30 hidden h-[calc(100svh-10rem)] overflow-hidden overscroll-none bg-transparent [--sidebar-menu-width:--spacing(56)] lg:flex"
      collapsible="none"
    >
      <div class="absolute top-12 right-2 bottom-0 hidden h-full w-px bg-[linear-gradient(to_bottom,transparent_0%,var(--border)_10%,var(--border)_90%,transparent_100%)] lg:flex" />
      <SidebarContent
        class="w-(--sidebar-menu-width) scroll-fade scrollbar-none overflow-x-hidden pl-2.5"
        data-docs-sidebar=""
      >
        {docsNav.map((group, index) => (
          <SidebarGroup class={index === 0 ? "pt-12" : undefined}>
            <SidebarGroupLabel class="font-medium text-muted-foreground">
              {group.title}
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu class="gap-0.5">
                {group.items.map((item) => (
                  <SidebarMenuItem>
                    <SidebarMenuButton
                      isActive={
                        index === 0
                          ? isActive(item.href, pathname)
                          : item.href === pathname
                      }
                      class={itemClass}
                      // The installed SidebarMenuButton types `render` as never (an
                      // intersection in the template); it renders the link fine.
                      render={(<a href={item.href} />) as never}
                    >
                      <span class="absolute inset-0 flex w-(--sidebar-menu-width) bg-transparent" />
                      {item.title}
                      {item.badge && (
                        <span
                          class="flex size-2 rounded-full bg-blue-500"
                          title={item.badge}
                        />
                      )}
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}
      </SidebarContent>
    </Sidebar>
  )
}

export function DocsLayout({
  pathname,
  children,
}: {
  pathname: string
  children?: unknown
}) {
  return (
    <div class="container-wrapper flex flex-1 flex-col px-2">
      <SidebarProvider
        class="min-h-min flex-1 items-start px-0 [--top-spacing:0] lg:grid lg:grid-cols-[var(--sidebar-width)_minmax(0,1fr)] lg:[--top-spacing:calc(var(--spacing)*4)] 3xl:fixed:container 3xl:fixed:px-3"
        style={{ "--sidebar-width": "calc(var(--spacing) * 72)" }}
      >
        <DocsSidebar pathname={pathname} />
        <div class="h-full w-full">{children as never}</div>
      </SidebarProvider>
    </div>
  )
}
