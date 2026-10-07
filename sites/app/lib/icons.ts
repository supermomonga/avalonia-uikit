/**
 * Every icon of `IconName`, read at build time for the Icons page: the names
 * the theme carries (src/AvaloniaUIKit/Themes/Icons/Lucide.g.axaml) and the
 * ones the generator adds to the apps that use them
 * (src/AvaloniaUIKit.Generators/Icons.g.tsv), the `IconName` that draws each
 * with `uikit:Icon` and the file GPUI Kit ships it as (Controls/IconName.g.cs).
 * `uikit-icons` (reference/icons) writes all three (ADR 36, ADR 38).
 *
 * A Lucide icon is drawn from the `lucide` package as Lucide draws it: its
 * strokes, which are what the theme's outlines are made from, so the page
 * needs no outlines for them, about a sixth of their weight. The build stops
 * unless the package is the Lucide release GPUI Kit ships, whose SVGs GPUI
 * Kit takes as released (crates/assets/lucide.json pins the release by its
 * checksum). GPUI Kit's own icons are drawn from the theme's outlines.
 */
import { type IconNode, icons as lucideIcons } from "lucide"
import lucidePackage from "lucide/package.json"
import generatorIcons from "../../../src/AvaloniaUIKit.Generators/Icons.g.tsv?raw"
import iconNamesSource from "../../../src/AvaloniaUIKit/Controls/IconName.g.cs?raw"
import lucideXaml from "../../../src/AvaloniaUIKit/Themes/Icons/Lucide.g.axaml?raw"
import { kebab } from "./demos"

/** An outline the theme draws, as SVG path data in the 24×24 icon box. */
export interface IconOutline {
  path: string
  fillRule: "nonzero" | "evenodd"
  /** A two-tone icon's faint part, which `uikit:Icon` paints under it at its opacity. */
  faint?: { path: string; opacity: number }
}

export interface ThemeIcon {
  /** The resource key without its prefix: `UIKit.Icon.<name>`. */
  name: string
  key: string
  /** The `IconName` that draws it with `uikit:Icon`; none for the icons only the themes draw. */
  kind?: string
  /** The SVG GPUI Kit ships it as, without `.svg` (`chevron-down`). */
  file: string
  /**
   * Whether the theme carries it; otherwise the generator adds it to an app
   * that names it (ADR 38).
   */
  carried: boolean
  /** A Lucide icon's SVG elements, drawn with Lucide's strokes. */
  lucide?: IconNode
  /** The outline of an icon Lucide does not have (GPUI Kit's own). */
  outline?: IconOutline
}

const prefix = "UIKit.Icon."

/** Avalonia path markup as SVG path data and its fill rule. */
function geometry(markup: string) {
  const [, rule, path] = /^F([01]) (.+)$/s.exec(markup.trim()) ?? []
  return {
    path: path ?? markup.trim(),
    fillRule: rule === "0" ? ("evenodd" as const) : ("nonzero" as const),
  }
}

// The Lucide release GPUI Kit ships, which `uikit-icons` writes in the
// generator's icons from GPUI Kit's crates/assets/lucide.json.
const lucideVersion = /Lucide (\d+\.\d+\.\d+)/.exec(generatorIcons)?.[1]
if (lucideVersion !== lucidePackage.version) {
  throw new Error(
    `The site's lucide is ${lucidePackage.version}, GPUI Kit's Lucide is ${lucideVersion}: set "lucide" in sites/package.json to ${lucideVersion}`
  )
}

/** The theme's geometries by name, then the generator's (`<IconName> TAB <geometry> [TAB <faint opacity> TAB <faint geometry>]`). */
const geometries = new Map(
  [
    ...lucideXaml.matchAll(
      /<StreamGeometry x:Key="UIKit\.Icon\.([\w.]+)">([^<]+)<\/StreamGeometry>/g
    ),
  ].map(([, name, markup]) => [name, { ...geometry(markup), carried: true }])
)
const generatorFaintOpacity = new Map<string, number>()
for (const line of generatorIcons.split("\n")) {
  const [name, markup, opacity, faint] = line.split("\t")
  if (!name || name.startsWith("#") || !markup) continue
  geometries.set(name, { ...geometry(markup), carried: false })
  if (faint) {
    geometries.set(`${name}.Faint`, { ...geometry(faint), carried: false })
    generatorFaintOpacity.set(name, Number(opacity))
  }
}

/** `IconName` variants by the key they draw, with their files (`/// <summary>icons/<file>.svg</summary>`). */
const files = new Map(
  [
    ...iconNamesSource.matchAll(
      /\/\/\/ <summary>icons\/([\w-]+)\.svg<\/summary>\s+(\w+),/g
    ),
  ].map(([, file, kind]) => [kind, file])
)
const kinds = new Map(
  [
    ...iconNamesSource.matchAll(/IconName\.(\w+) => "UIKit\.Icon\.([\w.]+)"/g),
  ].map(([, kind, name]) => [name, kind])
)
const faintOpacity = new Map(
  [
    ...(iconNamesSource.split("FaintOpacity")[1]?.split("};")[0] ?? "").matchAll(
      /IconName\.(\w+) => ([\d.]+),/g
    ),
  ].map(([, kind, opacity]) => [kind, Number(opacity)])
)

/** Lucide's icons by its names, which are GPUI Kit's IconName variants (crates/assets/build.rs). */
const lucide = lucideIcons as Record<string, IconNode | undefined>

/** Every icon, by name. */
export const themeIcons: ThemeIcon[] = [...geometries]
  .filter(([name]) => !name.endsWith(".Faint"))
  .map(([name, geometry]) => {
    const kind = kinds.get(name)
    const node = kind ? lucide[kind] : undefined
    const faint = geometries.get(`${name}.Faint`)
    const opacity =
      (kind ? faintOpacity.get(kind) : undefined) ?? generatorFaintOpacity.get(name)
    return {
      name,
      key: `${prefix}${name}`,
      kind,
      file: (kind && files.get(kind)) ?? kebab(name),
      carried: geometry.carried,
      ...(node && !faint
        ? { lucide: node }
        : {
            outline: {
              path: geometry.path,
              fillRule: geometry.fillRule,
              faint: faint && opacity ? { path: faint.path, opacity } : undefined,
            },
          }),
    }
  })
  .sort((a, b) => a.name.localeCompare(b.name, "en"))

if (!themeIcons.some((icon) => icon.carried)) {
  throw new Error("No icons in src/AvaloniaUIKit/Themes/Icons/Lucide.g.axaml")
}
if (!themeIcons.some((icon) => !icon.carried)) {
  throw new Error("No icons in src/AvaloniaUIKit.Generators/Icons.g.tsv")
}

/**
 * The sizes the theme's PathIcon has (Themes/Base/PathIcon.axaml): medium is
 * the default, the others are classes.
 */
export const iconSizes = [
  { name: "xsmall", px: 12 },
  { name: "small", px: 14 },
  { name: "medium", px: 16 },
  { name: "large", px: 24 },
] as const

/**
 * The colors the Icons page offers for `Foreground`: the theme's resources
 * that the site also has, so the preview shows them in the site's theme.
 * Without a resource the icon takes the Foreground it inherits.
 */
export const iconColors = [
  { name: "Default", resource: undefined, css: "var(--foreground)" },
  { name: "Muted", resource: "UIKit.MutedForeground", css: "var(--muted-foreground)" },
  { name: "Primary", resource: "UIKit.Primary", css: "var(--primary)" },
  { name: "Danger", resource: "UIKit.Danger", css: "var(--destructive)" },
  { name: "Warning", resource: "UIKit.Warning", css: "var(--warning)" },
  { name: "Success", resource: "UIKit.Success", css: "var(--success)" },
] as const
