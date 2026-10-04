import { components, uncovered } from "@/lib/catalog"
import { StatusLabel } from "./docs-layout"

/** Every component with its Avalonia control(s), status and optional package (lib/catalog.ts). */
export function CompatibilityTable() {
  return (
    <div class="doc-table">
      <table>
        <thead>
          <tr>
            <th>Component</th>
            <th>Avalonia</th>
            <th>Status</th>
            <th>Package</th>
          </tr>
        </thead>
        <tbody>
          {components.map((entry) => (
            <tr>
              <td>
                <a href={`/components/${entry.slug}`}>{entry.title}</a>
              </td>
              <td>
                {entry.avalonia.map((control, index) => (
                  <>
                    {index > 0 && ", "}
                    <code>{control}</code>
                  </>
                ))}
              </td>
              <td>
                <StatusLabel status={entry.status} />
              </td>
              <td>{entry.package ? <code>{entry.package}</code> : "–"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** Components the library does not cover. */
export function UncoveredList() {
  return (
    <ul>
      {uncovered.map((name) => (
        <li>{name}</li>
      ))}
    </ul>
  )
}
