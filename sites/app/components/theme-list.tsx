import { bundledThemes } from "@/lib/themes"

/** The `UIKitThemeVariants` property of a theme, as the generator names it: `macOS Classic Light` -> `MacOSClassicLight`. */
const property = (name: string) =>
  name
    .split(/[^A-Za-z0-9]+/)
    .filter(Boolean)
    .map((part) => part[0].toUpperCase() + part.slice(1))
    .join("")

/** The bundled color themes, for the Theming guide (content/docs/theming.mdx). */
export function ThemeList() {
  return (
    <div class="doc-table">
      <table>
        <thead>
          <tr>
            <th>Theme</th>
            <th>Mode</th>
            <th>Variant</th>
          </tr>
        </thead>
        <tbody>
          {bundledThemes.map((theme) => (
            <tr>
              <td>
                <span class="inline-flex items-center gap-2">
                  <span
                    class="size-3.5 shrink-0 rounded-full shadow-hairline"
                    style={{
                      background: `linear-gradient(135deg, ${theme.swatch[0]} 50%, ${theme.swatch[1]} 50%)`,
                    }}
                  />
                  {theme.name}
                </span>
              </td>
              <td>{theme.mode === "dark" ? "Dark" : "Light"}</td>
              <td>
                <code>UIKitThemeVariants.{property(theme.name)}</code>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
