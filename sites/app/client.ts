import { createClient } from "honox/client"

createClient()

/** Whether a key press belongs to a text field rather than to the page. */
function typing(event: KeyboardEvent): boolean {
  const target = event.target as HTMLElement | null
  return (
    !!target?.closest("input, textarea, select, [contenteditable='true']") ||
    event.metaKey ||
    event.ctrlKey ||
    event.altKey
  )
}

function dialog(id: string) {
  return document.getElementById(id) as HTMLDialogElement | null
}

// Copy buttons (components/code-block.tsx, the home page's install line).
document.addEventListener("click", async (event) => {
  const button = (event.target as Element).closest<HTMLElement>("[data-copy]")
  if (!button) return
  await navigator.clipboard.writeText(button.dataset.copy ?? "")
  button.dataset.copied = ""
  setTimeout(() => delete button.dataset.copied, 1600)
})

// Demos (components/demo.tsx): expand the code.
document.addEventListener("click", (event) => {
  const button = (event.target as Element).closest("[data-view-code]")
  button
    ?.closest<HTMLElement>('[data-slot="code"]')
    ?.setAttribute("data-open", "")
})

// Theme (components/theme-palette.tsx): `localStorage.theme` is light, dark,
// a bundled theme's id, or absent to follow the system. A bundled theme sets
// `<html data-theme>` (styles/themes.g.css) and, by its mode, `dark`.
const systemDark = matchMedia("(prefers-color-scheme: dark)")

function themeOptions() {
  return [
    ...document.querySelectorAll<HTMLElement>("[data-theme-option]"),
  ]
}

function themeOption(choice: string) {
  return themeOptions().find((o) => o.dataset.themeOption === choice)
}

function storedTheme(): string {
  try {
    const value = localStorage.theme
    return typeof value === "string" && themeOption(value) ? value : "system"
  } catch {
    return "system"
  }
}

function applyTheme(choice: string) {
  const mode = themeOption(choice)?.dataset.mode ?? "system"
  const root = document.documentElement
  root.classList.toggle(
    "dark",
    mode === "dark" || (mode === "system" && systemDark.matches)
  )
  if (["system", "light", "dark"].includes(choice)) delete root.dataset.theme
  else root.dataset.theme = choice
}

function saveTheme(choice: string) {
  try {
    if (choice === "system") localStorage.removeItem("theme")
    else localStorage.theme = choice
  } catch {}
  applyTheme(choice)
}

systemDark.addEventListener("change", () => applyTheme(storedTheme()))

function visibleThemeOptions() {
  return themeOptions().filter((o) => !o.closest("[hidden]") && !o.hidden)
}

function highlightTheme(option: HTMLElement | undefined, preview: boolean) {
  for (const o of themeOptions()) {
    o.toggleAttribute("data-highlighted", o === option)
  }
  if (!option) return
  option.scrollIntoView({ block: "nearest" })
  if (preview) applyTheme(option.dataset.themeOption!)
}

/** Shows the options whose name, family or mode contains the query. */
function filterThemes(query: string) {
  const q = query.trim().toLowerCase()
  for (const option of themeOptions()) {
    option.hidden = !option.dataset.search?.includes(q)
  }
  for (const group of document.querySelectorAll<HTMLElement>("[data-theme-group]")) {
    group.hidden = !group.querySelector("[data-theme-option]:not([hidden])")
  }
  const visible = visibleThemeOptions()
  const empty = document.querySelector<HTMLElement>("[data-theme-empty]")
  if (empty) empty.hidden = visible.length > 0
  const highlighted = visible.find((o) => o.hasAttribute("data-highlighted"))
  if (!highlighted) highlightTheme(visible[0], false)
}

/** The theme to restore when the palette closes without a choice. */
let committedTheme: string | undefined

function openThemePalette() {
  const palette = dialog("theme-palette")
  if (!palette || palette.open) return
  palette.showModal()
}

document.addEventListener(
  "toggle",
  (event) => {
    const target = event.target as HTMLDialogElement
    if (target.id !== "theme-palette") return
    if (target.open) {
      committedTheme = storedTheme()
      for (const option of themeOptions()) {
        option.setAttribute(
          "aria-selected",
          String(option.dataset.themeOption === committedTheme)
        )
      }
      const input = target.querySelector<HTMLInputElement>("[data-theme-search]")
      if (input) {
        input.value = ""
        input.focus()
      }
      filterThemes("")
      highlightTheme(themeOption(committedTheme), false)
    } else if (committedTheme) {
      applyTheme(committedTheme)
      committedTheme = undefined
    }
  },
  true
)

document.addEventListener("click", (event) => {
  const option = (event.target as Element).closest<HTMLElement>(
    "[data-theme-option]"
  )
  if (!option) return
  saveTheme(option.dataset.themeOption!)
  committedTheme = undefined
  dialog("theme-palette")?.close()
})

document.addEventListener("mousemove", (event) => {
  const option = (event.target as Element).closest<HTMLElement>(
    "[data-theme-option]"
  )
  if (!option || option.hasAttribute("data-highlighted")) return
  highlightTheme(option, false)
})

// Search (components/search.tsx): it filters /search.json.
interface SearchEntry {
  title: string
  href: string
  description?: string
}
let searchIndex:
  | Promise<{ pages: SearchEntry[]; components: SearchEntry[] }>
  | undefined

async function renderSearch(query: string) {
  const results = document.querySelector<HTMLElement>("[data-search-results]")
  if (!results) return
  searchIndex ??= fetch("/search.json").then((response) => response.json())
  const index = await searchIndex
  const q = query.trim().toLowerCase()
  const match = (entry: SearchEntry) =>
    !q ||
    entry.title.toLowerCase().includes(q) ||
    (entry.description ?? "").toLowerCase().includes(q)
  const groups = [
    { heading: "Docs", entries: index.pages.filter(match) },
    { heading: "Components", entries: index.components.filter(match) },
  ].filter((group) => group.entries.length > 0)
  results.replaceChildren()
  if (groups.length === 0) {
    const empty = document.createElement("div")
    empty.className = "flex flex-col items-center gap-1 py-14 text-center"
    empty.innerHTML =
      '<p class="text-sm font-medium">No results</p><p class="text-xs text-muted-foreground">Try a component name or an Avalonia control.</p>'
    results.append(empty)
    return
  }
  for (const group of groups) {
    const heading = document.createElement("div")
    heading.className = "kicker px-2.5 pt-3 pb-2"
    heading.textContent = group.heading
    results.append(heading)
    for (const entry of group.entries) {
      const link = document.createElement("a")
      link.href = entry.href
      link.role = "option"
      link.dataset.searchResult = ""
      link.className =
        "flex h-9 items-center justify-between gap-3 rounded-[var(--radius-control)] px-2.5 text-sm outline-none data-[selected]:bg-secondary"
      const title = document.createElement("span")
      title.textContent = entry.title
      const path = document.createElement("span")
      path.className = "truncate font-mono text-[0.7rem] text-muted-foreground"
      path.textContent = entry.href
      link.append(title, path)
      results.append(link)
    }
  }
  selectResult(0)
}

function selectResult(index: number) {
  const links = [
    ...document.querySelectorAll<HTMLElement>("[data-search-result]"),
  ]
  for (const [i, link] of links.entries()) {
    link.toggleAttribute("data-selected", i === index)
  }
  links[index]?.scrollIntoView({ block: "nearest" })
}

function openSearch() {
  const search = dialog("search")
  if (search && !search.open) search.showModal()
}

document.addEventListener("keydown", (event) => {
  if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
    event.preventDefault()
    openSearch()
    return
  }
  const search = dialog("search")
  const palette = dialog("theme-palette")
  if (search?.open) {
    const links = [
      ...document.querySelectorAll<HTMLElement>("[data-search-result]"),
    ]
    const current = links.findIndex((link) =>
      link.hasAttribute("data-selected")
    )
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault()
      const step = event.key === "ArrowDown" ? 1 : -1
      selectResult((current + step + links.length) % Math.max(links.length, 1))
    } else if (event.key === "Enter" && links[current]) {
      event.preventDefault()
      links[current].click()
    }
    return
  }
  if (palette?.open) {
    const options = visibleThemeOptions()
    const current = options.findIndex((o) => o.hasAttribute("data-highlighted"))
    if ((event.key === "ArrowDown" || event.key === "ArrowUp") && options.length > 0) {
      event.preventDefault()
      const step = event.key === "ArrowDown" ? 1 : -1
      const next = current < 0 && step < 0 ? options.length - 1 : (current + step + options.length) % options.length
      highlightTheme(options[next], true)
    } else if (event.key === "Enter" && options[current]) {
      event.preventDefault()
      options[current].click()
    }
    return
  }
  if (typing(event) || document.querySelector("dialog[open]")) return
  if (event.key === "Escape") {
    hideIcon()
    return
  }
  const tile = (event.target as Element).closest<HTMLElement>(
    "[data-icon-tile]"
  )
  if (tile && event.key.startsWith("Arrow")) {
    event.preventDefault()
    moveIconFocus(tile, event.key)
    return
  }
  if (event.key === "/") {
    event.preventDefault()
    openSearch()
  } else if (event.key === "t" || event.key === "T") {
    event.preventDefault()
    openThemePalette()
  }
})

document.addEventListener("input", (event) => {
  const input = event.target as HTMLInputElement
  if (input.matches("[data-search-input]")) void renderSearch(input.value)
  if (input.matches("[data-theme-search]")) filterThemes(input.value)
  if (input.matches("[data-icons-search]")) filterIcons(input.value)
})

document.addEventListener(
  "toggle",
  (event) => {
    const target = event.target as HTMLDialogElement
    if (target.id !== "search" || !target.open) return
    const input = target.querySelector<HTMLInputElement>("[data-search-input]")
    if (input) {
      input.value = ""
      input.focus()
    }
    void renderSearch("")
  },
  true
)

// Icons (components/icons-page.tsx): the search filters the grid, and the
// picked icon opens in the details beside it, which write the XAML for the
// chosen size, color and element. `?icon=<name>` keeps the pick in the URL.
function iconTiles() {
  return [...document.querySelectorAll<HTMLElement>("[data-icon-tile]")]
}

function iconDetail() {
  return document.querySelector<HTMLElement>("[data-icon-detail]")
}

/** Shows the icons whose name, file or key contains the query, ignoring case and separators. */
function filterIcons(query: string) {
  const q = query.toLowerCase().replace(/[^a-z0-9]/g, "")
  const tiles = iconTiles()
  let shown = 0
  for (const tile of tiles) {
    const match = tile.dataset.search?.includes(q) ?? false
    tile.parentElement!.hidden = !match
    if (match) shown++
  }
  const count = document.querySelector("[data-icons-count]")
  if (count) {
    count.textContent = q
      ? `${shown} of ${tiles.length} icons`
      : `${tiles.length} icons`
  }
  const empty = document.querySelector<HTMLElement>("[data-icons-empty]")
  if (empty) empty.hidden = shown > 0
}

/** A token of the XAML and its code color (the classes of app/style.css). */
type XamlToken = [text: string, className?: string]

/**
 * The XAML for the picked icon, as the docs write it: Classes, then Kind or
 * Data, then Foreground; one attribute per line, under the first, when they
 * do not fit the details' width on one.
 */
function iconXaml(detail: HTMLElement): XamlToken[][] {
  const { icon: name, kind, size, color, element } = detail.dataset
  const tag = element === "icon" ? "uikit:Icon" : "PathIcon"
  const attributes: [string, string][] = []
  if (size !== "medium") attributes.push(["Classes", size!])
  attributes.push(
    element === "icon"
      ? ["Kind", kind!]
      : ["Data", `{StaticResource UIKit.Icon.${name}}`]
  )
  const resource = detail.querySelector<HTMLElement>(
    `[data-icon-color="${color}"]`
  )?.dataset.resource
  if (resource) {
    attributes.push(["Foreground", `{DynamicResource ${resource}}`])
  }
  const oneLine =
    `<${tag} ${attributes.map(([n, v]) => `${n}="${v}"`).join(" ")} />`
      .length <= 46
  const lines: XamlToken[][] = []
  for (const [i, [attribute, value]] of attributes.entries()) {
    const tokens: XamlToken[] = [
      [attribute, "c-type"],
      ["="],
      [`"${value}"`, "c-str"],
    ]
    if (i === 0) lines.push([[`<${tag}`, "c-kw"], [" "], ...tokens])
    else if (oneLine) lines[0].push([" "], ...tokens)
    else lines.push([[" ".repeat(tag.length + 2)], ...tokens])
  }
  lines.at(-1)!.push([" "], ["/>", "c-kw"])
  return lines
}

function renderIconCode(detail: HTMLElement) {
  const lines = iconXaml(detail)
  // A wrapped line continues two columns in from the attributes.
  const hang = `${(detail.dataset.element === "icon" ? 10 : 8) + 4}ch`
  detail.querySelector("[data-icon-code]")?.replaceChildren(
    ...lines.map((tokens) => {
      const line = document.createElement("span")
      line.className = "line"
      line.style.setProperty("--hang", hang)
      for (const [text, className] of tokens) {
        if (!className) {
          line.append(text)
          continue
        }
        const span = document.createElement("span")
        span.className = className
        span.textContent = text
        line.append(span)
      }
      return line
    })
  )
  const copy = detail.querySelector<HTMLElement>("[data-icon-code-copy]")
  if (copy) {
    copy.dataset.copy = lines
      .map((tokens) => tokens.map(([text]) => text).join(""))
      .join("\n")
  }
}

/**
 * Marks the chosen size, color and element. `data-syntax` is the element
 * chosen; `data-element` is the one shown, PathIcon for an icon that no
 * IconName draws.
 */
function updateIconChoices(detail: HTMLElement) {
  const { syntax, kind } = detail.dataset
  detail.dataset.element = syntax === "icon" && kind ? "icon" : "path"
  for (const [group, value] of [
    ["size", detail.dataset.size],
    ["color", detail.dataset.color],
    ["syntax", detail.dataset.element],
  ]) {
    for (const option of detail.querySelectorAll(`[data-icon-${group}]`)) {
      option.setAttribute(
        "aria-checked",
        String(option.getAttribute(`data-icon-${group}`) === value)
      )
    }
  }
  const color = detail.querySelector<HTMLElement>(
    `[data-icon-color="${detail.dataset.color}"]`
  )
  if (color?.dataset.css) {
    detail.style.setProperty("--icon-color", color.dataset.css)
  }
  renderIconCode(detail)
}

function setText(root: HTMLElement, selector: string, text: string) {
  const element = root.querySelector(selector)
  if (element) element.textContent = text
}

function showIcon(tile: HTMLElement) {
  const detail = iconDetail()
  if (!detail) return
  const name = tile.dataset.iconTile!
  const { kind, file, faintOpacity } = tile.dataset
  const key = `UIKit.Icon.${name}`
  for (const t of iconTiles()) {
    t.setAttribute("aria-pressed", String(t === tile))
  }
  detail.dataset.icon = name
  if (kind) detail.dataset.kind = kind
  else delete detail.dataset.kind
  detail.toggleAttribute("data-no-kind", !kind)
  detail.toggleAttribute("data-faint", !!faintOpacity)
  setText(detail, "[data-icon-detail-name]", name)
  setText(detail, "[data-icon-detail-file]", `${file}.svg`)
  setText(detail, "[data-icon-detail-key]", key)
  setText(detail, "[data-icon-detail-kind]", kind ?? "")
  setText(detail, "[data-icon-faint-key]", `${key}.Faint`)
  setText(
    detail,
    "[data-icon-faint-opacity]",
    `${Math.round(Number(faintOpacity) * 100)}%`
  )
  for (const [selector, value] of [
    ["[data-icon-key-copy]", key],
    ["[data-icon-kind-copy]", kind ?? ""],
  ]) {
    const button = detail.querySelector<HTMLElement>(selector)
    if (button) button.dataset.copy = value
  }
  const iconOption = detail.querySelector<HTMLButtonElement>(
    '[data-icon-syntax="icon"]'
  )
  if (iconOption) iconOption.disabled = !kind
  const svg = tile.querySelector("svg")
  for (const preview of detail.querySelectorAll("[data-icon-preview]")) {
    preview.replaceChildren(...(svg ? [svg.cloneNode(true)] : []))
  }
  updateIconChoices(detail)
  detail.hidden = false
  document
    .querySelector("[data-icons-layout]")
    ?.setAttribute("data-detail-open", "")
  history.replaceState(
    history.state,
    "",
    `?icon=${encodeURIComponent(name)}`
  )
  // Opening the details narrows the grid, which moves the tiles.
  tile.scrollIntoView({ block: "nearest" })
  revealIconDetail(detail)
}

/**
 * Beside the grid, the details stick under the top bar until the grid ends,
 * then scroll away with it. If a pick near the end leaves them pushed up,
 * scroll back until they are whole; the picked icon, above the grid's end,
 * stays in view.
 */
function revealIconDetail(detail: HTMLElement) {
  const style = getComputedStyle(detail)
  if (style.position !== "sticky") return
  const pushed = Number.parseFloat(style.top) - detail.getBoundingClientRect().top
  if (pushed > 1) window.scrollBy({ top: -pushed })
}

function hideIcon() {
  const detail = iconDetail()
  if (!detail || detail.hidden) return
  const tile = iconTiles().find(
    (t) => t.getAttribute("aria-pressed") === "true"
  )
  detail.hidden = true
  document
    .querySelector("[data-icons-layout]")
    ?.removeAttribute("data-detail-open")
  tile?.setAttribute("aria-pressed", "false")
  history.replaceState(history.state, "", location.pathname)
  tile?.focus({ preventScroll: true })
}

/** Moves along the grid with the arrow keys, and shows the icon if the details are open. */
function moveIconFocus(tile: HTMLElement, key: string) {
  const tiles = iconTiles().filter((t) => !t.parentElement!.hidden)
  const secondRow = tiles.findIndex((t) => t.offsetTop !== tiles[0].offsetTop)
  const columns = secondRow === -1 ? tiles.length : secondRow
  const step: Record<string, number> = {
    ArrowLeft: -1,
    ArrowRight: 1,
    ArrowUp: -columns,
    ArrowDown: columns,
  }
  const next = tiles[tiles.indexOf(tile) + (step[key] ?? 0)]
  if (!next || next === tile) return
  next.focus()
  if (!iconDetail()?.hidden) showIcon(next)
}

document.addEventListener("click", (event) => {
  const target = event.target as Element
  const tile = target.closest<HTMLElement>("[data-icon-tile]")
  if (tile) {
    showIcon(tile)
    return
  }
  const detail = iconDetail()
  if (!detail?.contains(target)) return
  if (target.closest("[data-icon-detail-close]")) {
    hideIcon()
    return
  }
  for (const group of ["size", "color", "syntax"]) {
    const option = target.closest<HTMLElement>(`[data-icon-${group}]`)
    if (!option) continue
    detail.dataset[group] = option.getAttribute(`data-icon-${group}`)!
    updateIconChoices(detail)
    return
  }
})

function showLinkedIcon() {
  const name = new URLSearchParams(location.search).get("icon")
  const tile = iconTiles().find((t) => t.dataset.iconTile === name)
  if (!tile) return
  showIcon(tile)
  tile.scrollIntoView({ block: "center" })
  revealIconDetail(iconDetail()!)
}

// Table of contents: mark the section being read on the rail, the last
// heading above the upper third of the window.
function watchTableOfContents() {
  const links = [
    ...document.querySelectorAll<HTMLAnchorElement>("[data-toc] a[href^='#']"),
  ]
  if (links.length === 0) return
  const headings = links
    .map((link) =>
      document.getElementById(decodeURIComponent(link.hash.slice(1)))
    )
    .filter((heading): heading is HTMLElement => !!heading)
  let frame = 0
  const update = () => {
    frame = 0
    const line = Math.max(140, window.innerHeight * 0.3)
    const current = headings.findLast(
      (heading) => heading.getBoundingClientRect().top < line
    )
    for (const link of links) {
      link.dataset.active = String(
        !!current && link.hash.slice(1) === current.id
      )
    }
  }
  const schedule = () => {
    frame ||= requestAnimationFrame(update)
  }
  window.addEventListener("scroll", schedule, { passive: true })
  window.addEventListener("resize", schedule, { passive: true })
  update()
}

// Keep the current page in view in the sidebar.
function revealActiveSidebarItem() {
  const sidebar = document.querySelector<HTMLElement>("[data-docs-sidebar]")
  const item = sidebar?.querySelector<HTMLElement>("[aria-current='page']")
  if (!sidebar || !item) return
  if (item.offsetTop > sidebar.clientHeight - 80) {
    sidebar.scrollTop = item.offsetTop - sidebar.clientHeight / 2
  }
}

function init() {
  watchTableOfContents()
  revealActiveSidebarItem()
  showLinkedIcon()
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", init)
} else {
  init()
}
