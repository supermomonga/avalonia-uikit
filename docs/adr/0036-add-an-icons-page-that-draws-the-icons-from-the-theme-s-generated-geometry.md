---
number: 36
title: Add an Icons page that draws the icons from the theme's generated geometry
status: accepted
date: 2026-10-07
links:
- target: 22
  kind: amends
- target: 38
  kind: amendedby
---

# Add an Icons page that draws the icons from the theme's generated geometry

## Context and Problem Statement

テーマは GPUI Kit の `IconName` のアイコンと、テーマ自身が描くいくつかのアイコンを `StreamGeometry` として同梱している（ADR 13、`Themes/Icons/Lucide.g.axaml`）。これまでサイトでは `/docs/icons` の手書きの表でキーと名前を並べるだけで、形を見て選ぶことも、サイズや色を変えた XAML をそのまま貼ることもできなかった。上部のナビに Icons を足し、一覧と検索、選んだアイコンの詳細（サイズ別の見た目、サイズ別・色別の XAML のコピー）をページ遷移なしで見せたい。ADR 22 はサイトの構成をトップ、ガイド、コンポーネントの 3 つとし、gpui-kit.com にそろえると決めたが、gpui-kit.com にはアイコンの一覧のページがない。何からアイコンを描き、詳細をどう見せるかを決める。

## Decision Drivers

* ページに出す形は、テーマが実際に描く形と一致させたい（ストロークを輪郭にしたもの、`SortAscending` の薄い半分、タイトルバーのボタンのような Lucide にないもの）。
* アイコンの追加や再生成（`reference generate`）に、サイト側の手作業なしで追従したい。
* ページは静的 HTML のまま、ライブデモの .NET ランタイムを読み込まずに軽く開けるようにしたい（ADR 20）。
* 詳細はその場で開き、一覧を見ながら別のアイコンに切り替えられるようにしたい。

## Considered Options

* `Lucide.g.axaml` と `IconName.g.cs` をビルド時に読み、ジオメトリを SVG の `<path>` として描く
* サイトがすでに使っている `lucide` パッケージの SVG で描く
* Previews のようにヘッドレスの Avalonia で各アイコンの画像を描く
* ライブデモ（WebAssembly）でアイコンを描く

## Decision Outcome

Chosen option: "`Lucide.g.axaml` と `IconName.g.cs` をビルド時に読み、ジオメトリを SVG の `<path>` として描く", because テーマが描く形そのものを、追加の生成物も実行時の重さもなく出せるから。

* **構成:** 上部のナビに Components の右へ Icons（`/icons`）を足す。サイドバーは持たない。ADR 22 の 3 セクションに 1 つ足すだけで、ほかの構成と見た目は gpui-kit.com にそろえたままにする。
* **データ:** `sites/app/lib/icons.ts` が両ファイルを Vite の `?raw` で読む。キー（`UIKit.Icon.<名前>`）とパスは `Lucide.g.axaml`、`IconName` とそのファイル名、薄い部分の不透明度は `IconName.g.cs` から取る。Avalonia のパス記法は塗りの規則（`F1`）を除けば SVG と同じなので、そのまま `viewBox="0 0 24 24"` の `<path>` にする。`.Faint` は単独の項目にせず、本体の下に不透明度を付けて描く。
* **詳細:** 選んだアイコンは 960px 以上でグリッドの右に固定し、それより狭いと画面の下からのシートで開く。サイズ（PathIcon の xsmall、small、medium、large）、色（テーマのリソースのうちサイトにも同じ色があるもの）、要素（`uikit:Icon` か `PathIcon`）を選ぶと、その組み合わせの XAML を作ってコピーできる。`IconName` のないアイコンは `PathIcon` だけにする。選んだアイコンは `?icon=<名前>` で URL に残す。

### Consequences

* Good, because 生成器がアイコンを変えれば、次のビルドでページも変わる。手書きの一覧と違って食い違わない。
* Good, because ページは静的 HTML と小さなスクリプトだけで、プレビュー画像も .NET のランタイムも要らない。
* Good, because サイトのテーマの色（CSS 変数）で描くので、テーマを切り替えればアイコンの見本も切り替わる。
* Bad, because ブラウザーの SVG のラスタライズは Skia と同じではないので、小さいサイズの見本は実際の描画とわずかに違いうる。
* Bad, because 色の見本はサイトに同じ色がある 6 つ（既定、Muted、Primary、Danger、Warning、Success）に限られる。`UIKit.Info` のようにサイトの CSS 変数にない色を出すには、`tokens.rs` の `SITE_VARS` に足して再生成する必要がある。
* Neutral, because `/docs/icons` の表は手書きのまま残る。使い方の説明はガイドに、形の一覧は Icons のページに置く。

### Confirmation

* `sites/scripts/smoke.ts` が `/icons.html` のビルドを確かめる。`sites/app/lib/icons.ts` は `Lucide.g.axaml` からアイコンを 1 つも読めなければビルドを止める。
* 構成と約束事は `docs/site.md` にある。

## Pros and Cons of the Options

### `Lucide.g.axaml` と `IconName.g.cs` をビルド時に読み、ジオメトリを SVG の `<path>` として描く

* Good, because テーマと同じ元データなので、形、名前、キー、薄い部分が一致する。
* Good, because サイトのビルドだけで完結し、CI のジョブも生成物も増えない。
* Bad, because 生成物の書式（`x:Key`、`/// <summary>icons/…</summary>`）に依存する。書式を変えたら `icons.ts` も直す。

### サイトがすでに使っている `lucide` パッケージの SVG で描く

* Good, because 正規表現での読み取りが要らない。
* Bad, because GPUI Kit が同梱する SVG と同じとは限らない（`close` は Lucide の `x`、タイトルバーのボタン、`Github`、`ResizeCorner`、`SortAscending` の 2 色、`StarFill` は Lucide にない）。
* Bad, because パッケージの版がテーマの版とずれると、ページの形と描かれる形が食い違う。

### Previews のようにヘッドレスの Avalonia で各アイコンの画像を描く

* Good, because Skia で描いた実際の見た目を出せる。
* Bad, because アイコン、サイズ、色、テーマの組み合わせだけ画像が増え、CI の .NET のジョブに描画が加わる。サイトのテーマの切り替えに追従させるには、さらに画像が要る。

### ライブデモ（WebAssembly）でアイコンを描く

* Good, because 実際のコントロールがテーマのとおりに描く。
* Bad, because 一覧を見るだけで .NET のランタイムを読み込むことになり、ページが重くなる。109 個のアイコンを並べるのにビューを多数載せるのも向かない。

## More Information

* 実装: `sites/app/lib/icons.ts`、`sites/app/components/icons-page.tsx`、`sites/app/styles/icons.css`、`sites/app/client.ts` の Icons の節
* アイコンの同梱: ADR 13（`reference/src/icons.rs`）
* サイトの構成: ADR 22、`docs/site.md`
