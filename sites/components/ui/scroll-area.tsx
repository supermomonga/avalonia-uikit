// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/scroll-area (https://ui.shadcn.com/r/styles/base-nova/scroll-area.json)
// upstream-revision: sha256:15e804fb28218a1b0e2d3cbf31f56adec10e1d4f76cb20064c8b4bbed5f5958c; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
// client: <script type="module" src="/shadcn/scroll-area.js"> (public/shadcn/)
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.

import { cn } from "cn"
import type { JSX } from "hono/jsx"

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

/**
 * The viewport scrolls natively (with the browser's scrollbar) until the
 * client script `/shadcn/scroll-area.js` hides it and shows the custom
 * scrollbars, like Base UI.
 */
function ScrollAreaRootElement({ style, ...props }: ComponentProps<"div">) {
  return (
    <div
      role="presentation"
      data-scroll-area=""
      style={withScrollAreaStyle(style, {
        position: "relative",
        "--scroll-area-corner-height": "0px",
        "--scroll-area-corner-width": "0px",
      })}
      {...props}
    />
  )
}

function ScrollAreaViewportElement({ style, ...props }: ComponentProps<"div">) {
  return (
    <div
      role="presentation"
      data-scroll-area-viewport=""
      style={withScrollAreaStyle(style, { overflow: "auto" })}
      {...props}
    />
  )
}

type ScrollAreaScrollbarProps = ComponentProps<"div"> & {
  orientation?: "vertical" | "horizontal" | undefined
}

/** Hidden until the script finds overflow in its orientation. */
function ScrollAreaScrollbarElement({
  orientation = "vertical",
  style,
  ...props
}: ScrollAreaScrollbarProps) {
  const vertical = orientation === "vertical"
  return (
    <div
      hidden
      aria-hidden="true"
      data-orientation={orientation}
      data-scroll-area-scrollbar=""
      style={withScrollAreaStyle(style, {
        position: "absolute",
        "touch-action": "none",
        "user-select": "none",
        ...(vertical
          ? {
              top: "0",
              bottom: "var(--scroll-area-corner-height)",
              "inset-inline-end": "0",
            }
          : {
              "inset-inline-start": "0",
              "inset-inline-end": "var(--scroll-area-corner-width)",
              bottom: "0",
            }),
      })}
      {...props}
    />
  )
}

function ScrollAreaThumbElement({ style, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-scroll-area-thumb=""
      style={withScrollAreaStyle(style, {
        width: "var(--scroll-area-thumb-width)",
        height: "var(--scroll-area-thumb-height)",
      })}
      {...props}
    />
  )
}

function ScrollAreaCornerElement({ style, ...props }: ComponentProps<"div">) {
  return (
    <div
      hidden
      data-scroll-area-corner=""
      style={withScrollAreaStyle(style, {
        position: "absolute",
        bottom: "0",
        "inset-inline-end": "0",
        width: "var(--scroll-area-corner-width)",
        height: "var(--scroll-area-corner-height)",
      })}
      {...props}
    />
  )
}

/** Adds `extra` declarations to a string or object `style` prop (the prop wins). */
function withScrollAreaStyle(
  style: string | JSX.CSSProperties | undefined,
  extra: Record<string, string>
): string | JSX.CSSProperties {
  if (typeof style === "string") {
    const css = Object.entries(extra).map(([key, value]) => `${key}:${value}`)
    return [...css, style].join(";")
  }
  return { ...extra, ...style }
}

function ScrollArea({
  class: className,
  children,
  ...props
}: ComponentProps<"div">) {
  return (
    <ScrollAreaRootElement
      data-slot="scroll-area"
      class={cn("relative", className)}
      {...props}
    >
      <ScrollAreaViewportElement
        data-slot="scroll-area-viewport"
        class="size-full rounded-[inherit] transition-[color,box-shadow] outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-1"
      >
        {children}
      </ScrollAreaViewportElement>
      <ScrollBar />
      <ScrollAreaCornerElement />
    </ScrollAreaRootElement>
  )
}

function ScrollBar({
  class: className,
  orientation = "vertical",
  ...props
}: ScrollAreaScrollbarProps) {
  return (
    <ScrollAreaScrollbarElement
      data-slot="scroll-area-scrollbar"
      data-orientation={orientation}
      orientation={orientation}
      class={cn(
        "flex touch-none p-px transition-colors select-none data-horizontal:h-2.5 data-horizontal:flex-col data-horizontal:border-t data-horizontal:border-t-transparent data-vertical:h-full data-vertical:w-2.5 data-vertical:border-l data-vertical:border-l-transparent",
        className
      )}
      {...props}
    >
      <ScrollAreaThumbElement
        data-slot="scroll-area-thumb"
        class="relative flex-1 rounded-full bg-border"
      />
    </ScrollAreaScrollbarElement>
  )
}

export { ScrollArea, ScrollBar }
