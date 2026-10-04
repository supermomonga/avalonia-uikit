# ドキュメントサイト

https://avalonia-uikit.omofla.sh の構成と約束事。サイト本体は `sites/`、ライブデモとプレビュー画像の元になる .NET 側は `samples/` にある。

## 構成

| 場所 | 役割 |
| --- | --- |
| `sites/` | HonoX のサイト。`@hono/vite-ssg` で静的に出力し、Cloudflare Workers の static assets として配信する。UI は shadcnui-hono-jsx（`sites/components/ui`）。 |
| `sites/content/docs/*.mdx` | `/docs` 以下のガイド（Introduction、Installation など）。 |
| `sites/content/components/<slug>.mdx` | `/components/<slug>` のコンポーネントのページ。 |
| `sites/app/lib/catalog.ts` | コンポーネントの一覧（slug、名前、別名、Avalonia のコントロール、対応状況）。サイドバー、索引、検索、サイトマップの元。 |
| `samples/AvaloniaUIKit.Demos/` | デモの XAML。サイトのコード例とライブデモ、プレビュー画像の共通の元。 |
| `samples/AvaloniaUIKit.Previews/` | ヘッドレスで各デモを描き、`sites/public/previews/` に PNG と `manifest.json` を書く。 |
| `sites/scripts/images.ts` | OG 画像（`sites/public/og.png`）とアイコンを描く。生成物はコミットする。 |
| `samples/AvaloniaUIKit.Browser/` | `net10.0-browser` のアプリ。1 つの .NET ランタイムの上に複数の `AvaloniaView` を載せ、ページ内の `<avalonia-demo>` にデモを描く。publish の出力は `sites/public/wasm/<hash>/`（`sites/scripts/publish-wasm.sh`）。 |
| `.github/workflows/site.yml` | main への push で、デモの publish、プレビュー生成、サイトのビルド、`wrangler deploy` を行う。 |

`sites/public/previews/` と `sites/public/wasm/` は生成物なのでコミットしない。

## デザインと構成

構成と見た目は GPUI Kit の公式サイト（https://gpui-kit.com 、`longbridge/gpui-kit` の `website/`）にそろえる。部品は shadcnui-hono-jsx（`sites/components/ui`）を使い、その CSS 変数を GPUI Kit と同じ neutral の配色で上書きする（`sites/app/style.css`）。

- **セクション:** トップ（`/`）、ガイド（`/docs`）、コンポーネント（`/components`）。ドキュメントの 2 セクションはそれぞれ自分のサイドバーを持つ（`sites/app/lib/docs.ts` の `docsSections`）。旧 URL の `/docs/components/*` は `sites/public/_redirects` で `/components/*` に転送する。
- **トップ:** blueprint グリッドの上のヒーロー（見出し、2 つのボタン、事実の行、`App.axaml` の 1 行、コードのウィンドウ）、CAPABILITIES の 3×3 グリッド、3 パッケージのカード、PRINCIPLE の帯、フッター。数は `catalog.ts` から数え、文言はドキュメントに書かれた事実だけで組む。
- **ドキュメント:** 1280px の中に 220px のサイドバー、本文（最大 860px）、200px の目次。見出しは等幅の大文字、本文の型は `sites/app/styles/docs.css`。ページの最初のデモ（`title` なし）は macOS 風のウィンドウに入れたライブの例、`title` 付きのデモは見出し付きの枠になる。
- **テーマ:** ライト、ダーク、システムに従う、の 3 つ。パレット（T キー）で選び、`localStorage.theme` に `light` / `dark` を保存する（システムに従うときは消す）。
- **フォント:** サイトはシステムフォント、デモは同梱の Inter。
- **メタタグ:** `sites/app/routes/_renderer.tsx` が canonical、theme-color、Open Graph、X のカード、アイコン、manifest、JSON-LD（トップは `WebSite`、ほかは `WebPage` と `BreadcrumbList`）を出す。`<title>` は「Button — Components · Avalonia UIKit」の形。

## OG 画像とアイコン

`sites/public/og.png`（2400×1260）は、開発サーバーの `/og-image`（`sites/app/routes/og-image.tsx`、ビルドには含めない）を `sites/scripts/images.ts` が Playwright で 2 倍の解像度で撮ったもの。ロゴ、名前、タグライン、事実の行の横に、デモのプレビュー画像を並べたウィンドウを置く。アイコン（`apple-touch-icon.png`、`icon-192.png`、`icon-512.png`、`favicon.ico`）は同じスクリプトが `sites/public/logo.svg` から描く。どれもコミットし、CI では描かない。ロゴは直角だけで組んだ A で、クロスバーをテーマの青にしたもの（`logo.svg`、`logo-dark.svg`、`favicon.svg`、`site-header.tsx` の `Logo`）。

## デモ

- 1 つのデモは 1 つの `UserControl`。`samples/AvaloniaUIKit.Demos/Demos/<Component>/<Name>.axaml` に置き、`x:Class="AvaloniaUIKit.Demos.<Component><Name>"` とする（名前空間は `AvaloniaUIKit.Demos` の 1 つ。`Button` のような名前空間を作ると Avalonia の型名と衝突するため）。クラスの宣言とコンストラクターは `DemoRegistry.g.cs` に生成されるので、`.axaml.cs` はイベントハンドラーが要るときだけ `partial class` として添える（コンストラクターは書かない）。
- デモ ID は `<component-slug>/<name-slug>`。PascalCase をケバブケースに変換する（`ButtonGroup/IconOnly` → `button-group/icon-only`）。`<component-slug>` は `sites/app/lib/catalog.ts` の slug と一致させる。
- 各コンポーネントの最初のデモは `Demo`（ID は `<slug>/demo`）。ページの冒頭に出す代表例。
- サイトは XAML の**ルート要素の中身**をコード例として表示する。ルート要素（`UserControl`）の属性は表示しないので、コード例に出したい記述はすべて子要素に書く。`xmlns:uikit="using:AvaloniaUIKit"` はルートに書き、新規コントロールは `uikit:` 接頭辞で使う。
- 大きさは内容に任せる（自然な大きさで描く）。横幅の上限は 640（論理ピクセル）。ポップアップ（Flyout、ComboBox、DatePicker など）は `AvaloniaView` の範囲に重ねて描かれ、範囲の外には出られないので、開いた状態が収まる高さを `MinHeight` で確保する。
- デモの中でスクロールするもの（ListBox、ScrollViewer、TreeView、TableView、複数行の TextBox など）はカタログの `scroll: true` で宣言する。宣言のないデモでは、ページのスクロールを妨げないようにホイール操作をデモに渡さない。
- 登録は `samples/AvaloniaUIKit.Demos/DemoRegistry.g.cs` に生成する（`bun sites/scripts/demo-registry.ts`）。XAML を増やしたら再生成してコミットする。CI は生成結果が一致することを確かめる。
- フォントは同梱の Inter（`assets/fonts/inter/`）。ブラウザーにはシステムフォントがないので、Browser と Previews の両方で `UIKit.FontFamily` を Inter にする。
- DataGrid はトリミング非対応なので、WASM では動かないことがある。その場合はプレビュー画像だけを出す。

## プレビュー画像

`dotnet run --project samples/AvaloniaUIKit.Previews -- --out sites/public/previews` で、各デモを Light / Dark、スケール 2 で描く。

- `sites/public/previews/<component-slug>/<name-slug>.light.png`、`.dark.png`
- `sites/public/previews/manifest.json`: `{ "<demo id>": { "width": <論理px>, "height": <論理px> } }`
- `--only <component-slug>` で一部だけ描く。

サイトはプレビューを `<img>` として SSR し、JS なし・クローラー・モバイルでも見た目が伝わるようにする。

## ライブデモ

`sites/app/avalonia-demo.ts` が `<avalonia-demo demo="button/demo" width="…" height="…" data-wasm-base="/wasm/<hash>">` を定義する。`data-wasm-base` はサイトのビルド時に `sites/public/wasm/index.json`（`{"base":"/wasm/<hash>"}`）から埋める。属性がなければ（WASM を publish していなければ）プレビュー画像のままにする。

1. 初期表示は中のプレビュー画像。
2. 最初の `<avalonia-demo>` が画面に入ったら、`<data-wasm-base>/_framework/dotnet.js` を 1 回だけ読み込んで .NET ランタイムを起動する（`navigator.connection.saveData` のときは読み込まず、ボタンで明示的に読み込む）。
3. 起動後、画面に入ったデモから順に `Demos.Mount(hostId, demoId, heightChanged)` を呼び、プレビュー画像を `AvaloniaView` に差し替える。
4. `<html class="dark">` の変化を監視して `Demos.SetTheme(dark)` を呼ぶ。

ライブのデモは、要素の幅（プレビューの幅が上限）で高さを制限せずにレイアウトし、中央に置く。必要な高さは `heightChanged` で要素に返す（`samples/AvaloniaUIKit.Browser/DemoRoot.cs`）。プレビュー画像の縦横比で高さを決めると、狭い画面では折り返したデモの下が切れるため。

Avalonia は自分でホストと IME 用の `<input>` に `focus()` する。キーボードは全ビューで共有なので、あるデモを押すと、直前にフォーカスのあったデモのホストにも `focus()` が呼ばれ、ページがそこまでスクロールしてしまう。そこで `avalonia-demo.ts` はこれらの要素の `focus` を差し替え、押下の処理中は押されたデモの要素だけにフォーカスを許し、常に `preventScroll` を付ける。ホストのブラウザー既定のフォーカスリングは消す（フォーカスリングはデモの中で Avalonia が描く）。

Browser 側の JS から呼べる関数（`[JSExport]`、クラス `AvaloniaUIKit.Browser.Demos`）:

| 関数 | 内容 |
| --- | --- |
| `string[] List()` | 登録済みのデモ ID |
| `bool Mount(string hostId, string demoId, Action<double> heightChanged)` | `id="hostId"` の要素に `AvaloniaView` を作り、デモを載せる。デモが要素の幅で必要とする高さが変わるたびに `heightChanged` を呼ぶ。未知の ID や失敗は false |
| `void SetTheme(bool dark)` | `RequestedThemeVariant` を切り替える |

## 配信

- `sites/wrangler.jsonc`: `assets.directory = ./dist`、`not_found_handling = 404-page`、custom domain `avalonia-uikit.omofla.sh`。Worker のコードはない。
- `sites/public/_headers`: `/wasm/*` を `Cache-Control: public, max-age=31536000, immutable`。ディレクトリ名がバンドルのハッシュなので、中身は変わらない。`dotnet.js` のようにファイル名が固定のものもこれで安全に長期キャッシュできる。
- Cloudflare は `application/wasm` を Brotli 圧縮するので、publish が作る `.br` / `.gz` は配置しない。

## ビルドとデプロイ

| 目的 | コマンド |
| --- | --- |
| デモの登録を更新 | `bun sites/scripts/demo-registry.ts` |
| プレビュー | `dotnet run --project samples/AvaloniaUIKit.Previews -- --out sites/public/previews` |
| OG 画像とアイコン | `cd sites && bun run images`（プレビューを描いてから。Playwright の Chromium が要る） |
| WASM の publish | `sites/scripts/publish-wasm.sh`（`dotnet publish samples/AvaloniaUIKit.Browser -c Release` して `sites/public/wasm/<hash>/` に置き、`index.json` を書く。`wasm-tools` ワークロードが要る。別の SDK を使うなら `DOTNET=/path/to/dotnet`） |
| サイトの開発 | `cd sites && bun run dev` |
| サイトのビルド | `cd sites && bun run build`（`vite build --mode client && vite build`） |
| 公開 | `cd sites && bunx wrangler deploy`（`CLOUDFLARE_API_TOKEN` と `CLOUDFLARE_ACCOUNT_ID` は `mise.local.toml` にある） |

GitHub Actions は `sites/**`、`src/**`、`samples/**`、`assets/**` の変更で動き、上の手順をそのまま実行する。
