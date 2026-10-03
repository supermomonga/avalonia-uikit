/**
 * The site's client script on every page. HonoX's `<Script>` renders only on
 * pages with islands; the site's behaviors (copy buttons, the theme toggle,
 * package manager tabs) run on every page.
 */
export function SiteScript({ src }: { src: string }) {
  if (!import.meta.env.PROD) return <script type="module" src={src} />
  const manifests = import.meta.glob<{
    default: Record<string, { file: string }>
  }>("/dist/.vite/manifest.json", { eager: true })
  const manifest = Object.values(manifests)[0]?.default
  const entry = manifest?.[src.replace(/^\//, "")]
  if (!entry) throw new Error(`${src} is not in the client build manifest`)
  return <script type="module" src={`/${entry.file}`} />
}
