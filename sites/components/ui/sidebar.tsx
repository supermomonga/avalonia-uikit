// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/sidebar (https://ui.shadcn.com/r/styles/base-nova/sidebar.json)
// upstream-revision: sha256:6d0d115f3821dcdf5ddf691620dd33fefbf8bdb8a22a404d257ad365d07b2ea2; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: adapter
// client: <script type="module" src="/shadcn/sidebar.js"> (public/shadcn/)
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.
// Icons: lucide@1.48.0 (panel-left). Copyright (c) 2026 Lucide Icons and Contributors. ISC License (see LICENSE-shadcnui-hono-jsx.txt).

import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import type { Child, CSSProperties, JSX, JSXNode } from "hono/jsx"
import {
  cloneElement,
  createContext,
  isValidElement,
  useContext,
} from "hono/jsx"
import { Button } from "./button"
import { Input } from "./input"
import { Separator } from "./separator"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "./sheet"
import { Skeleton } from "./skeleton"
import { Tooltip, TooltipContent, TooltipTrigger } from "./tooltip"

/** Props of the intrinsic element `T`, following Hono JSX conventions (`class`, no `render`/`asChild`). */
type ComponentProps<
  T extends keyof JSX.IntrinsicElements | "svg",
  Render = never,
> = (T extends keyof JSX.IntrinsicElements
  ? JSX.IntrinsicElements[T]
  : JSX.HTMLAttributes) & {
  class?: string | undefined
  className?: never
  render?: Render
  asChild?: never
}

/** An element (or a function returning one) that replaces the default element, as in Base UI. */
type RenderProp = Child | ((props: Record<string, unknown>) => Child)

/**
 * Renders `render` in place of `element`, merging props like Base UI's
 * mergeProps: the render element's props win, classes are combined (the render
 * element's first) and styles are merged. `type` only applies to button and
 * input targets.
 */
function renderElement<Element>(
  element: Element,
  render: RenderProp | undefined
): Element {
  if (render === undefined) return element
  const own: Record<string, unknown> = {
    ...(element as unknown as JSXNode).props,
  }
  if (typeof render === "function") return render(own) as Element
  if (!isValidElement(render)) {
    throw new Error("render must be a JSX element or a function returning one")
  }
  const target = render as JSXNode
  const merged: Record<string, unknown> = { ...own }
  for (const [key, value] of Object.entries(target.props)) {
    if (key === "class" || key === "className") {
      merged.class = [value, own.class].filter(Boolean).join(" ")
    } else if (key === "style" && isObject(value) && isObject(own.style)) {
      merged.style = { ...own.style, ...value }
    } else {
      merged[key] = value
    }
  }
  if (
    typeof target.tag === "string" &&
    !["button", "input"].includes(target.tag)
  ) {
    if (!("type" in target.props)) delete merged.type
  }
  const { children, ...props } = merged
  return cloneElement(
    target,
    props,
    ...(children === undefined ? [] : [children as Child].flat())
  ) as Element
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null
}

const SIDEBAR_COOKIE_NAME = "sidebar_state"
const SIDEBAR_COOKIE_MAX_AGE = 60 * 60 * 24 * 7
const SIDEBAR_WIDTH = "16rem"
const SIDEBAR_WIDTH_MOBILE = "18rem"
const SIDEBAR_WIDTH_ICON = "3rem"
const SIDEBAR_KEYBOARD_SHORTCUT = "b"

type SidebarContextProps = {
  state: "expanded" | "collapsed"
  open: boolean
  setOpen: (open: boolean) => void
  openMobile: boolean
  setOpenMobile: (open: boolean) => void
  isMobile: boolean
  toggleSidebar: () => void
}

const SidebarContext = createContext<SidebarContextProps | null>(null)

function useSidebar() {
  const context = useContext(SidebarContext)
  if (!context) {
    throw new Error("useSidebar must be used within a SidebarProvider.")
  }

  return context
}
/** Opening and closing happen in the browser, with /shadcn/sidebar.js. */
function sidebarInBrowser(): never {
  throw new Error(
    "The sidebar opens and closes in the browser with /shadcn/sidebar.js; on the server, set open or defaultOpen on SidebarProvider."
  )
}

/** Adds the sidebar variables to a string or object `style` prop (the prop wins). */
function sidebarStyle(
  style: string | JSX.CSSProperties | undefined,
  variables: Record<string, string>
): string | JSX.CSSProperties {
  if (typeof style === "string") {
    return [
      ...Object.entries(variables).map(([key, value]) => `${key}:${value}`),
      style,
    ].join(";")
  }
  return { ...variables, ...style }
}

function SidebarProvider({
  defaultOpen = true,
  open: openProp,
  onOpenChange: _onOpenChange,
  class: className,
  style,
  children,
  ...props
}: ComponentProps<"div"> & {
  defaultOpen?: boolean
  open?: boolean
  onOpenChange?: (open: boolean) => void
}) {
  // On the server the state comes from the props (read the cookie named by
  // SIDEBAR_COOKIE_NAME for defaultOpen); /shadcn/sidebar.js toggles it.
  const open = openProp ?? defaultOpen
  const state = open ? "expanded" : "collapsed"
  const contextValue: SidebarContextProps = {
    state,
    open,
    setOpen: sidebarInBrowser,
    isMobile: false,
    openMobile: false,
    setOpenMobile: sidebarInBrowser,
    toggleSidebar: sidebarInBrowser,
  }
  return (
    <SidebarContext.Provider value={contextValue}>
      <div
        data-slot="sidebar-wrapper"
        style={sidebarStyle(style, {
          "--sidebar-width": SIDEBAR_WIDTH,
          "--sidebar-width-icon": SIDEBAR_WIDTH_ICON,
        })}
        class={cn(
          "group/sidebar-wrapper flex min-h-svh w-full has-data-[variant=inset]:bg-sidebar",
          className
        )}
        {...props}
        data-sidebar-cookie={SIDEBAR_COOKIE_NAME}
        data-sidebar-cookie-max-age={SIDEBAR_COOKIE_MAX_AGE}
        data-sidebar-shortcut={SIDEBAR_KEYBOARD_SHORTCUT}
      >
        {children}
      </div>
    </SidebarContext.Provider>
  )
}

function Sidebar({
  side = "left",
  variant = "sidebar",
  collapsible = "offcanvas",
  class: className,
  children,
  dir,
  ...props
}: ComponentProps<"div"> & {
  side?: "left" | "right"
  variant?: "sidebar" | "floating" | "inset"
  collapsible?: "offcanvas" | "icon" | "none"
}) {
  const { state } = useSidebar()

  if (collapsible === "none") {
    return (
      <div
        data-slot="sidebar"
        class={cn(
          "flex h-full w-(--sidebar-width) flex-col bg-sidebar text-sidebar-foreground",
          className
        )}
        {...props}
      >
        {children}
      </div>
    )
  }
  return (
    <>
      <div
        class="group peer hidden text-sidebar-foreground md:block"
        data-state={state}
        data-collapsible={state === "collapsed" ? collapsible : ""}
        data-variant={variant}
        data-side={side}
        data-slot="sidebar"
        data-sidebar-collapsible={collapsible}
      >
        {/* This is what handles the sidebar gap on desktop */}
        <div
          data-slot="sidebar-gap"
          class={cn(
            "relative w-(--sidebar-width) bg-transparent transition-[width] duration-200 ease-linear",
            "group-data-[collapsible=offcanvas]:w-0",
            "group-data-[side=right]:rotate-180",
            variant === "floating" || variant === "inset"
              ? "group-data-[collapsible=icon]:w-[calc(var(--sidebar-width-icon)+(--spacing(4)))]"
              : "group-data-[collapsible=icon]:w-(--sidebar-width-icon)"
          )}
        />
        <div
          data-slot="sidebar-container"
          data-side={side}
          class={cn(
            "fixed inset-y-0 z-10 hidden h-svh w-(--sidebar-width) transition-[left,right,width] duration-200 ease-linear data-[side=left]:left-0 data-[side=left]:group-data-[collapsible=offcanvas]:left-[calc(var(--sidebar-width)*-1)] data-[side=right]:right-0 data-[side=right]:group-data-[collapsible=offcanvas]:right-[calc(var(--sidebar-width)*-1)] md:flex",
            // Adjust the padding for floating and inset variants.
            variant === "floating" || variant === "inset"
              ? "p-2 group-data-[collapsible=icon]:w-[calc(var(--sidebar-width-icon)+(--spacing(4))+2px)]"
              : "group-data-[collapsible=icon]:w-(--sidebar-width-icon) group-data-[side=left]:border-r group-data-[side=right]:border-l",
            className
          )}
          {...props}
        >
          <div
            data-sidebar="sidebar"
            data-slot="sidebar-inner"
            class="flex size-full flex-col bg-sidebar group-data-[variant=floating]:rounded-lg group-data-[variant=floating]:shadow-sm group-data-[variant=floating]:ring-1 group-data-[variant=floating]:ring-sidebar-border"
          >
            {children}
          </div>
        </div>
      </div>
      <Sheet>
        <SheetContent
          dir={dir}
          data-sidebar="sidebar"
          data-slot="sidebar"
          data-mobile="true"
          class="w-(--sidebar-width) bg-sidebar p-0 text-sidebar-foreground [&>button]:hidden"
          style={
            {
              "--sidebar-width": String(SIDEBAR_WIDTH_MOBILE),
            } as CSSProperties
          }
          side={side}
        >
          <SheetHeader class="sr-only">
            <SheetTitle>Sidebar</SheetTitle>
            <SheetDescription>Displays the mobile sidebar.</SheetDescription>
          </SheetHeader>
          <div data-sidebar-mobile="" class="flex h-full w-full flex-col" />
        </SheetContent>
      </Sheet>
    </>
  )
}

function SidebarTrigger({
  class: className,
  onClick: _onClick,
  ...props
}: Parameters<typeof Button>[0]) {
  return (
    <Button
      data-sidebar="trigger"
      data-slot="sidebar-trigger"
      variant="ghost"
      size="icon-sm"
      class={cn(className)}
      {...props}
    >
      <PanelLeftIcon />
      <span class="sr-only">Toggle Sidebar</span>
    </Button>
  )
}

function SidebarRail({ class: className, ...props }: ComponentProps<"button">) {
  return (
    <button
      data-sidebar="rail"
      data-slot="sidebar-rail"
      aria-label="Toggle Sidebar"
      tabindex={-1}
      title="Toggle Sidebar"
      class={cn(
        "absolute inset-y-0 z-20 hidden w-4 transition-all ease-linear group-data-[side=left]:-right-4 group-data-[side=right]:left-0 after:absolute after:inset-y-0 after:start-1/2 after:w-[2px] hover:after:bg-sidebar-border sm:flex ltr:-translate-x-1/2 rtl:-translate-x-1/2",
        "in-data-[side=left]:cursor-w-resize in-data-[side=right]:cursor-e-resize",
        "[[data-side=left][data-state=collapsed]_&]:cursor-e-resize [[data-side=right][data-state=collapsed]_&]:cursor-w-resize",
        "group-data-[collapsible=offcanvas]:translate-x-0 group-data-[collapsible=offcanvas]:after:left-full hover:group-data-[collapsible=offcanvas]:bg-sidebar",
        "[[data-side=left][data-collapsible=offcanvas]_&]:-right-2",
        "[[data-side=right][data-collapsible=offcanvas]_&]:-left-2",
        className
      )}
      {...props}
    />
  )
}

function SidebarInset({ class: className, ...props }: ComponentProps<"main">) {
  return (
    <main
      data-slot="sidebar-inset"
      class={cn(
        "relative flex w-full flex-1 flex-col bg-background md:peer-data-[variant=inset]:m-2 md:peer-data-[variant=inset]:ml-0 md:peer-data-[variant=inset]:rounded-xl md:peer-data-[variant=inset]:shadow-sm md:peer-data-[variant=inset]:peer-data-[state=collapsed]:ml-2",
        className
      )}
      {...props}
    />
  )
}

function SidebarInput({
  class: className,
  ...props
}: Parameters<typeof Input>[0]) {
  return (
    <Input
      data-slot="sidebar-input"
      data-sidebar="input"
      class={cn("h-8 w-full bg-background shadow-none", className)}
      {...props}
    />
  )
}

function SidebarHeader({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-header"
      data-sidebar="header"
      class={cn("flex flex-col gap-2 p-2", className)}
      {...props}
    />
  )
}

function SidebarFooter({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-footer"
      data-sidebar="footer"
      class={cn("flex flex-col gap-2 p-2", className)}
      {...props}
    />
  )
}

function SidebarSeparator({
  class: className,
  ...props
}: Parameters<typeof Separator>[0]) {
  return (
    <Separator
      data-slot="sidebar-separator"
      data-sidebar="separator"
      class={cn("mx-2 w-auto bg-sidebar-border", className)}
      {...props}
    />
  )
}

function SidebarContent({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-content"
      data-sidebar="content"
      class={cn(
        "no-scrollbar flex min-h-0 flex-1 flex-col gap-0 overflow-auto group-data-[collapsible=icon]:overflow-hidden",
        className
      )}
      {...props}
    />
  )
}

function SidebarGroup({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-group"
      data-sidebar="group"
      class={cn("relative flex w-full min-w-0 flex-col p-2", className)}
      {...props}
    />
  )
}

function SidebarGroupLabel({
  class: className,
  render,
  ...props
}: ComponentProps<"div", RenderProp> & ComponentProps<"div">) {
  return renderElement(
    <div
      data-slot="sidebar-group-label"
      data-sidebar="group-label"
      class={cn(
        "flex h-8 shrink-0 items-center rounded-md px-2 text-xs font-medium text-sidebar-foreground/70 ring-sidebar-ring outline-hidden transition-[margin,opacity] duration-200 ease-linear group-data-[collapsible=icon]:-mt-8 group-data-[collapsible=icon]:opacity-0 focus-visible:ring-2 [&>svg]:size-4 [&>svg]:shrink-0",
        className
      )}
      {...props}
    />,
    render
  )
}

function SidebarGroupAction({
  class: className,
  render,
  ...props
}: ComponentProps<"button", RenderProp> & ComponentProps<"button">) {
  return renderElement(
    <button
      data-slot="sidebar-group-action"
      data-sidebar="group-action"
      class={cn(
        "absolute top-3.5 right-3 flex aspect-square w-5 items-center justify-center rounded-md p-0 text-sidebar-foreground ring-sidebar-ring outline-hidden transition-transform group-data-[collapsible=icon]:hidden after:absolute after:-inset-2 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 md:after:hidden [&>svg]:size-4 [&>svg]:shrink-0",
        className
      )}
      {...props}
    />,
    render
  )
}

function SidebarGroupContent({
  class: className,
  ...props
}: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-group-content"
      data-sidebar="group-content"
      class={cn("w-full text-sm", className)}
      {...props}
    />
  )
}

function SidebarMenu({ class: className, ...props }: ComponentProps<"ul">) {
  return (
    <ul
      data-slot="sidebar-menu"
      data-sidebar="menu"
      class={cn("flex w-full min-w-0 flex-col gap-0", className)}
      {...props}
    />
  )
}

function SidebarMenuItem({ class: className, ...props }: ComponentProps<"li">) {
  return (
    <li
      data-slot="sidebar-menu-item"
      data-sidebar="menu-item"
      class={cn("group/menu-item relative", className)}
      {...props}
    />
  )
}

const sidebarMenuButtonVariants = cva(
  "peer/menu-button group/menu-button flex w-full items-center gap-2 overflow-hidden rounded-md p-2 text-left text-sm ring-sidebar-ring outline-hidden transition-[width,height,padding] group-has-data-[sidebar=menu-action]/menu-item:pr-8 group-data-[collapsible=icon]:size-8! group-data-[collapsible=icon]:p-2! hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 active:bg-sidebar-accent active:text-sidebar-accent-foreground disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 data-open:hover:bg-sidebar-accent data-open:hover:text-sidebar-accent-foreground data-active:bg-sidebar-accent data-active:font-medium data-active:text-sidebar-accent-foreground [&_svg]:size-4 [&_svg]:shrink-0 [&>span:last-child]:truncate",
  {
    variants: {
      variant: {
        default: "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
        outline:
          "bg-background shadow-[0_0_0_1px_var(--sidebar-border)] hover:bg-sidebar-accent hover:text-sidebar-accent-foreground hover:shadow-[0_0_0_1px_var(--sidebar-accent)]",
      },
      size: {
        default: "h-8 text-sm",
        sm: "h-7 text-xs",
        lg: "h-12 text-sm group-data-[collapsible=icon]:p-0!",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function SidebarMenuButton({
  render,
  isActive = false,
  variant = "default",
  size = "default",
  tooltip,
  class: className,
  ...props
}: ComponentProps<"button", RenderProp> &
  ComponentProps<"button"> & {
    isActive?: boolean
    tooltip?: string | Parameters<typeof TooltipContent>[0]
  } & VariantProps<typeof sidebarMenuButtonVariants>) {
  const { isMobile, state } = useSidebar()
  const comp = renderElement(
    <button
      data-slot="sidebar-menu-button"
      data-sidebar="menu-button"
      data-size={size}
      data-active={isActive ? "" : undefined}
      class={cn(sidebarMenuButtonVariants({ variant, size }), className)}
      {...props}
    />,
    !tooltip ? render : <TooltipTrigger render={render} />
  )

  if (!tooltip) {
    return comp
  }

  if (typeof tooltip === "string") {
    tooltip = {
      children: tooltip,
    }
  }

  return (
    <Tooltip>
      {comp}
      <TooltipContent
        side="right"
        align="center"
        hidden={state !== "collapsed" || isMobile}
        {...tooltip}
        data-sidebar-tooltip=""
      />
    </Tooltip>
  )
}

function SidebarMenuAction({
  class: className,
  render,
  showOnHover = false,
  ...props
}: ComponentProps<"button", RenderProp> &
  ComponentProps<"button"> & {
    showOnHover?: boolean
  }) {
  return renderElement(
    <button
      data-slot="sidebar-menu-action"
      data-sidebar="menu-action"
      class={cn(
        "absolute top-1.5 right-1 flex aspect-square w-5 items-center justify-center rounded-md p-0 text-sidebar-foreground ring-sidebar-ring outline-hidden transition-transform group-data-[collapsible=icon]:hidden peer-hover/menu-button:text-sidebar-accent-foreground peer-data-[size=default]/menu-button:top-1.5 peer-data-[size=lg]/menu-button:top-2.5 peer-data-[size=sm]/menu-button:top-1 after:absolute after:-inset-2 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 md:after:hidden [&>svg]:size-4 [&>svg]:shrink-0",
        showOnHover &&
          "group-focus-within/menu-item:opacity-100 group-hover/menu-item:opacity-100 peer-data-active/menu-button:text-sidebar-accent-foreground aria-expanded:opacity-100 md:opacity-0",
        className
      )}
      {...props}
    />,
    render
  )
}

function SidebarMenuBadge({
  class: className,
  ...props
}: ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-menu-badge"
      data-sidebar="menu-badge"
      class={cn(
        "pointer-events-none absolute right-1 flex h-5 min-w-5 items-center justify-center rounded-md px-1 text-xs font-medium text-sidebar-foreground tabular-nums select-none group-data-[collapsible=icon]:hidden peer-hover/menu-button:text-sidebar-accent-foreground peer-data-[size=default]/menu-button:top-1.5 peer-data-[size=lg]/menu-button:top-2.5 peer-data-[size=sm]/menu-button:top-1 peer-data-active/menu-button:text-sidebar-accent-foreground",
        className
      )}
      {...props}
    />
  )
}

function SidebarMenuSkeleton({
  class: className,
  showIcon = false,
  ...props
}: ComponentProps<"div"> & {
  showIcon?: boolean
}) {
  // Random width between 50 to 90%.
  const width = (() => {
    return `${Math.floor(Math.random() * 40) + 50}%`
  })()

  return (
    <div
      data-slot="sidebar-menu-skeleton"
      data-sidebar="menu-skeleton"
      class={cn("flex h-8 items-center gap-2 rounded-md px-2", className)}
      {...props}
    >
      {showIcon && (
        <Skeleton class="size-4 rounded-md" data-sidebar="menu-skeleton-icon" />
      )}
      <Skeleton
        class="h-4 max-w-(--skeleton-width) flex-1"
        data-sidebar="menu-skeleton-text"
        style={
          {
            "--skeleton-width": String(width),
          } as CSSProperties
        }
      />
    </div>
  )
}

function SidebarMenuSub({ class: className, ...props }: ComponentProps<"ul">) {
  return (
    <ul
      data-slot="sidebar-menu-sub"
      data-sidebar="menu-sub"
      class={cn(
        "mx-3.5 flex min-w-0 translate-x-px flex-col gap-1 border-l border-sidebar-border px-2.5 py-0.5 group-data-[collapsible=icon]:hidden",
        className
      )}
      {...props}
    />
  )
}

function SidebarMenuSubItem({
  class: className,
  ...props
}: ComponentProps<"li">) {
  return (
    <li
      data-slot="sidebar-menu-sub-item"
      data-sidebar="menu-sub-item"
      class={cn("group/menu-sub-item relative", className)}
      {...props}
    />
  )
}

function SidebarMenuSubButton({
  render,
  size = "md",
  isActive = false,
  class: className,
  ...props
}: ComponentProps<"a", RenderProp> &
  ComponentProps<"a"> & {
    size?: "sm" | "md"
    isActive?: boolean
  }) {
  return renderElement(
    <a
      data-slot="sidebar-menu-sub-button"
      data-sidebar="menu-sub-button"
      data-size={size}
      data-active={isActive ? "" : undefined}
      class={cn(
        "flex h-7 min-w-0 -translate-x-px items-center gap-2 overflow-hidden rounded-md px-2 text-sidebar-foreground ring-sidebar-ring outline-hidden group-data-[collapsible=icon]:hidden hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:ring-2 active:bg-sidebar-accent active:text-sidebar-accent-foreground disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 data-[size=md]:text-sm data-[size=sm]:text-xs data-active:bg-sidebar-accent data-active:text-sidebar-accent-foreground [&>span:last-child]:truncate [&>svg]:size-4 [&>svg]:shrink-0 [&>svg]:text-sidebar-accent-foreground",
        className
      )}
      {...props}
    />,
    render
  )
}
/** Like lucide-react, icons are hidden from assistive technology unless labelled. */
function hasA11yProp(props: object): boolean {
  return Object.keys(props).some(
    (key) => key.startsWith("aria-") || key === "role" || key === "title"
  )
}

/** Lucide `panel-left` icon, inlined (ISC License, see LICENSE-shadcnui-hono-jsx.txt). */
function PanelLeftIcon({
  class: className,
  children,
  ...props
}: ComponentProps<"svg">) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      class={cn("lucide lucide-panel-left lucide-sidebar", className)}
      aria-hidden={children || hasA11yProp(props) ? undefined : "true"}
      {...props}
    >
      <rect width="18" height="18" x="3" y="3" rx="2" />
      <path d="M9 3v18" />
      {children}
    </svg>
  )
}

export {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupAction,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInput,
  SidebarInset,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSkeleton,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarProvider,
  SidebarRail,
  SidebarSeparator,
  SidebarTrigger,
  useSidebar,
}
