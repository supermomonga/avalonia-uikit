import { components } from "./catalog"
import { bundledThemes } from "./themes"

export const siteConfig = {
  name: "Avalonia UIKit",
  url: "https://avalonia-uikit.omofla.sh",
  /** The home page's headline, also on the social image. */
  tagline: "Modern looks and motion for Avalonia apps.",
  description: `Themes and controls for Avalonia in the Nova style of shadcn/ui and GPUI Kit: ${components.length} components in light, dark and ${bundledThemes.length} more color themes, verified pixel by pixel and frame by frame, NativeAOT ready.`,
  /** The social image (public/og.png, rendered by scripts/images.ts from routes/og-image.tsx). */
  ogImage: {
    url: "/og.png",
    width: 2400,
    height: 1260,
    alt: "Avalonia UIKit: modern looks and motion for Avalonia apps, beside real controls rendered by the theme.",
  },
  /** The browser UI's colors: the site's background. */
  themeColor: { light: "#ffffff", dark: "#0a0a0a" },
  links: {
    github: "https://github.com/supermomonga/avalonia-uikit",
    issues: "https://github.com/supermomonga/avalonia-uikit/issues",
    author: "https://github.com/supermomonga",
    avalonia: "https://avaloniaui.net",
    avaloniaDocs: "https://docs.avaloniaui.net",
    shadcn: "https://ui.shadcn.com",
    gpuiKit: "https://github.com/longbridge/gpui-kit",
    lucide: "https://lucide.dev",
    inter: "https://rsms.me/inter/",
  },
  /** The top bar's sections; each has its own sidebar. */
  navItems: [
    { href: "/docs", label: "Docs" },
    { href: "/components", label: "Components" },
  ],
  /** The top bar's Resources menu. */
  resources: [
    { href: "https://github.com/supermomonga/avalonia-uikit", label: "GitHub" },
    { href: "https://github.com/supermomonga/avalonia-uikit/issues", label: "Issues" },
    { href: "https://docs.avaloniaui.net", label: "Avalonia Docs" },
  ],
} as const

/** The section of the top bar a path belongs to. */
export function navSection(pathname: string): string | undefined {
  return siteConfig.navItems.find(
    (item) => pathname === item.href || pathname.startsWith(`${item.href}/`)
  )?.href
}
