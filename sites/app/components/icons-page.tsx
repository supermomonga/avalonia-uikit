import type { Child } from "hono/jsx"
import { Check, Copy, Search, X } from "lucide"
import { Button } from "@/components/ui/button"
import { iconColors, iconSizes, type ThemeIcon, themeIcons } from "@/lib/icons"
import { CopyButton } from "./code-block"
import { Icon } from "./icon"

export const iconsDescription = `The ${themeIcons.length} icons of IconName, as geometry for PathIcon and uikit:Icon. Pick one to see it at each size and color, and copy the XAML that draws it.`

/** Lower case letters and digits only, so `chevron-down` and `UIKit.Icon.ChevronDown` both match "chevron down". */
const searchText = (icon: ThemeIcon) =>
  [icon.name, icon.file, icon.key]
    .map((text) => text.toLowerCase().replace(/[^a-z0-9]/g, ""))
    .join(" ")

/**
 * The icon in its 24×24 box, scaled to the size in the color: a Lucide icon
 * as Lucide draws it, the strokes the theme outlines (lib/icons.ts), and
 * GPUI Kit's own icons as the theme's outline, with a two-tone icon's faint
 * part painted under it, as uikit:Icon does.
 */
function ThemeIconSvg({ icon }: { icon: ThemeIcon }) {
  if (icon.lucide) return <Icon icon={icon.lucide} />
  const outline = icon.outline!
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden="true"
    >
      {outline.faint && (
        <path
          d={outline.faint.path}
          opacity={outline.faint.opacity}
          data-faint=""
        />
      )}
      <path d={outline.path} fill-rule={outline.fillRule} />
    </svg>
  )
}

function IconTile({ icon }: { icon: ThemeIcon }) {
  return (
    <li>
      <button
        type="button"
        class="icon-tile"
        data-icon-tile={icon.name}
        data-kind={icon.kind}
        data-file={icon.file}
        data-faint-opacity={icon.outline?.faint?.opacity}
        data-generated={icon.carried ? undefined : ""}
        data-search={searchText(icon)}
        aria-pressed="false"
        title={icon.key}
      >
        <ThemeIconSvg icon={icon} />
        <span class="icon-tile__name">{icon.name}</span>
      </button>
    </li>
  )
}

/** A row of choices that act as radio buttons (app/client.ts). */
function Choice({
  group,
  value,
  checked,
  class: className,
  title,
  children,
  ...data
}: {
  group: string
  value: string
  checked: boolean
  class: string
  title?: string
  children?: Child
} & Record<`data-${string}`, string | undefined>) {
  return (
    <button
      type="button"
      role="radio"
      class={className}
      aria-checked={String(checked)}
      title={title}
      {...{ [`data-icon-${group}`]: value }}
      {...data}
    >
      {children}
    </button>
  )
}

/**
 * The picked icon, filled in by app/client.ts: its name and file, a large
 * preview, the sizes, the colors, and the XAML for the chosen size, color and
 * element, with the resource key and the IconName to copy. Beside the grid
 * from 960px, a sheet over the bottom of the window below.
 */
function IconDetail() {
  return (
    <aside
      class="icon-detail"
      data-icon-detail=""
      data-size="medium"
      data-color="Default"
      data-syntax="icon"
      aria-labelledby="icon-detail-name"
      hidden
    >
      <div class="icon-detail__header">
        <div class="min-w-0">
          <h2
            id="icon-detail-name"
            class="icon-detail__name"
            data-icon-detail-name=""
          />
          <p class="icon-detail__file" data-icon-detail-file="" />
        </div>
        <Button
          variant="ghost"
          size="icon-sm"
          class="-mr-1.5 text-muted-foreground hover:text-foreground"
          data-icon-detail-close=""
          aria-label="Close"
        >
          <Icon icon={X} />
        </Button>
      </div>

      <div class="icon-detail__stage blueprint" data-icon-preview="" />

      <section class="icon-detail__section">
        <p class="kicker">Size</p>
        <div class="icon-sizes" role="radiogroup" aria-label="Size">
          {iconSizes.map((size) => (
            <Choice
              group="size"
              value={size.name}
              checked={size.name === "medium"}
              class="icon-size"
            >
              <span
                class="icon-size__box"
                style={`--icon-px: ${size.px}px`}
                data-icon-preview=""
              />
              <span class="icon-size__name">{size.name}</span>
              <span class="icon-size__px">{size.px}px</span>
            </Choice>
          ))}
        </div>
      </section>

      <section class="icon-detail__section">
        <p class="kicker">Color</p>
        <div class="icon-colors" role="radiogroup" aria-label="Color">
          {iconColors.map((color) => (
            <Choice
              group="color"
              value={color.name}
              checked={color.name === "Default"}
              class="icon-color"
              title={color.resource ?? "The Foreground it inherits"}
              data-resource={color.resource}
              data-css={color.css}
            >
              <span class="icon-color__swatch" style={`background: ${color.css}`} />
              {color.name}
            </Choice>
          ))}
        </div>
      </section>

      <section class="icon-detail__section">
        <div class="flex items-center justify-between gap-3">
          <p class="kicker">XAML</p>
          <div class="icon-syntax" role="radiogroup" aria-label="Element">
            <Choice group="syntax" value="icon" checked class="icon-syntax__option">
              uikit:Icon
            </Choice>
            <Choice group="syntax" value="path" checked={false} class="icon-syntax__option">
              PathIcon
            </Choice>
          </div>
        </div>
        <figure class="code-block icon-detail__code">
          <pre class="shiki">
            <code data-icon-code="" />
          </pre>
        </figure>
        <Button
          variant="outline"
          size="sm"
          class="group/copy w-full"
          data-copy=""
          data-icon-code-copy=""
        >
          <Icon icon={Copy} class="group-data-copied/copy:hidden" />
          <Icon icon={Check} class="hidden group-data-copied/copy:block" />
          <span class="group-data-copied/copy:hidden">Copy XAML</span>
          <span class="hidden group-data-copied/copy:inline">Copied</span>
        </Button>
        <p class="icon-detail__note" data-icon-note="icon">
          <code>uikit</code> is{" "}
          <code>xmlns:uikit="using:AvaloniaUIKit"</code>.
        </p>
        <p class="icon-detail__note" data-icon-note="generated">
          The theme does not carry this icon: the generator adds it to an app
          that names it in C# or XAML. For one only chosen at run time, list
          it in the project: <code data-icon-generated-item="" />.
        </p>
        <p class="icon-detail__note" data-icon-note="no-kind">
          No <code>IconName</code> draws this icon, only its resource: use{" "}
          <code>PathIcon</code>.
        </p>
        <p class="icon-detail__note" data-icon-note="faint">
          <code>uikit:Icon</code> paints the faint part,{" "}
          <code data-icon-faint-key="" />, under the icon at{" "}
          <span data-icon-faint-opacity="" />; <code>PathIcon</code> draws only
          its <code>Data</code>.
        </p>
      </section>

      <dl class="icon-detail__meta">
        <div>
          <dt>Resource</dt>
          <dd>
            <code data-icon-detail-key="" />
            <CopyButton
              value=""
              label="Copy resource key"
              data-icon-key-copy=""
            />
          </dd>
        </div>
        <div data-icon-detail-kind-row="">
          <dt>IconName</dt>
          <dd>
            <code data-icon-detail-kind="" />
            <CopyButton
              value=""
              label="Copy IconName"
              data-icon-kind-copy=""
            />
          </dd>
        </div>
      </dl>
    </aside>
  )
}

/**
 * The Icons page: every icon in a grid that the search filters, and the
 * picked one's details beside it, without leaving the page (app/client.ts).
 */
export function IconsPage() {
  return (
    <main class="icons-page" id="content">
      <header class="icons-page__header">
        <h1 class="icons-page__title">Icons</h1>
        <p class="icons-page__standfirst">
          The {themeIcons.length} icons of <code>IconName</code>, as geometry
          for <code>PathIcon</code> and <code>uikit:Icon</code>: every Lucide
          icon GPUI Kit ships, and its own. The theme carries the ones its
          components draw; the generator adds any other to the app that names
          it. Pick one to see it at each size and color, and copy the XAML that
          draws it. See <a href="/docs/icons">Icons</a> in the docs for how
          they work; Lucide is licensed under the ISC License.
        </p>
      </header>

      <div class="icons-layout" data-icons-layout="">
        <div class="icons-browse">
          <div class="icons-toolbar">
            <label class="icons-search">
              <Icon icon={Search} class="size-4 text-muted-foreground" />
              <input
                type="search"
                data-icons-search=""
                placeholder={`Search ${themeIcons.length} icons`}
                autocomplete="off"
                spellcheck={false}
                aria-label="Search icons"
                aria-controls="icon-grid"
              />
            </label>
            <p class="icons-count" data-icons-count="" aria-live="polite">
              {themeIcons.length} icons
            </p>
          </div>
          <ul class="icon-grid" id="icon-grid" aria-label="Icons">
            {themeIcons.map((icon) => (
              <IconTile icon={icon} />
            ))}
          </ul>
          <div class="icons-empty" data-icons-empty="" hidden>
            <p class="text-sm font-medium">No icons found</p>
            <p class="text-xs text-muted-foreground">
              Try a Lucide name such as <code>chevron-down</code>, or an{" "}
              <code>IconName</code>.
            </p>
          </div>
        </div>
        <IconDetail />
      </div>
    </main>
  )
}
