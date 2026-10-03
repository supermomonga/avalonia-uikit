import path from "node:path"
import ssg from "@hono/vite-ssg"
import mdx from "@mdx-js/rollup"
import tailwindcss from "@tailwindcss/vite"
import honox from "honox/vite"
import remarkFrontmatter from "remark-frontmatter"
import remarkGfm from "remark-gfm"
import remarkMdxFrontmatter from "remark-mdx-frontmatter"
import { defineConfig } from "vite"
import { rehypeCode } from "./app/lib/mdx/rehype-code.ts"
import { remarkToc } from "./app/lib/mdx/remark-toc.ts"

const siteDir = import.meta.dirname
// The demos' XAML (samples/AvaloniaUIKit.Demos) is read from the repository.
const repositoryRoot = path.resolve(siteDir, "..")

export default defineConfig(({ mode }) => ({
  plugins: [
    honox({
      client: {
        input: ["/app/client.ts", "/app/avalonia-demo.ts", "/app/style.css"],
      },
    }),
    tailwindcss(),
    ...(mode === "client"
      ? []
      : [
          {
            enforce: "pre" as const,
            ...mdx({
              jsxImportSource: "hono/jsx",
              remarkPlugins: [
                remarkGfm,
                remarkFrontmatter,
                remarkMdxFrontmatter,
                remarkToc,
              ],
              rehypePlugins: [rehypeCode],
            }),
          },
          ssg({ entry: "./app/server.ts" }),
        ]),
  ],
  build: { emptyOutDir: mode === "client" },
  resolve: {
    alias: {
      "@/components/ui": path.join(siteDir, "components/ui"),
      "@": path.join(siteDir, "app"),
    },
  },
  server: { fs: { allow: [repositoryRoot] } },
}))
