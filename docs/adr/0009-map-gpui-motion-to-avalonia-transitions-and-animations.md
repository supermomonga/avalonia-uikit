---
number: 9
title: Map GPUI motion to Avalonia transitions and animations
status: accepted
date: 2026-10-03
---

# Map GPUI motion to Avalonia transitions and animations

## Context and Problem Statement

GPUI Kit の動きは、時間と cubic-bezier の Easing による遷移、状態を持つ spring、毎フレーム式で計算するアニメーションでできている。Avalonia で使えるのは `Transitions`（プロパティの遷移）、キーフレームの `Animation`、`Easing`（`SplineEasing`、`SpringEasing` など）だけ。新しいアニメーション機構は作らない（対応表の方針）。

## Decision Outcome

GPUI の動きを、同じ曲線になる Avalonia の標準機構に置き換える。

| GPUI | Avalonia |
| --- | --- |
| `duration_*` と `easing_enter / exit / move`（cubic-bezier） | `Transitions` の `Duration` と `SplineEasing` |
| spring（response、damping） | `SpringEasing`（`Mass=1`、`Stiffness=(ω·D)²`、`Damping=2ζ·ω·D`、ω = 2π / response）。`Duration` D は、GPUI の spring が収束判定（ε）を満たすまでの時間を 60Hz で模擬して求める。移動量ごとに D が変わるので、サイズごとに別の値を持つ。 |
| ease-out-cubic など名前付きの曲線 | `CubicEaseOut` などの組み込み Easing |
| 周期的な式（不定値 Progress、Spinner） | 無限に繰り返すキーフレーム。区間ごとの曲線は、その区間の終わりのキーフレームの `KeySpline` で表す。幅などの比例値は `TemplateSettings` から取る。 |
| フェードとスライドの入場（Tooltip） | テンプレート内の `PART_Motion` に対する入場アニメーション |
| スクロールバーの表示・消去（待ち時間、フェード、スライド） | `ScrollBar` の既存の `IsExpanded`、`ShowDelay`、`HideDelay` と、状態ごとの Opacity / RenderTransform の Transition |

* 動きの値は、テストでテーマの宣言から読み取って評価し、GPUI の記録と比べる（ADR 10 の (a)）。
* spring の途中で目標が変わったときの速度の引き継ぎは再現しない（緩和 R6）。spring の終端の扱いの差は 0.1px 以内（R8）。

### Consequences

* Good, because 新しい機構なしで、GPUI と同じ曲線になる（Switch のつまみで 0.26px 以内、チェックの不透明度で 0.002 以内）。
* Bad, because spring の時間 D を移動量ごとに計算して書く必要があり、寸法を変えると D も計算し直しになる。
