// Installed by shadcnui-hono-jsx for the shadcn/ui preset b2fA; `shadcnui-hono-jsx apply` rewrites this file.
// upstream: shadcn/ui base-nova/dialog (https://ui.shadcn.com/r/styles/base-nova/dialog.json)
// upstream-revision: sha256:356e4775f1edfd051ace0678b6a23bffbce042045f470971755438a7c199dafe; shadcn-ui/ui@98a1fe67b439324ddc857f47fbdce056600a4329
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
  if (!context) throw new Error("Dialog parts must be rendered inside <Dialog>")
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

function Dialog({ ...props }: DialogRootProps) {
  return <DialogRootElement data-slot="dialog" {...props} />
}

function DialogTrigger({ ...props }: ComponentProps<"button", RenderProp>) {
  return <DialogTriggerElement data-slot="dialog-trigger" {...props} />
}

function DialogPortal({ ...props }: { children?: Child }) {
  return <DialogPortalElement data-slot="dialog-portal" {...props} />
}

function DialogClose({ ...props }: ComponentProps<"button", RenderProp>) {
  return <DialogCloseElement data-slot="dialog-close" {...props} />
}

function DialogOverlay(_props: ComponentProps<"div">) {
  return null
}

function DialogContent({
  class: className,
  children,
  showCloseButton = true,
  ...props
}: ComponentProps<"dialog"> & {
  showCloseButton?: boolean
}) {
  return (
    <DialogPortal>
      <DialogOverlay />
      <DialogPopupElement
        data-slot="dialog-content"
        class={cn(
          "inset-auto h-auto max-h-none w-auto max-w-none overflow-visible",
          "backdrop:bg-black/10 backdrop:duration-100 supports-backdrop-filter:backdrop:backdrop-blur-xs open:backdrop:animate-in open:backdrop:fade-in-0 not-open:backdrop:animate-out not-open:backdrop:fade-out-0",
          "fixed top-1/2 left-1/2 z-50 grid w-full max-w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2 gap-4 rounded-xl bg-popover p-4 text-sm text-popover-foreground ring-1 ring-foreground/10 duration-100 outline-none sm:max-w-sm open:animate-in open:fade-in-0 open:zoom-in-95 not-open:animate-out not-open:fade-out-0 not-open:zoom-out-95 not-open:hidden transition-[display,overlay] transition-discrete",
          className
        )}
        {...props}
      >
        {children}
        {showCloseButton && (
          <DialogCloseElement
            data-slot="dialog-close"
            render={
              <Button
                variant="ghost"
                class="absolute top-2 right-2"
                size="icon-sm"
              />
            }
          >
            <XIcon />
            <span class="sr-only">Close</span>
          </DialogCloseElement>
        )}
      </DialogPopupElement>
    </DialogPortal>
  )
}

function DialogHeader({ class: className, ...props }: ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-header"
      class={cn("flex flex-col gap-2", className)}
      {...props}
    />
  )
}

function DialogFooter({
  class: className,
  showCloseButton = false,
  children,
  ...props
}: ComponentProps<"div"> & {
  showCloseButton?: boolean
}) {
  return (
    <div
      data-slot="dialog-footer"
      class={cn(
        "-mx-4 -mb-4 flex flex-col-reverse gap-2 rounded-b-xl border-t bg-muted/50 p-4 sm:flex-row sm:justify-end",
        className
      )}
      {...props}
    >
      {children}
      {showCloseButton && (
        <DialogCloseElement render={<Button variant="outline" />}>
          Close
        </DialogCloseElement>
      )}
    </div>
  )
}

function DialogTitle({ class: className, ...props }: ComponentProps<"h2">) {
  return (
    <DialogTitleElement
      data-slot="dialog-title"
      class={cn("font-heading text-base leading-none font-medium", className)}
      {...props}
    />
  )
}

function DialogDescription({
  class: className,
  ...props
}: ComponentProps<"p">) {
  return (
    <DialogDescriptionElement
      data-slot="dialog-description"
      class={cn(
        "text-sm text-muted-foreground *:[a]:underline *:[a]:underline-offset-3 *:[a]:hover:text-foreground",
        className
      )}
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
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
  DialogTrigger,
}
