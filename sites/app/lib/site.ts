export const siteConfig = {
  name: "Avalonia UIKit",
  url: "https://avalonia-uikit.omofla.sh",
  description:
    "Avalonia themes and controls based on shadcn/ui and GPUI Kit. Light and dark, with every variant, size and state verified pixel by pixel.",
  /** The social image (public/og.png, rendered by samples/AvaloniaUIKit.Previews). */
  ogImage: {
    url: "/og.png",
    width: 1200,
    height: 630,
    alt: "Avalonia UIKit: Nova-style themes and controls for Avalonia",
  },
  /** The browser UI's colors: the nova preset's background. */
  themeColor: { light: "#ffffff", dark: "#0a0a0a" },
  links: {
    github: "https://github.com/supermomonga/avalonia-uikit",
    nuget: "https://www.nuget.org/packages/AvaloniaUIKit",
    shadcn: "https://ui.shadcn.com",
    gpuiKit: "https://github.com/longbridge/gpui-kit",
    avalonia: "https://avaloniaui.net",
  },
  navItems: [
    { href: "/", label: "Home" },
    { href: "/docs", label: "Docs" },
    { href: "/docs/components", label: "Components" },
  ],
} as const

/**
 * The components' client scripts (public/shadcn/) for the components the
 * site renders on every page: the docs sidebar (sidebar.js needs core.js).
 */
export const CLIENT_SCRIPTS = ["core", "sidebar"]
