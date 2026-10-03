/**
 * The components: GPUI Kit's name, the Avalonia control(s) the theme covers
 * (or the new `gpui:` control), and how far the port goes. The sidebar, the
 * index, the search and the sitemap are built from this list
 * (docs/references/compatibility-list.md is the source).
 */
export type Status = "full" | "partial" | "new"

export type Package = "AvaloniaUIKit.ColorPicker" | "AvaloniaUIKit.DataGrid"

export interface ComponentEntry {
  /** URL slug and the demos' `<component-slug>` (docs/site.md). */
  slug: string
  title: string
  /** GPUI Kit's component name(s). */
  gpui: string
  /** The Avalonia control(s), as written in XAML. */
  avalonia: string[]
  /** `full`: the control's look and motion; `partial`: a documented subset; `new`: a control Avalonia lacks. */
  status: Status
  /** The optional package whose theme covers it (ADR 16). */
  package?: Package
  group: Group
  /** The demo scrolls, so the live demo keeps wheel events (docs/site.md). */
  scroll?: boolean
}

export const groups = [
  "Buttons",
  "Forms",
  "Data display",
  "Navigation",
  "Overlays",
  "Feedback",
  "Layout",
] as const

export type Group = (typeof groups)[number]

export const statusLabels: Record<Status, string> = {
  full: "Full",
  partial: "Partial",
  new: "New control",
}

const entry = (
  slug: string,
  title: string,
  gpui: string,
  avalonia: string,
  status: Status,
  group: Group,
  extras: { package?: Package; scroll?: boolean } = {}
): ComponentEntry => ({
  slug,
  title,
  gpui,
  avalonia: avalonia.split(", "),
  status,
  group,
  ...extras,
})

export const components: ComponentEntry[] = [
  entry("accordion", "Accordion", "Accordion", "Expander, StackPanel.accordion", "partial", "Layout"),
  entry("alert", "Alert", "Alert", "gpui:Alert", "new", "Feedback"),
  entry("avatar", "Avatar", "Avatar, AvatarGroup", "gpui:Avatar, gpui:AvatarGroup", "new", "Data display"),
  entry("badge", "Badge", "Badge", "gpui:Badge", "new", "Data display"),
  entry("breadcrumb", "Breadcrumb", "Breadcrumb", "gpui:Breadcrumb, gpui:BreadcrumbItem", "new", "Navigation"),
  entry("bubble", "Bubble", "Bubble", "gpui:Bubble", "new", "Data display"),
  entry("button", "Button", "Button", "Button", "full", "Buttons"),
  entry("button-group", "Button Group", "ButtonGroup", "StackPanel.button-group", "partial", "Buttons"),
  entry("calendar", "Calendar", "Calendar", "Calendar", "partial", "Forms"),
  entry("carousel", "Carousel", "Carousel", "Carousel, PipsPager.carousel", "partial", "Layout"),
  entry("checkbox", "Checkbox", "Checkbox", "CheckBox", "full", "Forms"),
  entry("clipboard", "Clipboard", "Clipboard", "gpui:Clipboard", "new", "Forms"),
  entry("collapsible", "Collapsible", "Collapsible", "Expander (GpuiCollapsible)", "partial", "Layout"),
  entry("color-picker", "Color Picker", "ColorPicker", "ColorPicker", "partial", "Forms", { package: "AvaloniaUIKit.ColorPicker" }),
  entry("combobox", "Combobox", "Combobox", "ComboBox.combobox, AutoCompleteBox", "partial", "Forms"),
  entry("data-table", "Data Table", "DataTable", "TableView, DataGrid", "partial", "Data display", { package: "AvaloniaUIKit.DataGrid", scroll: true }),
  entry("date-picker", "Date Picker", "DatePicker", "CalendarDatePicker", "partial", "Forms"),
  entry("description-list", "Description List", "DescriptionList", "gpui:DescriptionList", "new", "Data display"),
  entry("dropdown-button", "Dropdown Button", "DropdownButton", "SplitButton", "full", "Buttons"),
  entry("empty", "Empty", "Empty", "gpui:EmptyState", "new", "Feedback"),
  entry("form", "Form", "Form, Field", "gpui:Form, gpui:FormField", "new", "Forms"),
  entry("group-box", "Group Box", "GroupBox", "GroupBox", "full", "Layout"),
  entry("hover-card", "Hover Card", "HoverCard", "gpui:HoverCard", "new", "Overlays"),
  entry("icon", "Icon", "Icon", "PathIcon", "partial", "Data display"),
  entry("image", "Image", "Image", "Image", "partial", "Data display"),
  entry("input", "Input", "Input", "TextBox", "partial", "Forms"),
  entry("input-group", "Input Group", "InputGroup", "TextBox.group", "partial", "Forms"),
  entry("kbd", "Kbd", "Kbd", "gpui:Kbd", "new", "Data display"),
  entry("label", "Label", "Label", "TextBlock.label, Label", "partial", "Data display"),
  entry("link", "Link", "Link", "HyperlinkButton", "full", "Buttons"),
  entry("list", "List", "List", "ListBox", "partial", "Data display", { scroll: true }),
  entry("marker", "Marker", "Marker", "gpui:Marker", "new", "Feedback"),
  entry("menu", "Menu", "Menu, ContextMenu, DropdownMenu, AppMenuBar", "Menu, ContextMenu, MenuFlyout", "full", "Overlays"),
  entry("message", "Message", "Message", "gpui:Message", "new", "Data display"),
  entry("notification", "Notification", "Notification", "WindowNotificationManager, NotificationCard", "partial", "Feedback"),
  entry("number-input", "Number Input", "NumberInput", "NumericUpDown", "full", "Forms"),
  entry("pagination", "Pagination", "Pagination", "PipsPager", "partial", "Navigation"),
  entry("popover", "Popover", "Popover", "Flyout", "partial", "Overlays"),
  entry("progress", "Progress", "Progress", "ProgressBar", "full", "Feedback"),
  entry("progress-circle", "Progress Circle", "ProgressCircle", "ProgressBar (GpuiProgressCircle)", "partial", "Feedback"),
  entry("radio", "Radio", "Radio", "RadioButton", "full", "Forms"),
  entry("rating", "Rating", "Rating", "gpui:Rating", "new", "Forms"),
  entry("resizable", "Resizable", "Resizable", "GridSplitter", "partial", "Layout"),
  entry("scrollable", "Scrollable", "Scrollable, Scrollbar", "ScrollViewer", "full", "Layout", { scroll: true }),
  entry("select", "Select", "Select", "ComboBox", "partial", "Forms"),
  entry("separator", "Separator", "Separator", "Separator", "full", "Layout"),
  entry("sheet", "Sheet", "Sheet", "DrawerPage.sheet", "partial", "Overlays"),
  entry("shimmer", "Shimmer", "Shimmer", "gpui:ShimmerText", "new", "Feedback"),
  entry("sidebar", "Sidebar", "Sidebar", "SplitView, DrawerPage", "partial", "Navigation"),
  entry("skeleton", "Skeleton", "Skeleton", "gpui:Skeleton", "new", "Feedback"),
  entry("slider", "Slider", "Slider", "Slider", "partial", "Forms"),
  entry("spinner", "Spinner", "Spinner", "ProgressBar (GpuiSpinner)", "full", "Feedback"),
  entry("status-bar", "Status Bar", "StatusBar", "gpui:StatusBar", "new", "Data display"),
  entry("stepper", "Stepper", "Stepper", "gpui:Stepper", "new", "Navigation"),
  entry("switch", "Switch", "Switch", "ToggleSwitch", "full", "Forms"),
  entry("table", "Table", "Table", "TableView (GpuiTable)", "partial", "Data display", { scroll: true }),
  entry("tabs", "Tabs", "Tabs, TabBar", "TabStrip, TabControl", "partial", "Navigation"),
  entry("tag", "Tag", "Tag", "gpui:TagLabel", "new", "Data display"),
  entry("textarea", "Textarea", "Textarea", "TextBox (AcceptsReturn)", "partial", "Forms", { scroll: true }),
  entry("time-field", "Time Field", "TimeField", "TimePicker", "partial", "Forms"),
  entry("title-bar", "Title Bar", "TitleBar", "WindowDrawnDecorations", "partial", "Layout"),
  entry("toggle", "Toggle", "Toggle", "ToggleButton", "full", "Buttons"),
  entry("toggle-group", "Toggle Group", "ToggleGroup", "ListBox.toggle-group", "partial", "Buttons"),
  entry("toolbar", "Toolbar", "Toolbar", "CommandBar", "partial", "Navigation"),
  entry("tooltip", "Tooltip", "Tooltip", "ToolTip", "full", "Overlays"),
  entry("tree", "Tree", "Tree", "TreeView", "partial", "Data display", { scroll: true }),
  entry("virtual-list", "Virtual List", "VirtualList", "ListBox, VirtualizingStackPanel", "partial", "Data display", { scroll: true }),
]

/** GPUI Kit components the library does not port (ADR 19). */
export const uncovered = [
  "Dialog / AlertDialog",
  "OtpInput",
  "Command",
  "Dock",
  "Settings",
  "Questionnaire",
  "Editor",
  "TextView / Markdown",
  "Chart",
  "Plot",
  "Speech",
  "Attachment",
  "MessageScroller",
]

export function findComponent(slug: string): ComponentEntry | undefined {
  return components.find((entry) => entry.slug === slug)
}

/** The components of each group, in the catalog's order. */
export function componentsByGroup(): { group: Group; items: ComponentEntry[] }[] {
  return groups.map((group) => ({
    group,
    items: components.filter((entry) => entry.group === group),
  }))
}
