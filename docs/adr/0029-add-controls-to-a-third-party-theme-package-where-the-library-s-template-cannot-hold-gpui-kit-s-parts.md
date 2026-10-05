---
number: 29
title: Add controls to a third-party theme package where the library's template cannot hold GPUI Kit's parts
status: accepted
date: 2026-10-04
links:
- target: 15
  kind: amends
- target: 30
  kind: amendedby
---

# Add controls to a third-party theme package where the library's template cannot hold GPUI Kit's parts

## Context and Problem Statement

ADR 15 は見た目だけを変えるコード（Converter、Behavior）を許し、新規コントロールと、選択など論理的な状態を変える処理を禁じている。ADR 19 は小さなコンポーネントに限ってコントロールを足した。第三者のライブラリのテーマ（ADR 28）では、ライブラリのテンプレートの約束が GPUI Kit の部品を置ける形になっていないことがある。

* Tabalonia の `TabsControl` は、テンプレートの `TopPanel` が 6 つの部品（`PART_LeftContent`、`PART_LeftDragWindowThumb`、`PART_ItemsPresenter`、`PART_AddItemButton`、`PART_RightDragWindowThumb`、`PART_RightContent`）を名前で探して横に並べる。ほかの部品は置けず、`PART_ItemsPresenter` を `ScrollViewer` に包むこともできない。
* GPUI Kit の TabBar には、あふれたタブを横にスクロールする動き（`overflow_x_scroll`）と、タブの一覧から選ぶメニュー（`menu(true)`）がある。メニューは選択を変えるので、ADR 15 の Behavior では作れない。

計画の段階では、ライブラリのコントロールを包むラッパーのコントロール（アプリがラッパーを使う）を作ることにしていた。

## Decision Drivers

* GPUI Kit の Tabs のページの機能（スクロール、メニュー）をできるだけ再現する。
* アプリは上流の README のとおりにライブラリのコントロールを書き、テーマを足すだけで済むようにする（ADR 15 の使い方）。
* ライブラリが「見た目の部品集」であることは保つ（ADR 19）。

## Considered Options

* テーマのパッケージに、テンプレートの部品として使うコントロールを足す
* ライブラリのコントロールを包むラッパーのコントロールを作る
* 対応しない

## Decision Outcome

Chosen option: "テーマのパッケージに、テンプレートの部品として使うコントロールを足す", because アプリはライブラリのコントロールをそのまま使えて、足したコントロールはテーマのテンプレートの中に閉じるから。

* **スクロールはテンプレートで済む。** `TopPanel` が求めるのは 6 つの名前の子だけなので、`TopPanel` ごとテンプレートの `ScrollViewer` に入れられる。prefix と suffix（`LeftContent` / `RightContent`）はスクロールの外の別の `ContentPresenter` に出し、`TopPanel` の中の 2 つの部品は空の `Panel` にする。これで、スクロールのためのラッパーは要らなくなった。
* **対象:** ライブラリのテンプレートの約束では GPUI Kit の部品を置けず、見た目だけのコードでも作れないもの。最初の例は `TabsMenuButton`（GPUI の `menu(true)`）。Button の派生で、Button のテーマを使う。テンプレートが `TabsControl` の `menu` クラスで表示する。
* **置き場所:** そのライブラリのテーマのパッケージ（名前空間 `AvaloniaUIKit`）。ライブラリの型に依存する見た目だけのコード（ADR 15 の範囲）も同じパッケージに置く。Tabalonia の `DragTabs.FollowsDrag`、Dock の `DockSplitters.Straddles`（分割線を境界の上に重ね、向きをクラスで渡す）、`DockTargets.MarksTab`（ドロップ先のタブにクラスを付ける）、`DockConverters`。
* **テンプレートの構造で済むものはコードにしない:** Dock のドロップ先の決め方（GPUI の 35% / 65% の領域）は、Dock の十字のセレクターを、領域を覆う透明な要素に置き換えるだけで再現できた。
* **処理の範囲:** 足したコントロールは、ライブラリのコントロールが持つ操作（選択、閉じる、追加）を呼ぶだけで、データは持たない。
* **ラッパー:** アプリが書くコントロールを変えるラッパーは、テンプレートの中ではどうしても作れない場合に限って作る。

### Consequences

* Good, because アプリの XAML は上流の README のままで、テーマを足すだけで GPUI Kit の部品が付く。
* Good, because スクロールとメニューを、ライブラリの機能（並べ替え、閉じる、追加）と一緒に使える。
* Bad, because 公開する型が増え、互換性を保つ対象が増える。
* Neutral, because ラッパーの案は、スクロールをテンプレートで作れたので使わなかった。

### Confirmation

* `cases/tabalonia.toml` の menu のケース（閉じた状態と開いた状態）を GPUI Kit の描画と比べる。
* `TabaloniaBehaviorTests` で、メニューがタブを一覧して選ぶこと、あふれたタブがスクロールして選んだタブが見えること、ドラッグ中のインジケーターがタブと一緒に動くことを確かめる。

## Pros and Cons of the Options

### ライブラリのコントロールを包むラッパーのコントロールを作る

* Good, because ライブラリのテンプレートの約束に縛られずに部品を置ける。
* Bad, because アプリはライブラリのコントロールの代わりにラッパーを書くことになり、上流の README やサンプルと書き方が変わる。
* Bad, because ラッパーとライブラリの両方に同じ役割のプロパティ（prefix と `LeftContent` など）ができる。

### 対応しない

* Bad, because GPUI Kit の TabBar のメニューが使えない。

## More Information

* 見た目だけのコードの範囲は ADR 15、小さなコントロールは ADR 19、第三者のライブラリのテーマは ADR 28。
