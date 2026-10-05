---
number: 30
title: Build the GPUI Kit features Avalonia's controls lack as attached properties and new controls
status: accepted
date: 2026-10-04
links:
- target: 15
  kind: amends
- target: 19
  kind: amends
- target: 29
  kind: amends
---

# Build the GPUI Kit features Avalonia's controls lack as attached properties and new controls

## Context and Problem Statement

対応表の「部分対応」の行は、既存のコントロールにテーマを当て、そのコントロールが持たない機能を対象外にしている（ADR 15、ADR 19）。2026-10-05 に、GPUI Kit `2c5162f` と Avalonia 12.1.3 のソースで各行の機能差を洗い直した。差は 3 種類に分かれた。

* **テーマで直せるもの。** Avalonia が持つ機能を、このリポジトリのテンプレートが落としている（ComboBox の `IsEditable`、NumericUpDown の `InnerLeftContent` / `InnerRightContent`、TextBox の右クリックメニューとクリアボタン、ComboBox のクリアボタン、DropDownButton のテーマ、TableView と DataGrid の空の表示）。これはテーマの修正で、この ADR の対象ではない。
* **既存のコントロールに機能がないもの。** Button の loading、TextBox の検証と Esc でのクリア、Select のポップアップ内の検索と複数選択、List のセクション、Tree の仮想化、Calendar の複数月の表示、Notification の重なり、Resizable の連鎖リサイズなど。
* **作っても解消しないもの。** テキスト編集エンジンや OS の機能が要るもの（インライントークン、複数カーソル、自動入力ヒント、OS の通知）と、Avalonia に keymap がないことによるもの（Action からのショートカットの解決）。

2 つ目の種類は、ADR 15（コントロールの論理的な状態や操作を変える処理を作らない）と ADR 19（新しいコントロールは見た目が中心で小さなものに限る）により、作らないことになっていた。これを作れるようにしたい。

## Decision Drivers

* GPUI Kit のコンポーネントの機能を、Avalonia でもできるだけそのまま使えるようにする。
* 今の使い方（標準のコントロールにテーマを当てるだけ）は変えない。機能はアプリが選んだときだけ加わる。
* NativeAOT で動き、リフレクションを使わない（ADR 11）。参照データとの比較で検証する（ADR 10）。
* ライブラリが公開する型と API は、GPUI Kit の名前と形にそろえ、必要以上に増やさない。

## Considered Options

* 既存のコントロールに足せる機能は添付プロパティにし、構成や操作が違うものだけを新しいコントロールにする
* すべてを新しいコントロールにする（既存のコントロールの派生を含む）
* 今の範囲（ADR 15、ADR 19）のままにする

## Decision Outcome

Chosen option: "既存のコントロールに足せる機能は添付プロパティにし、構成や操作が違うものだけを新しいコントロールにする", because アプリは今の XAML のまま必要な機能だけを足せて、既存のコントロールと同じ役割の型を重ねて作らずに済むから。

* **形の選び方:**
  * **既存のコントロールにそのまま足せる機能**は、そのコントロールに付ける添付プロパティにする。アプリが設定したときだけ挙動が変わる。例: Button の loading とクリックでフォーカスを奪わない動作（`uikit:Buttons`）、TextBox の検証・数値のマスク・Esc でのクリア・選択行のインデント（`uikit:Inputs`）。
  * **テンプレートに差し込み口を足すだけのもの**は、添付プロパティと、それを置くテーマのテンプレートにする。例: GroupBox の footer、Separator の label、Spinner のアイコン、ProgressCircle の中央の内容、TabStrip / TabControl の prefix と suffix。
  * **値の扱いだけを変えれば足りるもの**は、既存のコントロールの派生にし、`StyleKeyOverride` で既存のテーマを使う。例: NumberInput（`NumericUpDown` の派生。入力中の文字の制限と正規化、値によって変わる刻み）。
  * **構成や操作が既存のコントロールと違うもの**は、新しいコントロールにする。
* **新しいコントロール（この ADR で作るもの）:** ButtonGroup、ToggleGroup、Accordion、Toolbar、InputGroup、RangeSlider、Pagination、Carousel の前後のボタン、TextLabel、AsyncImage、Icon、Select、ListView、Tree、Table、CalendarView、DateField、TimeField、ColorSelect、Sidebar、Sheet、ResizablePanelGroup、NotificationList、TitleBar と、その子の部品。
* **名前:** GPUI Kit に合わせる。Avalonia の型と同じ名前、または大文字小文字だけが違う名前になるものは変える（ADR 19 と同じ）。
  * Calendar → `CalendarView`、DatePicker → `DateField`、Label → `TextLabel`、`img()` → `AsyncImage`、List → `ListView`、Slider の範囲 → `RangeSlider`、Resizable → `ResizablePanelGroup` / `ResizablePanel`。
  * Combobox は `ComboBox` と大文字小文字しか違わないので、別の型にしない。`Select` の `combobox` クラス（トリガーは確定した選択だけを表示する）にし、検索欄・複数選択・footer・トリガーのテンプレートを `Select` のプロパティにする。
  * ColorPicker（スウォッチ）と ColorSelect（欄）は 1 つの `ColorSelect` にし、欄の見た目は `field` クラスにする（今の ColorPicker のテーマと同じ分け方）。`ColorSelect` は本体に置き、`Avalonia.Controls.ColorPicker` に依存しない。
  * 通知は Avalonia の `Notification` と同じ名前にせず `NotificationItem`（`NotificationCard` の派生）、GPUI の Anchor は意味が広すぎるので `NotificationPlacement` にする。
  * Icon は `Kind` で形を選ぶ `Icon`（PathIcon の派生）と、生成する列挙 `IconName`（GPUI Kit の IconName の 106 個）にする。
  * 単一値の対数スライダーも `RangeSlider`（`IsRange="False"`）にする。Avalonia の `Slider` は値と位置の対応が線形で、添付プロパティで対数にすると `Value` の意味が変わるため。
  * Sheet は XAML に置かず、`Show(visual)` でウィンドウの OverlayLayer に開く（GPUI の `open_sheet_at`）。
  * Carousel のトラックは、添付プロパティ `Carousels.TracksPointer` がテーマの ItemsPanel を `CarouselTrack` にする形にし、前後のボタンは `CarouselPrevious` / `CarouselNext` にする。
  * 差し込み口は `GroupBoxes.Footer`、`Separators.Label`、`Spinners.Icon`、`ProgressCircles.Content`、`Tabs.Prefix` / `Suffix`。
* **データの受け取り方:** リフレクションを使えないので、項目の文字列・キー・子の一覧は、関数（`Func<object?, string>` など）か、コンパイル済みのバインディングとテンプレート（`ITreeDataTemplate.ItemsSelector` など）で受け取る。名前でプロパティを探すことはしない。
* **既存のテーマとの関係:** 標準のコントロールのテーマはそのまま残し、新しいコントロールは追加にする。新しいコントロールの中の Button、TextBox、ListBox、Popup などは、テーマ済みの標準のコントロールを使う。
* **置き場所:** コントロールと、挙動を変える添付プロパティは `src/AvaloniaUIKit/Controls/` に置く。`src/AvaloniaUIKit/Behaviors/` は見た目だけを変えるコード（ADR 15）のままで、値を置くだけの差し込み口の添付プロパティもここに置く。
  * ADR 29 で Tabalonia のパッケージに足した `TabsMenuButton` は、本体の TabStrip / TabControl の `menu` クラスも使うので、本体（`src/AvaloniaUIKit/Controls/`、名前空間は同じ `AvaloniaUIKit`）に移す。ADR 29 の置き場所のこの例を改める。
* **既定の挙動:** 挙動を変える添付プロパティは、アプリが設定したときだけ効く。GPUI と違う Avalonia の既定（ボタンの押下でフォーカスを移すなど）は変えず、GPUI の動作は `Buttons.TakesFocusOnPointer="False"` のようにアプリが選ぶ。新しいコントロールが Avalonia の約束と GPUI で迷うところは Avalonia に合わせる（無効な行はクリックでも選べない、`SelectionChanged` はコードからの変更でも出す）。差は対応表の各行に書く。
* **共有する処理:** 項目を指定の位置までスクロールする `ItemsScrolling.ScrollToItem`（`ScrollStrategy`）は、ListView、Tree と任意の `ItemsControl`（VirtualList の ListBox）で共有する。GPUI の単精度の HSL の計算（`ColorConverters`）は ColorSelect と ColorPicker のパッケージで共有する。
* **テーマの修正との関係:** Context の 1 つ目の種類は同じ時期にテーマで直した。GPUI の cleanable は Fluent と同じクラス名 `clearButton`（TextBox、ComboBox）、右クリックメニューは TextBox の既定の `ContextFlyout`、`dropdown_caret` は Button のテーマを継ぐ `DropDownButton` のテーマにした。
* **範囲外のまま:**
  * テキスト編集エンジンが要るもの: TextBox の中のインライントークン、複数カーソルと矩形選択、TextBox の中の検索一致の強調と置換、折り返した行の字下げ。
  * OS の機能が要るもの: 自動入力のヒント（`content_type`）、OS の通知への配信。
  * SVG とアニメーション画像（GIF / WebP）の読み込み。必要になったら、読み込みのライブラリを使う別パッケージ（ADR 28 の形）で判断する。
  * Action と keymap からのショートカットの解決（Tooltip、Menu、Kbd）。
  * DataTable の機能（ソート、列の移動と固定、セルと列の選択、多段の見出し、無限読み込み）。規模が大きいので別に判断する。
  * ADR 19 で範囲外にしたもの（Dialog、Command、Editor、Chart など）。
* **ADR 15、ADR 19 との関係:** ADR 15 の「コントロールの論理的な状態や操作を変える処理を作らない」を、アプリが設定する添付プロパティと新しいコントロールについて改める。テーマから自動で当たる Behavior は、今までどおり見た目だけを変える。ADR 19 の「見た目が中心で小さく作れるもの」という基準を、選択・検索・レイアウトを持つコンポーネントまで広げる。

### Consequences

* Good, because GPUI Kit の機能（検索付きの Select、仮想化した Tree、複数月の CalendarView、重なる通知など）を、ほかのコンポーネントと同じ見た目で使える。
* Good, because 標準のコントロールを使う今のアプリは何も変えずに済み、必要なところだけ添付プロパティや新しいコントロールに替えられる。
* Bad, because 公開する型と API が大きく増え、互換性を保つ対象と、Avalonia の更新で確かめる対象が増える。
* Bad, because ComboBox のテーマと `Select` のように、同じコンポーネントに 2 つの使い方ができる。サイトと対応表に、どちらを使えばよいかを書く。
* Neutral, because 対応表の「部分対応」の行は、テーマで扱う範囲と、添付プロパティ・新しいコントロールで足した機能を分けて書き直す。

### Confirmation

* 新しいコントロールと、添付プロパティで加わる状態を、GPUI Kit が描いた参照データと比べる（`cases/*.toml`、`reference/src/cases/`）。
* 操作（選択、検索、開閉、キー操作、ドラッグ）は挙動テストで確かめる。
* NativeAOT のギャラリー（`samples/AvaloniaUIKit.AotSmoke`）に入れ、警告なしで publish できることを確かめる。

## Pros and Cons of the Options

### 既存のコントロールに足せる機能は添付プロパティにし、構成や操作が違うものだけを新しいコントロールにする

* Good, because アプリは `Button` や `TextBox` を書いたまま、必要な機能だけを足せる。InputGroup の中のボタンのように、ほかのコントロールの中にある既存のコントロールにも使える。
* Good, because 既存のコントロールと同じ役割の型を重ねて作らない。
* Bad, because 添付プロパティで挙動を変えると、Avalonia の既存のコントロールの処理の順序に依存する箇所が出る。挙動テストで守る。

### すべてを新しいコントロールにする（既存のコントロールの派生を含む）

* Good, because 機能の置き場所が型ごとにまとまり、見つけやすい。
* Bad, because loading のためだけに Button、ToggleButton、SplitButton の派生が要るなど、同じ役割の型が増える。
* Bad, because アプリは機能を使うために、書いているコントロールを差し替えることになる。

### 今の範囲（ADR 15、ADR 19）のままにする

* Good, because 公開する API が増えない。
* Bad, because GPUI Kit の機能の多くが Avalonia では使えないまま残る。

## More Information

* 機能差の一覧は `docs/references/compatibility-list.md` の各行に書く。
* 見た目だけのコードの範囲は ADR 15、小さなコンポーネントの新規実装は ADR 19、第三者のライブラリのテーマは ADR 28 と ADR 29。
