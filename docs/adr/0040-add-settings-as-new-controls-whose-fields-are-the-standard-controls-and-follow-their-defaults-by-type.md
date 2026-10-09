---
number: 40
title: Add Settings as new controls whose fields are the standard controls and follow their defaults by type
status: accepted
date: 2026-10-09
links:
- target: 19
  kind: amends
- target: 30
  kind: amends
---

# Add Settings as new controls whose fields are the standard controls and follow their defaults by type

## Context and Problem Statement

GPUI Kit の Settings（`crates/component/src/setting`）は、検索欄付きのサイドバーとページを並べた設定画面で、`Settings` → `SettingPage` → `SettingGroup`（GroupBox の見た目）→ `SettingItem` の 4 層からなる。項目のフィールドは `SettingField` が作り（switch、checkbox、input、dropdown、number_input と任意の要素）、値の読み書きの関数と `default_value` を持つ。ページは、表示中の項目の値が既定と違う間だけリセットボタンを出し、押すと既定に戻す。

ADR 19 は、設定画面の構成・フィールド生成・reset 管理を持つ標準型がないとして、Settings を範囲外にした。2026-10-09 に、Settings もアプリで使えるようにしたいという要望があった。ADR 30 の後には、作るための部品（Sidebar の面と SidebarMenu、ResizablePanelGroup、GroupBox の見た目）がそろっている。決めることは、Avalonia でフィールドと既定値・リセットをどう表すかである。

## Decision Drivers

* GPUI Kit の Settings の見た目と操作（検索と選択の規則、group へのスクロール、リセット、狭いページでの縦並び）をそのまま使えるようにする。
* ADR 30 の方針どおり、既存のコントロールと同じ役割の型を重ねて作らない。
* NativeAOT で動き、リフレクションを使わない（ADR 11）。値をプロパティ名で探さない。
* アプリの既存のバインディング（`IsChecked="{Binding DarkMode}"` など）をそのまま使える。

## Considered Options

* 項目の中身に標準のコントロールを置き、`DefaultValue` があれば既知の型の値を型ごとの分岐で追う
* GPUI の `SettingField` に合わせたフィールドのコントロール（SwitchField、CheckboxField、InputField、DropdownField、NumberField）を作る
* 範囲外のままにする（ADR 19）

## Decision Outcome

Chosen option: "項目の中身に標準のコントロールを置き、`DefaultValue` があれば既知の型の値を型ごとの分岐で追う", because アプリは ToggleSwitch や TextBox を今の書き方とバインディングのまま項目に入れられ、標準のコントロールを包む型を増やさずに GPUI のリセットの仕組みを再現できるから。2026-10-09 にユーザーがこの形を選んだ。

* **コントロール:** `uikit:Settings`、`uikit:SettingPage`、`uikit:SettingGroup`、`uikit:SettingItem`（ADR 19 の置き場所と名前の決まりに従う。GPUI と同じ名前で、Avalonia の型とぶつからない）。`Settings` はページの、`SettingPage` は group の、`SettingGroup` は項目の `ItemsControl`。`SettingItem` は `ContentControl` で、中身がフィールド。
* **見た目:** サイドバーは Sidebar の面（色、余白、`SidebarMenu`）をテンプレートで組み、GPUI の `w(relative(1.)).border_0()` のとおり枠なしで全幅にする。`uikit:Sidebar` は使わない（幅と枠を外から変える口がないため）。ページとサイドバーの境は `ResizablePanelGroup`。group の面は GroupBox のテーマと同じ値を自分のテンプレートに持つ（GPUI は Settings の group にだけ `gap_4` を付け、variant によらずタイトルと面の間が 16px になるため、GroupBox をそのまま使えない）。
* **既定値とリセット:** `SettingItem.DefaultValue` を書くと、中身が ToggleButton（ToggleSwitch、CheckBox）、TextBox、NumericUpDown、RangeBase（Slider）、SelectingItemsControl（ComboBox、ListBox）、`uikit:Select` のときは値を追い、既定と違う間 `IsModified` を立てる。既定は値そのものか、XAML の文字列を型に変換したもの（ComboBoxItem の中身の文字列も比べる）。リセットは `SetCurrentValue` で既定を戻すので、双方向のバインディングを通って元の値も変わる。それ以外の中身はアプリが `IsModified` を設定し、`Reset` イベントか `ResetCommand` で戻す（GPUI の `on_reset`）。
* **GPUI の API との対応:** `Settings::with_group_variant` は継承する `Settings.GroupVariant`、`SettingGroup::variant` は `SettingGroup.Variant`、`default_open` は `SettingPage.IsOpen`、`default_selected_index` は `SelectedIndex` と `SelectedGroup`、`with_size` は Settings のサイズクラス（各項目の中身に付ける）、`layout(Axis)` は `SettingItem.Orientation`、`keywords` はカンマ区切りの `Keywords`。タイトルのない項目が GPUI の `SettingItem::render`。
* **範囲外:** GPUI の dropdown フィールドの見た目（outline のボタンとチェック付きメニュー）を自動では作らない。同じ見た目は `DropDownButton.outline` と MenuFlyout で作れるが、値は追わないので、値を追うなら ComboBox を使う。説明文の Markdown は TextBlock の Inlines で代える。

### Consequences

* Good, because アプリは ToggleSwitch、TextBox などを、いま書いているバインディングのまま設定画面に入れられる。
* Good, because 標準のコントロールを包む型を 5 つ作らずに済み、フィールドの見た目は各コントロールのテーマのまま GPUI と一致する。
* Bad, because 値を追う型は固定の一覧で、それ以外（DatePicker、ColorPicker など）は `IsModified` と `Reset` をアプリが書く。
* Bad, because ページ・group・項目の検索と選択の状態を `Settings` が持つので、公開する型と API が増える（ADR 19、30 と同じ）。

### Confirmation

* `cases/settings.toml` と `reference/src/cases/settings.rs` で GPUI Kit が描いた設定画面（3 つの variant、メニューの hover とクリック、group へのスクロール、検索、サイズ、リセット、無効、group の variant、About ページ、狭いページ）と画素・構造を比べる（`SettingsTests`）。
* 検索と選択の規則、group へのスクロールは、GPUI Kit の `setting/tests.rs` と同じ例を挙動テストで確かめる。既定値の追従とリセット、サイズクラス、狭いページ、IsOpen の同期も挙動テストにする（`SettingsBehaviorTests`）。
* NativeAOT のギャラリー（`samples/AvaloniaUIKit.AotSmoke`）に入れ、警告なしで publish できることを確かめる。

## Pros and Cons of the Options

### 項目の中身に標準のコントロールを置き、`DefaultValue` があれば既知の型の値を型ごとの分岐で追う

* Good, because Avalonia の書き方（中身に置いたコントロールとバインディング）のまま使える。
* Good, because 型ごとの分岐（`switch` の型パターン）と `AvaloniaProperty` の読み書きだけで、リフレクションを使わない。
* Bad, because 既定値の変換（XAML の文字列から bool、decimal、double）と比べ方を、型ごとに持つ必要がある。

### GPUI の `SettingField` に合わせたフィールドのコントロール（SwitchField、CheckboxField、InputField、DropdownField、NumberField）を作る

* Good, because GPUI の API に近く、dropdown フィールドの見た目も自動で出る。
* Bad, because ToggleSwitch や TextBox を包む型が 5 つ増え、ADR 30 の「既存のコントロールと同じ役割の型を重ねて作らない」に反する。
* Bad, because アプリは標準のコントロールのプロパティ（Watermark、Minimum など）を包んだ型に移し替えることになる。

### 範囲外のままにする（ADR 19）

* Good, because 公開する API が増えない。
* Bad, because アプリが Sidebar、ResizablePanelGroup、GroupBox から設定画面を組み、検索と選択の規則を自分で書くことになる。

## More Information

* GPUI Kit との対応と差は `docs/references/compatibility-list.md` の Settings の行、使い方はサイトの Settings のページ（`sites/content/components/settings.mdx`）に書く。
* 小さなコンポーネントの新規実装は ADR 19、機能を足す新しいコントロールの方針は ADR 30。
