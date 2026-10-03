import { cn } from "cn"
import { Button } from "@/components/ui/button"
import { findComponent } from "@/lib/catalog"
import { demoSource } from "@/lib/demos"
import { highlight } from "@/lib/highlight"
import { previewSize, previewUrls, wasmBase } from "@/lib/previews"
import { CodeBlock } from "./code-block"

/** Lines of XAML shown before the code is collapsed behind "View Code". */
const COLLAPSE_AFTER = 12

/**
 * A demo of a docs page: its preview image, which app/avalonia-demo.ts
 * replaces with the live control once the .NET runtime is up, and the XAML
 * of samples/AvaloniaUIKit.Demos as the code example (docs/site.md).
 */
export async function Demo({
  name,
  title,
  class: className,
}: {
  /** The demo id, `<component-slug>/<name-slug>`. */
  name: string
  title?: string
  class?: string
}) {
  const source = demoSource(name)
  const size = previewSize(name)
  const entry = findComponent(name.split("/")[0])
  const urls = previewUrls(name)
  const alt = `${entry?.title ?? name} demo${title ? `: ${title}` : ""}`
  return (
    <figure
      data-slot="demo"
      data-not-typeset=""
      class={cn(
        "group relative mt-4 mb-12 flex flex-col overflow-hidden rounded-2xl border",
        className
      )}
    >
      {title && (
        <figcaption class="border-b px-4 py-2 text-sm font-medium">
          {title}
        </figcaption>
      )}
      <div
        data-demo-frame=""
        class="relative flex min-h-48 w-full items-center justify-center p-10"
      >
        {size ? (
          <>
            <span
              data-demo-badge=""
              class="absolute top-3 right-3 rounded-md border bg-background px-1.5 py-0.5 text-[0.7rem] font-medium text-muted-foreground empty:hidden"
            >
              {wasmBase ? "Interactive" : ""}
            </span>
            <avalonia-demo
              demo={name}
              width={String(size.width)}
              height={String(size.height)}
              scroll={entry?.scroll ? "" : undefined}
              data-state="idle"
              data-wasm-base={wasmBase}
              style={`width:${size.width}px`}
            >
              <img
                src={urls.light}
                alt={alt}
                width={size.width}
                height={size.height}
                loading="lazy"
                decoding="async"
                class="dark:hidden"
              />
              <img
                src={urls.dark}
                alt={alt}
                width={size.width}
                height={size.height}
                loading="lazy"
                decoding="async"
                class="hidden dark:block"
              />
            </avalonia-demo>
          </>
        ) : (
          <p class="text-sm text-muted-foreground">Preview not generated yet</p>
        )}
      </div>
      {source && (
        <div
          data-slot="code"
          data-open={
            source.split("\n").length <= COLLAPSE_AFTER ? "" : undefined
          }
          class="group/code relative overflow-hidden [&_[data-code-figure]]:m-0! [&_[data-code-figure]]:rounded-none [&_[data-code-figure]]:border-t [&_pre]:max-h-72 data-[open]:[&_pre]:max-h-none"
        >
          <CodeBlock
            html={await highlight(source, "xml")}
            raw={source}
            language="xml"
          />
          <div class="absolute inset-x-0 bottom-0 flex h-24 items-end justify-center pb-4 group-data-[open]/code:hidden">
            <div
              class="absolute inset-0"
              style={{
                background:
                  "linear-gradient(to top, var(--color-code), color-mix(in oklab, var(--color-code) 60%, transparent), transparent)",
              }}
            />
            <Button
              size="sm"
              variant="outline"
              class="relative z-10 rounded-lg bg-background text-foreground shadow-none hover:bg-muted"
              data-view-code=""
            >
              View Code
            </Button>
          </div>
        </div>
      )}
    </figure>
  )
}
