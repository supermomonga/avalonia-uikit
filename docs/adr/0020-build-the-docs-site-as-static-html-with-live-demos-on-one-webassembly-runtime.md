---
number: 20
title: Build the docs site as static HTML with live demos on one WebAssembly runtime
status: accepted
date: 2026-10-03
links:
- target: 22
  kind: amendedby
- target: 23
  kind: amendedby
- target: 24
  kind: amendedby
- target: 32
  kind: amendedby
---

# Build the docs site as static HTML with live demos on one WebAssembly runtime

## Context and Problem Statement

GPUI Kit の公式サイト（gpui-kit.com）のように、コンポーネントをページ上で実際に触れるドキュメントサイトを https://avalonia-uikit.omofla.sh に置きたい。Avalonia は WebAssembly（`Avalonia.Browser`）で動くので、ライブデモはブラウザー内の本物の Avalonia で描ける。問題は、サイト全体を Avalonia のアプリにするか、HTML のサイトにデモを埋め込むか、埋め込むなら .NET ランタイムをいくつ動かすか、そして HTML の生成とデプロイをどう組むかである。

## Decision Drivers

* ドキュメントは検索エンジンに載り、文字が選択・コピーでき、JS なしでも読める必要がある。サイト全体を canvas に描くとこれを失う。
* .NET のランタイムと Skia を含むバンドルは非圧縮で約 17 MB、Brotli 後で約 5 MB ある。デモごとに別のランタイムを動かすと帯域もメモリも成り立たない。
* コード例とライブデモ、プレビュー画像が食い違わないこと。1 つの元から 3 つを作りたい。
* 既存のサイト（shadcn-hono.omofla.sh）と同じ道具立て（HonoX、shadcnui-hono-jsx、Cloudflare Workers の static assets、GitHub Actions）で運用したい。
* ライブラリは NativeAOT とトリミングに対応している（ADR 11）ので、WASM のトリミングと相性が良い。

## Considered Options

* HTML のサイト（HonoX の SSG）に、ページごとに 1 つの .NET ランタイムを遅延ロードし、その上に複数の `AvaloniaView` を載せてデモを描く
* サイト全体を Avalonia の WebAssembly アプリにする
* gpui-kit.com のように、ギャラリーアプリを別に作り、各ページに iframe で深リンクする
* 静的なスクリーンショットだけを載せ、ライブデモは置かない

## Decision Outcome

Chosen option: "HTML のサイトに 1 つのランタイムと複数の `AvaloniaView`", because 文章は HTML のまま軽く保て、`SetupBrowserAppAsync` と `AvaloniaView(divId)` で 1 つのランタイムに任意の数のビューを載せられることを Avalonia 自身が想定している（`samples/ControlCatalog.Browser`）から。約束事は `docs/site.md` にまとめる。

* **デモが唯一の元:** 1 つのデモは `samples/AvaloniaUIKit.Demos/Demos/<Component>/<Name>.axaml` の `UserControl`。サイトはその中身をコード例として表示し、`samples/AvaloniaUIKit.Previews` がヘッドレス（Skia、同梱の Inter）で Light / Dark のプレビュー PNG と OG 画像を描き、`samples/AvaloniaUIKit.Browser` が同じクラスをブラウザーで動かす。登録表（`DemoRegistry.g.cs`）はファイル名から生成し、CI で一致を確かめる。
* **初期表示はプレビュー画像:** `<avalonia-demo>` は SSR されたプレビュー画像を持ち、最初のデモが画面に入ったときにだけ `dotnet.js` を読み込む。`navigator.connection.saveData` のときはボタンで明示的に読み込む。JS なし、クローラー、モバイルでも見た目は伝わる。
* **ホイールの扱い:** Avalonia の入力処理はホスト要素上の wheel を無条件に `preventDefault` するので、スクロールしないデモでは capture 段階で `stopPropagation` してページのスクロールを守る。スクロールするデモはカタログで宣言する。
* **スレッドなし:** `WasmEnableThreads` は false。有効にすると COOP / COEP ヘッダーが要り、埋め込みサイトには向かない。
* **配信:** publish の出力はバンドルのハッシュを名前にしたディレクトリ（`sites/public/wasm/<hash>/`）に置き、`/wasm/*` を immutable でキャッシュする。`application/wasm` は Cloudflare が Brotli 圧縮するので `.br` / `.gz` は置かない。サイトは `@hono/vite-ssg` の静的出力だけで、Worker のコードはない。
* **DataGrid:** `Avalonia.Controls.DataGrid` はトリミング非対応なので、WASM ではアセンブリを丸ごと残し、トリミングの警告をエラーにしない。それでも動かない場合はプレビュー画像だけを出す。

### Consequences

* Good, because 文章のページは HTML だけで軽く、デモの重さはデモを見る人だけが払う。2 回目以降はキャッシュで 0 になる。
* Good, because コード例、プレビュー、ライブデモが同じ XAML から出るので食い違わない。
* Good, because 1 ページに何個デモがあってもランタイムは 1 つで、メモリと帯域が増えない。
* Bad, because 初回のデモ表示に数 MB の読み込みと起動の数秒がかかる。HonoX は MPA なのでページ遷移ごとに起動し直す。
* Bad, because ポップアップは `AvaloniaView` の範囲の外に出られないので、デモ側で開いた状態が収まる高さを確保する必要がある。
* Bad, because ブラウザーの WASM ビルドには `wasm-tools` ワークロードが要り、CI の時間が延びる。
* Neutral, because iframe 方式（gpui-kit.com）はページのスクロールやフォーカスの問題を iframe の境界で避けられるが、iframe ごとにランタイムが増え、テーマの同期に postMessage が要る。ランタイムを 1 つにしたいので採らなかった。

### Confirmation

* `.github/workflows/site.yml` が main への push で、登録表の一致確認、プレビューの描画、WASM の publish、サイトのビルド、`bun scripts/smoke.ts`、`wrangler deploy` を順に行う。Pull Request では deploy の dry-run まで行う。
* `samples/AvaloniaUIKit.Browser/wwwroot/index.html` は全デモを 1 ページに載せる手動確認用のページで、`dotnet publish` の出力をそのまま配信して確かめられる。

## Pros and Cons of the Options

### HTML のサイトに 1 つのランタイムと複数の `AvaloniaView`

* Good, because 文章は HTML、デモは本物の Avalonia という分担が明確。
* Good, because Avalonia が公式にサポートする使い方で、独自のパッチが要らない。
* Bad, because ホイール、フォーカス、ポップアップの範囲など、埋め込み特有の扱いを自分で書く。

### サイト全体を Avalonia の WebAssembly アプリにする

* Good, because サイトもライブラリのショーケースになる。
* Bad, because 検索エンジン、文字の選択、リンク、JS なしの閲覧をすべて失う。初回に数 MB を読み込むまで何も見えない。

### ギャラリーアプリを iframe で深リンクする

* Good, because gpui-kit.com が実際にこの方式で、埋め込み特有の問題を iframe の境界で避けられる。
* Bad, because iframe ごとにランタイムが起動し、1 ページに複数のデモを置けない。テーマの同期に postMessage が要る。

### 静的なスクリーンショットだけ

* Good, because 最も軽く、ビルドも単純。
* Bad, because 動きと操作感が GPUI Kit と一致していることがこのライブラリの価値であり、それを見せられない。
