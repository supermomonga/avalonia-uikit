---
number: 17
title: Hand a control's look to code-built parts through inherited values
status: accepted
date: 2026-10-04
---

# Hand a control's look to code-built parts through inherited values

## Context and Problem Statement

GPUI Kit では、親の大きさや種類（サイズ、Table か DataTable か、通知が下から来るか）が子の描き方を決める。Avalonia では、その子の一部がコードで作られ、親のテンプレートの部品ではない。

* `TableView` の行・セル・列見出しは、それぞれのコントロールや presenter がコードで作る。セルと見出しのテーマは型ごとに 1 つしかなく、Table と DataTable の両方の見た目に使われる。
* `NotificationCard` は `WindowNotificationManager` の項目で、出てくる向き（上か下か）は親の配置で決まる。
* ControlTheme の入れ子のスタイルは `/template/` を 1 段しか書けず、親のテーマから子の部品には届かない。グローバルなスタイルは ControlTheme より優先されるため、状態の色と組み合わせると順序が崩れる。

## Considered Options

* 親のテーマが継承される添付プロパティに値を置き、子のテーマが読む
* グローバルな子孫セレクター（`TableView.small TableViewCell` 等）で子に直接設定する
* 子のテーマを見た目ごとに用意し、アプリに切り替えさせる

## Decision Outcome

Chosen option: "親のテーマが継承される添付プロパティに値を置き、子のテーマが読む", because 1 つの子のテーマでどの親の見た目にも合わせられ、アプリは親にクラスを付けるだけで済むから。

* 値を運ぶだけの添付プロパティ（ADR 15 の範囲内）:
  * `Tables.CellPadding`、`CellVerticalAlignment`、`RowHeight`、`ShowsResizeHandles`（TableView と DataGrid）
  * `Notifications.FromBottom`（通知の入退場の向き）
  * `TextLines.RoundsWidthUp`（`:is(TopLevel)` から全体に継承）
* 子のテーマは `{Binding $self.(gpui:Tables.CellPadding)}` や `{TemplateBinding (gpui:Tables.RowHeight)}` で読む。
* 状態の色は、テンプレートの部品（`Border#Border` など）に置く。行そのものの `Background` はグローバルなスタイル（縞模様）に残し、優先順位がぶつからないようにする。

### Consequences

* Good, because 子のテーマが 1 つで済み、アプリのテーマ切り替え（`Theme="{StaticResource GpuiTable}"`）もそのまま子に効く。
* Good, because 別アセンブリのテーマ（ADR 16）も同じ値を読める。
* Bad, because 値の出どころが親のテーマにあり、子のテーマだけを読んでも見た目が決まらない。各テーマの冒頭のコメントで出どころを示す。
