// @ts-check
/**
 * Scroll areas: hides the viewport's native scrollbar and shows the custom
 * scrollbars that have overflow, sizing and moving their thumbs as the
 * viewport scrolls or resizes. Pressing the track jumps there and dragging
 * the thumb scrolls, like Base UI. Without this script the area keeps the
 * browser's scrollbar.
 */

const ROOT = "[data-scroll-area]"
/** Base UI's smallest thumb, in pixels. */
const MIN_THUMB_SIZE = 16
/** How long `data-scrolling` stays after the last scroll, in milliseconds. */
const SCROLL_TIMEOUT = 500

/** @type {WeakMap<HTMLElement, ReturnType<typeof setTimeout>>} */
const scrollTimers = new WeakMap()

/**
 * Parts of a scroll area, not those of nested ones.
 *
 * @param {HTMLElement} root
 */
function partsOf(root) {
  /** @param {string} selector */
  const own = (selector) =>
    /** @type {HTMLElement[]} */ ([...root.querySelectorAll(selector)]).filter(
      (element) => element.closest(ROOT) === root
    )
  const scrollbars = own("[data-scroll-area-scrollbar]")
  const vertical =
    scrollbars.find((s) => s.dataset.orientation !== "horizontal") ?? null
  const horizontal =
    scrollbars.find((s) => s.dataset.orientation === "horizontal") ?? null
  /** @param {HTMLElement | null} scrollbar */
  const thumbOf = (scrollbar) =>
    /** @type {HTMLElement | null} */ (
      scrollbar?.querySelector("[data-scroll-area-thumb]") ?? null
    )
  return {
    viewport: own("[data-scroll-area-viewport]")[0] ?? null,
    vertical,
    horizontal,
    thumbY: thumbOf(vertical),
    thumbX: thumbOf(horizontal),
    corner: own("[data-scroll-area-corner]")[0] ?? null,
  }
}

/**
 * @param {HTMLElement | null} element
 * @param {"padding" | "margin"} box
 * @param {"x" | "y"} axis
 */
function offset(element, box, axis) {
  if (!element) return 0
  const style = getComputedStyle(element)
  const [start, end] = axis === "x" ? ["Left", "Right"] : ["Top", "Bottom"]
  return (
    Number.parseFloat(style.getPropertyValue(`${box}-${start.toLowerCase()}`)) +
    Number.parseFloat(style.getPropertyValue(`${box}-${end.toLowerCase()}`))
  )
}

/**
 * @param {HTMLElement[]} elements
 * @param {string} name
 * @param {boolean} value
 */
function flag(elements, name, value) {
  for (const element of elements) element.toggleAttribute(name, value)
}

/**
 * Measures the viewport and updates the scrollbars, thumbs and state
 * attributes, following Base UI's `computeThumbPosition`.
 *
 * @param {HTMLElement} root
 */
function update(root) {
  const parts = partsOf(root)
  const { viewport, vertical, horizontal, thumbX, thumbY, corner } = parts
  if (!viewport) return
  const contentHeight = viewport.scrollHeight
  const contentWidth = viewport.scrollWidth
  if (contentHeight === 0 || contentWidth === 0) return
  const hiddenY = viewport.clientHeight >= contentHeight
  const hiddenX = viewport.clientWidth >= contentWidth
  if (vertical) vertical.hidden = hiddenY
  if (horizontal) horizontal.hidden = hiddenX
  const both = !hiddenX && !hiddenY
  if (corner) corner.hidden = !both
  root.style.setProperty(
    "--scroll-area-corner-width",
    `${both ? (vertical?.offsetWidth ?? 0) : 0}px`
  )
  root.style.setProperty(
    "--scroll-area-corner-height",
    `${both ? (horizontal?.offsetHeight ?? 0) : 0}px`
  )
  viewport.tabIndex = hiddenX && hiddenY ? -1 : 0

  const rtl = getComputedStyle(viewport).direction === "rtl"
  const maxLeft = Math.max(0, contentWidth - viewport.clientWidth)
  const maxTop = Math.max(0, contentHeight - viewport.clientHeight)
  const clamp = (/** @type {number} */ value, /** @type {number} */ max) =>
    Math.min(Math.max(value, 0), max)
  const leftFromStart = hiddenX
    ? 0
    : clamp(rtl ? -viewport.scrollLeft : viewport.scrollLeft, maxLeft)
  const topFromStart = hiddenY ? 0 : clamp(viewport.scrollTop, maxTop)
  const edges = {
    "x-start": leftFromStart,
    "x-end": hiddenX ? 0 : maxLeft - leftFromStart,
    "y-start": topFromStart,
    "y-end": hiddenY ? 0 : maxTop - topFromStart,
  }
  for (const [edge, value] of Object.entries(edges)) {
    viewport.style.setProperty(`--scroll-area-overflow-${edge}`, `${value}px`)
  }

  const scrollbars = [vertical, horizontal].filter((s) => s !== null)
  const stateful = [root, viewport, ...scrollbars]
  flag(stateful, "data-has-overflow-x", !hiddenX)
  flag(stateful, "data-has-overflow-y", !hiddenY)
  for (const [edge, value] of Object.entries(edges)) {
    flag(stateful, `data-overflow-${edge}`, value > 0)
  }

  if (vertical && thumbY && !hiddenY) {
    const track = Math.min(
      vertical.offsetHeight,
      viewport.clientHeight -
        offset(vertical, "padding", "y") -
        offset(thumbY, "margin", "y")
    )
    const size = Math.max(
      MIN_THUMB_SIZE,
      (track * viewport.clientHeight) / contentHeight
    )
    vertical.style.setProperty("--scroll-area-thumb-height", `${size}px`)
    const max =
      vertical.offsetHeight -
      size -
      offset(vertical, "padding", "y") -
      offset(thumbY, "margin", "y")
    const position = maxTop ? (topFromStart / maxTop) * max : 0
    thumbY.style.transform = `translate3d(0,${position}px,0)`
  }
  if (horizontal && thumbX && !hiddenX) {
    const track = Math.min(
      horizontal.offsetWidth,
      viewport.clientWidth -
        offset(horizontal, "padding", "x") -
        offset(thumbX, "margin", "x")
    )
    const size = Math.max(
      MIN_THUMB_SIZE,
      (track * viewport.clientWidth) / contentWidth
    )
    horizontal.style.setProperty("--scroll-area-thumb-width", `${size}px`)
    const max =
      horizontal.offsetWidth -
      size -
      offset(horizontal, "padding", "x") -
      offset(thumbX, "margin", "x")
    const position = maxLeft ? (leftFromStart / maxLeft) * max : 0
    thumbX.style.transform = `translate3d(${rtl ? -position : position}px,0,0)`
  }
}

const resizes = new ResizeObserver((entries) => {
  for (const entry of entries) {
    const root = entry.target.closest(ROOT)
    if (root instanceof HTMLElement) update(root)
  }
})

/** @param {HTMLElement} root */
function setup(root) {
  const { viewport } = partsOf(root)
  if (!viewport || viewport.hasAttribute("data-scrollbars")) return
  viewport.setAttribute("data-scrollbars", "")
  // Base UI hides the native scrollbar and always reserves no space for it.
  viewport.style.overflow = "scroll"
  viewport.style.setProperty("scrollbar-width", "none")
  resizes.observe(viewport)
  for (const child of viewport.children) resizes.observe(child)
  update(root)
}

/** @param {ParentNode} scope */
function setupAll(scope) {
  for (const root of scope.querySelectorAll(ROOT)) {
    if (root instanceof HTMLElement) setup(root)
  }
}

setupAll(document)
// Scroll areas rendered later (htmx swaps, streaming).
new MutationObserver((records) => {
  for (const record of records) {
    for (const node of record.addedNodes) {
      if (!(node instanceof HTMLElement)) continue
      if (node.matches(ROOT)) setup(node)
      setupAll(node)
    }
  }
}).observe(document.documentElement, { childList: true, subtree: true })

// `scroll` does not bubble, so it is captured.
document.addEventListener(
  "scroll",
  (event) => {
    const viewport = event.target
    if (
      !(viewport instanceof HTMLElement) ||
      !viewport.matches("[data-scroll-area-viewport]")
    )
      return
    const root = viewport.closest(ROOT)
    if (!(root instanceof HTMLElement)) return
    update(root)
    const parts = partsOf(root)
    const scrolling = [
      root,
      viewport,
      parts.vertical,
      parts.horizontal,
      parts.thumbX,
      parts.thumbY,
    ].filter((element) => element !== null)
    flag(scrolling, "data-scrolling", true)
    clearTimeout(scrollTimers.get(root))
    scrollTimers.set(
      root,
      setTimeout(() => flag(scrolling, "data-scrolling", false), SCROLL_TIMEOUT)
    )
  },
  true
)

// Pressing the track jumps there; dragging the thumb scrolls.
document.addEventListener("pointerdown", (event) => {
  const target = event.target
  if (!(target instanceof Element) || event.button !== 0) return
  const scrollbar = target.closest("[data-scroll-area-scrollbar]")
  const root = scrollbar?.closest(ROOT)
  if (!(scrollbar instanceof HTMLElement) || !(root instanceof HTMLElement))
    return
  const { viewport } = partsOf(root)
  const thumb = scrollbar.querySelector("[data-scroll-area-thumb]")
  if (!viewport || !(thumb instanceof HTMLElement)) return
  const vertical = scrollbar.dataset.orientation !== "horizontal"
  const axis = vertical ? "y" : "x"
  const thumbSize = vertical ? thumb.offsetHeight : thumb.offsetWidth
  const track = vertical ? scrollbar.offsetHeight : scrollbar.offsetWidth
  const max =
    track -
    thumbSize -
    offset(scrollbar, "padding", axis) -
    offset(thumb, "margin", axis)
  const scrollable = vertical
    ? viewport.scrollHeight - viewport.clientHeight
    : viewport.scrollWidth - viewport.clientWidth
  if (max <= 0) return
  if (!thumb.contains(target)) {
    const rect = scrollbar.getBoundingClientRect()
    const position =
      (vertical ? event.clientY - rect.top : event.clientX - rect.left) -
      thumbSize / 2 -
      offset(scrollbar, "padding", axis) +
      offset(thumb, "margin", axis) / 2
    const ratio = position / max
    if (vertical) viewport.scrollTop = ratio * scrollable
    else if (getComputedStyle(viewport).direction === "rtl")
      viewport.scrollLeft = -(1 - ratio) * scrollable
    else viewport.scrollLeft = ratio * scrollable
  }
  const start = vertical ? event.clientY : event.clientX
  const startScroll = vertical ? viewport.scrollTop : viewport.scrollLeft
  scrollbar.setPointerCapture(event.pointerId)
  /** @param {PointerEvent} move */
  const onMove = (move) => {
    if (move.buttons % 2 === 0) return onUp()
    const delta = (vertical ? move.clientY : move.clientX) - start
    const next = startScroll + (delta / max) * scrollable
    if (vertical) viewport.scrollTop = next
    else viewport.scrollLeft = next
  }
  const onUp = () => {
    scrollbar.removeEventListener("pointermove", onMove)
    scrollbar.removeEventListener("pointerup", onUp)
    scrollbar.removeEventListener("pointercancel", onUp)
  }
  scrollbar.addEventListener("pointermove", onMove)
  scrollbar.addEventListener("pointerup", onUp)
  scrollbar.addEventListener("pointercancel", onUp)
})

// Native scrollbars do not take focus when pressed.
document.addEventListener("mousedown", (event) => {
  if (
    event.target instanceof Element &&
    event.target.closest("[data-scroll-area-scrollbar]")
  ) {
    event.preventDefault()
  }
})

// A module: its declarations stay local.
export {}
