---
number: 15
title: Allow presentation-only code alongside the theme
status: accepted
date: 2026-10-03
links:
- target: 19
  kind: amendedby
- target: 29
  kind: amendedby
- target: 30
  kind: amendedby
---

# Allow presentation-only code alongside the theme

## Context and Problem Statement

対応表は当初、テーマ（Style、ControlTheme、ControlTemplate）だけで移植し、Behavior や独自のアニメーション処理は作らないとしていた。そのため、GPUI Kit の見た目や動きのうち、XAML だけでは書けないものを対象外にしていた。

* ばねの途中反転で速度を引き継ぐ動き（緩和 R6）。Avalonia の Easing は進み具合しか受け取らず、独自の Transition も作れない（`Transition<T>.DoTransition` が internal）。
* スクロール中だけ表示するスクロールバー（R20）。ScrollViewer には「スクロール中」の状態がない。
* 部分対応の行にも、同じ理由で対象外にしたものが多い（ProgressCircle の値から角度への変換、Tabs の選択を追う下線、Collapsible の高さを測った開閉など）。

GPUI Kit との一致を目指すには、この制約がいちばんの障害になる。一方で、コードを足すほど、テーマとしての手軽さや保守のしやすさは下がる。

## Considered Options

* 見た目だけを変えるコードに限って許可する
* テーマだけの制約を続ける
* 新しいコントロールも含めて制約をなくす

## Decision Outcome

Chosen option: "見た目だけを変えるコードに限って許可する", because 見た目と動きの一致に必要なものは書けて、テーマとして使う形（アプリはテーマを追加するだけ）は保てるから。

* **使えるもの:**
  * 状態を持たない値変換（Converter）
  * 既存のコントロールの状態やイベントを読み、見た目のプロパティだけを動かす添付プロパティ型の Behavior
* **適用の仕方:** テーマのスタイルやテンプレートから適用する。アプリのコードは変えずに済む。
* **作らないもの:**
  * 新規コントロール
  * コントロールの論理的な状態や操作を変える処理（ポップアップを閉じるのを遅らせる、選択を変える等）
  * 選択・検索・データ管理の機能
* **条件:** NativeAOT で動き、リフレクションを使わない（ADR 11）。参照データとの比較テストで検証する。
* コードは `src/AvaloniaUIKit/Behaviors/` に置く。最初の例は `Motion.SettledTransitions`（最初のレイアウトの後に Transition を付ける）。

### Consequences

* Good, because R6・R20 と、部分対応の行の多くの見た目・動きを移植できるようになる。
* Good, because アプリから見た使い方は変わらない。
* Bad, because ライブラリにコードが増え、Avalonia の既存コントロールの振る舞い（どの順に値を設定するか等）に依存する箇所が出る。比較テストで守る。
* Neutral, because 対応表の各行の「対象外」は、この条件で表せるものを対象に戻して見直す。
