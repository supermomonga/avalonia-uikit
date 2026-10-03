// @ts-check
/**
 * Sidebars: the trigger, the rail and Ctrl/Cmd+B (the wrapper's
 * `data-sidebar-shortcut`) toggle the sidebar like upstream's
 * `SidebarProvider`. On wide screens it expands or collapses (`data-state`,
 * `data-collapsible`) and the state is stored in the wrapper's cookie; below
 * upstream's mobile breakpoint the content moves into the mobile sheet (a
 * native dialog) while it is open. Tooltips of collapsed menu buttons show
 * only while the sidebar is collapsed.
 */

/** Upstream's `use-mobile` breakpoint, in CSS pixels. */
const MOBILE_BREAKPOINT = 768

const WRAPPER = '[data-slot="sidebar-wrapper"]'

const isMobile = () => window.innerWidth < MOBILE_BREAKPOINT

/**
 * Parts of a sidebar wrapper, not those of nested ones.
 *
 * @param {HTMLElement} wrapper
 * @param {string} selector
 */
const own = (wrapper, selector) =>
  /** @type {HTMLElement[]} */ ([...wrapper.querySelectorAll(selector)]).filter(
    (element) => element.closest(WRAPPER) === wrapper
  )

/** @param {HTMLElement} wrapper */
const desktopOf = (wrapper) => own(wrapper, "[data-sidebar-collapsible]")

/** @param {HTMLElement} wrapper */
const sheetOf = (wrapper) => {
  const [sheet] = own(wrapper, 'dialog[data-mobile="true"]')
  return sheet instanceof HTMLDialogElement ? sheet : null
}

/** @param {HTMLElement} wrapper */
const isOpen = (wrapper) => desktopOf(wrapper)[0]?.dataset.state !== "collapsed"

/**
 * Shows the collapsed-sidebar tooltips only while collapsed on wide screens.
 *
 * @param {HTMLElement} wrapper
 */
function updateTooltips(wrapper) {
  const hidden = isOpen(wrapper) || isMobile()
  for (const button of own(wrapper, '[data-sidebar="menu-button"]')) {
    const id = button.dataset.hoverPopup
    const popup = id ? document.getElementById(id) : null
    if (popup?.hasAttribute("data-sidebar-tooltip")) popup.hidden = hidden
  }
}

/**
 * Expands or collapses the desktop sidebar and remembers it in the cookie.
 *
 * @param {HTMLElement} wrapper
 * @param {boolean} open
 */
function setOpen(wrapper, open) {
  for (const sidebar of desktopOf(wrapper)) {
    sidebar.dataset.state = open ? "expanded" : "collapsed"
    sidebar.dataset.collapsible = open
      ? ""
      : (sidebar.dataset.sidebarCollapsible ?? "offcanvas")
  }
  updateTooltips(wrapper)
  const name = wrapper.dataset.sidebarCookie
  if (name) {
    const maxAge = wrapper.dataset.sidebarCookieMaxAge ?? ""
    // biome-ignore lint/suspicious/noDocumentCookie: upstream stores the state in this cookie
    document.cookie = `${name}=${open}; path=/; max-age=${maxAge}`
  }
}

/**
 * Moves the sidebar's content between the desktop sidebar and the sheet.
 *
 * @param {HTMLElement} wrapper
 * @param {boolean} intoSheet
 */
function moveContent(wrapper, intoSheet) {
  const sheet = sheetOf(wrapper)
  const [inner] = own(wrapper, '[data-slot="sidebar-inner"]')
  const slot = sheet?.querySelector("[data-sidebar-mobile]")
  if (!inner || !slot) return
  const [from, to] = intoSheet ? [inner, slot] : [slot, inner]
  to.append(...from.childNodes)
}

/** @param {HTMLElement} wrapper */
function toggle(wrapper) {
  if (!isMobile()) {
    setOpen(wrapper, !isOpen(wrapper))
    return
  }
  const sheet = sheetOf(wrapper)
  if (!sheet) return
  if (sheet.open) {
    sheet.close()
  } else {
    // The server renders the tooltips for wide screens.
    updateTooltips(wrapper)
    moveContent(wrapper, true)
    sheet.showModal()
  }
}

document.addEventListener("click", (event) => {
  const target = event.target
  if (!(target instanceof Element)) return
  const control = target.closest(
    '[data-sidebar="trigger"], [data-sidebar="rail"]'
  )
  const wrapper = control?.closest(WRAPPER)
  if (wrapper instanceof HTMLElement) toggle(wrapper)
})

document.addEventListener("keydown", (event) => {
  if (!(event.metaKey || event.ctrlKey)) return
  for (const wrapper of document.querySelectorAll(WRAPPER)) {
    if (!(wrapper instanceof HTMLElement)) continue
    if (event.key !== wrapper.dataset.sidebarShortcut) continue
    event.preventDefault()
    toggle(wrapper)
  }
})

// A closed sheet returns the content once its exit transition ends.
document.addEventListener(
  "close",
  (event) => {
    const sheet = event.target
    if (
      !(sheet instanceof HTMLDialogElement) ||
      sheet.dataset.mobile !== "true"
    )
      return
    const wrapper = sheet.closest(WRAPPER)
    if (!(wrapper instanceof HTMLElement)) return
    Promise.all(
      sheet.getAnimations().map((a) => a.finished.catch(() => undefined))
    ).then(() => {
      if (!sheet.open) moveContent(wrapper, false)
    })
  },
  true
)

// Leaving the mobile layout closes the sheet; tooltips follow the layout.
window.addEventListener("resize", () => {
  for (const wrapper of document.querySelectorAll(WRAPPER)) {
    if (!(wrapper instanceof HTMLElement)) continue
    const sheet = sheetOf(wrapper)
    if (!isMobile() && sheet?.open) sheet.close()
    updateTooltips(wrapper)
  }
})

// A module: its declarations stay local.
export {}
