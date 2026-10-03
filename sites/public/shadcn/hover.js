// @ts-check
/**
 * Tooltips and hover cards: the trigger (`data-hover-popup`) opens a manual
 * native popover after a delay when hovered or focused from the keyboard, the
 * popup stays open while it is hovered, and it closes after a delay when the
 * pointer leaves, on blur, on Escape and when the trigger is pressed, like
 * Base UI. Only one hover popup is open at a time.
 */
import { delegate } from "./core.js"

const TRIGGER = "[data-hover-popup]"

/** @type {WeakMap<HTMLElement, ReturnType<typeof setTimeout>>} */
const timers = new WeakMap()

/** @param {HTMLElement} trigger */
const popupOf = (trigger) =>
  document.getElementById(trigger.dataset.hoverPopup ?? "")

/** @param {HTMLElement} popup */
const triggerOf = (popup) =>
  /** @type {HTMLElement | null} */ (
    document.querySelector(`[data-hover-popup="${CSS.escape(popup.id)}"]`)
  )

/**
 * @param {HTMLElement} popup
 * @param {() => void} action
 * @param {number} delay
 */
function schedule(popup, action, delay) {
  clearTimeout(timers.get(popup))
  if (delay <= 0) action()
  else timers.set(popup, setTimeout(action, delay))
}

/** @param {HTMLElement} trigger */
function open(trigger) {
  const popup = popupOf(trigger)
  if (!popup) return
  clearTimeout(timers.get(popup))
  if (popup.matches(":popover-open")) return
  for (const other of document.querySelectorAll("[popover]:popover-open")) {
    if (other !== popup && other instanceof HTMLElement && triggerOf(other)) {
      close(other)
    }
  }
  popup.showPopover()
  trigger.toggleAttribute("data-popup-open", true)
}

/** @param {HTMLElement} popup */
function close(popup) {
  clearTimeout(timers.get(popup))
  if (popup.matches(":popover-open")) popup.hidePopover()
  triggerOf(popup)?.toggleAttribute("data-popup-open", false)
}

/**
 * @param {string | undefined} value
 * @param {number} fallback
 */
const delayOf = (value, fallback) =>
  value === undefined ? fallback : Number(value) || 0

// The pointer entering or leaving a trigger or its popup (these events do not
// bubble, so they are captured).
document.addEventListener(
  "pointerenter",
  (event) => {
    const target = event.target
    if (!(target instanceof HTMLElement) || event.pointerType === "touch") {
      return
    }
    if (target.matches(TRIGGER)) {
      const popup = popupOf(target)
      if (popup) {
        schedule(popup, () => open(target), delayOf(target.dataset.delay, 0))
      }
    } else if (target.matches("[popover]") && triggerOf(target)) {
      // Moving onto the popup keeps it open.
      clearTimeout(timers.get(target))
    }
  },
  true
)

document.addEventListener(
  "pointerleave",
  (event) => {
    const target = event.target
    if (!(target instanceof HTMLElement)) return
    const trigger = target.matches(TRIGGER)
      ? target
      : target.matches("[popover]")
        ? triggerOf(target)
        : null
    const popup = trigger ? popupOf(trigger) : null
    if (!trigger || !popup) return
    schedule(popup, () => close(popup), delayOf(trigger.dataset.closeDelay, 0))
  },
  true
)

// Keyboard focus opens right away; losing focus closes.
delegate("focusin", TRIGGER, (_event, trigger) => {
  if (trigger.matches(":focus-visible")) open(trigger)
})

delegate("focusout", TRIGGER, (_event, trigger) => {
  const popup = popupOf(trigger)
  if (popup) close(popup)
})

// Pressing the trigger closes its popup.
delegate("pointerdown", TRIGGER, (_event, trigger) => {
  const popup = popupOf(trigger)
  if (popup) close(popup)
})

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape") return
  for (const popup of document.querySelectorAll("[popover]:popover-open")) {
    if (popup instanceof HTMLElement && triggerOf(popup)) close(popup)
  }
})
