// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/tooltip (https://ui.shadcn.com/r/styles/base-nova/tooltip.json)
// upstream-revision: sha256:999f24433b0bb597cedfdef3a5de7f6dbfa5e819672ae66d581c0780e275766b; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
// client: <script type="module" src="/shadcn/hover.js"> (public/shadcn/)
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.

import { cn } from "cn"
import type { Child, JSX, JSXNode } from "hono/jsx"
import {
  cloneElement,
  createContext,
  isValidElement,
  useContext,
  useId,
} from "hono/jsx"

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

type HoverRootProps = {
  id?: string | undefined
  /** Milliseconds before the popup opens on hover. */
  delay?: number | undefined
  /** Milliseconds before the popup closes after the pointer leaves. */
  closeDelay?: number | undefined
  children?: Child
}

interface HoverContextValue {
  id: string
  anchor: string
  delay: number | undefined
  closeDelay: number | undefined
}

const HoverContext = createContext<HoverContextValue | null>(null)

function useHoverContext(): HoverContextValue {
  const context = useContext(HoverContext)
  if (!context)
    throw new Error(
      "Tooltip and hover card parts must be rendered inside their root"
    )
  return context
}

/** Delays set by a tooltip provider for the tooltips inside it. */
const HoverDelayContext = createContext<{
  delay?: number | undefined
  closeDelay?: number | undefined
}>({})

function HoverProviderElement({ delay, closeDelay, children }: HoverRootProps) {
  return (
    <HoverDelayContext.Provider value={{ delay, closeDelay }}>
      {children}
    </HoverDelayContext.Provider>
  )
}

function HoverRootElement({ id, delay, closeDelay, children }: HoverRootProps) {
  const generated = useId().replaceAll(":", "-")
  const provided = useContext(HoverDelayContext)
  const popupId = id ?? `hover${generated}`
  return (
    <HoverContext.Provider
      value={{
        id: popupId,
        anchor: `--${popupId}`,
        delay: delay ?? provided.delay ?? HOVER_DEFAULTS.delay,
        closeDelay:
          closeDelay ?? provided.closeDelay ?? HOVER_DEFAULTS.closeDelay,
      }}
    >
      {children}
    </HoverContext.Provider>
  )
}

/** Hover state for the script: the popup to open and its delays. */
function hoverTriggerAttributes(
  hover: HoverContextValue,
  style: string | JSX.CSSProperties | undefined
) {
  return {
    "data-hover-popup": hover.id,
    "data-delay": hover.delay === undefined ? undefined : String(hover.delay),
    "data-close-delay":
      hover.closeDelay === undefined ? undefined : String(hover.closeDelay),
    style: withStyle(style, { "anchor-name": hover.anchor }),
  }
}

function HoverPortalElement({ children }: { children?: Child }) {
  return <>{children}</>
}

/** The arrow sits centered on the popup's edge toward the trigger. */
function HoverArrowElement({ style, ...props }: ComponentProps<"div">) {
  const placement = useContext(AnchorPlacementContext)
  const block = placement.side === "top" || placement.side === "bottom"
  return (
    <div
      aria-hidden="true"
      data-side={placement.side}
      data-align={placement.align}
      style={withStyle(style, {
        position: "absolute",
        ...(block ? { left: "0", right: "0", "margin-inline": "auto" } : {}),
      })}
      {...props}
    />
  )
}

/** Base UI's default delays for this component, in milliseconds. */
const HOVER_DEFAULTS = { delay: 600, closeDelay: 0 }

function TooltipTriggerElement({
  type = "button",
  render,
  style,
  ...props
}: ComponentProps<"button", RenderProp>) {
  const hover = useHoverContext()
  return renderElement(
    <button
      type={type}
      aria-describedby={hover.id}
      {...hoverTriggerAttributes(hover, style)}
      {...props}
    />,
    render
  )
}

function TooltipPopupElement({ style, ...props }: ComponentProps<"div">) {
  const hover = useHoverContext()
  const placement = useContext(AnchorPlacementContext)
  return (
    <div
      id={hover.id}
      popover="manual"
      role="tooltip"
      data-side={placement.side}
      data-align={placement.align}
      style={withStyle(style, anchorPlacementStyle(hover.anchor, placement))}
      {...props}
    />
  )
}

type AnchorSide =
  | "top"
  | "bottom"
  | "left"
  | "right"
  | "inline-start"
  | "inline-end"

type AnchorAlign = "start" | "center" | "end"

interface AnchorPlacement {
  side: AnchorSide
  align: AnchorAlign
  sideOffset: number
  alignOffset: number
}

type AnchorPositionerProps = {
  side?: AnchorSide | undefined
  align?: AnchorAlign | undefined
  sideOffset?: number | undefined
  alignOffset?: number | undefined
  class?: string | undefined
  children?: Child
}

const AnchorPlacementContext = createContext<AnchorPlacement>({
  side: "bottom",
  align: "center",
  sideOffset: 0,
  alignOffset: 0,
})

/** Placement for the popup inside; the popup itself is positioned with CSS. */
function AnchorPositioner({
  side = "bottom",
  align = "center",
  sideOffset = 0,
  alignOffset = 0,
  children,
}: AnchorPositionerProps) {
  return (
    <AnchorPlacementContext.Provider
      value={{ side, align, sideOffset, alignOffset }}
    >
      {children}
    </AnchorPlacementContext.Provider>
  )
}

/** Inline styles that anchor a popup to `anchor` like Base UI's positioner. */
function anchorPlacementStyle(
  anchor: string,
  p: AnchorPlacement
): Record<string, string> {
  const block = p.side === "top" || p.side === "bottom"
  const logical = p.side === "inline-start" || p.side === "inline-end"
  const span = block
    ? { start: "span-x-end", center: "", end: "span-x-start" }[p.align]
    : logical
      ? { start: "span-block-end", center: "", end: "span-block-start" }[
          p.align
        ]
      : { start: "span-y-end", center: "", end: "span-y-start" }[p.align]
  const toward = {
    top: "margin-bottom",
    bottom: "margin-top",
    left: "margin-right",
    right: "margin-left",
    "inline-start": "margin-inline-end",
    "inline-end": "margin-inline-start",
  }[p.side]
  const across = block
    ? p.align === "end"
      ? "margin-inline-end"
      : "margin-inline-start"
    : p.align === "end"
      ? "margin-block-end"
      : "margin-block-start"
  const edge = { start: "0%", center: "50%", end: "100%" }[p.align]
  const origin = {
    top: `${edge} 100%`,
    bottom: `${edge} 0%`,
    left: `100% ${edge}`,
    right: `0% ${edge}`,
    "inline-start": `100% ${edge}`,
    "inline-end": `0% ${edge}`,
  }[p.side]
  return {
    "position-anchor": anchor,
    "position-area": [p.side, span].filter(Boolean).join(" "),
    "position-try-fallbacks": block ? "flip-block" : "flip-inline",
    [toward]: `${p.sideOffset}px`,
    // A centered popup is centered with its margins, so a start margin of
    // twice the offset shifts it by the offset: toward the end of the line
    // (mirrored in right-to-left text, like Base UI) or down.
    ...(p.alignOffset === 0
      ? {}
      : p.align === "center"
        ? {
            [block ? "margin-inline-start" : "margin-block-start"]:
              `${2 * p.alignOffset}px`,
          }
        : { [across]: `${p.alignOffset}px` }),
    "--transform-origin": origin,
    // Base UI's size variables for popup classes (`w-(--anchor-width)`, ...).
    "--anchor-width": "anchor-size(width)",
    "--anchor-height": "anchor-size(height)",
    "--available-width": "100%",
    "--available-height": "100%",
  }
}

/** Adds `extra` declarations to a string or object `style` prop (the prop wins). */
function withStyle(
  style: string | JSX.CSSProperties | undefined,
  extra: Record<string, string>
): string | JSX.CSSProperties {
  if (typeof style === "string") {
    const css = Object.entries(extra).map(([key, value]) => `${key}:${value}`)
    return [...css, style].join(";")
  }
  return { ...extra, ...style }
}

function TooltipProvider({ delay = 0, ...props }: HoverRootProps) {
  return (
    <HoverProviderElement
      data-slot="tooltip-provider"
      delay={delay}
      {...props}
    />
  )
}

function Tooltip({ ...props }: HoverRootProps) {
  return <HoverRootElement data-slot="tooltip" {...props} />
}

function TooltipTrigger({ ...props }: ComponentProps<"button", RenderProp>) {
  return <TooltipTriggerElement data-slot="tooltip-trigger" {...props} />
}

function TooltipContent({
  class: className,
  side = "top",
  sideOffset = 4,
  align = "center",
  alignOffset = 0,
  children,
  ...props
}: ComponentProps<"div"> &
  Pick<
    AnchorPositionerProps,
    "align" | "alignOffset" | "side" | "sideOffset"
  >) {
  return (
    <HoverPortalElement>
      <AnchorPositioner
        align={align}
        alignOffset={alignOffset}
        side={side}
        sideOffset={sideOffset}
        class="isolate z-50"
      >
        <TooltipPopupElement
          data-slot="tooltip-content"
          class={cn(
            "m-auto supports-[position-area:bottom]:m-0",
            "z-50 inline-flex w-fit max-w-xs origin-(--transform-origin) items-center gap-1.5 rounded-md bg-foreground px-3 py-1.5 text-xs text-background has-data-[slot=kbd]:pr-1.5 data-[side=bottom]:slide-in-from-top-2 data-[side=inline-end]:slide-in-from-left-2 data-[side=inline-start]:slide-in-from-right-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 **:data-[slot=kbd]:relative **:data-[slot=kbd]:isolate **:data-[slot=kbd]:z-50 **:data-[slot=kbd]:rounded-sm data-[state=delayed-open]:animate-in data-[state=delayed-open]:fade-in-0 data-[state=delayed-open]:zoom-in-95 open:animate-in open:fade-in-0 open:zoom-in-95 not-open:animate-out not-open:fade-out-0 not-open:zoom-out-95 not-open:hidden transition-[display,overlay] transition-discrete",
            className
          )}
          {...props}
        >
          {children}
          <HoverArrowElement class="z-50 size-2.5 translate-y-[calc(-50%-2px)] rotate-45 rounded-[2px] bg-foreground fill-foreground data-[side=bottom]:top-1 data-[side=inline-end]:top-1/2! data-[side=inline-end]:-left-1 data-[side=inline-end]:-translate-y-1/2 data-[side=inline-start]:top-1/2! data-[side=inline-start]:-right-1 data-[side=inline-start]:-translate-y-1/2 data-[side=left]:top-1/2! data-[side=left]:-right-1 data-[side=left]:-translate-y-1/2 data-[side=right]:top-1/2! data-[side=right]:-left-1 data-[side=right]:-translate-y-1/2 data-[side=top]:-bottom-2.5" />
        </TooltipPopupElement>
      </AnchorPositioner>
    </HoverPortalElement>
  )
}

export { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger }
