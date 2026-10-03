// @ts-check
/**
 * Tabs: selecting a tab with the pointer, Enter or Space, and moving focus
 * between tabs with the arrow keys, Home and End (activating on focus when
 * the list has `data-activate-on-focus`), like Base UI. Without this script
 * the server-selected panel is shown.
 */
import { arrowTarget, delegate, ownItems } from "./core.js"

const ROOT = '[data-slot="tabs"]'
const TAB = '[data-slot="tabs-trigger"]'

/** @param {HTMLElement} tab */
const disabled = (tab) => tab.getAttribute("aria-disabled") === "true"

/** @param {HTMLElement} tab */
function select(tab) {
  const root = tab.closest(ROOT)
  if (!root || disabled(tab)) return
  for (const item of ownItems(root, ROOT, TAB)) {
    const active = item === tab
    item.setAttribute("aria-selected", String(active))
    item.toggleAttribute("data-active", active)
    item.tabIndex = active ? 0 : -1
    const panel = document.getElementById(
      item.getAttribute("aria-controls") ?? ""
    )
    panel?.toggleAttribute("hidden", !active)
  }
}

delegate("click", TAB, (_event, tab) => select(tab))

delegate("keydown", TAB, (event, tab) => {
  const root = tab.closest(ROOT)
  const list = tab.closest('[role="tablist"]')
  if (!root || !(list instanceof HTMLElement)) return
  // Like Base UI, disabled tabs can be focused but not selected.
  const target = arrowTarget(event, ownItems(root, ROOT, TAB), tab, {
    orientation:
      list.getAttribute("aria-orientation") === "vertical"
        ? "vertical"
        : "horizontal",
    loop: list.dataset.loopFocus !== "false",
  })
  if (!target) return
  event.preventDefault()
  target.focus()
  if ("activateOnFocus" in list.dataset) select(target)
})
