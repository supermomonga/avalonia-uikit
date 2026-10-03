// @ts-check
/**
 * Shared helpers for the optional client scripts of shadcnui-hono-jsx
 * (docs/adr/0025). Components render complete, accessible markup on the
 * server; a script only adds behavior the browser does not provide.
 *
 * Behaviors listen on the document, never on each component, and find
 * components by their `data-slot` attributes, so markup rendered later (htmx
 * swaps, streaming) works without initialization. Behaviors that keep state
 * per element (scroll area observers, toast timers) also watch the document
 * for inserted markup. Module scripts run once per page, however many
 * components use them.
 */

/**
 * Calls `handler` for events whose target is inside an element matching
 * `selector`, with that element. Each call adds one document listener.
 *
 * @template {keyof DocumentEventMap} T
 * @param {T} type
 * @param {string} selector
 * @param {(event: DocumentEventMap[T], element: HTMLElement) => void} handler
 * @param {AddEventListenerOptions} [options]
 */
export function delegate(type, selector, handler, options) {
  document.addEventListener(
    type,
    (event) => {
      const target = event.target
      const element =
        target instanceof Element ? target.closest(selector) : null
      if (element instanceof HTMLElement) handler(event, element)
    },
    options
  )
}

/**
 * Elements matching `selector` inside `root` that belong to it, not to a
 * nested component with the same `rootSelector`.
 *
 * @param {Element} root
 * @param {string} rootSelector
 * @param {string} selector
 * @returns {HTMLElement[]}
 */
export function ownItems(root, rootSelector, selector) {
  return [...root.querySelectorAll(selector)].filter(
    /** @returns {item is HTMLElement} */
    (item) => item instanceof HTMLElement && item.closest(rootSelector) === root
  )
}

/**
 * The item an arrow, Home or End key moves to in a composite widget, or
 * null for other keys. Right and left follow the reading direction.
 *
 * @param {KeyboardEvent} event
 * @param {HTMLElement[]} items focusable items, in order
 * @param {HTMLElement} current
 * @param {{ orientation: "horizontal" | "vertical", loop: boolean }} options
 * @returns {HTMLElement | null}
 */
export function arrowTarget(event, items, current, { orientation, loop }) {
  const rtl = getComputedStyle(current).direction === "rtl"
  const next =
    orientation === "vertical" ? "ArrowDown" : rtl ? "ArrowLeft" : "ArrowRight"
  const previous =
    orientation === "vertical" ? "ArrowUp" : rtl ? "ArrowRight" : "ArrowLeft"
  const index = items.indexOf(current)
  const last = items.length - 1
  if (event.key === "Home") return items[0] ?? null
  if (event.key === "End") return items[last] ?? null
  if (event.key === next) {
    return items[index < last ? index + 1 : loop ? 0 : last] ?? null
  }
  if (event.key === previous) {
    return items[index > 0 ? index - 1 : loop ? last : 0] ?? null
  }
  return null
}
