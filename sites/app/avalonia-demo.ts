/**
 * `<avalonia-demo demo="button/demo" width="…" height="…" [scroll]
 * data-wasm-base="/wasm/<hash>">`: the preview image inside it until the
 * .NET runtime (samples/AvaloniaUIKit.Browser) mounts an AvaloniaView over it.
 * See docs/site.md, "ライブデモ". Framework-free: this is a client entry.
 *
 * - The runtime loads once, when the first demo nears the viewport (not on
 *   `navigator.connection.saveData`: a button loads it instead).
 * - `data-state`: idle | loading | live | error | static (no bundle).
 * - Without `scroll`, wheel and touch events stop here so the page scrolls.
 */
type State = "idle" | "loading" | "live" | "error" | "static"

/** `[JSExport]` class AvaloniaUIKit.Browser.Demos. */
interface DemosApi {
  List(): string[]
  Mount(hostId: string, demoId: string): boolean | Promise<boolean>
  SetTheme(dark: boolean): void
}

interface DotnetRuntime {
  getConfig(): { mainAssemblyName: string }
  getAssemblyExports(assembly: string): Promise<Record<string, unknown>>
  runMain(assembly: string, args: string[]): Promise<number>
}

interface DotnetModule {
  dotnet: {
    withApplicationArguments(...args: string[]): {
      create(): Promise<DotnetRuntime>
    }
  }
}

const BADGE_TEXT: Record<State, string> = {
  idle: "Interactive",
  loading: "Loading…",
  live: "Live",
  error: "Preview",
  static: "Preview",
}

const isDark = () => document.documentElement.classList.contains("dark")

let runtime: Promise<DemosApi | null> | undefined
let hosts = 0

/** Starts the runtime from `base`; null when the bundle is not published. */
async function startRuntime(base: string): Promise<DemosApi | null> {
  const url = `${base}/_framework/dotnet.js`
  try {
    const head = await fetch(url, { method: "HEAD" })
    if (!head.ok) return null
  } catch {
    return null
  }
  const { dotnet } = (await import(/* @vite-ignore */ url)) as DotnetModule
  const dotnetRuntime = await dotnet.withApplicationArguments().create()
  const config = dotnetRuntime.getConfig()
  const exports = await dotnetRuntime.getAssemblyExports(
    config.mainAssemblyName
  )
  // Main returns after SetupBrowserAppAsync, so this resolves.
  await dotnetRuntime.runMain(config.mainAssemblyName, [])
  const api = (
    exports as { AvaloniaUIKit: { Browser: { Demos: DemosApi } } }
  ).AvaloniaUIKit.Browser.Demos
  api.SetTheme(isDark())
  new MutationObserver(() => api.SetTheme(isDark())).observe(
    document.documentElement,
    { attributes: true, attributeFilter: ["class"] }
  )
  return api
}

function markAllStatic() {
  for (const element of document.querySelectorAll<AvaloniaDemo>(
    "avalonia-demo"
  )) {
    element.setState("static")
  }
}

const stopEvent = (event: Event) => event.stopPropagation()

class AvaloniaDemo extends HTMLElement {
  static observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue
        AvaloniaDemo.observer.unobserve(entry.target)
        void (entry.target as AvaloniaDemo).load()
      }
    },
    { rootMargin: "200px" }
  )

  state: State = "idle"
  private host?: HTMLDivElement
  private button?: HTMLButtonElement

  connectedCallback() {
    if (this.host) return
    const base = this.dataset.wasmBase
    if (!base) {
      this.setState("static")
      return
    }
    this.setState("idle")
    const host = document.createElement("div")
    host.id = `avalonia-host-${++hosts}`
    host.setAttribute("data-demo-host", "")
    this.append(host)
    this.host = host
    if (!this.hasAttribute("scroll")) {
      // Avalonia listens on the host; the page scrolls natively instead.
      this.addEventListener("wheel", stopEvent, { capture: true, passive: true })
      this.addEventListener("touchstart", stopEvent, {
        capture: true,
        passive: true,
      })
      this.addEventListener("touchmove", stopEvent, { capture: true, passive: true })
    }
    const connection = (
      navigator as Navigator & { connection?: { saveData?: boolean } }
    ).connection
    if (connection?.saveData) {
      this.renderButton()
    } else {
      AvaloniaDemo.observer.observe(this)
    }
  }

  disconnectedCallback() {
    AvaloniaDemo.observer.unobserve(this)
  }

  /** A button to load the demo explicitly (data saver). */
  private renderButton() {
    const button = document.createElement("button")
    button.type = "button"
    button.textContent = "Load interactive demo"
    button.className =
      "absolute bottom-2 left-1/2 z-10 -translate-x-1/2 rounded-md border bg-background px-2.5 py-1 text-xs font-medium shadow-sm hover:bg-muted"
    button.addEventListener("click", () => {
      button.remove()
      this.button = undefined
      void this.load()
    })
    this.append(button)
    this.button = button
  }

  setState(state: State) {
    this.state = state
    this.dataset.state = state
    const badge = this.closest("[data-demo-frame]")?.querySelector(
      "[data-demo-badge]"
    )
    if (badge) badge.textContent = BADGE_TEXT[state]
  }

  async load() {
    if (this.state !== "idle" || !this.host) return
    const base = this.dataset.wasmBase
    const demo = this.getAttribute("demo")
    if (!base || !demo) {
      this.setState("static")
      return
    }
    this.setState("loading")
    try {
      runtime ??= startRuntime(base)
      const api = await runtime
      if (!api) {
        markAllStatic()
        return
      }
      const mounted = await api.Mount(this.host.id, demo)
      this.setState(mounted ? "live" : "error")
    } catch (error) {
      console.error(`avalonia-demo ${demo}:`, error)
      this.setState("error")
    }
  }
}

if (!customElements.get("avalonia-demo")) {
  customElements.define("avalonia-demo", AvaloniaDemo)
}
