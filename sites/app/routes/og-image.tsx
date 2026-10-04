import { disableSSG } from "hono/ssg"
import { createRoute } from "honox/factory"
import { Logo } from "@/components/site-header"
import { components } from "@/lib/catalog"
import { previewSize, previewUrls } from "@/lib/previews"
import { siteConfig } from "@/lib/site"

/** A demo's light preview at its logical size. */
function Preview({ id, class: className }: { id: string; class?: string }) {
  const size = previewSize(id)
  if (!size) return null
  return (
    <img
      src={previewUrls(id).light}
      alt=""
      width={size.width}
      height={size.height}
      class={`max-w-none shrink-0 ${className ?? ""}`}
      style={{ width: `${size.width}px`, height: `${size.height}px` }}
    />
  )
}

/**
 * The social image (public/og.png), as gpui-kit.com's: the mark and the name,
 * the tagline over a hairline and a mono line of facts, here beside a window
 * of real controls (the demos' previews). scripts/images.ts screenshots this
 * page at 1200×630, twice the pixels; it is never built.
 */
export default createRoute(disableSSG(), (c) =>
  c.render(
    <div class="relative h-[630px] w-[1200px] overflow-hidden bg-background">
      <div
        class="blueprint absolute inset-0"
        style={{
          "--grid-size": "56px",
          maskImage:
            "radial-gradient(120% 110% at 0% 0%, black 25%, transparent 78%)",
        }}
      />
      <div class="absolute top-0 left-[72px] flex h-full w-[540px] flex-col justify-center">
        <div class="flex items-center gap-4">
          <Logo class="size-[60px]" />
          <span class="text-[60px] leading-none font-[660] tracking-[-0.045em]">
            {siteConfig.name}
          </span>
        </div>
        <p class="mt-8 max-w-[500px] text-[38px] leading-[1.12] font-[620] tracking-[-0.035em] text-balance">
          {siteConfig.tagline}
        </p>
        <p class="mt-5 max-w-[470px] text-[19px] leading-[1.55] text-muted-foreground">
          Themes and controls in the Nova style of shadcn/ui, verified pixel by
          pixel in light and dark.
        </p>
        <div class="mt-9 h-px w-full bg-border" />
        <div class="mt-5 flex items-center justify-between font-mono text-[15px] text-muted-foreground">
          <span>
            <strong class="font-semibold text-foreground">
              {components.length}
            </strong>{" "}
            components · NativeAOT
          </span>
          <span>avalonia-uikit.omofla.sh</span>
        </div>
      </div>
      <div class="mac-window absolute top-[64px] left-[664px] w-[620px]">
        <div class="mac-window__bar">
          <span class="mac-window__light bg-[#ff5f57]" />
          <span class="mac-window__light bg-[#febc2e]" />
          <span class="mac-window__light bg-[#28c840]" />
          <span class="mac-window__title">Avalonia UIKit — Demos</span>
        </div>
        <div class="flex h-[560px] flex-col gap-7 p-8">
          <Preview id="button/demo" />
          <div class="flex gap-9">
            <Preview id="form/demo" />
            <div class="flex flex-col gap-6">
              <Preview id="switch/demo" />
              <Preview id="checkbox/demo" />
            </div>
          </div>
          <Preview id="stepper/demo" />
          <div class="flex items-center gap-8">
            <Preview id="avatar/demo" />
            <Preview id="badge/demo" />
          </div>
        </div>
      </div>
    </div>,
    { title: "Social image", bare: true }
  )
)
