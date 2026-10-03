---
number: 13
title: Bundle Lucide icons as geometry with their license
status: accepted
date: 2026-10-03
links:
- target: 21
  kind: amendedby
---

# Bundle Lucide icons as geometry with their license

## Context and Problem Statement

GPUI Kit のコンポーネントは Lucide のアイコン（check、minus、plus、chevron、loader など）を SVG のストロークで描く。Avalonia には Lucide が同梱されていない。任意の SVG を読み込む仕組みを作ることは対応表の方針で避ける。Lucide は ISC ライセンスで、再配布には著作権表示が要る。

## Considered Options

* テーマが使うアイコンだけを、塗りの輪郭に変換した `StreamGeometry` としてリソースにする
* SVG ファイルを同梱し、実行時に読み込む
* Avalonia の SVG パッケージに依存する

## Decision Outcome

Chosen option: "必要なアイコンだけを `StreamGeometry` にする", because 追加の依存も実行時の読み込みもなく、`PathIcon` で描けるから。

* 参照データの生成器が、GPUI Kit が同梱する SVG（`crates/assets/assets/icons`）を `usvg` で読み、ストロークを `tiny-skia-path` で塗りの輪郭に変換して `Themes/Icons/Lucide.g.axaml` を書き出す。キーは `Gpui.Icon.<名前>`。
* 24×24 の枠を保つため、各ジオメトリの先頭に (0,0) と (24,24) の空の図形を置く。
* ライセンスは `Themes/Icons/LUCIDE-LICENSE.txt` に置き、生成物のヘッダーから参照する。テスト用フォントの Inter は `assets/fonts/inter/OFL.txt`。

### Consequences

* Good, because アイコンの形は GPUI Kit と同じ元データから作られる。
* Bad, because アイコンを足すには生成器を変えて再生成する必要がある。ストロークの輪郭化と SVG のラスタライズの差は緩和 R4 で扱う。
