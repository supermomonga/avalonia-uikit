---
number: 21
title: Name the themes after Nova and prefix resources with UIKit
status: accepted
date: 2026-10-03
links:
- target: 2
  kind: amends
- target: 6
  kind: amends
- target: 7
  kind: amends
- target: 13
  kind: amends
- target: 16
  kind: amends
---

# Name the themes after Nova and prefix resources with UIKit

## Context and Problem Statement

公開 API の名前に移植元の GPUI が入っていた。テーマ `GpuiTheme`、`GpuiColorPickerTheme`、`GpuiDataGridTheme`、リソースキー `Gpui.*`（色トークン、アイコン、状態ごとの色）、名前付きテーマのキー `GpuiSpinner`、`GpuiTable` など、ドキュメントとサンプルの XAML 名前空間の接頭辞 `gpui:` である。利用者が書くのは Avalonia のコードで、GPUI ではない。GPUI Kit の見た目は shadcn/ui の Nova スタイルを元にしている。テーマの名前は Nova に決まった。残る問題は、リソースキーと名前付きテーマのキーに付ける接頭辞である。

## Considered Options

* `UIKit`（`UIKit.Icon.Search`、`UIKitSpinner`）
* `Nova`（`Nova.Icon.Search`、`NovaSpinner`）
* 接頭辞なし（`Icon.Search`、`Spinner`）

## Decision Outcome

Chosen option: "`UIKit`", because ライブラリ名と XAML の接頭辞 `uikit:` にそろい、テーマに依存しないから。アイコンのようにテーマと関係のないキーにもテーマ名が付く、ということがない。別のテーマを足しても同じキーを使える。

* テーマは `NovaTheme`、`NovaColorPickerTheme`、`NovaDataGridTheme`（ファイル名も同じ）。
* リソースキーは `UIKit.*`（`UIKit.Primary`、`UIKit.Icon.Search`、`UIKit.Button.Primary.Hover.Background` など）。生成器（`reference/src/tokens.rs`、`reference/src/icons.rs`、`scripts/gen_*.py`）もこの接頭辞で書き出す。
* 名前付きテーマなど、キーで参照するリソースは `UIKit<名前>`（`UIKitSpinner`、`UIKitProgressCircle`、`UIKitCollapsible`、`UIKitTable` など）。テンプレート内部のスタイルクラスは `uikit-calendar-weekday`。
* XAML の名前空間の接頭辞は `uikit`（`xmlns:uikit="using:AvaloniaUIKit"`）。テーマの XAML、サンプル、ドキュメントでそろえる。
* `GpuiColorConverters` は `ColorPickerConverters` にする。
* 比較の相手としての GPUI Kit の名前は残す。参照データ（`goldens/gpui-2c5162f`）、その生成器（`reference/`）、テストで参照データを読む部分、コメントの出典（`popup_menu.rs` など）がそれにあたる。
* 旧名の別名は用意しない。パッケージは NuGet に未公開で、旧名に依存する利用者がいないから。

ADR 2、6、7、13、16 の決定はそのまま有効で、名前だけをこの ADR で置き換える。ADR 5、9、17、19 の本文に出てくる旧名も、新しい名前に読み替える。

### Consequences

* Good, because API の名前を GPUI を知らなくても読める。
* Good, because 色とアイコンのキーがテーマ名を含まないので、同じキーを定義する別のテーマに替えても、アプリの XAML はそのまま使える。
* Bad, because 既存の ADR の本文は旧名のままなので、読むときに読み替えが要る。
