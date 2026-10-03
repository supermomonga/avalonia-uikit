---
number: 19
title: Add controls for small GPUI Kit components that Avalonia lacks
status: accepted
date: 2026-10-03
links:
- target: 15
  kind: amends
---

# Add controls for small GPUI Kit components that Avalonia lacks

## Context and Problem Statement

対応表の「対応」「部分対応」の 50 行はすべて実装した（ADR 15 までの方針）。残る GPUI Kit のコンポーネント 32 行は、Avalonia 本体と公式の別パッケージに対応するコントロールがないため、「新規コントロールを作らない」（ADR 15）により対象外にしていた。

そのうち Badge、Tag、Alert のように、見た目が中心で状態がいくつかのプロパティで表せるものは、テーマ済みの部品（Button、PathIcon、TextBlock、Image）を組み合わせた小さなコントロールで作れる。一方で、Dialog、Command、Dock、Editor、Chart のように、モーダル・検索・ドッキング・編集・可視化の仕組みそのものが要るものは、コントロールを足しても小さく収まらない。

## Decision Drivers

* GPUI Kit の見た目を、できるだけ多くのコンポーネントで Avalonia でも使えるようにする。
* ライブラリが「データ管理や業務処理を代行しない、見た目の部品」であることは保つ。
* NativeAOT とリフレクション禁止（ADR 11）、参照データとの比較による検証（ADR 10）を新しいコントロールにも適用する。

## Considered Options

* 小さなコンポーネントに限って新しいコントロールを作る
* 新規コントロールを作らない方針を続ける
* 対象外のコンポーネントをすべて新しいコントロールとして作る

## Decision Outcome

Chosen option: "小さなコンポーネントに限って新しいコントロールを作る", because 見た目の部品として価値の高いものを足せて、仕組みごと作る必要のある大きなものは引き続き範囲外にできるから。

* **選ぶ基準:**
  * 見た目が中心で、状態をいくつかのプロパティ（種類、数、値など）で表せる。
  * 中身はテーマ済みの部品の組み合わせで描ける。
  * データ管理、検索、ポップアップの寿命管理、フォーカス管理を新しく作らずに済む。
  * 参照データとの比較で検証できる。
* **対象:**
  * 第 1 弾: Badge、Tag、Alert、Skeleton、StatusBar、Breadcrumb、Kbd、Clipboard、Rating、Avatar / AvatarGroup、Empty。
  * 第 2 弾: DescriptionList、Stepper、Form / Field、HoverCard、ShimmerText、Marker、Bubble / Message。
* **範囲外のまま:** Dialog / AlertDialog、OtpInput、Command、Dock、Settings、Questionnaire、Editor、TextView / Markdown、Chart、Plot、Speech、Attachment、MessageScroller。Dialog は価値が高いが、モーダルの重ね表示・フォーカスの閉じ込め・重なりの管理が要るので、別の判断にする。
* **作り方:**
  * `src/AvaloniaUIKit/Controls/` に置き、名前空間は `AvaloniaUIKit`（XAML では Behavior と同じ `gpui:`）。
  * コントロールはプロパティ、疑似クラス、テンプレートの部品だけを持ち、見た目は `GpuiTheme` の ControlTheme で付ける。処理は表示に必要な最小限にする（Clipboard のコピー、Rating の値の変更、HoverCard の開閉など、そのコンポーネント自身の操作だけ）。
  * 名前は GPUI に合わせる。ただし Avalonia のメンバーとぶつかるもの、Avalonia のアプリで意味が広すぎるものは変える（Tag → `TagLabel`（`Control.Tag` と同名になる）、Empty → `EmptyState`、Field → `FormField`）。
  * NativeAOT で動き、リフレクションを使わない。ギャラリー（`samples/AvaloniaUIKit.AotSmoke`）に入れる。
  * 検証は既存のコンポーネントと同じく、GPUI Kit が描いた参照データと構造・画素・動きを比べる。
* **ADR 15 との関係:** 既存のコントロールの見た目を補うコードの範囲（Converter、Behavior）は ADR 15 のまま。ADR 15 の「新規コントロールを作らない」を、上の基準を満たすものに限って改める。

### Consequences

* Good, because GPUI Kit の小さな表示部品の多く（バッジ、タグ、警告、アバター、パンくず、ステップ表示など）を Avalonia でも同じ見た目で使える。
* Good, because 大きな仕組みが要るものは範囲外のままなので、ライブラリの性格（見た目の部品集）は変わらない。
* Bad, because ライブラリが公開する型と API が増え、互換性を保つ対象が増える。プロパティは GPUI の API に合わせて小さく保つ。
* Neutral, because 新しいコントロールは FluentTheme にテーマがないので、FluentTheme の上に重ねたときも `GpuiTheme` の見た目になる。
