---
number: 33
title: Replace the Tabalonia theme with drag, close, add and detach on the standard tabs
status: accepted
date: 2026-10-05
links:
- target: 28
  kind: amends
- target: 29
  kind: amends
- target: 30
  kind: amends
- target: 34
  kind: amendedby
---

# Replace the Tabalonia theme with drag, close, add and detach on the standard tabs

## Context and Problem Statement

ADR 28 で、Tabalonia 12.0.0 の `TabsControl` に GPUI Kit の見た目を付けるパッケージ `AvaloniaUIKit.Tabalonia` を作った。タブを閉じる・追加する・ドラッグで並べ替える操作と、ウィンドウへの切り離し（Tabalonia 12.0.0 の #29）を、本体の TabStrip / TabControl と同じ見た目で使うためだった。

ドキュメントサイトの Tabalonia のデモで、タブが消えたり別のタブバーへ移ったりする不具合が見つかった。原因は Tabalonia の設計にあり、テーマでは直せない。

* **全インスタンスを static なリストで共有する。** `TabsControl` はプロセス内のすべてのインスタンスを登録し、タブを離した点や、バーから 32px 以上外へ出した点の「スクリーン座標」の下にある別の `TabsControl` へタブを移す（`EnableTabAttaching` は既定で true）。同じ画面に並べた別のタブバーへも移る。
* **スクリーン座標が TopLevel をまたいで比べられる前提。** Avalonia.Browser の `PointToScreen` は恒等変換で、各 `AvaloniaView` の座標はそのビューの中の座標になる。1 つのランタイムに複数のビューを載せるサイト（ADR 20）では、別のデモのタブバーが同じ座標にあり、普通の並べ替えでもタブが画面外のデモへ移る。
* **TopLevel をまたいでコントロールを移すと例外が出る。** タブそのもの（`DragTabItem`）を別の TopLevel へ移すと、レイアウト中に `Attempt to call InvalidateArrange on wrong LayoutManager` が出て、ドラッグの状態（`IsDragging`、ポインターのキャプチャ）が残る。以降のクリックでも別のタブが動く。

ほかにも、Tabalonia はすべてのタブを `TabItemWidth` の 1 つの幅に並べるので GPUI Kit の文字の幅のタブと一致しない（ADR 28 の Bad）。版を固定してテンプレートの約束を追う手間もあり、テーマの生成も本体と Tabalonia の 2 系統になっている。`AvaloniaUIKit.Tabalonia` はまだ公開していない（タグも publish の workflow もない）。

## Decision Drivers

* ページに複数のビューを載せるブラウザーでも、関係のないタブバーの間でタブが動かないこと。
* Tabalonia で使えた機能（閉じる、追加、並べ替え、ウィンドウへの切り離しと別のタブバーへの移動）を失わないこと。
* GPUI Kit の TabBar の見た目（文字の幅のタブ、閉じるボタンを suffix に置いた tabs story の closable）に一致させること。
* ADR 30 の形（既存のコントロールに足せる機能は添付プロパティ、Avalonia と同じ名前の型を作らない）と NativeAOT（ADR 11）を守ること。

## Considered Options

* 標準の TabStrip / TabControl に添付プロパティとして足し、Tabalonia のパッケージを廃止する
* 新しいコントロール（`uikit:TabBar`）を作り、Tabalonia のパッケージを廃止する
* Tabalonia のパッケージを残し、デモと文書で `EnableTabAttaching="False"` を勧める
* Tabalonia のパッケージを廃止し、閉じる・並べ替えなどは作らない

## Decision Outcome

Chosen option: "標準の TabStrip / TabControl に添付プロパティとして足し、Tabalonia のパッケージを廃止する", because 本体の TabStrip / TabControl がすでに GPUI Kit の TabBar の見た目（5 つの variant、4 つのサイズ、インジケーター、スクロール、メニュー、prefix と suffix）を持ち、足りないのはタブの操作だけだから。操作を足すだけなら ADR 30 の添付プロパティの形に収まり、タブを移す範囲もアプリが決められる。

* **API（`uikit:Tabs`、TabStrip と TabControl に付ける）:**
  * `Closable`（bool。継承するので、バーに付けると全タブ、タブに付けるとそのタブ）: タブの suffix に閉じるボタン（GPUI の tabs story の closable。ghost xsmall の Close、タブは px_2）を出す。押すと `TabClosing`（routed event、`Cancel` で止められる）を出し、止められなければ項目を一覧から除く。選択中のタブを閉じたら隣（右、なければ左）を選ぶ。
  * `NewTabFactory`（`Func<object?>`）: 最後のタブの後ろに追加ボタン（ghost xsmall の Plus。GPUI にはない）を出し、押すと返した項目を末尾に足して選ぶ。
  * `Reorderable`（bool）: タブをバーに沿ってドラッグして並べ替える。ドラッグ中のタブはポインターに付いていき、ほかのタブは 200ms で場所を空ける（Tabalonia と同じ見せ方）。離したときに一覧の順番を変える。インジケーターはドラッグ中のタブにばねなしで付いていく。タブがあふれたバーでは、端で自動でスクロールする。
  * `DragGroup`（string）: 同じグループのバーの間で、ドラッグしたタブを移す。タブをほかのバーの上へ持っていくと、そのバーへ移り、そのままドラッグを続けられる。
  * `DetachedWindowFactory`（`Func<object?, Window?>`）: `DragGroup` のあるバーから、どのバーからも離れた所（今のバーから 32px 以上）へタブを出すと、ファクトリーが返したウィンドウへ移す。ウィンドウは同じグループのバー（空の TabStrip か TabControl）を持つこと。ウィンドウはドラッグの間ポインターに付いていき、ほかのバーの上へ戻せばそのバーへ移る。
* **タブを移す範囲:** 同じ `DragGroup` のバーの間だけ。グループを付けないバーは、ほかのバーと関わらない。別の TopLevel のバーへ移すのは、両方が `Window` のとき（デスクトップ）だけにする。ブラウザーのビューの間ではスクリーン座標を比べられないため。同じ TopLevel の中なら、ブラウザーでもグループのバーの間で移せる。
* **ウィンドウ:** 切り離しは、バーの TopLevel が `Window` で、ファクトリーがあるときだけ。ファクトリーが作ったウィンドウは、最後のタブがドラッグで出ていくか閉じられたら閉じる。そのウィンドウのタブが 1 つだけのときにドラッグすると、新しいウィンドウを作らずにウィンドウごと動かす。アプリが自分で開いたウィンドウは閉じない。
* **一覧:** 操作は `ItemsSource`（変更できる `IList`）か `Items` を変える。固定長や読み取り専用の一覧では、閉じる・並べ替え・移動をしない。別のバーの一覧が項目の型を受け付けなければ移さない。
* **テンプレート:** TabStripItem と TabItem のテンプレートに閉じるボタン（`PART_CloseButton`）を、バーのタブの行の末尾に追加ボタン（`PART_AddButton`）を置く（`scripts/gen_tabs_theme.py`）。タブの行は、タブ、追加ボタン、末尾の余白（`last_empty_space`）の順に並べる。追加ボタンがないときは、今までどおりタブが行を埋める（GPUI の flex_1 のタブ。ColorSelect のポップオーバーの segmented が使う）。
* **廃止するもの:** `AvaloniaUIKit.Tabalonia`（`UIKitTabaloniaTheme`、`DragTabs`）と Tabalonia への依存、サイトの Tabalonia のページ（Tabs のページへ転送する）、`cases/tabalonia.toml` と参照データ。
* **範囲外:** キーボードでの並べ替え、GPUI Kit の Dock の TabPanel が描く挿入線とドラッグのプレビュー（Dock.Avalonia のテーマで扱う）、中ボタンのクリックで閉じる操作、Wayland でのウィンドウの追従（Wayland はアプリがウィンドウの位置を決められない）。
* **ADR 28、29、30 との関係:** ADR 28 の対象から Tabalonia を外す（Dock.Avalonia はそのまま）。ADR 29 は Dock の部品（`DockSplitters`、`DockTargets`、`DockConverters`）にだけ当てはまる。ADR 30 の `uikit:Tabs` に、タブの操作の添付プロパティを加える。

### Consequences

* Good, because Tabalonia の static なリストとスクリーン座標の前提がなくなり、サイトのデモのように 1 つのページに複数のタブバーを置いても、グループの外へタブが動かない。
* Good, because タブが文字の幅になり、GPUI Kit の TabBar と一致する。ADR 28 で諦めた差が消える。
* Good, because 第三者のライブラリの版を追う作業がなくなり、タブのテーマの生成も 1 系統になる。
* Bad, because ドラッグ、ウィンドウへの切り離し、別のバーへの移動を自分で保守する。GPUI Kit にこの操作の参照データはなく、挙動テストだけで確かめる。
* Bad, because headless のウィンドウはどれも原点に開くので、ウィンドウをまたぐ操作のテストは、バーをウィンドウの中の別の位置に置いて区別する形になる。
* Neutral, because Tabalonia を直接使いたいアプリは、上流のテーマを使うことになる（このライブラリはテーマを出さない）。

### Confirmation

* `cases/tabs.toml` に closable のケース（5 つの variant × 4 つのサイズ、閉じるボタンの hover と押下）を足し、GPUI Kit の描画と比べる。
* 挙動テストで、閉じる（`TabClosing` の取り消し、隣の選択）、追加、並べ替え（`Items` と `ItemsSource`、ドラッグ中のインジケーター、あふれたバーの自動スクロール）、同じグループのバーの間の移動、グループの違うバーや別のビューへ移らないこと、ウィンドウへの切り離しと戻し、ファクトリーのウィンドウが空になったら閉じることを確かめる。
* NativeAOT のギャラリー（`samples/AvaloniaUIKit.AotSmoke`）で、Tabalonia の代わりに TabControl の操作を使い、警告なしで publish できることを確かめる。

## Pros and Cons of the Options

### 標準の TabStrip / TabControl に添付プロパティとして足し、Tabalonia のパッケージを廃止する

* Good, because アプリは今の TabStrip / TabControl に添付プロパティを足すだけで使え、見た目のテーマを二重に持たない。
* Good, because タブを移す範囲（グループ、TopLevel の種類）とウィンドウの作り方をアプリが決められる。
* Bad, because 標準のコントロールの処理（選択、`AutoScrollToSelectedItem`、コンテナーの生成）の上に作るので、その順序に依存する箇所が出る。挙動テストで守る。

### 新しいコントロール（`uikit:TabBar`）を作り、Tabalonia のパッケージを廃止する

* Good, because ドラッグのための配置を専用のパネルで持てる。
* Bad, because TabStrip / TabControl と同じ役割の型が増え、見た目のテーマも重なる（ADR 30 で避けた形）。
* Bad, because アプリはタブを操作したいだけでコントロールを差し替えることになる。

### Tabalonia のパッケージを残し、デモと文書で `EnableTabAttaching="False"` を勧める

* Good, because 今回のサイトの不具合は、属性を 1 つ足すだけで止まる。
* Bad, because タブをほかのバーやウィンドウへ移す機能を使うと、同じ不具合（関係のないバーへの移動、TopLevel をまたいだ例外）が起きうる。
* Bad, because 文字の幅のタブの差と、版を追う作業が残る。

### Tabalonia のパッケージを廃止し、閉じる・並べ替えなどは作らない

* Good, because 保守するものが最も少ない。
* Bad, because タブを閉じる・並べ替える・切り離すアプリの手段がなくなる。

## More Information

* 第三者のライブラリのテーマは ADR 28、そのパッケージに足すコントロールは ADR 29、添付プロパティと新しいコントロールは ADR 30、ブラウザーのデモは ADR 20。
* Tabalonia の不具合は v12.0.0 の `TabsControl`（`RegisteredTabsControls`、`TryFindDropTarget`、`MoveItemToAnotherTabsControl`）と、Avalonia.Browser の `BrowserTopLevelImpl.PointToScreen` で確かめた。
