import {
  createHighlighter,
  type Highlighter,
  type ThemeRegistration,
} from "shiki"

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

/**
 * A theme of CSS variables (app/style.css), so code follows the site's light
 * and dark themes: the code colors of gpui-kit.com, which take after Xcode's
 * classic themes.
 */
const theme: ThemeRegistration = {
  name: "uikit",
  type: "light",
  colors: {
    "editor.foreground": "var(--code-fg)",
    "editor.background": "var(--code-bg)",
  },
  tokenColors: [
    {
      scope: ["comment", "punctuation.definition.comment"],
      settings: { foreground: "var(--code-comment)" },
    },
    {
      scope: ["string", "punctuation.definition.string", "string.quoted"],
      settings: { foreground: "var(--code-string)" },
    },
    {
      scope: [
        "entity.name.tag",
        "keyword",
        "storage",
        "storage.type",
        "storage.modifier",
        "constant.language",
        "punctuation.definition.tag",
      ],
      settings: { foreground: "var(--code-keyword)" },
    },
    {
      scope: [
        "entity.other.attribute-name",
        "support.type.property-name",
        "entity.name.type",
        "entity.name.class",
        "support.class",
        "support.type",
      ],
      settings: { foreground: "var(--code-type)" },
    },
    {
      scope: [
        "entity.name.function",
        "support.function",
        "constant.numeric",
        "constant.character",
        "variable.parameter",
      ],
      settings: { foreground: "var(--code-fn)" },
    },
  ],
}

let highlighter: Promise<Highlighter> | undefined

/** Code highlighted at build time, colored by the site's CSS variables. */
export async function highlight(code: string, lang = "xml"): Promise<string> {
  highlighter ??= createHighlighter({ themes: [theme], langs: [...LANGUAGES] })
  const h = await highlighter
  const language = (LANGUAGES as readonly string[]).includes(lang)
    ? lang
    : "text"
  return h.codeToHtml(code.replace(/\n$/, ""), {
    lang: language,
    theme: "uikit",
    transformers: [
      {
        pre(node) {
          node.properties.class = "shiki"
          delete node.properties.style
          delete node.properties.tabindex
        },
      },
    ],
  })
}
