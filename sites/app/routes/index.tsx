import type { Child } from "hono/jsx"
import { raw } from "hono/html"
import { createRoute } from "honox/factory"
import {
  Activity,
  ArrowRight,
  Bell,
  Blocks,
  Check,
  Copy,
  Globe,
  type IconNode,
  Mail,
  Monitor,
  PackagePlus,
  Palette,
  PanelsTopLeft,
  ScanEye,
  Search,
  Settings,
  Shapes,
  Star,
  SunMoon,
  Table2,
  Zap,
} from "lucide"
import { Icon } from "@/components/icon"
import { Button } from "@/components/ui/button"
import { components } from "@/lib/catalog"
import { highlight } from "@/lib/highlight"
import { siteConfig } from "@/lib/site"
import { bundledThemes } from "@/lib/themes"

const newControls = components.filter((entry) => entry.status === "new").length
const thirdPartyControls = components.filter((entry) => entry.library).length
const ownControls = components.length - newControls - thirdPartyControls

const heroCode = `<Window xmlns="https://github.com/avaloniaui"
        xmlns:uikit="using:AvaloniaUIKit"
        Title="Hello, World!">
  <uikit:Form Width="360">
    <uikit:FormField Label="Name">
      <TextBox PlaceholderText="Enter your name" />
    </uikit:FormField>
    <uikit:FormField Label="Plan">
      <ComboBox SelectedIndex="0">
        <ComboBoxItem Content="Free" />
        <ComboBoxItem Content="Pro" />
      </ComboBox>
    </uikit:FormField>
    <uikit:Form.Footer>
      <Button Classes="primary" Content="Let's Go!"
              Click="OnGo" />
    </uikit:Form.Footer>
  </uikit:Form>
</Window>`

const installLine = "<uikit:UIKitTheme />"

function Hero({ code }: { code: string }) {
  return (
    <section class="band hero overflow-hidden">
      <div class="hero__grid blueprint" />
      <div class="band__inner hero__inner">
        <div class="rise flex flex-col items-start">
          <span class="eyebrow">
            <span class="eyebrow__dot" />
            Verified against 6,300+ reference renders
          </span>
          <h1 class="hero__title mt-6">{siteConfig.tagline}</h1>
          <p class="hero__lead mt-6">
            A theme for Avalonia's own controls in the Nova style of shadcn/ui,
            plus the controls and features Avalonia lacks: {components.length}{" "}
            components in light, dark and GPUI Kit's {bundledThemes.length}{" "}
            color themes, with every variant, size and state checked pixel by
            pixel and the motion frame by frame.
          </p>
          <div class="mt-8 flex flex-wrap gap-3">
            <Button
              class="h-[2.6rem] gap-2 rounded-[var(--radius-control)] px-[1.05rem] text-[0.875rem] font-semibold shadow-raise"
              render={<a href="/docs/installation" />}
            >
              Get started
              <Icon icon={ArrowRight} />
            </Button>
            <Button
              variant="outline"
              class="h-[2.6rem] rounded-[var(--radius-control)] px-[1.05rem] text-[0.875rem] font-semibold shadow-raise"
              render={<a href="/components" />}
            >
              Browse components
            </Button>
          </div>
          <div class="mt-7 flex flex-col gap-2 text-[0.8rem] text-muted-foreground">
            <div class="flex flex-wrap items-center gap-x-5 gap-y-2">
              <span class="flex items-center gap-1.5">
                <Icon icon={Blocks} class="size-3.5" />
                <strong class="font-semibold text-foreground">
                  {components.length}
                </strong>
                components
              </span>
              <span class="flex items-center gap-1.5">
                <Icon icon={SunMoon} class="size-3.5" />
                {bundledThemes.length + 2} color themes
              </span>
              <span class="flex items-center gap-1.5">
                <Icon icon={Zap} class="size-3.5" />
                NativeAOT ready
              </span>
            </div>
            <span class="flex items-center gap-1.5">
              <Icon icon={Monitor} class="size-3.5" />
              Windows, macOS, Linux, WebAssembly
            </span>
          </div>
          <div class="install mt-7">
            <span class="install__label">App.axaml</span>
            <code class="install__code">
              <span class="c-kw">&lt;uikit:UIKitTheme</span> <span class="c-kw">/&gt;</span>
            </code>
            <Button
              variant="ghost"
              size="icon-sm"
              class="group/copy mr-1 text-muted-foreground hover:text-foreground"
              data-copy={installLine}
              aria-label="Copy"
            >
              <Icon icon={Copy} class="group-data-copied/copy:hidden" />
              <Icon icon={Check} class="hidden group-data-copied/copy:block" />
            </Button>
          </div>
        </div>
        <div class="hero__window rise">
          <div class="mac-window code-window">
            <div class="mac-window__bar">
              <span class="mac-window__light bg-[#ff5f57]" />
              <span class="mac-window__light bg-[#febc2e]" />
              <span class="mac-window__light bg-[#28c840]" />
              <span class="mac-window__title">MainWindow.axaml</span>
            </div>
            {raw(code)}
          </div>
        </div>
      </div>
    </section>
  )
}

function SectionHead({
  kicker,
  title,
  children,
}: {
  kicker: string
  title: string
  children?: Child
}) {
  return (
    <div class="section-head">
      <p class="kicker">{kicker}</p>
      <h2 class="section-head__title">{title}</h2>
      <p class="section-head__text">{children}</p>
    </div>
  )
}

/* The capability cards' diagrams, drawn with plain boxes as on gpui-kit.com. */

function PixelDiagram() {
  const pane = (label: string, highlight: boolean) => (
    <div class="mini flex w-[38%] flex-col gap-1.5 p-2">
      <span class="font-mono text-[0.55rem] text-muted-foreground">{label}</span>
      <div class="flex gap-1">
        <span class="h-3 w-8 rounded-[3px] bg-foreground/85" />
        <span class="h-3 w-6 rounded-[3px] border border-border" />
      </div>
      <span class={`pill w-[70%] ${highlight ? "pill--blue" : "pill--strong"}`} />
    </div>
  )
  return (
    <div class="diagram flex items-center justify-center gap-3 px-4">
      {pane("reference", false)}
      <span class="font-mono text-xs text-muted-foreground">=</span>
      {pane("avalonia", false)}
      <span class="absolute right-2.5 bottom-2 flex items-center gap-1 font-mono text-[0.6rem] text-success">
        <Icon icon={Check} class="size-3" />0 px
      </span>
    </div>
  )
}

function SpringDiagram() {
  // A spring settling: overshoot, then rest, sampled as frames.
  const points = Array.from({ length: 24 }, (_, i) => {
    const t = i / 23
    const y = 1 - Math.exp(-5.2 * t) * Math.cos(9.5 * t)
    return { x: 14 + t * 252, y: 78 - y * 52 }
  })
  const path = points.map((p, i) => `${i ? "L" : "M"}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join("")
  return (
    <div class="diagram">
      <svg viewBox="0 0 280 96" class="size-full" preserveAspectRatio="none" aria-hidden="true">
        <line x1="14" x2="266" y1="26" y2="26" stroke="var(--border)" stroke-dasharray="3 3" />
        <path d={path} fill="none" stroke="var(--data-2)" stroke-width="1.5" />
        {points.map((p) => (
          <circle cx={p.x} cy={p.y} r="1.8" fill="var(--data-2)" />
        ))}
      </svg>
    </div>
  )
}

function ControlsDiagram() {
  return (
    <div class="diagram flex items-center justify-center gap-3 px-4">
      <span class="flex h-6 items-center rounded-[4px] bg-foreground px-2.5 text-[0.6rem] font-medium text-background">
        Button
      </span>
      <span class="mini flex h-6 w-24 items-center px-2 text-[0.6rem] text-muted-foreground">
        TextBox
      </span>
      <span class="flex items-center gap-1.5 text-[0.6rem] text-muted-foreground">
        <span class="flex size-3.5 items-center justify-center rounded-[3px] bg-foreground text-background">
          <Icon icon={Check} class="size-2.5" />
        </span>
        CheckBox
      </span>
    </div>
  )
}

function NewControlsDiagram() {
  return (
    <div class="diagram flex flex-col items-center justify-center gap-3 px-6">
      <div class="flex w-full max-w-56 items-center">
        {[true, true, false, false].map((done, index) => (
          <>
            {index > 0 && (
              <span class={`h-px flex-1 ${done ? "bg-foreground/60" : "bg-border"}`} />
            )}
            <span
              class={`flex size-4 items-center justify-center rounded-full font-mono text-[0.5rem] ${done ? "bg-foreground text-background" : "border border-border bg-background text-muted-foreground"}`}
            >
              {index + 1}
            </span>
          </>
        ))}
      </div>
      <div class="flex gap-1.5">
        <span class="rounded-full bg-data-2 px-2 py-px text-[0.55rem] font-medium text-white">New</span>
        <span class="rounded-full border border-border bg-background px-2 py-px text-[0.55rem]">Tag</span>
        <span class="rounded-full bg-success/15 px-2 py-px text-[0.55rem] text-success">Done</span>
      </div>
    </div>
  )
}

function TableDiagram() {
  return (
    <div class="diagram flex flex-col justify-center gap-2.5 px-4">
      {[0, 1, 2, 3, 4].map((row) => (
        <div class="grid grid-cols-[1.6fr_1.2fr_0.8fr] gap-1.5">
          {[0, 1, 2].map(() => (
            <span
              class={`pill ${row === 0 ? "pill--strong" : row === 2 ? "pill--blue" : ""}`}
            />
          ))}
        </div>
      ))}
    </div>
  )
}

function SwatchDiagram() {
  return (
    <div class="diagram flex items-center gap-2 px-4">
      {["#0a0a0a", "#404040", "#a3a3a3", "#f5f5f5", "#ffffff", "var(--data-2)"].map(
        (color) => (
          <span
            class="h-[4.25rem] flex-1 rounded-[4px] shadow-hairline"
            style={{ background: color }}
          />
        )
      )}
    </div>
  )
}

function IconsDiagram() {
  const icons: IconNode[] = [Search, Bell, Mail, Settings, Star, Palette]
  return (
    <div class="diagram flex items-center justify-center gap-4 px-4">
      {icons.map((icon, index) => (
        <Icon
          icon={icon}
          class={`size-5 ${index === 4 ? "text-data-2" : "text-muted-foreground"}`}
        />
      ))}
    </div>
  )
}

function AotDiagram() {
  const box = (label: string, strong = false) => (
    <span
      class={`flex h-8 items-center rounded-[4px] px-2.5 font-mono text-[0.6rem] ${strong ? "bg-foreground text-background" : "mini text-muted-foreground"}`}
    >
      {label}
    </span>
  )
  return (
    <div class="diagram flex items-center justify-center gap-2 px-3">
      {box(".axaml")}
      <span class="font-mono text-[0.6rem] text-muted-foreground">⇢</span>
      {box("compiled")}
      <span class="font-mono text-[0.6rem] text-muted-foreground">⇢</span>
      {box("native", true)}
    </div>
  )
}

function WebDiagram() {
  const window = (label: string, color: string) => (
    <div class="mini flex h-[3.6rem] w-[5.5rem] flex-col overflow-hidden">
      <div class="flex h-3 items-center gap-0.5 border-b border-border px-1">
        <span class="size-1 rounded-full bg-border" />
        <span class="size-1 rounded-full bg-border" />
        <span class={`ml-auto font-mono text-[0.45rem] ${color}`}>{label}</span>
      </div>
      <div class="flex flex-1 flex-col justify-center gap-1 px-1.5">
        <span class="pill w-[80%] pill--strong" />
        <span class="pill w-[60%]" />
      </div>
    </div>
  )
  return (
    <div class="diagram flex items-center justify-center gap-2.5 px-3">
      {window("Desktop", "text-muted-foreground")}
      <span class="font-mono text-[0.6rem] text-muted-foreground">⇢</span>
      <span class="flex size-9 items-end justify-center rounded-[4px] bg-[#654ff0] pb-0.5 font-mono text-[0.6rem] font-bold text-white">
        WA
      </span>
      <span class="font-mono text-[0.6rem] text-success">⇢</span>
      {window("Web", "text-success")}
    </div>
  )
}

const capabilities: {
  icon: IconNode
  title: string
  text: string
  chips: string[]
  diagram: () => Child
}[] = [
  {
    icon: ScanEye,
    title: "Verified pixel by pixel",
    text: "Each variant, size and state is compared with reference renders in light and dark, over 6,300 cases on every change.",
    chips: ["Light", "Dark", "Headless"],
    diagram: PixelDiagram,
  },
  {
    icon: Activity,
    title: "Motion, frame by frame",
    text: "Springs drive the transitions, and the animation is compared with the reference frame by frame instead of by eye.",
    chips: ["UIKit.Spring.Control", "UIKit.Spring.Move"],
    diagram: SpringDiagram,
  },
  {
    icon: Blocks,
    title: "Avalonia's own controls",
    text: `For ${ownControls} of the components, UIKitTheme replaces the Avalonia control's ControlTheme: colors, padding, outlines, inner layout, states and animations.`,
    chips: ["Button", "TextBox", "ComboBox", "TabControl"],
    diagram: ControlsDiagram,
  },
  {
    icon: PackagePlus,
    title: "What Avalonia lacks",
    text: `${newControls} new controls in the uikit: namespace, from Badge and Stepper to a Select with search and a virtualized Tree, plus attached properties that add what Avalonia's controls lack, such as a loading Button.`,
    chips: ["uikit:Badge", "uikit:Select", "uikit:Buttons"],
    diagram: NewControlsDiagram,
  },
  {
    icon: Table2,
    title: "Data tables",
    text: "TableView in the main package, and Avalonia's DataGrid with sorting and column resizing from an optional one.",
    chips: ["TableView", "DataGrid"],
    diagram: TableDiagram,
  },
  {
    icon: SunMoon,
    title: "Color themes",
    text: `Default Light, Default Dark and the ${bundledThemes.length} themes GPUI Kit bundles, Aurora's gradients included. Switch with RequestedThemeVariant, for the app or any part of it.`,
    chips: ["UIKitThemeVariants", "UIKit.Primary"],
    diagram: SwatchDiagram,
  },
  {
    icon: Shapes,
    title: "Lucide icons",
    text: "The icons the components draw ship as compiled geometry for PathIcon, in four sizes that match the controls.",
    chips: ["PathIcon", "UIKit.Icon.Search"],
    diagram: IconsDiagram,
  },
  {
    icon: Zap,
    title: "NativeAOT ready",
    text: "Compiled bindings only and no reflection. A NativeAOT smoke test renders every control in both themes on each change.",
    chips: ["Trimming", "NativeAOT"],
    diagram: AotDiagram,
  },
  {
    icon: Globe,
    title: "Runs on the web",
    text: "The same controls run in the browser on Avalonia's WebAssembly backend. Every demo on this site is live.",
    chips: ["Avalonia.Browser", "WASM"],
    diagram: WebDiagram,
  },
]

function Capabilities() {
  return (
    <section class="band">
      <div class="band__inner">
        <SectionHead
          kicker="Capabilities"
          title="Built to match the reference, down to the pixel."
        >
          A look is only finished when it holds in every state, so each one is
          rendered, compared and kept in both themes.
        </SectionHead>
        <div class="capabilities">
          {capabilities.map((item) => (
            <article class="capability">
              <h3 class="capability__title">
                <Icon icon={item.icon} />
                {item.title}
              </h3>
              <p class="capability__text">{item.text}</p>
              <div class="capability__chips">
                {item.chips.map((chip) => (
                  <span class="chip">{chip}</span>
                ))}
              </div>
              {item.diagram()}
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

const packages: {
  icon: IconNode
  name: string
  title: string
  text: string
  code: string
  points: string[]
  link: { href: string; label: string }
}[] = [
  {
    icon: Blocks,
    name: "AvaloniaUIKit",
    title: "The theme and the new controls",
    text: "UIKitTheme for Avalonia's own controls, the uikit: controls and attached properties, the color tokens and the icons.",
    code: `<Application.Styles>
  <uikit:UIKitTheme />
</Application.Styles>`,
    points: [
      `${ownControls} components on Avalonia's own controls`,
      `${newControls} new uikit: controls`,
      "Works alone or on top of FluentTheme",
    ],
    link: { href: "/docs/installation", label: "Get started" },
  },
  {
    icon: Palette,
    name: "AvaloniaUIKit.ColorPicker",
    title: "Avalonia's ColorPicker",
    text: "UIKitColorPickerTheme gives the ColorPicker package a swatch or a field with a palette popover. The main package's uikit:ColorSelect needs neither.",
    code: `<Application.Styles>
  <uikit:UIKitTheme />
  <uikitcolor:UIKitColorPickerTheme />
</Application.Styles>`,
    points: [
      "Swatch and input field looks",
      "Palette and components on segmented tabs",
      "Trimmable and NativeAOT compatible",
    ],
    link: { href: "/components/color-picker", label: "Read the Color Picker docs" },
  },
  {
    icon: Table2,
    name: "AvaloniaUIKit.DataGrid",
    title: "Avalonia's DataGrid",
    text: "UIKitDataGridTheme gives DataGrid the data table look of TableView, with the grid's own features.",
    code: `<Application.Styles>
  <uikit:UIKitTheme />
  <uikitgrid:UIKitDataGridTheme />
</Application.Styles>`,
    points: [
      "Sort indicators, column resizing and reordering",
      "The stripe, borderless and size classes",
      "Only for apps that already use DataGrid",
    ],
    link: { href: "/components/data-table", label: "Read the Data Table docs" },
  },
  {
    icon: PanelsTopLeft,
    name: "AvaloniaUIKit.Dock",
    title: "Dock.Avalonia's docking",
    text: "UIKitDockTheme gives Dock.Avalonia's dock control the tab bars, title bars, split handles and drop targets of a dock.",
    code: `<Application.Styles>
  <uikit:UIKitTheme />
  <uikitdock:UIKitDockTheme />
</Application.Styles>`,
    points: [
      "Tab bars and title bars with a panel menu",
      "Drop targets by where the pointer is",
      "Only for apps that already use Dock.Avalonia",
    ],
    link: { href: "/components/dock", label: "Read the Dock docs" },
  },
]

function Packages({ codes }: { codes: string[] }) {
  return (
    <section class="band">
      <div class="band__inner">
        <SectionHead
          kicker="Four packages. One theme."
          title="Take only the themes your app uses."
        >
          UIKitTheme covers Avalonia's built-in controls and the new ones.
          Avalonia ships ColorPicker and DataGrid as packages of their own, and
          so does this library; the theme for Dock.Avalonia is separate too, so
          an app takes no dependency it does not use.
        </SectionHead>
        <div class="paths">
          {packages.map((item, index) => (
            <article class="path">
              <p class="path__meta">
                <Icon icon={item.icon} />
                {item.name}
              </p>
              <h3 class="path__title">{item.title}</h3>
              <p class="path__text">{item.text}</p>
              {raw(codes[index])}
              <ul class="path__list">
                {item.points.map((point) => (
                  <li>
                    <Icon icon={Check} />
                    {point}
                  </li>
                ))}
              </ul>
              <a href={item.link.href} class="path__link">
                {item.link.label}
                <Icon icon={ArrowRight} />
              </a>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

function Principle() {
  return (
    <section class="band band--principle overflow-hidden">
      <div class="principle__grid blueprint" />
      <div class="band__inner principle">
        <div>
          <p class="kicker">Principle</p>
          <blockquote class="principle__quote">
            Behavior belongs to Avalonia.
            <br />
            Looks and motion belong to the theme.
          </blockquote>
        </div>
        <div class="flex flex-col items-start gap-6">
          <p class="text-[1.03rem] leading-[1.7] text-muted-foreground">
            UIKitTheme replaces each control's ControlTheme and leaves the
            control itself alone: its behavior, input handling, accessibility
            and API stay exactly as Avalonia ships them. What the controls
            lack is added only where the app asks for it: an attached
            property it sets, or a uikit: control it places.
          </p>
          <Button
            class="h-[2.6rem] gap-2 rounded-[var(--radius-control)] px-[1.05rem] text-[0.875rem] font-semibold shadow-raise"
            render={<a href="/components" />}
          >
            Explore the components
            <Icon icon={ArrowRight} />
          </Button>
        </div>
      </div>
    </section>
  )
}

export default createRoute(async (c) => {
  const code = await highlight(heroCode, "xml")
  const codes = await Promise.all(
    packages.map((item) => highlight(item.code, "xml"))
  )
  return c.render(
    <main id="content" class="flex-1">
      <Hero code={code} />
      <Capabilities />
      <Packages codes={codes} />
      <Principle />
    </main>,
    { description: siteConfig.description }
  )
})
