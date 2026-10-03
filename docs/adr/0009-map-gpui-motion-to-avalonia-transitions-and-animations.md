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
| spring（response、damping、epsilon） | `Motion.Spring`（ADR 15 の方針で追加した Behavior）。GPUI の `SpringConfig::step` と同じ解析解で位置と速度を進め、目標が変わっても速度を引き継ぐ。時刻は要素の時計で動く Animation から得る。当初は `SpringEasing`（`Stiffness=(ω·D)²`、`Damping=2ζ·ω·D`、D は収束までの時間）で近似していたが、速度を引き継げなかった。 |
| ease-out-cubic など名前付きの曲線 | `CubicEaseOut` などの組み込み Easing |
| 周期的な式（不定値 Progress、Spinner） | 無限に繰り返すキーフレーム。区間ごとの曲線は、その区間の終わりのキーフレームの `KeySpline` で表す。幅などの比例値は `TemplateSettings` から取る。 |
| フェードとスライドの入場（Tooltip） | テンプレート内の `PART_Motion` に対する入場アニメーション |
| スクロールバーの表示・消去（待ち時間、フェード、スライド） | `ScrollBar` の既存の `IsExpanded`、`ShowDelay`、`HideDelay` と、状態ごとの Opacity / RenderTransform の Transition |

* 動きは、仮想時計で GPUI と同じ時刻まで進めて描いたフレームで比べる（ADR 14）。
* 1 つの状態のスタイルで Transition と値をどちらも変えるときは、Transition の Setter を値の Setter より先に書く。逆にすると、値が古い Transition で動き出した直後にその Transition が外れ、値が飛ぶ。
* コントロールが読み込み時に収まる値（ProgressBar の幅など）は動かさない。GPUI は変化を動かすが、最初のフレームは動かさないため。`gpui:Motion.SettledTransitions` で、最初のレイアウトの後に Transition を付ける。
* spring の途中で目標が変わったときも、GPUI と同じく速度を引き継ぐ。止まるかどうかの判定の時刻の差は ε 以内（R8）。

### Consequences

* Good, because 新しい機構なしで、GPUI と同じ曲線になる（Switch のつまみで 0.26px 以内、チェックの不透明度で 0.002 以内）。
* Bad, because spring だけは Behavior のコードに頼る。
