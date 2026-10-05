# ドキュメントサイト

https://avalonia-uikit.omofla.sh の構成と約束事。サイト本体は `sites/`、ライブデモとプレビュー画像の元になる .NET 側は `samples/` にある。

## 構成

| 場所 | 役割 |
| --- | --- |
| `sites/` | HonoX のサイト。`@hono/vite-ssg` で静的に出力し、Cloudflare Workers の static assets として配信する。UI は shadcnui-hono-jsx（`sites/components/ui`）。 |
| `sites/content/docs/*.mdx` | `/docs` 以下のガイド（Introduction、Installation など）。 |
| `sites/content/components/<slug>.mdx` | `/components/<slug>` のコンポーネントのページ。 |
| `sites/app/lib/catalog.ts` | コンポーネントの一覧（slug、名前、別名、Avalonia のコントロール、対応状況）。サイドバー、索引、検索、サイトマップの元。 |
| `sites/app/lib/themes.g.json`、`sites/app/styles/themes.g.css` | GPUI Kit が同梱するテーマの一覧と、各テーマでのサイトの色（CSS 変数）。`reference tokens`（`reference/src/tokens.rs` の `write_site`）が書き出し、コミットする。 |
| `samples/AvaloniaUIKit.Demos/` | デモの XAML。サイトのコード例とライブデモ、プレビュー画像の共通の元。 |
| `samples/AvaloniaUIKit.Previews/` | ヘッドレスで各デモを描き、`sites/public/previews/` に PNG と `manifest.json` を書く。 |
| `sites/scripts/images.ts` | OG 画像（`sites/public/og.png`）とアイコンを描く。生成物はコミットする。 |
| `samples/AvaloniaUIKit.Browser/` | `net10.0-browser` のアプリ。1 つの .NET ランタイムの上に複数の `AvaloniaView` を載せ、ページ内の `<avalonia-demo>` にデモを描く。publish の出力は `sites/public/wasm/<hash>/`（`sites/scripts/publish-wasm.sh`）。 |
| `.github/workflows/site.yml` | main への push で、デモの大きさの計測、ブラウザーのアプリの publish、サイトのビルド、`wrangler deploy` を行う（「ビルドとデプロイ」）。 |

`sites/public/previews/` と `sites/public/wasm/` は生成物なのでコミットしない。

## デザインと構成

構成と見た目は GPUI Kit の公式サイト（https://gpui-kit.com 、`longbridge/gpui-kit` の `website/`）にそろえる。部品は shadcnui-hono-jsx（`sites/components/ui`）を使い、その CSS 変数を GPUI Kit と同じ neutral の配色で上書きする（`sites/app/style.css`）。

- **セクション:** トップ（`/`）、ガイド（`/docs`）、コンポーネント（`/components`）。ドキュメントの 2 セクションはそれぞれ自分のサイドバーを持つ（`sites/app/lib/docs.ts` の `docsSections`）。コンポーネントのサイドバーは、索引（Overview）の下を「Avalonia Controls」（Avalonia 標準のコントロールのテーマ）、「UIKit Controls」（`uikit:` の新しいコントロール、カタログの `status: "new"`）、「Third-party Controls」（第三者のライブラリのコントロールのテーマ、カタログの `library`。ADR 28）の 3 つのグループに分け、それぞれアルファベット順に並べる。標準のコントロールの代わりに使う `uikit:` のコントロール（`uikit:Select`、`uikit:Tree` など。ADR 30）も、標準のコントロールのページには載せず、UIKit Controls に自分のページを持つ。GPUI Kit の名前が標準のコントロールのページと重なるので、タイトルは型名（`ListView`、`Select`）、slug は `uikit-` と型名のケバブケース（`uikit-list-view`、`uikit-select`）にし、標準のコントロールのページの Usage から 1 文でリンクする。標準のコントロールに付ける添付プロパティ（`uikit:Buttons`、`uikit:Inputs` など）と、標準のコントロールに添える部品（`uikit:CarouselPrevious` / `CarouselNext`）は、そのコントロールのページに載せ、カタログの `avalonia` にも並べる（`status` は変えない）。旧 URL の `/docs/components/*` は `sites/public/_redirects` で `/components/*` に転送する。
- **トップ:** blueprint グリッドの上のヒーロー（見出し、2 つのボタン、事実の行、`App.axaml` の 1 行、コードのウィンドウ）、CAPABILITIES の 3×3 グリッド、5 パッケージのカード、PRINCIPLE の帯、フッター。数は `catalog.ts` から数え、文言はドキュメントに書かれた事実だけで組む。
- **ドキュメント:** 1280px の中に 220px のサイドバー、本文（最大 860px）、200px の目次。見出しは等幅の大文字、本文の型は `sites/app/styles/docs.css`。ページの最初のデモ（`title` なし）は macOS 風のウィンドウに入れたライブの例、`title` 付きのデモは見出し付きの枠になる。
- **GPUI Kit との対応状況は出さない:** 利用者に GPUI Kit との互換性を意識させる必要はないので、Full / Partial の別や、GPUI Kit の機能のうち扱わないもの（Not covered）はサイトに書かない。これらは開発者向けの `docs/references/compatibility-list.md` にだけ書く。カタログの `status` は、新しいコントロール（`new`）かどうかを分けるためだけに使う（トップの数え上げとサイドバーのグループ）。
- **テーマ:** gpui-kit.com と同じく、システムに従う、Default Light、Default Dark と、GPUI Kit が同梱する 36 のテーマ（Light と Dark に分けて名前順）。パレット（T キー）で選ぶ。入力で絞り込み、↑↓ でプレビュー、Enter で決定、Esc で元に戻す。`localStorage.theme` に `light` / `dark` / 同梱テーマの slug を保存する（システムに従うときは消す）。同梱テーマは `<html data-theme="<slug>">` で `themes.g.css` の色に切り替え、モードで `dark` クラスを付ける。描画前に `THEME_SCRIPT`（`components/theme-palette.tsx`）が同じことをする。サイトの色への対応は gpui-kit.com（`website/src/lib/theme-catalog.ts`）にそろえ、値は GPUI Kit が解決した色を使う（`tokens.rs` の `SITE_VARS`）。コードの色はテーマのファイルの `highlight` から取る。ライブデモも同じテーマで描く（下の「ライブデモ」）。
- **フォント:** サイトはシステムフォント、デモは同梱の Inter。
- **メタタグ:** `sites/app/routes/_renderer.tsx` が canonical、theme-color、Open Graph、X のカード、アイコン、manifest、JSON-LD（トップは `WebSite`、ほかは `WebPage` と `BreadcrumbList`）を出す。`<title>` は「Button — Components · Avalonia UIKit」の形。

## OG 画像とアイコン

`sites/public/og.png`（2400×1260）は、開発サーバーの `/og-image`（`sites/app/routes/og-image.tsx`、ビルドには含めない）を `sites/scripts/images.ts` が Playwright で 2 倍の解像度で撮ったもの。ロゴ、名前、タグライン、事実の行の横に、デモのプレビュー画像を並べたウィンドウを置く。アイコン（`apple-touch-icon.png`、`icon-192.png`、`icon-512.png`、`favicon.ico`）は同じスクリプトが `sites/public/logo.svg` から描く。どれもコミットし、CI では描かない。ロゴは直角だけで組んだ A で、クロスバーをテーマの青にしたもの（`logo.svg`、`logo-dark.svg`、`favicon.svg`、`site-header.tsx` の `Logo`）。

## デモ

- 1 つのデモは 1 つの `UserControl`。`samples/AvaloniaUIKit.Demos/Demos/<Component>/<Name>.axaml` に置き、`x:Class="AvaloniaUIKit.Demos.<Component><Name>"` とする（名前空間は `AvaloniaUIKit.Demos` の 1 つ。`Button` のような名前空間を作ると Avalonia の型名と衝突するため）。クラスの宣言とコンストラクターは `DemoRegistry.g.cs` に生成されるので、`.axaml.cs` はイベントハンドラーが要るときだけ `partial class` として添える（コンストラクターは書かない）。
- デモ ID は `<component-slug>/<name-slug>`。PascalCase をケバブケースに変換する（`ButtonGroup/IconOnly` → `button-group/icon-only`、`UikitListView/Search` → `uikit-list-view/search`）。`<component-slug>` は `sites/app/lib/catalog.ts` の slug と一致させる。
- 各コンポーネントの最初のデモは `Demo`（ID は `<slug>/demo`）。ページの冒頭に出す代表例。
- サイトは XAML の**ルート要素の中身**をコード例として表示する。ルート要素（`UserControl`）の属性は表示しないので、コード例に出したい記述はすべて子要素に書く。`xmlns:uikit="using:AvaloniaUIKit"` はルートに書き、新規コントロールは `uikit:` 接頭辞で使う。
- 大きさは内容に任せる（自然な大きさで描く）。横幅の上限は 640（論理ピクセル）。ポップアップ（Flyout、ComboBox、DatePicker など）と、コードから開く `uikit:Sheet` は `AvaloniaView` の範囲に重ねて描かれ、範囲の外には出られないので、開いた状態が収まる大きさをデモ自身が確保する（`MinHeight` など）。
- デモの中でスクロールするもの（ListBox、ScrollViewer、TreeView、TableView、複数行の TextBox など）はカタログの `scroll: true` で宣言する。宣言のないデモでは、ページのスクロールを妨げないようにホイール操作をデモに渡さない。
- 登録は `samples/AvaloniaUIKit.Demos/DemoRegistry.g.cs` に生成する（`bun sites/scripts/demo-registry.ts`）。XAML を増やしたら再生成してコミットする。CI は生成結果が一致することを確かめる。
- フォントは同梱の Inter（`assets/fonts/inter/`）。ブラウザーにはシステムフォントがないので、Browser と Previews の両方で `UIKit.FontFamily` を Inter にする。
- DataGrid はトリミング非対応なので、WASM では動かないことがある。その場合は「Live demo unavailable」と出す（下の「ライブデモ」）。
- 第三者のライブラリのデモ（ADR 28）は、ページにウィンドウがないことに合わせる。Tabalonia は `EnableTabDetaching="False"`、Dock は Browser のアプリが `DockSettings.UseManagedWindows` で浮動ウィンドウを DockControl の中に描く。Dock 12.1.0.6 の管理モードのドラッグのプレビューは Avalonia 12 では左上に残る（`docs/testing.md` の R35）ので、Browser のアプリはそれを隠す。Tabalonia の `ItemsSource` は変更できるリストでなければならないので、デモは `TabList`（`ObservableCollection`）に `DragTabItem` を並べる。

## プレビュー画像

`dotnet run --project samples/AvaloniaUIKit.Previews -- --out sites/public/previews` で、各デモを Light / Dark、スケール 2 で描く。

- `sites/public/previews/<component-slug>/<name-slug>.light.png`、`.dark.png`
- `sites/public/previews/manifest.json`: `{ "<demo id>": { "width": <論理px>, "height": <論理px> } }`
- `--only <component-slug>` で一部だけ描く。
- `--manifest-only` で `manifest.json` だけを書き、PNG は描かない（CI はこれ）。

サイトはデモの枠の大きさに `manifest.json` を使う（PNG は OG 画像だけが使い、ページには出さない）。そのため CI は PNG を作らず、サイトにも置かない。

デモは、読み込み時の遷移（通知のカードの入場、Sheet のスライドなど）が終わるまで実時間で約 0.5 秒待ってから大きさを測り、撮る。この待ちをデモごとに払わないよう、32 個ずつデモの Light と Dark をまとめて表示し、1 回待ってから順に撮る。待つ間に例外が出たら、その 32 個を 1 つずつ描き直して、どのデモが失敗したかを示す。

## ライブデモ

`sites/app/avalonia-demo.ts` が `<avalonia-demo demo="button/demo" width="…" height="…" data-wasm-base="/wasm/<hash>">` を定義する。`data-wasm-base` はサイトのビルド時に `sites/public/wasm/index.json`（`{"base":"/wasm/<hash>"}`）から埋める。属性がなければ（WASM を publish していなければ）、またはマウントに失敗したら、スケルトンの点滅を止めて「Live demo unavailable」と出す。

1. 初期表示は、プレビューの大きさ（`manifest.json`）のスケルトン（shadcnui-hono-jsx の `Skeleton`）。
2. 最初の `<avalonia-demo>` が画面に入ったら、`<data-wasm-base>/_framework/dotnet.js` を 1 回だけ読み込んで .NET ランタイムを起動する（`navigator.connection.saveData` のときは読み込まず、ボタンで明示的に読み込む）。
3. 起動後、画面に入ったデモから順に `Demos.Mount(hostId, demoId, inset, heightChanged)` を呼び、スケルトンを `AvaloniaView` に差し替える。
4. `<html>` の `class`（`dark`）と `data-theme` の変化を監視して、`Demos.SetTheme(name)` に GPUI Kit のテーマ名（`Default Light`、`Default Dark`、同梱テーマの名前。パレットの項目の `data-theme-name`）を渡す。

ライブのデモは、要素の幅（プレビューの幅が上限）で高さを制限せずにレイアウトし、中央に置く。必要な高さは `heightChanged` で要素に返す（`samples/AvaloniaUIKit.Browser/DemoRoot.cs`）。プレビュー画像の縦横比で高さを決めると、狭い画面では折り返したデモの下が切れるため。

ビューのホストは `<avalonia-demo>` から上下左右に `--demo-inset`（`sites/app/style.css`、12px）だけはみ出させ、`DemoRoot` はその分を `Padding` として空ける。さらにデモのルート（`UserControl`）の `ClipToBounds` を切る（テンプレートを持つコントロールは既定で自分の範囲で切り取る）。デモの位置と大きさは要素のままで、フォーカスリング（`Margin="-3"`）や影のようにデモの外へ描くものが切れない。値は CSS にだけ書き、`avalonia-demo.ts` が読んで `Mount` の `inset` に渡す。

Avalonia は自分でホストと IME 用の `<input>` に `focus()` する。キーボードは全ビューで共有なので、あるデモを押すと、直前にフォーカスのあったデモのホストにも `focus()` が呼ばれ、ページがそこまでスクロールしてしまう。そこで `avalonia-demo.ts` はこれらの要素の `focus` を差し替え、押下の処理中は押されたデモの要素だけにフォーカスを許し、常に `preventScroll` を付ける。ホストのブラウザー既定のフォーカスリングは消す（フォーカスリングはデモの中で Avalonia が描く）。

Browser 側の JS から呼べる関数（`[JSExport]`、クラス `AvaloniaUIKit.Browser.Demos`）:

| 関数 | 内容 |
| --- | --- |
| `string[] List()` | 登録済みのデモ ID |
| `bool Mount(string hostId, string demoId, double inset, Action<double> heightChanged)` | `id="hostId"` の要素に `AvaloniaView` を作り、デモを `inset` だけ内側に載せる。デモがその幅で必要とする高さ（`inset` を含まない）が変わるたびに `heightChanged` を呼ぶ。未知の ID や失敗は false |
| `void SetTheme(string name)` | `RequestedThemeVariant` を `UIKitThemeVariants.Find(name)` に切り替える（知らない名前は Light） |

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
| サイトのテーマ | `reference tokens`（`reference/` を vendor してビルドした生成器。`scripts/generate-goldens.sh` も書き出す。macOS 専用） |
| サイトの開発 | `cd sites && bun run dev`（ライブデモは動かない。Vite の開発サーバーは `public/` の `dotnet.js` を動的 import できないため。確かめるときはビルドして `bunx wrangler dev`） |
| サイトのビルド | `cd sites && bun run build`（`vite build --mode client && vite build`） |
| 公開 | `cd sites && bunx wrangler deploy`（`CLOUDFLARE_API_TOKEN` と `CLOUDFLARE_ACCOUNT_ID` は `mise.local.toml` にある） |

GitHub Actions は `sites/**`、`src/**`、`samples/**`、`assets/**` などの変更で動き、上の手順を 3 つのジョブで実行する。

| ジョブ | 内容 |
| --- | --- |
| `previews` | `--manifest-only` でデモの大きさを測る。 |
| `wasm` | `wasm-tools` を入れ、`publish-wasm.sh` を実行する。 |
| `site` | 2 つのジョブの出力を受け取り、デモの登録の確認、型の検査、ビルド、`smoke.ts` のあと、main なら `wrangler deploy`、PR なら `--dry-run` を行う。 |

`previews` と `wasm` は並んで走り、出力（`sites/public/previews/`、`sites/public/wasm/`）を Actions のキャッシュに保存する。キーは解決された SDK のバージョン（`dotnet --version`）と、出力の元になるファイル（`src/**`、Demos と Previews または Browser、`assets/**`、ルートの props、`global.json`、`site.yml`、wasm は `publish-wasm.sh` も）のハッシュ。同じキーの出力がすでにあれば .NET のビルドを丸ごと省くので、サイトだけの変更では .NET をビルドしない。`site` はキーをジョブの出力で受け取ってキャッシュから復元する。出力の元を増やしたら（新しいプロジェクトの参照、`assets/` の外のファイルなど）、`site.yml` のハッシュの対象にも加える。加え忘れると、古い出力のまま配信される。

PR で保存したキャッシュは main からは読めない（GitHub のキャッシュの範囲）ので、main への push では作り直す。PR は main のキャッシュを読める。
