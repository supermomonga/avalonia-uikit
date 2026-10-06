/**
 * Generates samples/AvaloniaUIKit.Demo.ControlCatalog/Catalog/Catalog.g.cs, the
 * control catalog app's table of components, from what the site is built from
 * (docs/control-catalog.md):
 *
 * - the components (app/lib/catalog.ts): the title, the sidebar group, the
 *   Avalonia controls, the package;
 * - each component's page (content/components/<slug>.mdx): the description and
 *   the demos in the page's order, each with its title, the heading it is under
 *   and the paragraph before it;
 * - the code colors of every theme (app/style.css, app/styles/themes.g.css), so
 *   that the app highlights XAML as the site does.
 *
 *   bun sites/scripts/control-catalog.ts          # write
 *   bun sites/scripts/control-catalog.ts --check  # fail if the file is stale
 */
import { readFileSync, writeFileSync } from "node:fs"
import path from "node:path"
import { components, type ComponentEntry } from "../app/lib/catalog"
import { listDemos } from "./demo-registry"

const root = path.resolve(import.meta.dirname, "../..")
const contentDir = path.join(root, "sites/content/components")
const output = path.join(root, "samples/AvaloniaUIKit.Demo.ControlCatalog/Catalog/Catalog.g.cs")

interface Demo {
  id: string
  title?: string
  heading?: string
  description?: string
}

interface Page {
  description: string
  demos: Demo[]
}

/** Markdown inline text as the app shows it: links and emphasis lose their markup, `code` stays. */
function inline(text: string): string {
  return text
    .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1")
    .replace(/\*\*([^*]+)\*\*/g, "$1")
    .replace(/\s+/g, " ")
    .trim()
}

/** A block that is a paragraph of prose (not a table, list, code, heading or component). */
function isParagraph(block: string): boolean {
  return !/^(#|\||```|<|- |\* |\d+\. |>)/.test(block)
}

function readPage(entry: ComponentEntry): Page {
  const file = path.join(contentDir, `${entry.slug}.mdx`)
  const text = readFileSync(file, "utf8").replace(/\r\n/g, "\n")
  const front = /^---\n([\s\S]*?)\n---\n/.exec(text)
  const description = /^description:\s*"((?:[^"\\]|\\.)*)"/m.exec(front?.[1] ?? "")?.[1] ?? ""
  const body = front ? text.slice(front[0].length) : text

  // Blocks are separated by blank lines; fenced code is one block however many blank lines it holds.
  const blocks: string[] = []
  let current: string[] = []
  let fenced = false
  for (const line of body.split("\n")) {
    if (line.startsWith("```")) fenced = !fenced
    if (!fenced && line.trim() === "") {
      if (current.length > 0) blocks.push(current.join("\n"))
      current = []
    } else {
      current.push(line)
    }
  }
  if (current.length > 0) blocks.push(current.join("\n"))

  const demos: Demo[] = []
  let heading: string | undefined
  let paragraph: string | undefined
  let lead: string | undefined
  for (const block of blocks) {
    const title = /^#{2,3}\s+(.*)$/.exec(block)
    if (title) {
      heading = inline(title[1])
      paragraph = undefined
      continue
    }
    const demo = /^<Demo\s+name="([^"]+)"(?:\s+title="([^"]*)")?\s*\/>$/.exec(block.trim())
    if (demo) {
      demos.push({ id: demo[1], title: demo[2], heading, description: paragraph })
      paragraph = undefined
      continue
    }
    if (isParagraph(block)) {
      paragraph = inline(block)
      // The page's first demo has no title: the usage text says what it shows ("The demo has ...").
      if (/^The demos? /.test(paragraph)) lead ??= paragraph
    }
  }
  if (demos.length > 0 && !demos[0].title && !demos[0].description) {
    demos[0].description = lead
  }
  return { description: inline(description), demos }
}

const titleOf = (slug: string) =>
  slug
    .split("-")
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ")

/** The sidebar sections of the site (docs/site.md): new controls, third-party controls, the rest. */
function sectionOf(entry: ComponentEntry): string {
  if (entry.library) return "ThirdParty"
  return entry.status === "new" ? "UIKit" : "Avalonia"
}

// --- Code colors -----------------------------------------------------------

const CODE_VARS = ["bg", "fg", "keyword", "string", "comment", "fn", "type"] as const

function codeColors(css: string, selector: string): string[] | undefined {
  const start = css.indexOf(`${selector} {`)
  if (start < 0) return undefined
  const block = css.slice(start, css.indexOf("}", start))
  const colors = CODE_VARS.map((name) => new RegExp(`--code-${name}:\\s*(#[0-9a-fA-F]{6})\\s*;`).exec(block)?.[1])
  return colors.every((color) => color) ? (colors as string[]) : undefined
}

interface ThemeInfo {
  id: string
  name: string
}

function palettes(): [string, string[]][] {
  const base = readFileSync(path.join(root, "sites/app/style.css"), "utf8")
  const bundled = readFileSync(path.join(root, "sites/app/styles/themes.g.css"), "utf8")
  const themes = (JSON.parse(readFileSync(path.join(root, "sites/app/lib/themes.g.json"), "utf8")) as { themes: ThemeInfo[] }).themes
  const result: [string, string[]][] = []
  const light = codeColors(base, ":root")
  const dark = codeColors(base, ".dark")
  if (!light || !dark) throw new Error("sites/app/style.css: :root and .dark must set every --code-* color")
  result.push(["Default Light", light], ["Default Dark", dark])
  for (const theme of themes) {
    const colors = codeColors(bundled, `html[data-theme="${theme.id}"]`)
    if (!colors) throw new Error(`themes.g.css: no code colors for ${theme.id}`)
    result.push([theme.name, colors])
  }
  return result
}

// --- Output ----------------------------------------------------------------

const str = (value: string | undefined) => (value === undefined ? "null" : JSON.stringify(value))
const argb = (hex: string) => `0xFF${hex.slice(1).toUpperCase()}`

function render(): string {
  const files = listDemos()
  const registered = files.map((demo) => demo.id)
  const known = new Set(registered)
  // `Button/Variants`: the XAML's path under the demos' directory, without the extension.
  const sourceOf = (id: string) => {
    const file = files.find((demo) => demo.id === id)!.file
    return path.relative(path.join(root, "samples/AvaloniaUIKit.Demos/Demos"), file).replace(/\\/g, "/").replace(/\.axaml$/, "")
  }
  const used = new Set<string>()
  const entries = components.map((entry) => {
    const page = readPage(entry)
    for (const demo of page.demos) {
      if (!known.has(demo.id)) throw new Error(`${entry.slug}.mdx: no demo ${demo.id}`)
      used.add(demo.id)
    }
    // Demos the page does not show still belong to the component.
    for (const id of registered) {
      if (id.startsWith(`${entry.slug}/`) && !page.demos.some((demo) => demo.id === id)) {
        page.demos.push({ id, title: titleOf(id.slice(entry.slug.length + 1)) })
        used.add(id)
      }
    }
    const demos = page.demos
      .map(
        (demo) =>
          `                new(${str(demo.id)}, ${str(sourceOf(demo.id))}, ${str(demo.title)}, ${str(demo.heading)}, ${str(demo.description)}),`
      )
      .join("\n")
    const controls = entry.avalonia.map((control) => JSON.stringify(control)).join(", ")
    return `        new(
            ${str(entry.slug)}, ${str(entry.title)}, ${str(entry.aliases)},
            CatalogSection.${sectionOf(entry)}, ${str(entry.group)}, [${controls}],
            ${str(entry.package)}, ${str(entry.library)},
            ${str(page.description)},
            [
${demos}
            ]),`
  })
  const orphans = registered.filter((id) => !used.has(id))
  if (orphans.length > 0) throw new Error(`demos of no component: ${orphans.join(", ")}`)

  const colors = palettes()
    .map(([name, values]) => `        [${str(name)}] = new(${values.map(argb).join(", ")}),`)
    .join("\n")

  return `// <auto-generated/>
// Generated by \`bun sites/scripts/control-catalog.ts\` from sites/app/lib/catalog.ts,
// sites/content/components/*.mdx and the site's code colors. Do not edit.
using AvaloniaUIKit.Demo.ControlCatalog.Code;

namespace AvaloniaUIKit.Demo.ControlCatalog;

public static partial class Catalog
{
    /// <summary>Every component, in the site's catalog order.</summary>
    public static IReadOnlyList<CatalogComponent> Components { get; } =
    [
${entries.join("\n")}
    ];

    /// <summary>The site's code colors by GPUI Kit theme name (app/style.css, app/styles/themes.g.css).</summary>
    public static IReadOnlyDictionary<string, CodePalette> CodePalettes { get; } = new Dictionary<string, CodePalette>(StringComparer.OrdinalIgnoreCase)
    {
${colors}
    };
}
`
}

if (import.meta.main) {
  const text = render()
  if (process.argv.includes("--check")) {
    let current = ""
    try {
      current = readFileSync(output, "utf8")
    } catch {}
    if (current !== text) {
      console.error(`${path.relative(root, output)} is stale: run \`bun sites/scripts/control-catalog.ts\``)
      process.exit(1)
    }
    console.log(`${path.relative(root, output)} is up to date.`)
  } else {
    writeFileSync(output, text)
    console.log(`Wrote ${path.relative(root, output)} (${components.length} components).`)
  }
}
