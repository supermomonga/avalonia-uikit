// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/separator (https://ui.shadcn.com/r/styles/base-nova/separator.json)
// upstream-revision: sha256:e331ea0c4e835484b0a3fcefa172a884864bdc0e1bfe39d12c119c66ad4e3195; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.

import { cn } from "cn"
import type { Child, JSX, JSXNode } from "hono/jsx"
import { cloneElement, isValidElement } from "hono/jsx"

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

function Separator({
  class: className,
  orientation = "horizontal",
  render,
  ...props
}: ComponentProps<"div", RenderProp> & {
  orientation?: "horizontal" | "vertical" | undefined
}) {
  return renderElement(
    <div
      data-slot="separator"
      aria-orientation={orientation}
      data-orientation={orientation}
      class={cn(
        "shrink-0 bg-border data-horizontal:h-px data-horizontal:w-full data-vertical:w-px data-vertical:self-stretch",
        className
      )}
      role="separator"
      {...props}
    />,
    render
  )
}

export { Separator }
