import generated from "./themes.g.json"

/**
 * A color theme GPUI Kit bundles, as the theme palette offers it. The list and
 * the site's colors in each theme (styles/themes.g.css) are written by
 * `reference generate` from the colors GPUI Kit resolves; the live demos show
 * the same theme through UIKitThemeVariants (docs/site.md, "テーマ").
 */
export interface BundledTheme {
  /** The slug, `<html data-theme>`'s value and `localStorage.theme`: `ayu-dark`. */
  id: string
  /** GPUI Kit's name, which the live demos take: `Ayu Dark`. */
  name: string
  family: string
  mode: "light" | "dark"
  /** The background and the primary color. */
  swatch: [string, string]
}

export const bundledThemes = generated.themes as BundledTheme[]
