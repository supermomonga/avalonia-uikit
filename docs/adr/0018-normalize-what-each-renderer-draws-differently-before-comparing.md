---
number: 18
title: Normalize what each renderer draws differently before comparing
status: accepted
date: 2026-10-04
links:
- target: 25
  kind: amendedby
---

# Normalize what each renderer draws differently before comparing

## Context and Problem Statement

構造の比較（ADR 10）は、GPUI の Scene と Avalonia の可視ツリーを同じ種類の図形にそろえて 1 対 1 で照合する。部分対応のコンポーネントを移植すると、同じ見た目を両者が別の図形で表す場面が増えた。

* 片側だけの罫線: GPUI は quad の下枠、DataGrid は 1px の `Rectangle`。
* グラデーション: GPUI の Scene は `linear_gradient` を持つが、エクスポーターは単色しか書き出していなかった。GPUI のシェーダーはグラデーションをディザする。
* 画像: GPUI はポリクロームのスプライト、Avalonia は `Image`。拡大した画像の縁を GPUI はアトラスの透明な隣接画素と補間して薄める。
* 二色のアイコン: GPUI では 1 枚のスプライト（薄い半分はマスクの中）、Avalonia では不透明度の違う 2 枚の `PathIcon`。
* 文字: TextBlock の箱は文字そのものより広く高い。窓の外に出かけた文字を、GPUI はグリフ単位で描かない。
* 切り取られた図形、窓の外の要素、消えかけの線（5% 未満）は、GPUI が描かないことがある。

## Considered Options

* 両者の違いを正規化してから照合し、原理的な違いだけを緩和 ID 付きで許す
* ケースごとに許容値を緩める
* 違いの出るケースを外す

## Decision Outcome

Chosen option: "両者の違いを正規化してから照合し、原理的な違いだけを緩和 ID 付きで許す", because 見た目が同じものは同じとして扱い、違いの検出力は保てるから。

* 正規化（同じ見た目を同じ図形にする）:
  * 角丸のない四角の、片側だけの枠は帯（Fill）にする。
  * 2 色の線形グラデーションは `Gradient`（始点色・終点色・角度）として比べる。参照側は Scene の `Background` を書き出す。
  * 画像は `Image`（描かれる矩形と角丸）として比べる。
  * `Rectangle` はインクではなく箱として比べる（構造の側で照合する）。
  * 不透明度の低い `PathIcon` は、同じ色のより濃い GPUI のスプライトの一部として認める。
  * 窓の外かどうかは、文字なら行ごとのインクの帯（行の幅 × フォントの ascent + descent）で判定する。完全に切り取られた図形は数えない。
* 画素の領域を足す: `Image`、`ImageEdge`（R31）、`Gradient`（R32）。クリップが図形を切る位置は Edge とする。
* 新しい緩和: R30（5% 未満の線とインクは画素だけで比べる）、R31（画像の縁）、R32（グラデーションのディザ）。

### Consequences

* Good, because テーマの側で GPUI の図形の作り方をまねる必要がなく、Avalonia として自然な部品（DataGrid の罫線、二色アイコンの重ね）を使える。
* Good, because 正規化は両側に同じ規則でかけるので、既存のケースはそのまま通る。
* Bad, because 比較のコードが増え、「何を同じとみなすか」を docs/testing.md で追う必要がある。
