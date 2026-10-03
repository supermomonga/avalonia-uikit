/**
 * The demos' XAML (samples/AvaloniaUIKit.Demos/Demos/<Component>/<Name>.axaml),
 * read at build time. A demo's id is `<component-slug>/<name-slug>`, and the
 * site shows the content of the root element as the code example
 * (docs/site.md).
 */
const files = import.meta.glob<string>(
  "../../../samples/AvaloniaUIKit.Demos/Demos/**/*.axaml",
  { query: "?raw", import: "default", eager: true }
)

/** PascalCase to kebab-case, as scripts/demo-registry.ts does (`ButtonGroup` → `button-group`). */
export const kebab = (name: string) =>
  name
    .replace(/([a-z0-9])([A-Z])/g, "$1-$2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1-$2")
    .toLowerCase()

function idOf(file: string): string | undefined {
  const match = /Demos\/([A-Za-z0-9]+)\/([A-Za-z0-9]+)\.axaml$/.exec(file)
  return match ? `${kebab(match[1])}/${kebab(match[2])}` : undefined
}

const sources = new Map<string, string>(
  Object.entries(files).flatMap(([file, xaml]) => {
    const id = idOf(file)
    return id ? [[id, innerXaml(xaml)]] : []
  })
)

/** The end of the root element's start tag: the first `>` outside quotes. */
function startTagEnd(xaml: string, from: number): number {
  let quote: string | undefined
  for (let i = from; i < xaml.length; i++) {
    const char = xaml[i]
    if (quote) {
      if (char === quote) quote = undefined
    } else if (char === '"' || char === "'") {
      quote = char
    } else if (char === ">") {
      return i
    }
  }
  return -1
}

/** The content of the root element, with the common indentation removed. */
export function innerXaml(xaml: string): string {
  const open = /<([A-Za-z_][\w.:-]*)/.exec(
    xaml.replace(/<\?xml[^>]*\?>/, "").replace(/<!--[\s\S]*?-->/g, "")
  )
  if (!open) return xaml.trim()
  const name = open[1]
  const start = xaml.indexOf(`<${name}`)
  const end = startTagEnd(xaml, start)
  const close = xaml.lastIndexOf(`</${name}`)
  if (start === -1 || end === -1 || close <= end) return xaml.trim()
  return dedent(xaml.slice(end + 1, close))
}

function dedent(text: string): string {
  const lines = text.replace(/\r\n/g, "\n").split("\n")
  while (lines.length > 0 && lines[0].trim() === "") lines.shift()
  while (lines.length > 0 && lines[lines.length - 1].trim() === "") lines.pop()
  const indent = Math.min(
    ...lines
      .filter((line) => line.trim() !== "")
      .map((line) => /^[ \t]*/.exec(line)?.[0].length ?? 0)
  )
  return lines
    .map((line) => (line.trim() === "" ? "" : line.slice(indent)))
    .join("\n")
}

/** The code example of the demo, or undefined when there is no such XAML. */
export function demoSource(id: string): string | undefined {
  return sources.get(id)
}

/** Every demo id, in order. */
export function demoIds(): string[] {
  return [...sources.keys()].sort()
}
