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
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", init)
} else {
  init()
}
