# Avalonia UIKit

Avalonia themes and controls based on the Nova style of
[shadcn/ui](https://ui.shadcn.com) and on
[GPUI Kit](https://github.com/longbridge/gpui-kit).

**Documentation and live demos: [avalonia-uikit.omofla.sh](https://avalonia-uikit.omofla.sh)**

- `NovaTheme` restyles Avalonia's own controls (Button, TextBox, ComboBox,
  Calendar, Tabs, Menus, ScrollViewer and 40 more) and adds the small
  components Avalonia lacks (Badge, Tag, Alert, Avatar, Stepper, Form and
  others). Optional packages cover `ColorPicker` and `DataGrid`.
- Every theme is verified pixel by pixel, frame by frame, against renders of
  GPUI Kit itself (3,500+ automated cases, `docs/testing.md`).
- No reflection; NativeAOT and trimming are supported.

## Repository

| Path | Contents |
| --- | --- |
| `src/` | The theme and controls (`AvaloniaUIKit`), `AvaloniaUIKit.ColorPicker`, `AvaloniaUIKit.DataGrid` |
| `samples/` | A NativeAOT gallery, the site's demos, the preview renderer and the browser (WebAssembly) app |
| `sites/` | The documentation site (HonoX, Cloudflare Workers) — see `docs/site.md` |
| `tests/` | The comparison tests against GPUI Kit's reference renders |
| `docs/` | Design records (`docs/adr`), the testing guide, the compatibility list |

## Building

```sh
scripts/verify.sh          # build and run every test
scripts/aot-smoke.sh       # publish the gallery with NativeAOT
```

The site's demos, previews and WebAssembly bundle are described in `docs/site.md`.
