import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { findComponent } from "@/lib/catalog"
import { demoSource } from "@/lib/demos"
import { highlight } from "@/lib/highlight"
import { previewSize, wasmBase } from "@/lib/previews"
import { siteConfig } from "@/lib/site"
import { CodeBlock } from "./code-block"

/** Lines of XAML shown before the code is collapsed behind "View code". */
const COLLAPSE_AFTER = 12

/** The badge's text before app/avalonia-demo.ts takes over. */
const initialBadge = wasmBase ? "Interactive" : "Unavailable"

/**
 * A skeleton the size of the demo's preview, which app/avalonia-demo.ts
 * replaces with the live control.
 */
function LiveDemo({ name }: { name: string }) {
  const size = previewSize(name)
  const entry = findComponent(name.split("/")[0])
  if (!size) {
    return <p class="text-sm text-muted-foreground">Preview not generated yet</p>
  }
  return (
    <avalonia-demo
      demo={name}
      width={String(size.width)}
      height={String(size.height)}
      scroll={entry?.scroll ? "" : undefined}
      data-state="idle"
      data-wasm-base={wasmBase}
      style={`width:${size.width}px;height:${size.height}px`}
    >
      <Skeleton class="size-full">
        <span data-demo-unavailable="">Live demo unavailable</span>
      </Skeleton>
    </avalonia-demo>
  )
}

/** The demo's XAML, collapsed behind "View code" when it is long. */
async function DemoCode({ name }: { name: string }) {
  const source = demoSource(name)
  if (!source) return <></>
  return (
    <div
      class="demo__code"
      data-slot="code"
      data-open={source.split("\n").length <= COLLAPSE_AFTER ? "" : undefined}
    >
      <CodeBlock
        html={await highlight(source, "xml")}
        raw={source}
        language="xml"
      />
      <div class="demo__expand">
        <Button
          size="sm"
          variant="outline"
          class="bg-background shadow-raise"
          data-view-code=""
        >
          View code
        </Button>
      </div>
    </div>
  )
}

/**
 * A demo of samples/AvaloniaUIKit.Demos (docs/site.md): a skeleton, which
 * becomes the live control once the .NET runtime is up, and its XAML. The
 * page's first demo (no title) is its live example, in a window as on
 * gpui-kit.com; the others are framed examples with a caption.
 */
export async function Demo({
  name,
  title,
}: {
  /** The demo id, `<component-slug>/<name-slug>`. */
  name: string
  title?: string
}) {
  const component = findComponent(name.split("/")[0])?.title ?? name
  if (!title) {
    return (
      <section class="example" data-demo-frame="" aria-label="Example">
        <div class="example__label">
          <span>Example</span>
          <span class="example__live">
            <span class="example__dot" />
            <span data-demo-badge="">{initialBadge}</span>
          </span>
        </div>
        <div class="mac-window">
          <div class="mac-window__bar">
            <span class="mac-window__light bg-[#ff5f57]" />
            <span class="mac-window__light bg-[#febc2e]" />
            <span class="mac-window__light bg-[#28c840]" />
            <span class="mac-window__title">
              {component} — {siteConfig.name}
            </span>
          </div>
          <div class="demo__stage">
            <LiveDemo name={name} />
          </div>
        </div>
        <DemoCode name={name} />
      </section>
    )
  }
  return (
    <figure class="demo" data-demo-frame="">
      <figcaption class="demo__caption">
        {title}
        <span data-demo-badge="" class="chip h-5 bg-transparent">
          {initialBadge}
        </span>
      </figcaption>
      <div class="demo__stage">
        <LiveDemo name={name} />
      </div>
      <DemoCode name={name} />
    </figure>
  )
}
