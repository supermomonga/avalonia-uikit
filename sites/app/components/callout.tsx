import { cn } from "cn"
import type { Child } from "hono/jsx"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"

export function Callout({
  title,
  icon,
  class: className,
  variant = "default",
  children,
}: {
  title?: string
  icon?: Child
  class?: string
  variant?: "default" | "info" | "warning"
  children?: Child
}) {
  return (
    <Alert
      data-variant={variant}
      data-not-typeset=""
      class={cn(
        "mt-6 w-auto border-none bg-surface text-surface-foreground md:-mx-1 **:[code]:border [&_a]:font-medium [&_a]:underline [&_a]:underline-offset-4",
        className
      )}
    >
      {icon}
      {title && <AlertTitle>{title}</AlertTitle>}
      <AlertDescription class="text-card-foreground/80">
        {children}
      </AlertDescription>
    </Alert>
  )
}

export function Steps({
  class: className,
  children,
}: {
  class?: string
  children?: Child
}) {
  return (
    <div
      class={cn(
        "steps mb-12 [counter-reset:step] md:ml-4 md:border-l md:pl-8 [&>h3]:step",
        className
      )}
    >
      {children}
    </div>
  )
}

export function Step({ children }: { children?: Child }) {
  return (
    <h3 class="font-heading mt-8 scroll-m-32 text-lg font-medium tracking-tight">
      {children}
    </h3>
  )
}
