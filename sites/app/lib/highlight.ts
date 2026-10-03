import { createHighlighter, type Highlighter } from "shiki"

/** XAML is highlighted as XML. */
const LANGUAGES = [
  "xml",
  "csharp",
  "bash",
  "json",
  "jsonc",
  "toml",
  "yaml",
  "text",
  "diff",
] as const

let highlighter: Promise<Highlighter> | undefined

/** Code highlighted at build time with GitHub's themes. */
export async function highlight(code: string, lang = "xml"): Promise<string> {
  highlighter ??= createHighlighter({
    themes: ["github-light", "github-dark"],
    langs: [...LANGUAGES],
  })
  const h = await highlighter
  const language = (LANGUAGES as readonly string[]).includes(lang)
    ? lang
    : "text"
  return h.codeToHtml(code.replace(/\n$/, ""), {
    lang: language,
    themes: { light: "github-light", dark: "github-dark" },
    defaultColor: false,
    transformers: [
      {
        pre(node) {
          node.properties.class =
            "no-scrollbar min-w-0 overflow-x-auto overflow-y-auto overscroll-x-contain overscroll-y-auto px-4 py-3.5 outline-none !bg-transparent"
          delete node.properties.style
          delete node.properties.tabindex
        },
        line(node) {
          node.properties["data-line"] = ""
        },
      },
    ],
  })
}
