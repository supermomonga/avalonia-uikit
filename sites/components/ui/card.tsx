// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/card (https://ui.shadcn.com/r/styles/base-nova/card.json)
// upstream-revision: sha256:766d3bb130e4a7b7ff3db8720a523330b9a0a6637043eb39bbde43a8691e17ba; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
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

function Card({
  class: className,
  size = "default",
  ...props
}: ComponentProps<"div"> & { size?: "default" | "sm" }) {
  return (
    <div
      data-slot="card"
      data-size={size}
      class={cn(
        "group/card flex flex-col gap-(--card-spacing) overflow-hidden rounded-xl bg-card py-(--card-spacing) text-sm text-card-foreground ring-1 ring-foreground/10 [--card-spacing:--spacing(4)] has-data-[slot=card-footer]:pb-0 has-[>img:first-child]:pt-0 data-[size=sm]:[--card-spacing:--spacing(3)] data-[size=sm]:has-data-[slot=card-footer]:pb-0 *:[img:first-child]:rounded-t-xl *:[img:last-child]:rounded-b-xl",
        className
      )}
      {...props}
    />
  )
}

function CardHeader({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-header"
      class={cn(
        "group/card-header @container/card-header grid auto-rows-min items-start gap-1 rounded-t-xl px-(--card-spacing) has-data-[slot=card-action]:grid-cols-[1fr_auto] has-data-[slot=card-description]:grid-rows-[auto_auto] [.border-b]:pb-(--card-spacing)",
        className
      )}
      {...props}
    />
  )
}

function CardTitle({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-title"
      class={cn(
        "font-heading text-base leading-snug font-medium group-data-[size=sm]/card:text-sm",
        className
      )}
      {...props}
    />
  )
}

function CardDescription({
  class: className,
  ...props
}: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-description"
      class={cn("text-sm text-muted-foreground", className)}
      {...props}
    />
  )
}

function CardAction({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-action"
      class={cn(
        "col-start-2 row-span-2 row-start-1 self-start justify-self-end",
        className
      )}
      {...props}
    />
  )
}

function CardContent({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-content"
      class={cn("px-(--card-spacing)", className)}
      {...props}
    />
  )
}

function CardFooter({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="card-footer"
      class={cn(
        "flex items-center rounded-b-xl border-t bg-muted/50 p-(--card-spacing)",
        className
      )}
      {...props}
    />
  )
}

export {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
}
