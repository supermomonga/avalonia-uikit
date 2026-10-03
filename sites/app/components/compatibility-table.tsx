import { components, uncovered } from "@/lib/catalog"
import { StatusBadge } from "./docs-page"

/** Every component with its Avalonia control(s), status and optional package (lib/catalog.ts). */
export function CompatibilityTable() {
  return (
    <div class="typeset-scroll scroll-fade-x scrollbar-none *:[table]:w-full">
      <table>
        <thead>
          <tr>
            <th>Component</th>
            <th>GPUI Kit</th>
            <th>Avalonia</th>
            <th>Status</th>
            <th>Package</th>
          </tr>
        </thead>
        <tbody>
          {components.map((entry) => (
            <tr>
              <td>
                <a href={`/docs/components/${entry.slug}`}>{entry.title}</a>
              </td>
              <td>{entry.gpui}</td>
              <td>
                {entry.avalonia.map((control, index) => (
                  <>
                    {index > 0 && ", "}
                    <code>{control}</code>
                  </>
                ))}
              </td>
              <td>
                <StatusBadge status={entry.status} />
              </td>
              <td>{entry.package ? <code>{entry.package}</code> : "–"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/** GPUI Kit components the library does not port. */
export function UncoveredList() {
  return (
    <ul>
      {uncovered.map((name) => (
        <li>{name}</li>
      ))}
    </ul>
  )
}
