import { createRoute } from "honox/factory"
import {
  PageActions,
  PageHeader,
  PageHeaderDescription,
  PageHeaderHeading,
} from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { components, findComponent } from "@/lib/catalog"
import { previewSize, previewUrls } from "@/lib/previews"
import { siteConfig } from "@/lib/site"

const title = "Nova-style themes and controls for Avalonia"

/** Representative components on the home page, in order. */
const featured = [
  "button",
  "tabs",
  "calendar",
  "form",
  "alert",
  "data-table",
  "slider",
  "avatar",
]

const features = [
  {
    title: "Verified pixel by pixel",
    text: "Every theme is compared with reference renders: Light and Dark, each variant, size and state, and the motion frame by frame.",
  },
  {
    title: `${components.length} components`,
    text: "Themes for Avalonia's own controls, plus small new controls (Badge, Tag, Alert, Avatar, Stepper and more) for what Avalonia lacks.",
  },
  {
    title: "NativeAOT ready",
    text: "Compiled bindings, no reflection. A NativeAOT smoke test renders every control in both themes on each change.",
  },
  {
    title: "Light and dark",
    text: "Switch with RequestedThemeVariant. Every color token has a light and a dark value.",
  },
  {
    title: "One line to install",
    text: "Add <uikit:NovaTheme /> to App.axaml. It works on its own, or layered on top of FluentTheme for the controls it does not cover.",
  },
  {
    title: "Your font, not ours",
    text: "The theme uses the platform's UI font by default. Set UIKit.FontFamily to Inter to match the demos on this site.",
  },
]

function FeaturedPreview({ slug }: { slug: string }) {
  const entry = findComponent(slug)
  if (!entry) return null
  const id = `${slug}/demo`
  const size = previewSize(id)
  const urls = previewUrls(id)
  return (
    <a
      href={`/docs/components/${slug}`}
      class="group flex flex-col overflow-hidden rounded-2xl border bg-background transition-colors hover:bg-muted/40"
    >
      <div class="flex aspect-[4/2.5] items-center justify-center overflow-hidden bg-surface p-6">
        {size ? (
          <>
            <img
              src={urls.light}
              alt={`${entry.title} rendered by Avalonia UIKit`}
              width={size.width}
              height={size.height}
              loading="lazy"
              decoding="async"
              class="max-h-full w-auto max-w-full object-contain dark:hidden"
            />
            <img
              src={urls.dark}
              alt={`${entry.title} rendered by Avalonia UIKit`}
              width={size.width}
              height={size.height}
              loading="lazy"
              decoding="async"
              class="hidden max-h-full w-auto max-w-full object-contain dark:block"
            />
          </>
        ) : (
          <span class="text-lg font-medium text-muted-foreground">
            {entry.title}
          </span>
        )}
      </div>
      <div class="flex items-center justify-between border-t px-4 py-3 text-sm">
        <span class="font-medium">{entry.title}</span>
        <span class="text-muted-foreground">{entry.avalonia[0]}</span>
      </div>
    </a>
  )
}

export default createRoute((c) =>
  c.render(
    <div class="flex flex-1 flex-col">
      <PageHeader class="md:**:[.container]:pb-8 lg:**:[.container]:pb-12">
        <PageHeaderHeading class="max-w-4xl">{title}</PageHeaderHeading>
        <PageHeaderDescription>{siteConfig.description}</PageHeaderDescription>
        <PageActions>
          <Button class="h-[35px]" render={<a href="/docs/installation" />}>
            Get Started
          </Button>
          <Button variant="secondary" render={<a href="/docs/components" />}>
            Components
          </Button>
        </PageActions>
      </PageHeader>
      <div class="container-wrapper flex-1 p-0">
        <div class="container px-4 pb-16 lg:px-8">
          <section class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {featured.map((slug) => (
              <FeaturedPreview slug={slug} />
            ))}
          </section>
          <section class="mt-16 grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((feature) => (
              <div class="flex flex-col gap-2">
                <h2 class="text-base font-semibold tracking-tight">
                  {feature.title}
                </h2>
                <p class="text-sm leading-relaxed text-muted-foreground">
                  {feature.text}
                </p>
              </div>
            ))}
          </section>
        </div>
      </div>
    </div>,
    { description: siteConfig.description }
  )
)
