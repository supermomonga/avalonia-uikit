// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/popover (https://ui.shadcn.com/r/styles/base-nova/popover.json)
// upstream-revision: sha256:3c2d5214fbdae3e5d40444df7e18b8810c3a592bfefbdbfdc60abcec0b7fbd46; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
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
type PopoverRootProps = { id?: string | undefined; children?: Child }

interface PopoverContextValue {
  id: string
  anchor: string
  titleId: string
  descriptionId: string
}

const PopoverContext = createContext<PopoverContextValue | null>(null)

function usePopoverContext(): PopoverContextValue {
  const context = useContext(PopoverContext)
  if (!context)
    throw new Error("Popover parts must be rendered inside a popover")
  return context
}

/** Connects the trigger (the anchor), the native popover and its title and description. */
function PopoverRootElement({ id, children }: PopoverRootProps) {
  const generated = useId().replaceAll(":", "-")
  const popoverId = id ?? `popover${generated}`
  return (
    <PopoverContext.Provider
      value={{
        id: popoverId,
        anchor: `--${popoverId}`,
        titleId: `${popoverId}-title`,
        descriptionId: `${popoverId}-description`,
      }}
    >
      {children}
    </PopoverContext.Provider>
  )
}

function PopoverTriggerElement({
  type = "button",
  render,
  style,
  ...props
}: ComponentProps<"button", RenderProp>) {
  const { id, anchor } = usePopoverContext()
  return renderElement(
    <button
      type={type}
      command="toggle-popover"
      commandfor={id}
      aria-haspopup="dialog"
      style={withStyle(style, { "anchor-name": anchor })}
      {...props}
    />,
    render
  )
}

/** The native popover renders in the top layer, so no portal is needed. */
function PopoverPortalElement({ children }: { children?: Child }) {
  return <>{children}</>
}

function PopoverPopupElement({ style, ...props }: ComponentProps<"div">) {
  const { id, anchor, titleId, descriptionId } = usePopoverContext()
  const placement = useContext(AnchorPlacementContext)
  return (
    <div
      id={id}
      popover="auto"
      role="dialog"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      data-side={placement.side}
      data-align={placement.align}
      style={withStyle(style, anchorPlacementStyle(anchor, placement))}
      {...props}
    />
  )
}

function PopoverTitleElement(props: ComponentProps<"h2">) {
  return <h2 id={useContext(PopoverContext)?.titleId} {...props} />
}

function PopoverDescriptionElement(props: ComponentProps<"p">) {
  return <p id={useContext(PopoverContext)?.descriptionId} {...props} />
}

function Popover({ ...props }: PopoverRootProps) {
  return <PopoverRootElement data-slot="popover" {...props} />
}

function PopoverTrigger({ ...props }: ComponentProps<"button", RenderProp>) {
  return <PopoverTriggerElement data-slot="popover-trigger" {...props} />
}

function PopoverContent({
  class: className,
  align = "center",
  alignOffset = 0,
  side = "bottom",
  sideOffset = 4,
  ...props
}: ComponentProps<"div"> &
  Pick<
    AnchorPositionerProps,
    "align" | "alignOffset" | "side" | "sideOffset"
  >) {
  return (
    <PopoverPortalElement>
      <AnchorPositioner
        align={align}
        alignOffset={alignOffset}
        side={side}
        sideOffset={sideOffset}
        class="isolate z-50"
      >
        <PopoverPopupElement
          data-slot="popover-content"
          class={cn(
            "m-auto supports-[position-area:bottom]:m-0",
            "z-50 flex w-72 origin-(--transform-origin) flex-col gap-2.5 rounded-lg bg-popover p-2.5 text-sm text-popover-foreground shadow-md ring-1 ring-foreground/10 outline-hidden duration-100 data-[side=bottom]:slide-in-from-top-2 data-[side=inline-end]:slide-in-from-left-2 data-[side=inline-start]:slide-in-from-right-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 open:animate-in open:fade-in-0 open:zoom-in-95 not-open:animate-out not-open:fade-out-0 not-open:zoom-out-95 not-open:hidden transition-[display,overlay] transition-discrete",
            className
          )}
          {...props}
        />
      </AnchorPositioner>
    </PopoverPortalElement>
  )
}

function PopoverHeader({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="popover-header"
      class={cn("flex flex-col gap-0.5 text-sm", className)}
      {...props}
    />
  )
}

function PopoverTitle({ class: className, ...props }: ComponentProps<"h2">) {
  return (
    <PopoverTitleElement
      data-slot="popover-title"
      class={cn("font-medium", className)}
      {...props}
    />
  )
}

function PopoverDescription({
  class: className,
  ...props
}: ComponentProps<"p">) {
  return (
    <PopoverDescriptionElement
      data-slot="popover-description"
      class={cn("text-muted-foreground", className)}
      {...props}
    />
  )
}

export {
  Popover,
  PopoverContent,
  PopoverDescription,
  PopoverHeader,
  PopoverTitle,
  PopoverTrigger,
}
