// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/sheet (https://ui.shadcn.com/r/styles/base-nova/sheet.json)
// upstream-revision: sha256:51102b31cae673de5682e63b8a0e35c8ec421f9e93010634ceb9ac41c5c41efd; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
// mode: generated
// Derived from shadcn/ui. Copyright (c) 2023 shadcn.
// Original additions and modifications: Copyright (c) 2026 supermomonga.
// SPDX-License-Identifier: MIT
// Full license: LICENSE-shadcnui-hono-jsx.txt at the project root.
// Icons: lucide@1.48.0 (x). Copyright (c) 2026 Lucide Icons and Contributors. ISC License (see LICENSE-shadcnui-hono-jsx.txt).

import { cn } from "cn"
import type { Child, JSX, JSXNode } from "hono/jsx"
import {
  cloneElement,
  createContext,
  isValidElement,
  useContext,
  useId,
} from "hono/jsx"

import { Button } from "./button"

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

type DialogRootProps = { id?: string | undefined; children?: Child }

interface DialogContextValue {
  id: string
  titleId: string
  descriptionId: string
}

const DialogContext = createContext<DialogContextValue | null>(null)

function useDialogContext(): DialogContextValue {
  const context = useContext(DialogContext)
  if (!context) throw new Error("Sheet parts must be rendered inside <Sheet>")
  return context
}

/**
 * Connects the trigger, the native `<dialog>`, its title and description.
 * Opening and closing use Invoker Commands, so no JavaScript is shipped.
 */
function DialogRootElement({ id, children }: DialogRootProps) {
  const generated = useId()
  const dialogId = id ?? `dialog${generated.replaceAll(":", "-")}`
  return (
    <DialogContext.Provider
      value={{
        id: dialogId,
        titleId: `${dialogId}-title`,
        descriptionId: `${dialogId}-description`,
      }}
    >
      {children}
    </DialogContext.Provider>
  )
}

function DialogTriggerElement({
  type = "button",
  render,
  ...props
}: ComponentProps<"button", RenderProp>) {
  const { id } = useDialogContext()
  return renderElement(
    <button
      type={type}
      command="show-modal"
      commandfor={id}
      aria-haspopup="dialog"
      {...props}
    />,
    render
  )
}

/** The native dialog renders in the top layer, so no portal is needed. */
function DialogPortalElement({ children }: { children?: Child }) {
  return <>{children}</>
}

function DialogPopupElement(props: ComponentProps<"dialog">) {
  const { id, titleId, descriptionId } = useDialogContext()
  return (
    <dialog
      id={id}
      closedby="any"
      aria-labelledby={titleId}
      aria-describedby={descriptionId}
      {...props}
    />
  )
}

function DialogTitleElement(props: ComponentProps<"h2">) {
  return <h2 id={useContext(DialogContext)?.titleId} {...props} />
}

function DialogDescriptionElement(props: ComponentProps<"p">) {
  return <p id={useContext(DialogContext)?.descriptionId} {...props} />
}

function DialogCloseElement({
  type = "button",
  render,
  ...props
}: ComponentProps<"button", RenderProp>) {
  const { id } = useDialogContext()
  return renderElement(
    <button type={type} command="close" commandfor={id} {...props} />,
    render
  )
}

function Sheet({ ...props }: DialogRootProps) {
  return <DialogRootElement data-slot="sheet" {...props} />
}

function SheetTrigger({ ...props }: ComponentProps<"button", RenderProp>) {
  return <DialogTriggerElement data-slot="sheet-trigger" {...props} />
}

function SheetClose({ ...props }: ComponentProps<"button", RenderProp>) {
  return <DialogCloseElement data-slot="sheet-close" {...props} />
}

function SheetPortal({ ...props }: { children?: Child }) {
  return <DialogPortalElement data-slot="sheet-portal" {...props} />
}

function SheetOverlay(_props: ComponentProps<"div">) {
  return null
}

function SheetContent({
  class: className,
  children,
  side = "right",
  showCloseButton = true,
  ...props
}: ComponentProps<"dialog"> & {
  side?: "top" | "right" | "bottom" | "left"
  showCloseButton?: boolean
}) {
  return (
    <SheetPortal>
      <SheetOverlay />
      <DialogPopupElement
        data-slot="sheet-content"
        data-side={side}
        class={cn(
          "inset-auto h-auto max-h-none w-auto max-w-none overflow-visible",
          "backdrop:bg-black/10 backdrop:transition-opacity backdrop:duration-150 not-open:backdrop:opacity-0 starting:backdrop:opacity-0 supports-backdrop-filter:backdrop:backdrop-blur-xs",
          "fixed z-50 flex flex-col gap-4 bg-popover bg-clip-padding text-sm text-popover-foreground shadow-lg transition duration-200 ease-in-out not-open:opacity-0 starting:opacity-0 data-[side=bottom]:inset-x-0 data-[side=bottom]:bottom-0 data-[side=bottom]:h-auto data-[side=bottom]:border-t data-[side=bottom]:not-open:translate-y-[2.5rem] data-[side=bottom]:starting:translate-y-[2.5rem] data-[side=left]:inset-y-0 data-[side=left]:left-0 data-[side=left]:h-full data-[side=left]:w-3/4 data-[side=left]:border-r data-[side=left]:not-open:translate-x-[-2.5rem] data-[side=left]:starting:translate-x-[-2.5rem] data-[side=right]:inset-y-0 data-[side=right]:right-0 data-[side=right]:h-full data-[side=right]:w-3/4 data-[side=right]:border-l data-[side=right]:not-open:translate-x-[2.5rem] data-[side=right]:starting:translate-x-[2.5rem] data-[side=top]:inset-x-0 data-[side=top]:top-0 data-[side=top]:h-auto data-[side=top]:border-b data-[side=top]:not-open:translate-y-[-2.5rem] data-[side=top]:starting:translate-y-[-2.5rem] data-[side=left]:sm:max-w-sm data-[side=right]:sm:max-w-sm not-open:hidden transition-discrete",
          className
        )}
        {...props}
      >
        {children}
        {showCloseButton && (
          <DialogCloseElement
            data-slot="sheet-close"
            render={
              <Button
                variant="ghost"
                class="absolute top-3 right-3"
                size="icon-sm"
              />
            }
          >
            <XIcon />
            <span class="sr-only">Close</span>
          </DialogCloseElement>
        )}
      </DialogPopupElement>
    </SheetPortal>
  )
}

function SheetHeader({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sheet-header"
      class={cn("flex flex-col gap-0.5 p-4", className)}
      {...props}
    />
  )
}

function SheetFooter({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="sheet-footer"
      class={cn("mt-auto flex flex-col gap-2 p-4", className)}
      {...props}
    />
  )
}

function SheetTitle({ class: className, ...props }: ComponentProps<"h2">) {
  return (
    <DialogTitleElement
      data-slot="sheet-title"
      class={cn(
        "font-heading text-base font-medium text-foreground",
        className
      )}
      {...props}
    />
  )
}

function SheetDescription({ class: className, ...props }: ComponentProps<"p">) {
  return (
    <DialogDescriptionElement
      data-slot="sheet-description"
      class={cn("text-sm text-muted-foreground", className)}
      {...props}
    />
  )
}
/** Like lucide-react, icons are hidden from assistive technology unless labelled. */
function hasA11yProp(props: object): boolean {
  return Object.keys(props).some(
    (key) => key.startsWith("aria-") || key === "role" || key === "title"
  )
}

/** Lucide `x` icon, inlined (ISC License, see LICENSE-shadcnui-hono-jsx.txt). */
function XIcon({
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
      class={cn("lucide lucide-x", className)}
      aria-hidden={children || hasA11yProp(props) ? undefined : "true"}
      {...props}
    >
      <path d="M18 6 6 18" />
      <path d="m6 6 12 12" />
      {children}
    </svg>
  )
}

export {
  Sheet,
  SheetClose,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
}
