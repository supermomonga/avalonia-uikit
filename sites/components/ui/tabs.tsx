// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/tabs (https://ui.shadcn.com/r/styles/base-nova/tabs.json)
// upstream-revision: sha256:3f882d61007b43252ce03dd07caa699fcf6b783f06ee5a6d914fa4aacdc09273; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
// client: <script type="module" src="/shadcn/tabs.js"> (public/shadcn/)
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.

import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import type { JSX } from "hono/jsx"
import { createContext, useContext, useId } from "hono/jsx"

/** Props of the intrinsic element `T`, following Hono JSX conventions (`class`, no `render`/`asChild`). */
type ComponentProps<
  T extends keyof JSX.IntrinsicElements,
  Render = never,
> = JSX.IntrinsicElements[T] & {
  class?: string | undefined
  className?: never
  render?: Render
  asChild?: never
}

type TabsValue = string | number

type TabsRootProps = ComponentProps<"div"> & {
  /** Value of the tab that is selected on load (the first enabled tab by default). */
  defaultValue?: TabsValue | undefined
  value?: TabsValue | undefined
  orientation?: "horizontal" | "vertical" | undefined
}

type TabsListProps = ComponentProps<"div"> & {
  /** Select tabs as the arrow keys focus them (otherwise Enter or Space selects). */
  activateOnFocus?: boolean | undefined
  /** Wrap focus from the last tab to the first with the arrow keys. */
  loopFocus?: boolean | undefined
}

type TabsTabProps = ComponentProps<"button"> & {
  value: TabsValue
  disabled?: boolean | undefined
}

type TabsPanelProps = ComponentProps<"div"> & { value: TabsValue }

interface TabsState {
  id: string
  /** Set by the first enabled tab when no value is given. */
  value: TabsValue | undefined
  orientation: "horizontal" | "vertical"
}

const TabsContext = createContext<TabsState | null>(null)

function useTabsContext(): TabsState {
  const context = useContext(TabsContext)
  if (!context) throw new Error("Tabs parts must be rendered inside <Tabs>")
  return context
}

/** Id of a tab or panel for a value (ids cannot contain spaces). */
function tabsId(
  state: TabsState,
  part: "tab" | "panel",
  value: TabsValue
): string {
  return `${state.id}-${part}-${String(value).replace(/[^\w-]/g, "_")}`
}

function TabsRootElement({
  value,
  defaultValue,
  orientation = "horizontal",
  ...props
}: TabsRootProps) {
  const id = useId().replaceAll(":", "-")
  return (
    <TabsContext.Provider
      value={{ id: `tabs${id}`, value: value ?? defaultValue, orientation }}
    >
      <div data-orientation={orientation} {...props} />
    </TabsContext.Provider>
  )
}

function TabsListElement({
  activateOnFocus,
  loopFocus,
  ...props
}: TabsListProps) {
  const { orientation } = useTabsContext()
  return (
    <div
      role="tablist"
      aria-orientation={orientation === "vertical" ? "vertical" : undefined}
      data-orientation={orientation}
      data-activate-on-focus={activateOnFocus ? "" : undefined}
      data-loop-focus={loopFocus === false ? "false" : undefined}
      {...props}
    />
  )
}

function TabsTabElement({ value, disabled, ...props }: TabsTabProps) {
  const state = useTabsContext()
  if (state.value === undefined && !disabled) state.value = value
  const active = state.value === value
  return (
    <button
      type="button"
      role="tab"
      id={tabsId(state, "tab", value)}
      aria-controls={tabsId(state, "panel", value)}
      aria-selected={active ? "true" : "false"}
      aria-disabled={disabled ? "true" : undefined}
      tabindex={active ? 0 : -1}
      data-active={active ? "" : undefined}
      data-disabled={disabled ? "" : undefined}
      data-orientation={state.orientation}
      {...props}
    />
  )
}

function TabsPanelElement({ value, ...props }: TabsPanelProps) {
  const state = useTabsContext()
  return (
    <div
      role="tabpanel"
      id={tabsId(state, "panel", value)}
      aria-labelledby={tabsId(state, "tab", value)}
      tabindex={0}
      hidden={state.value !== value || undefined}
      data-orientation={state.orientation}
      {...props}
    />
  )
}

function Tabs({
  class: className,
  orientation = "horizontal",
  ...props
}: TabsRootProps) {
  return (
    <TabsRootElement
      data-slot="tabs"
      data-orientation={orientation}
      class={cn("group/tabs flex gap-2 data-horizontal:flex-col", className)}
      {...props}
    />
  )
}

const tabsListVariants = cva(
  "group/tabs-list inline-flex w-fit items-center justify-center rounded-lg p-[3px] text-muted-foreground group-data-horizontal/tabs:h-8 group-data-vertical/tabs:h-fit group-data-vertical/tabs:flex-col data-[variant=line]:rounded-none",
  {
    variants: {
      variant: {
        default: "bg-muted",
        line: "gap-1 bg-transparent",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function TabsList({
  class: className,
  variant = "default",
  ...props
}: TabsListProps & VariantProps<typeof tabsListVariants>) {
  return (
    <TabsListElement
      data-slot="tabs-list"
      data-variant={variant}
      class={cn(tabsListVariants({ variant }), className)}
      {...props}
    />
  )
}

function TabsTrigger({ class: className, ...props }: TabsTabProps) {
  return (
    <TabsTabElement
      data-slot="tabs-trigger"
      class={cn(
        "relative inline-flex h-[calc(100%-1px)] flex-1 items-center justify-center gap-1.5 rounded-md border border-transparent px-1.5 py-0.5 text-sm font-medium whitespace-nowrap text-foreground/60 transition-all group-data-vertical/tabs:w-full group-data-vertical/tabs:justify-start hover:text-foreground focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-1 focus-visible:outline-ring disabled:pointer-events-none disabled:opacity-50 has-data-[icon=inline-end]:pr-1 has-data-[icon=inline-start]:pl-1 aria-disabled:pointer-events-none aria-disabled:opacity-50 dark:text-muted-foreground dark:hover:text-foreground group-data-[variant=default]/tabs-list:data-active:shadow-sm group-data-[variant=line]/tabs-list:data-active:shadow-none [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
        "group-data-[variant=line]/tabs-list:bg-transparent group-data-[variant=line]/tabs-list:data-active:bg-transparent dark:group-data-[variant=line]/tabs-list:data-active:border-transparent dark:group-data-[variant=line]/tabs-list:data-active:bg-transparent",
        "data-active:bg-background data-active:text-foreground dark:data-active:border-input dark:data-active:bg-input/30 dark:data-active:text-foreground",
        "after:absolute after:bg-foreground after:opacity-0 after:transition-opacity group-data-horizontal/tabs:after:inset-x-0 group-data-horizontal/tabs:after:bottom-[-5px] group-data-horizontal/tabs:after:h-0.5 group-data-vertical/tabs:after:inset-y-0 group-data-vertical/tabs:after:-right-1 group-data-vertical/tabs:after:w-0.5 group-data-[variant=line]/tabs-list:data-active:after:opacity-100",
        className
      )}
      {...props}
    />
  )
}

function TabsContent({ class: className, ...props }: TabsPanelProps) {
  return (
    <TabsPanelElement
      data-slot="tabs-content"
      class={cn("flex-1 text-sm outline-none", className)}
      {...props}
    />
  )
}

export { Tabs, TabsContent, TabsList, TabsTrigger, tabsListVariants }
