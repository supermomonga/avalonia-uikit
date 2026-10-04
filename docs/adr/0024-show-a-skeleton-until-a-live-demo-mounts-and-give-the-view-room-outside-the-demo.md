---
number: 24
title: Show a skeleton until a live demo mounts and give the view room outside the demo
status: accepted
date: 2026-10-04
links:
- target: 20
  kind: amends
- target: 23
  kind: amends
---

# Show a skeleton until a live demo mounts and give the view room outside the demo

## Context and Problem Statement

ADR 20 では、`<avalonia-demo>` の初期表示を SSR したプレビュー画像（Light / Dark の PNG）にし、.NET ランタイムが起動したら `AvaloniaView` に差し替えると決めた。WASM で動かないデモ（DataGrid）でもプレビュー画像を出し続ける想定だった。公開後、2 つの要望と不具合が出た。

1. ロード前の表示はスクリーンショットでなく、shadcnui-hono-jsx のスケルトンでよい（サイトの持ち主の要望）。
2. デモの外側に描かれるもの、たとえば Switch のフォーカスリング（`PART_FocusRing` の `Margin="-3"`）が、左と上で切れていた。原因は 2 つあった。ビューのホストが `<avalonia-demo>` と同じ大きさで、デモの外側に描く余地がなかった。加えて、デモのルートの `UserControl` が自分の範囲で描画を切り取っていた。Avalonia では `TemplatedControl` の `ClipToBounds` の既定値が true である（`TemplatedControl.cs` の `ClipToBoundsProperty.OverrideDefaultValue<TemplatedControl>(true)`）。ホストを広げるだけでは、リングは切れたままだった。

## Decision Drivers

* ロード中の見た目はサイトの部品（shadcnui-hono-jsx）と統一したい。
* ライブデモに差し替わるときに、レイアウトがずれないこと（ADR 23 で高さを内容に合わせたあとも、最初の枠の大きさは保ちたい）。
* デモの見かけの位置と大きさは変えずに、フォーカスリングや影が切れないようにすること。
* ライブラリのテーマ（`src/`）にはサイトの都合を持ち込まないこと。

## Considered Options

* 初期表示: プレビューの大きさ（`manifest.json`）のスケルトンにする
* 初期表示: これまでどおりプレビュー画像にする
* 初期表示: ロード中はスケルトン、失敗したときだけプレビュー画像にする
* 余白: ビューのホストを要素から `--demo-inset` だけはみ出させ、`DemoRoot` で同じ幅を `Padding` として空け、デモのルートの `ClipToBounds` を切る
* 余白: `<avalonia-demo>` 自体を余白の分だけ大きくする
* 余白: NovaTheme で `UserControl` の `ClipToBounds` を false にする

## Decision Outcome

Chosen option: 「スケルトン」と「ホストをはみ出させ、`DemoRoot` で空け、ルートの `ClipToBounds` を切る」。前者はサイトの持ち主の要望で、要素の大きさを変えずに済む。後者はデモの見かけを変えずにリングと影を描けて、ライブラリにも触れない。

* **初期表示:** `sites/app/components/demo.tsx` は `<avalonia-demo>` の中に `Skeleton`（`components/ui/skeleton.tsx`）を置き、要素の幅と高さを `manifest.json` のプレビューの大きさにする。ライブになったらスケルトンを隠す。WASM を publish していない（`static`）とき、またはマウントに失敗した（`error`）ときは、スケルトンの点滅を止めて「Live demo unavailable」と出す。バッジは「Unavailable」にする。ADR 20 の「初期表示はプレビュー画像」と「DataGrid が動かない場合はプレビュー画像だけを出す」をこの ADR で置き換える。
* **プレビュー画像:** ページには出さない。Previews は引き続き CI で描く。`manifest.json` はデモの枠の大きさに使い、PNG は OG 画像（ADR 22）だけが使う。
* **余白:** ビューのホストは `<avalonia-demo>` から上下左右に `--demo-inset`（`sites/app/style.css`、12px）だけはみ出す。`sites/app/avalonia-demo.ts` はこの値を CSS から読み、`Demos.Mount(hostId, demoId, inset, heightChanged)` の新しい 3 番目の引数として渡す。`samples/AvaloniaUIKit.Browser/DemoRoot.cs` は `inset` を `Padding` にし、デモのルートの `ClipToBounds` を false にする。`heightChanged` に返す高さは `inset` を含まないデモの高さなので、要素の大きさの決め方は ADR 23 と変わらない。ADR 23 の `Mount` の引数の並びだけをこの ADR で変える。

### Consequences

* Good, because ロード中の表示がサイトの部品と同じ見た目になり、プレビュー画像とライブデモのわずかな違い（文字の測り方など）で、差し替えのときに見た目が跳ねることもなくなる。
* Good, because デモの外側 12px までのフォーカスリングと影が切れずに見える。デモの位置と要素の大きさは変わらない。
* Good, because ページがプレビューの PNG を読み込まなくなる。
* Bad, because JS なしの閲覧者やクローラーには、デモの見た目が伝わらなくなる（コード例は残る）。ADR 20 の「JS なし、クローラー、モバイルでも見た目は伝わる」は成り立たない。
* Bad, because WASM で動かないデモがあると、画像の代わりに「Live demo unavailable」しか出ない。2026-10-04 の時点では全 208 デモが動く。
* Bad, because 12px を超えて外側に描く影は、これまでどおりビューの端で切れる。
* Neutral, because ビューの背景（テーマの背景色）がデモの周り 12px にも広がり、枠のグリッド線がその分だけ隠れる。

### Confirmation

* `bun run build` と `bun scripts/smoke.ts` が通る。
* Playwright（幅 1400、devicePixelRatio 1）で、Switch のデモを押して Tab で戻したとき、フォーカスリングが四辺とも丸く描かれることを確かめた。`ClipToBounds` を切る前は、余白を足しても左と上が切れていた。
* `dotnet.js` を止めた状態でスケルトンが、404 にした状態で「Live demo unavailable」が出ることを確かめた。
* 全 208 デモが幅 1400 と 375 でライブになり、幅 375 で枠に横スクロールが出ないことを確かめた。

## Pros and Cons of the Options

### 初期表示: スケルトン

* Good, because サイトの部品だけで組め、ライブデモへの差し替えで見た目が跳ねない。
* Bad, because JS なしでは何も見えない。

### 初期表示: プレビュー画像

* Good, because JS なし、クローラーでも見た目が伝わる。
* Bad, because ロード中の見た目について、サイトの持ち主の要望に合わない。ページごとに Light / Dark の PNG を読み込む。

### 初期表示: ロード中はスケルトン、失敗したときだけプレビュー画像

* Good, because WASM で動かないデモでも見た目が残る。
* Bad, because 画像を隠しておく仕組み（隠れた `loading="lazy"` の画像をブラウザーが読み込むかどうかの差も含む）が要る。現時点では全デモが動くので、その複雑さに見合わない。

### 余白: ホストをはみ出させ、`DemoRoot` で空け、ルートの `ClipToBounds` を切る

* Good, because 要素はデモの大きさのままなので、スケルトン、`manifest.json`、`heightChanged` の意味が変わらない。
* Bad, because 値を CSS と .NET の両方で使う。`Mount` の引数で渡すので、定義は CSS の 1 か所で済む。

### 余白: `<avalonia-demo>` 自体を大きくする

* Good, because ホストをはみ出させる CSS が要らない。
* Bad, because 要素の大きさがプレビューの大きさとずれ、スケルトンの大きさと `heightChanged` の値に余白の分を足し引きする必要がある。

### 余白: NovaTheme で `UserControl` の `ClipToBounds` を false にする

* Good, because サイト以外でも、`UserControl` の端に置いたコントロールのリングが切れなくなる。
* Bad, because ライブラリの利用者のアプリの描画を変える。Avalonia の既定値から外れる判断は、サイトの都合とは別に検討すべきである。

## More Information

ADR 20 の「初期表示はプレビュー画像」と DataGrid のフォールバック、ADR 23 の `Mount` の引数を変える。約束事は `docs/site.md` の「ライブデモ」にまとめる。
