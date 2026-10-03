---
number: 8
title: Match GPUI geometry with Avalonia borders and shadows
status: accepted
date: 2026-10-03
---

# Match GPUI geometry with Avalonia borders and shadows

## Context and Problem Statement

GPUI と Avalonia では、角丸、枠線、影、フォーカスリングの定義が違う。同じ数値を書いても同じ形にならない。

* GPUI の `corner_radii` は外側の輪郭の半径。Avalonia の `Border` は、枠線の中心線の半径を `CornerRadius` に取る（WinUI と同じ）。
* GPUI の影の `blur_radius` はガウス分布の σ。Avalonia の `BoxShadow` の Blur は Skia のぼかし半径。
* GPUI のフォーカスリングは要素の外側に描く 3px の帯。Avalonia の既定のフォーカス表示（FocusAdorner）とは別物。

## Decision Outcome

次の規約で GPUI の形に合わせる。どれも構造比較（ADR 10）で、角丸と寸法が ±0.26px 以内であることを確かめている。

* **角丸:** 枠線の太さ t の要素は `CornerRadius = R − t/2`（R は GPUI の半径）。例: 半径 6、枠 1px のボタンは 5.5。背景は `BackgroundSizing="OuterBorderEdge"` にして、半透明の枠の下まで塗る（GPUI と同じ）。
* **影:** σ を `Blur = (σ − 0.5) / 0.288675` に変換する（Skia の σ = 0.288675 × Blur + 0.5）。例: σ 3 → 8.6603、σ 6 → 19.0526。spread の扱いの違いは緩和 R3。
* **フォーカスリング:** テンプレート内に `PART_FocusRing`（`Margin=-3`、`BorderThickness=3`、ring の 50%）を置き、`:focus-visible` で表示する。ToggleSwitch はトラックを、CheckBox / RadioButton は行全体を囲む。ToggleSwitch は GPUI と同じく `:focus` で表示する。
* **はみ出す描画:** リングと影が切れないよう、`ClipToBounds=False` を Button、ポップアップのホスト、`LayoutTransformControl` などに設定する。
* **行の高さ:** GPUI は文字の行高を φ（1.618）× 文字サイズや `line_height` で決める。テンプレートで `LineHeight` を明示して合わせる。
* **色の切り替え:** GPUI は hover などの色を即座に切り替えるので、Brush に Transition を付けない。

### Consequences

* Good, because 枠付きの要素、影、リングの輪郭が GPUI と同じ位置に来る。
* Bad, because Avalonia の慣習（Fluent のテンプレートなど）と値の意味が違うので、テーマを直すときは GPUI の値から換算し直す必要がある。各テーマのコメントに換算前の値を書いている。
