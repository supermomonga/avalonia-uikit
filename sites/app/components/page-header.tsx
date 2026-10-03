import { cn } from "cn"
import type { Child } from "hono/jsx"

type Props = { class?: string; children?: Child }

export function PageHeader({ class: className, children }: Props) {
  return (
    <section class={cn("border-grid", className)}>
      <div class="container-wrapper">
        <div class="container flex flex-col items-center gap-2 px-6 py-8 text-center md:py-16 lg:py-20 xl:gap-4">
          {children}
        </div>
      </div>
    </section>
  )
}

export function PageHeaderHeading({ class: className, children }: Props) {
  return (
    <h1
      class={cn(
        "leading-tighter max-w-3xl text-3xl font-semibold tracking-tight text-balance text-primary lg:leading-[1.1] lg:font-semibold xl:text-5xl xl:tracking-tighter",
        className
      )}
    >
      {children}
    </h1>
  )
}

export function PageHeaderDescription({ class: className, children }: Props) {
  return (
    <p
      class={cn(
        "max-w-4xl text-base text-balance text-foreground sm:text-lg",
        className
      )}
    >
      {children}
    </p>
  )
}

export function PageActions({ class: className, children }: Props) {
  return (
    <div
      class={cn(
        "flex w-full items-center justify-center gap-2 pt-2 **:data-[slot=button]:shadow-none",
        className
      )}
    >
      {children}
    </div>
  )
}
