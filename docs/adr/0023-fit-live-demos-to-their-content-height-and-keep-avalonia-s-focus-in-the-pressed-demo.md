---
number: 23
title: Fit live demos to their content height and keep Avalonia's focus in the pressed demo
status: accepted
date: 2026-10-04
links:
- target: 20
  kind: amends
- target: 24
  kind: amendedby
---

# Fit live demos to their content height and keep Avalonia's focus in the pressed demo

## Context and Problem Statement

ADR 20 の方式（1 つのランタイムの上に複数の `AvaloniaView`）で公開したサイトに、埋め込みに特有の不具合が 3 つ見つかった。

1. ライブデモの高さは、プレビュー画像を要素の幅に縮めたときの高さで決まっていた。プレビューより狭い画面では、デモは要素の幅で折り返すのにビューの高さは縮んだままになり、下が切れる。Button のデモ（636×32）は高さ 24px ほどのビューに描かれ、ボタンの上下と角丸が切れて四角に見えた。
2. Avalonia.Browser はホスト要素に `tabindex=0` を付け、ビューのフォーカスが変わるたびにホストへ `focus()` する（`AvaloniaView` の `GotFocus`、`BrowserTextInputMethod`）。デモを押すとデモ全体をブラウザー既定のフォーカスリングが囲った。
3. Avalonia のキーボード（`KeyboardDevice` とその `TextInputMethodManager`）は全ビューで 1 つしかない。デモ B を押してフォーカスが B に移ると、直前にフォーカスのあったデモ A の IME が解除され、A のホストに `focus()` が呼ばれる。その結果、フォーカスは A に戻り、ページは A までスクロールした。

## Decision Drivers

* Avalonia のパッケージには手を入れない（ADR 20 の「独自のパッチが要らない」を保つ）。
* 狭い画面でも、デモは本物の大きさで操作できること。
* プレビュー画像とライブデモは同じ XAML、同じ大きさの決め方から出ること。

## Considered Options

* 高さ: ビューを内容に合わせる（要素の幅で高さを制限せずにレイアウトし、必要な高さをページに返す）
* 高さ: ビューをプレビューの大きさのまま CSS で縮小する
* 高さ: ビューをプレビューの大きさのまま置き、枠を横にスクロールさせる
* フォーカス: ページ側で Avalonia の `focus()` を差し替える
* フォーカス: Avalonia.Browser を改変する（フォーク、またはパッチ）

## Decision Outcome

Chosen option: 「ビューを内容に合わせる」と「ページ側で `focus()` を差し替える」。理由は、パッケージに触れずに 3 つの不具合をすべて解消でき、狭い画面でもデモを本来の大きさで触れるから。

* **高さ:** `samples/AvaloniaUIKit.Browser/DemoRoot.cs` をビューの中身にする。デモを要素の幅、高さ無制限で測る。これはプレビューの描き方（`SizeToContent`、幅 640 まで）と同じである。測ったデモは中央に置き、高さが変わるたびに `Demos.Mount` の 3 番目の引数 `heightChanged` を呼ぶ。`sites/app/avalonia-demo.ts` はその値を `<avalonia-demo>` の高さにする。要素の幅はプレビューの幅を上限に、親の幅まで縮む。
* **フォーカス:** `avalonia-demo.ts` はマウント後にホストと IME 用の `<input>` の `focus` を差し替え、常に `preventScroll` を付ける。あるデモで `pointerdown` を処理している間（capture で印を付け、`setTimeout(0)` で外す）は、ほかのデモの要素への `focus()` を捨てる。Avalonia はこのポインター入力を同期的に処理する。
* **フォーカスリング:** ホストの `outline` は消す。フォーカスの表示はデモの中で Avalonia が描く。

### Consequences

* Good, because 狭い画面ではデモが本来の大きさのまま折り返し、切れなくなる。
* Good, because デモを押してもページはスクロールせず、フォーカスとキー入力は押したデモに残る。
* Bad, because プレビューより狭い画面では、ライブデモに差し替わるときに高さが変わる（レイアウトのずれ）。
* Bad, because 折り返せない固定幅のデモは、これまでどおり右が切れる。Avalonia の `DesiredSize` は与えた幅で頭打ちになるので、ページは必要な幅を知ることができない。
* Bad, because `focus()` の差し替えは、Avalonia.Browser がポインター入力を同期的に処理し、要素の `focus()` を呼ぶという実装に頼っている。Avalonia の更新でこれが変わると効かなくなる（スクロールが戻るだけで、壊れはしない）。

### Confirmation

* `bun run build` 後に、デモを順に押してもスクロール位置が変わらず、`document.activeElement` が押したデモのホストになることを Playwright で確かめた。
* 幅 375 / 525 / 700 / 1400 で Button のデモが折り返して切れないこと、広げると 1 行に戻ることを確かめた。
* 全 208 デモが幅 1400 と 375 でライブになることを確かめた。幅 1400 での高さはプレビューと同じか数 px 高い。差は、ブラウザーと Skia の文字の測り方の違いと、Kbd の表記がプラットフォームで変わる（macOS の記号と `Ctrl+K`）ことによる。以前はこの差の分だけ下が切れていた。

## Pros and Cons of the Options

### 高さ: ビューを内容に合わせる

* Good, because 本物のコントロールが本来の大きさで、その幅の Avalonia アプリと同じように並ぶ。
* Bad, because .NET からページへのコールバック（`JSType.Function`）が要る。

### 高さ: プレビューの大きさのまま CSS で縮小する

* Good, because プレビュー画像と同じ見た目のまま差し替わり、.NET 側の変更が要らない。
* Bad, because 幅 300px ほどの枠では 636px のデモが半分以下に縮み、文字が読めず、指で押せない。

### 高さ: プレビューの大きさのまま横にスクロールさせる

* Good, because 固定幅のデモも切れない。
* Bad, because 中央寄せの flex で左にはみ出した部分に届かなくなるので、配置を作り直す必要がある。さらに、Avalonia がホストに付ける `touch-action` のせいで、デモの上を横にスワイプしてもスクロールしない。

### フォーカス: ページ側で `focus()` を差し替える

* Good, because パッケージを触らず、`avalonia-demo.ts` の中で完結する。
* Bad, because Avalonia の内部の振る舞いに依存する（Consequences を参照）。

### フォーカス: Avalonia.Browser を改変する

* Good, because 共有キーボードが別のビューのホストにフォーカスを戻すという原因そのものを直せる。
* Bad, because パッケージをフォークし、Avalonia の更新のたびに追従する必要がある。

## More Information

ADR 20 の「ホイールの扱い」と同じく、埋め込みに特有の扱いである。約束事は `docs/site.md` の「ライブデモ」にまとめる。
