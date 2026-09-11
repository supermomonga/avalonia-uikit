# GPUI Kit → Avalonia コンポーネント対応表

調査日: 2026-09-11

## 結論

GPUI Kit の見た目を、Avalonia の既存コントロールに適用するテーマとして移植することは可能。配色だけでなく、`ControlTheme` と `ControlTemplate` を差し替えることで、余白、輪郭、内部の配置、状態表示、アニメーションも変更できる。ただし、**対応する既存コントロールの機能が上限**となる。

ボタン、入力欄、選択欄、タブ、メニュー、カレンダー、一覧、表、通知などは対象にできる。標準に存在しないコンポーネントや、標準を超える検索・選択・ドッキング・編集機能は実装しない。対応表の「部分対応」は、記載した部分だけをテーマ化する意味であり、残りの機能を後から自作する予定を意味しない。

この資料は移植範囲を決めるためのソース・資料調査であり、テーマの実装、ビルド、画面比較、アニメーションの実測はまだ行っていない。「対応」は実装済みという意味ではない。

## 調査対象と判定基準

| 項目 | 基準 |
| --- | --- |
| 移植元 | ワークスペースの **longbridge/gpui-kit**。依頼文の `pgui-kit` はこのリポジトリを指すものとして調査した。 |
| GPUI Kit の固定点 | commit [`32030edcdfad813c83174cf9fea5cf595a46dc46`][gp-commit]。`gpui-component` の manifest は `0.6.1`。リリース版そのものではなく、この checkout を基準とする。 |
| コンポーネントの範囲 | [`website/component` の全 74 Markdown ページ][gp-catalog]と、[`crates/component/src/lib.rs` の公開モジュール・再公開][gp-lib]。子部品は親の行にまとめ、資料にない `Breadcrumb`、`Link`、`NativeMenu` なども補足する。 |
| Avalonia の固定点 | 調査時点の最新安定版 [`12.1.2`][av-release]。同タグのソースと同梱テーマを確認した。11.x や将来版への対応はこの資料では約束しない。 |
| 外観の基準 | GPUI Kit の [`Default Light` / `Default Dark`][gp-default-theme]、サイズ定義、各コンポーネントの描画、モーション定義。全テーマ JSON の読み込み機能や Rust API の互換層は対象外。 |

本資料では「標準」を次のように区別する。公式の別パッケージも対象に含める前提で整理しているが、本体同梱と混同しない。

| 分類 | この移植での扱い |
| --- | --- |
| Avalonia 本体 | `Avalonia.Controls` と標準の描画・レイアウト・アニメーション機構。テーマの基本対象。`Primitives`、`Notifications`、`Chrome` 名前空間の公開型も含む。 |
| 公式の別パッケージ | `Avalonia.Controls.ColorPicker`、`Avalonia.Controls.DataGrid`。既存コントロールへのテーマ適用は可能だが、追加参照が必要なため別表に記載する。 |
| 本体・上記の公式別パッケージにないもの | 非対応。FluentAvalonia、第三者製コントロール、Avalonia Pro の Charts / TreeDataGrid / Markdown 等を導入して対象を広げない。公式サイトに紹介ページがあることだけでは、本体標準とは判定しない。 |

判定は **対応**、**部分対応**、**非対応** の三つ。前二つがテーマの対象、最後が対象外となる。

- `Style` は既存プロパティと状態ごとの外観を、`ControlTheme` は型ごとの外観一式を、`ControlTemplate` はコントロール内部の表示構造を変更するものとして使う。
- テンプレート内の `Border`、`Path`、`TextBlock` 等の利用は可能。既存の状態・プロパティ・テンプレート部品に結び付ける。
- 新規コントロール、操作を追加する Behavior、選択や検索の状態管理、独自描画・アニメーションエンジンは作らない。既存状態から表示値への変換が必要な場合は、その必要性を明示する。状態を持たない表示専用の変換と、新しい操作・機能の追加を区別する。
- `Border` や `ItemsControl` を組み合わせれば描ける、という理由だけで標準に存在すると判定しない。たとえば `Badge` や `Breadcrumb` 専用の構成・契約を新設することは対象外。
- アプリによる通常のデータバインディング、コマンド、内容の指定は必要。テーマがアプリのデータ取得や業務処理を代行するものではない。

根拠: [Avalonia の ControlTheme][av-doc-themes]、[本体のコントロール実装][av-controls]、[Fluent の標準テンプレート][av-fluent]。

## Avalonia 本体に対応するもの

### ボタン・入力

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Button][gp-button] | 対応 | [`Button`][av-button] | 色違い、outline / ghost / link 風の表示、サイズ、アイコンを含む内容、hover / pressed / focus / disabled。`loading` 専用 API や非同期処理との連携は追加しない。 |
| ButtonGroup（Button の子部品） | 部分対応 | `Button`、標準 Panel | 各ボタンと、通常のレイアウトで隣接させたときの外観。専用のグループ型・グループ単位の操作 API は作らない。 |
| [DropdownButton][gp-dropdown_button] | 対応 | [`SplitButton`][av-split-button] | 主操作とメニューを開く操作が独立した二分割ボタン。名称の似た `DropDownButton` は全体がメニュー起動ボタンなので対応先を分ける。 |
| [Toggle][gp-toggle] | 対応 | [`ToggleButton`][av-toggle-button] | checked / unchecked / disabled、アイコン、枠線、サイズ。 |
| ToggleGroup（Toggle の子部品） | 部分対応 | [`ListBox`][av-listbox] / `ListBoxItem` | 標準の単一・複数選択を使うボタン状の項目表示。GPUI のグループ API や選択ルールは移植しない。 |
| [Checkbox][gp-checkbox] | 対応 | [`CheckBox`][av-checkbox] | チェック記号、ラベル、枠、状態表示。標準の `IsThreeState` / `IsChecked` を使用する。 |
| [Radio / RadioGroup][gp-radio] | 対応 | [`RadioButton`][av-radio] | 円形マークとラベル。グループ選択は標準の `GroupName` に従い、専用 RadioGroup 型は追加しない。 |
| [Switch][gp-switch] | 対応 | [`ToggleSwitch`][av-switch] | トラック、つまみ、ラベル、checked / disabled、つまみの移動。GPUI のばね運動との完全一致は保証しない。 |
| [Input][gp-input] | 部分対応 | [`TextBox`][av-textbox]、[`MaskedTextBox`][av-masked-textbox] | 枠、placeholder、選択色、キャレット、read-only / disabled、`PasswordChar` / `RevealPassword`、左右の内容領域。入力マスクは Avalonia の書式に従う。GPUI の検証・マスク構文、Esc でクリアする契約などは追加しない。 |
| [Textarea][gp-textarea] | 部分対応 | `TextBox` | `AcceptsReturn`、`TextWrapping`、`MinLines` / `MaxLines` による複数行・高さ制限、スクロール部分をテーマ化。検索 UI や GPUI の編集 API は対象外。 |
| [NumberInput][gp-number-input] | 対応 | [`NumericUpDown`][av-number-input]、`ButtonSpinner` | 数値入力、増減ボタン、書式、最小値・最大値・刻み幅という既存機能の外観。GPUI 独自の正規化・動的な刻み計算は移植しない。 |
| [Slider][gp-slider] | 部分対応 | [`Slider`][av-slider]、`Track`、`Thumb`、`TickBar` | 単一値、縦横、トラック、つまみ、目盛りと状態表示。二つのつまみを持つ Range Slider や独自スケールは非対応。 |

### 選択・一覧・日付・表

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Select][gp-select] | 部分対応 | [`ComboBox`][av-combobox] / `ComboBoxItem` | 単一選択の閉じた欄、キャレット、候補リスト、選択・無効状態。GPUI のポップアップ内検索欄、グループ管理、クリア API は追加しない。 |
| [Combobox][gp-combobox] | 部分対応 | `ComboBox`、[`AutoCompleteBox`][av-autocomplete] | 単一選択は `ComboBox`、入力による標準の候補絞り込みは `AutoCompleteBox` の外観を使用。GPUI の trigger ＋別の検索欄という構成、複数選択・選択済みチップ・独自検索基盤は非対応。12.1.2 の `ComboBox.IsEditable` も複数選択機能ではない。 |
| [List][gp-list] | 部分対応 | `ListBox` / `ListBoxItem` | 行の色・余白・選択・hover、アプリの `ItemTemplate` に配置する内容。内蔵検索、非同期の追加取得、並べ替え D&D、ローディング管理は追加しない。 |
| [Tree][gp-tree] | 部分対応 | [`TreeView`][av-tree] / `TreeViewItem` | 階層行、インデント、展開マーク、選択・無効表示。GPUI の平坦化データモデルや独自の仮想化・データ取得処理は移植しない。 |
| [Calendar][gp-calendar] | 部分対応 | [`Calendar`][av-calendar]、`CalendarItem`、`CalendarButton`、`CalendarDayButton` | 月・年・年代の表示、日セル、今日・選択・無効日。範囲選択は標準 `SelectionMode` の範囲で対応。複数月の同時表示や GPUI の `RangeMatcher` は追加しない。 |
| [DatePicker][gp-date-picker] | 部分対応 | [`CalendarDatePicker`][av-date-picker] | 欄とポップアップ内の Calendar をテーマ化。カレンダーを開く UI なので、ホイール状の標準 `DatePicker` よりこちらが対応する。開始・終了の二日付を持つ Date Range Picker、日付プリセットは非対応。 |
| [Table][gp-table] | 部分対応 | [`TableView`][av-tableview]、`TableViewColumnHeader`、`TableViewRow`、`TableViewCell` | ヘッダー、セル、罫線、外枠、行の余白。GPUI の宣言的な `TableFooter` / `TableCaption` API は新設しない。`TableView` は Avalonia 12.1 からの本体標準。 |
| [DataTable][gp-data-table] | 部分対応 | `TableView` | 表示専用の表、行選択、標準の行仮想化、列幅の調整までを利用。ソート、列の移動・固定、セル範囲選択、無限読み込みは `TableView` に追加しない。公式別パッケージを使う場合は後述。 |
| [VirtualList][gp-virtual-list] | 部分対応 | `ListBox`、[`VirtualizingStackPanel`][av-virtualizing-stack-panel] | 標準の仮想化を維持して行をテーマ化。GPUI のスクロールハンドル、独自サイズキャッシュ、二次元仮想化の仕組みは移植しない。 |

### ナビゲーション・レイアウト

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Tabs / TabBar / Tab][gp-tabs] | 部分対応 | [`TabControl`][av-tabs] / `TabItem`、[`TabStrip`][av-tabstrip] / `TabStripItem` | タブと内容を持つ場合は `TabControl`、選択列だけなら `TabStrip`。下線・pill・segment 等の外観。タブの閉じる処理、D&D 移動、GPUI のメニュー API、選択項目間を追従する共有インジケーターは追加しない。 |
| [Accordion / AccordionItem][gp-accordion] | 部分対応 | [`Expander`][av-expander] | 個々の開閉項目、見出し、境界線、矢印。複数の Expander は独立に開閉する。常に一項目だけを開く Accordion 全体の排他制御は非対応。 |
| [Collapsible][gp-collapsible] | 部分対応 | `Expander` | 一つの領域を展開・折り畳みする表示。`ContentTransition` と標準のアニメーションを使う。GPUI の自然高を測定した可逆なばねアニメーションそのものは移植しない。 |
| [Carousel][gp-carousel] | 部分対応 | [`Carousel`][av-carousel]、`PipsPager` | ページ表示と `PageTransition`、標準の `IsSwipeEnabled` / `ViewportFraction` / `WrapSelection` を使用。隣接ページの表示と循環も 12.1.2 の既存機能内で扱う。GPUI の任意の item 幅、トラックレイアウト、トラックパッドの慣性・スナップ規則の一致は保証しない。 |
| [Pagination][gp-pagination] | 部分対応 | [`PipsPager`][av-pips-pager] | 前後ボタン、選択状態、ページ項目。`TemplateSettings.Pips` に 1 始まりの番号があるので、点を番号表示へ差し替えられる。省略記号付きの番号生成やサーバーのページ取得は追加しない。 |
| [GroupBox][gp-group-box] | 対応 | [`GroupBox`][av-groupbox] | 見出し、内容、背景、outline / fill、余白・角丸。 |
| [Resizable][gp-resizable] | 部分対応 | [`GridSplitter`][av-grid-splitter] と `Grid` | 通常の行・列分割、仕切りの太さ・色・hover・ドラッグ表示。パネルの登録・保存・復元 API は作らない。 |
| [Sidebar][gp-sidebar] | 部分対応 | [`SplitView`][av-splitview]、[`DrawerPage`][av-drawer-page] | 側面の領域、境界線、展開・縮小・overlay。`DrawerPage` なら標準の header / footer 領域も使用できる。SidebarMenu 等の専用型、階層ナビゲーションモデル、バッジは追加しない。 |
| [Sheet][gp-sheet] | 部分対応 | `DrawerPage` | 左右上下の引き出し、背景の暗転、標準の開閉・外側クリック・Esc とその外観。GPUI の root layer、複数 sheet 管理、ドラッグによるサイズ変更は移植しない。`SplitView` 単体と `DrawerPage` の機能を混同しない。 |
| [Scrollable / Scrollbar][gp-scrollable] | 対応 | [`ScrollViewer`][av-scrollviewer]、[`ScrollBar`][av-scrollbar]、`Thumb` | トラック、つまみ、余白、標準の表示条件・拡張状態に対応する外観と遷移。GPUI 固有の idle 時間や表示判定のためにタイマー・入力監視を追加しない。 |
| [Separator][gp-separator-source]（公開モジュール） | 対応 | [`Separator`][av-separator] | 線の色・太さ・余白。メニュー中の区切りも同様。 |

### テキスト・画像・フィードバック・メニュー

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Label][gp-label] | 部分対応 | [`TextBlock`][av-textblock]、`SelectableTextBlock`、[`Label`][av-label] | 文字サイズ、色、行間、折り返し、配置。GPUI Label は表示テキストが中心なので、単純に同名型へ寄せない。検索一致の自動強調やマスク処理は追加しない。 |
| [Link][gp-link-source]（公開モジュール） | 対応 | [`HyperlinkButton`][av-hyperlink] | リンクの色・装飾・hover・pressed。遷移は標準の `NavigateUri` / コマンドに従う。 |
| [Icon][gp-icon] | 部分対応 | [`PathIcon`][av-pathicon]、`Path` / `DrawingImage` | テンプレートで必要なアイコン形状・色・線幅・サイズをリソース化。Lucide は別のアセットであり、Avalonia に同梱されているとは扱わない。任意 SVG の読み込み器や `IconName` 互換 API は追加しない。 |
| [Image][gp-image] | 部分対応 | [`Image`][av-image] | 標準が読める画像の配置、拡縮、周囲の余白。URL 取得、非同期状態、失敗画像の差し替え、任意 SVG 対応は移植しない。`Image` 自体はテンプレートを持たないので `Style` を使う。 |
| [Progress][gp-progress] | 対応 | [`ProgressBar`][av-progress] | 横・縦のバー、トラック、確定値／不定値、色・角丸・値の変化。元コントロールの値と範囲の意味を維持する。 |
| [ProgressCircle][gp-progress-circle-source]（Progress の子部品） | 部分対応 | `ProgressBar` の専用テンプレート、[`Arc`][av-arc] | 進捗の値・範囲・不定値は既存の ProgressBar に任せ、円形の描画を差し替える。不定値は標準の Animation で表現可能。確定値は `Percentage × 3.6` を `SweepAngle` に渡す**表示専用の値変換が必要**で、Setter の差し替えだけでは完結しない。新しい進捗管理型や GPUI の任意の中央コンテンツ API は追加しない。 |
| [Spinner][gp-spinner] | 対応 | `ProgressBar`（`IsIndeterminate=true`）の専用テーマ | 回転するローディング記号として表現する。Avalonia の同名 `Spinner` / `ButtonSpinner` は数値の増減用であり、ローディングの対応先ではない。 |
| [Tooltip][gp-tooltip] | 対応 | [`ToolTip`][av-tooltip] | 背景、枠、影、文字、余白、標準の表示遅延。GPUI の Action からキー表示を自動解決する機能は移植しない。 |
| [Popover][gp-popover] | 部分対応 | [`Flyout`][av-flyout] / `FlyoutPresenter` | アンカーに対する配置、内容、枠・影、開くときの表示。論理的な開閉は標準 Flyout が担当。閉じるアニメーションを待って Popup を破棄する制御は追加しない。 |
| [Menu / ContextMenu / DropdownMenu][gp-menu] | 対応 | [`ContextMenu`][av-contextmenu]、[`MenuFlyout`][av-menu-flyout]、[`MenuItem`][av-menu-item] | 項目、チェック、サブメニュー、アイコン、ショートカット表示、区切り、hover / disabled。クリック型トリガーには標準 `DropDownButton` も使える。 |
| AppMenuBar（menu の公開型） | 対応 | [`Menu`][av-menu] | アプリ内に描画するメニューバーと項目。OS のネイティブメニューとは分ける。 |
| [Notification][gp-notification] | 部分対応 | [`WindowNotificationManager`][av-notification-manager] / [`NotificationCard`][av-notification-card] | アプリ内通知の色、アイコン、内容、位置、標準の自動消去と入退場表示。GPUI の重複排除 ID、重なりを hover で展開するスタックやばねによる並べ直しは移植しない。 |
| [TitleBar][gp-title-bar] / WindowBorder | 部分対応 | [`WindowDrawnDecorations`][av-window-decorations]、`Window.WindowDecorationsTheme` | Avalonia が描くタイトルバー・枠のテンプレート、標準のキャプションボタン・状態をテーマ化。OS が描く装飾は対象外。GPUI のタイトルバーへの任意コンテンツ挿入 API やプラットフォーム処理は移植しない。実際に描画される部位は OS ごとに確認が必要。 |

## 公式の別パッケージに対応するもの

これらは本体のテーマと依存関係を区別する。本体だけを対象にする場合は、この節のコントロール用テーマを除外できる。ここでは新しいパッケージ分割や配布方式までは決定しない。

| GPUI Kit | 判定 | 公式の対応先 | テーマで移植する範囲／制約 |
| --- | --- | --- | --- |
| [ColorPicker][gp-color-picker] | 部分対応 | [`Avalonia.Controls.ColorPicker`][av-colorpicker] の `ColorPicker` / `ColorView` / `ColorSpectrum` / `ColorSlider` / `ColorPreviewer` | 色見本、ドロップダウン、パレット、数値欄、周辺のボタンをテーマ化できる。色のモデル・編集は標準の RGB / HSV 等に従い、GPUI の HSL 編集 UI や独自パレット生成機能は追加しない。本体と対応する版の追加参照が必要。 |
| [DataTable][gp-data-table] | 部分対応 | [`Avalonia.Controls.DataGrid` 12.1.2][av-datagrid] | 行・セル・ヘッダー・ソート表示・列リサイズ・列移動・固定列など、DataGrid が持つ機能の外観。GPUI と同じセル範囲選択、無限取得、データモデルは追加しない。**公式に非推奨**とされているため、新規テーマの基本対応先は `TableView`。DataGrid はこの追加パッケージを利用する場合の対応先として扱う。 |

根拠: [ColorPicker の追加参照と再テンプレート化][av-doc-colorpicker]、[DataGrid の配布条件・非推奨の案内][av-doc-datagrid]。公式の別パッケージがあることと、本体に機能を新設せず使えることは両立するが、必要な依存は明示する。

## 対応する標準コンポーネントがないもの

以下は非対応。近い部品が存在する場合も、組み合わせて新しいコンポーネントを提供することはしない。対応済みのボタン・文字等をアプリ側で使った結果として外観の一部が揃うことは、ここでのコンポーネント対応には数えない。

| GPUI Kit | 非対応とする理由 |
| --- | --- |
| [Alert][gp-alert] | インラインの警告・バナー専用コントロールがない。通知カードとは用途と表示管理が異なる。 |
| [Dialog][gp-dialog] / [AlertDialog][gp-alert-dialog] と子部品 | 同一画面内のモーダル、背景 overlay、標準アクションを備えた対応コントロールがない。`Window.ShowDialog` は別ウィンドウであり、Popover 用 Popup も同等のモーダル機構ではない。 |
| [Attachment][gp-attachment] と子部品 | 添付ファイルの preview・metadata・actions・状態表示という専用の構成がない。アップロード状態の管理もテーマの範囲外。 |
| [Avatar / AvatarGroup][gp-avatar] | 頭文字表示、画像なし時の表示、重なった集合表示を持つ専用型がない。単なる画像のテーマとは分ける。 |
| [Badge][gp-badge] | カウント・dot・アイコンを他の要素に重ねる専用型がない。 |
| [Breadcrumb / BreadcrumbItem][gp-breadcrumb-source]（公開モジュール） | パンくずナビゲーションの専用型がない。リンク列を組み立てる機能は追加しない。 |
| [Bubble][gp-bubble] と子部品 | メッセージの吹き出し・reaction 領域の専用型がない。 |
| [Clipboard][gp-clipboard] | Avalonia の Clipboard はサービス API。コピー操作と一時的な完了表示を持つコントロールはない。Button のテーマからコピー処理を追加しない。 |
| [Command][gp-command] / CommandGroup / CommandItem | コマンドパレットとしての検索・項目管理・キー表示を持つ標準型がない。標準 `CommandBar` はツールバーであり対応先ではない。 |
| [DescriptionList][gp-description-list] / DescriptionItem | ラベル・値・列数・セル結合を扱う専用型がない。Grid による新規実装は行わない。 |
| [Dock][gp-dock] / DockArea / Panel / TabPanel / Tiles | ドッキング、タブの移動、分離、配置の保存・復元を持つ標準型がない。標準 `DockPanel` は子を辺に配置するレイアウトであり、ドッキング機構ではない。 |
| [Editor][gp-editor] | シンタックスハイライト、LSP、折り畳み、補完、巨大テキストの編集エンジンは TextBox のテーマでは追加できない。 |
| [Empty][gp-empty] と子部品 | 空状態の media・title・description・actions をまとめる専用型がない。 |
| [Form][gp-form] / Field | フォーム・フィールドのレイアウトと説明をまとめる標準の Form / Field 型がない。`DataValidationErrors` の外観は入力コントロールのテーマの一部として扱えるが、フォーム機構は作らない。 |
| [HoverCard][gp-hover-card] | trigger からカードにマウスを移した間の開閉維持など、操作可能な hover card の標準型がない。説明表示の ToolTip と同一視しない。 |
| [Kbd][gp-kbd] | キーキャップ表示・OS ごとの記号整形・Action からの解決を持つ専用型がない。MenuItem の標準ショートカット表示は Menu の対象範囲。 |
| [Marker][gp-marker] と子部品 | 会話やタイムラインの区切りにアイコン・内容・線を配置する専用型がない。Separator 単体と分ける。 |
| [Message][gp-message] / MessageGroup と子部品 | avatar・header・content・footer・配置をまとめる会話 UI の専用型がない。 |
| [MessageScroller][gp-message-scroller] | 会話の末尾追従・アンカー保持を管理する専用型がない。通常の ScrollViewer のテーマに追従処理は追加しない。 |
| [OtpInput][gp-otp-input] | 複数桁に分離した入力欄、貼り付け時の配分、桁間移動の標準型がない。MaskedTextBox と同一視しない。 |
| [Rating][gp-rating] | 星による評価入力の標準型がない。Slider に評価選択の操作を追加しない。 |
| [Settings][gp-settings] / SettingPage / SettingGroup / SettingItem | 設定画面の構成・フィールド生成・reset 管理を持つ標準型がない。含まれる通常の入力コントロールだけがテーマの対象になる。 |
| [Skeleton][gp-skeleton] | 読み込み中の代替表示を表す専用型がない。描画用 Border があることを Skeleton の標準対応とは数えない。 |
| [Shimmer / ShimmerText][gp-shimmer] | テキスト上のハイライト移動を提供する標準型がない。専用マスク・描画機能は作らない。 |
| [StatusBar][gp-status-bar] | ステータスバー専用型がない。任意の下部レイアウトを新しいコンポーネントとして提供しない。 |
| [Stepper][gp-stepper] / StepperItem / StepperTrigger | 工程の完了・現在・未到達を示す標準型がない。ページ番号を選ぶ PipsPager とは異なる。 |
| [Tag][gp-tag] | 意味別のラベル・チップを表す専用型がない。 |
| [TextView / Markdown / HTML][gp-text-view] | Markdown / HTML の解析・レイアウト・装飾を行う本体標準型がない。TextBlock の Inlines は markup parser の代わりにならない。 |
| [Chart][gp-chart] | Line / Bar / Area / Pie / Radar / Candlestick / Sankey の各 Chart は本体標準にない。Avalonia Pro の Charts は本件の標準範囲に含めない。 |
| [Plot][gp-plot] | ScaleLinear / ScaleBand / ScalePoint / ScaleOrdinal、Bar / Line / Area / Pie / Arc / Stack / PlotAxis 等のデータ可視化基盤はない。標準 Shape の Arc とプロット用のスケール・軸は別物。 |

### 標準の対応機構はあるが、テーマの移植対象ではないもの

| GPUI Kit | 扱い |
| --- | --- |
| [NativeMenu][gp-native-menu-source] | Avalonia にも [`NativeMenu`][av-native-menu] はあるが、OS が描画するため通常の ControlTheme では外観を上書きできない。**外観移植は非対応**。アプリ内 Menu とは区別する。 |
| [Root][gp-root]、WindowExt、WindowBorder のプラットフォーム処理 | Root は GPUI のフォーカス・表示レイヤー等の基盤。Avalonia の Window / TopLevel 等を通常どおり使用し、GPUI Root 型やサービスは移植しない。WindowBorder のテーマ化可能な部分は前表の WindowDrawnDecorations を参照。 |
| [FocusTrap][gp-focus-trap] | 既存のフォーカス制御を利用する。FocusTrap 専用 API を追加したり、これだけで Dialog の代替ができるとは判定しない。 |
| [Theme][gp-theme]、Size、StyledExt 等 | 色・サイズ・書体・影・動きの定義はテーマリソースへ反映する。Rust の型、ビルダー、JSON registry、OS 監視処理は移植しない。 |
| animation、history、highlighter、searchable_list、input の言語／LSP 基盤 | 見た目だけのコンポーネントではない。Avalonia 標準の入力・アニメーション処理を使用し、GPUI の状態・履歴・検索・編集エンジンは追加しない。 |
| GlobalState、IndexPath、Measure、Inspector、各種ユーティリティ | GPUI の状態管理・計測・開発用 API であり対象外。`gpui-base` 全体、JavaScript 拡張用の `gpui-shell`、WebView など別 crate の移植も含めない。 |

## 見た目・アニメーションの移植可能性

### 共通の外観

| 要素 | 移植方法と制約 |
| --- | --- |
| 色・Light / Dark | GPUI の意味別の色を `ResourceDictionary` / `ThemeDictionaries` と `DynamicResource` に反映する。元 JSON の色参照を解決した値を使い、別の配色を推測して作らない。 |
| 書体・サイズ・余白 | GPUI の [`Size`][gp-sizing]、書体・行高・spacing の定義を参照し、コントロールごとに反映する。同じ `Small` でも全型共通の高さと決めつけない。フォントと描画エンジンが異なるのでピクセル単位の同一表示は未保証。 |
| 角丸・影・境界線 | 標準の Border / Shape / BoxShadow 等で表現する。GPUI はテーマの radius が 0 のとき丸型・pill も角をなくす設計なので、固定の角丸値を散在させない。 |
| 状態 | Avalonia が実際に持つ `:pointerover`、`:pressed`、`:focus-visible`、`:disabled`、`:checked`、`:selected`、`:expanded` 等に結び付ける。存在しない `:loading` 等をテーマだけで生成できるとは扱わない。 |
| アイコン | 必要な形状は静的な描画リソースとして扱う。GPUI 本体の Apache-2.0 と Lucide アセットのライセンス・帰属表記をそれぞれ保持する。 |

### 動き

| GPUI の表現 | 判定 | Avalonia での範囲 |
| --- | --- | --- |
| hover / pressed / checked の色・透明度変化 | 対応 | `BrushTransition`、`DoubleTransition` 等。対象プロパティの型に合う Transition を使う。 |
| つまみ移動、記号の回転・拡縮 | 対応 | `TransformOperationsTransition` またはキーフレーム。操作中の Slider の値やつまみを遅らせず、描画側の状態に適用する。 |
| Spinner・不定値 Progress の繰り返し | 対応 | テンプレート内の Shape / アイコンに `Animation` を適用する。新しい進捗管理型は不要。 |
| Carousel のページ切り替え | 対応 | 標準 `PageSlide` / `CrossFade` 等を `PageTransition` に指定する。 |
| Expander / Sheet の開閉 | 部分対応 | `ContentTransition`、DrawerPage / SplitView の既存状態・テンプレートを使用する。GPUI の自然高測定とばね運動の再現は含めない。 |
| Popup / Flyout / Menu / Tooltip の入退場 | 部分対応 | 内容が生存する間の表示開始アニメーションは可能。非表示・破棄後は描画できないので、閉じるアニメーションのために独自の表示寿命管理を追加しない。 |
| Notification の入退場 | 対応範囲あり | `NotificationCard` は `IsClosing` / `IsClosed` を持ち、標準テーマがアニメーションと閉じる完了を結び付けている。この契約を維持して外観と時間を変更する。 |
| 途中で反転しても速度を維持する spring | 完全再現は非対応 | Avalonia の `SpringEasing` は利用できるが、GPUI の状態を持つ spring と同一の仕組みではない。通常の Transition / Easing で表せる動きまでに限定する。 |
| 選択タブを追いかける下線、通知の重なり・並べ直し | 非対応 | 別要素間の位置計測や専用の状態管理が必要。各項目内の選択表示は対象だが、共有する可動インジケーター等は追加しない。 |
| 自然高を計測し、レイアウト高も滑らかに変える reveal | 完全再現は非対応 | `Height=Auto` と数値の遷移だけでは GPUI の `MotionReveal` に相当しない。ScaleY も親のレイアウト高を変えないため、同じ効果とは扱わない。 |
| OS の reduced-motion 設定との同一連動 | 未保証 | GPUI の低減処理をそのまま移植しない。今回確認した Avalonia の公開 [`IPlatformSettings`][av-platform-settings] には同等の共通設定取得契約がない。テーマの動きの定義と OS の設定検出は別に評価する。 |

共通の時間・曲線は、GPUI の [`MotionTokens`][gp-motion] を出発点にできる。

| 元の定義 | 値 |
| --- | --- |
| duration_instant / fast / normal / slow | 0 / 120 / 180 / 280 ms |
| easing_enter | cubic-bezier(0.16, 1.0, 0.3, 1.0) |
| easing_exit | cubic-bezier(0.4, 0.0, 1.0, 1.0) |
| easing_move | cubic-bezier(0.2, 0.0, 0.0, 1.0) |
| spring_control / spring_move | response 180 / 280 ms。後者は damping 0.85。response は固定の終了時刻ではない。 |
| distance_short / distance_medium | 0.25 / 0.5 rem。Avalonia の描画単位に移す際は基準の文字サイズとの関係を明示する。 |

全コンポーネントがこの値だけを使うわけではない。たとえば [`Popover`][gp-popover-source] の表示開始は 150 ms、[`Spinner`][gp-spinner-source] は 800 ms、[`Scrollbar` のモーション定義][gp-theme-source]は表示 300 ms・消去 500 ms・太さ変更 300 ms。移植時は各実装の利用箇所を確認し、GPUI にない動きを一律に加えない。

根拠: [GPUI の Styling and Motion][gp-styling-motion]、[Avalonia の Control Transitions][av-doc-transitions]、[Keyframe Animations][av-doc-animation]、[`SpringEasing` 実装][av-spring]、[`NotificationCard` の標準テーマ][av-notification-theme]。

## 実装時に確認する項目

1. 基本コントロールの必要な `PART_*`、型、バインディング、標準の疑似クラスを維持する。外側だけを似せて入力・スクロール・キーボード操作を壊さない。
2. 表と一覧は標準の仮想化用 Panel・Presenter を維持する。通常の StackPanel に差し替えて全件を描画することをしない。
3. Light / Dark、通常・hover・押下・フォーカス・無効・選択・検証エラーを比較する。GPUI に存在しない状態も、Avalonia が持つ状態は読める表示を保つ。
4. アニメーションを短時間に反転させた場合、Popup が閉じる場合、Notification が削除される場合を確認する。標準の処理が支えない効果は対象外のままにする。
5. DPI、長い文字列、キーボード操作、フォーカス表示、各 OS のウィンドウ装飾を実画面で確認する。実装前の本資料を、見た目が一致したという検証結果として扱わない。
6. 円形 Progress は `PART_Indicator` 等の既存契約、Percentage の更新、0 / 100 %・任意の Minimum / Maximum を確認する。標準側の横幅計算と円弧の配置が干渉しないテンプレートを検証する。表示専用の値変換を許さず XAML の既存機構だけに限定する場合は、確定値の円形表示を対象から外す。

## 根拠の参照方法

各 GPUI 名のリンクは調査 commit のドキュメント、Avalonia 型名のリンクは `12.1.2` のソースを指す。機能の有無はこの固定ソースを優先し、更新される公式資料とは区別する。対象版または「標準」の範囲が変わった場合は、とくに TableView、DrawerPage、Carousel、ComboBox、ウィンドウ装飾、公式別パッケージを再判定する。

[av-arc]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Shapes/Arc.cs
[av-autocomplete]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/AutoCompleteBox/AutoCompleteBox.cs
[av-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Button.cs
[av-calendar]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Calendar/Calendar.cs
[av-carousel]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Carousel.cs
[av-checkbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/CheckBox.cs
[av-colorpicker]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls.ColorPicker/ColorPicker/ColorPicker.cs
[av-combobox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ComboBox.cs
[av-contextmenu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ContextMenu.cs
[av-controls]: https://github.com/AvaloniaUI/Avalonia/tree/12.1.2/src/Avalonia.Controls
[av-datagrid]: https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/12.1.2/src/Avalonia.Controls.DataGrid/DataGrid.cs
[av-date-picker]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/CalendarDatePicker/CalendarDatePicker.cs
[av-doc-animation]: https://docs.avaloniaui.net/docs/graphics-animation/keyframe-animations
[av-doc-colorpicker]: https://docs.avaloniaui.net/controls/input/selectors/colorpicker
[av-doc-datagrid]: https://docs.avaloniaui.net/controls/data-display/structured-data/datagrid
[av-doc-themes]: https://docs.avaloniaui.net/docs/styling/control-themes
[av-doc-transitions]: https://docs.avaloniaui.net/docs/graphics-animation/control-transitions
[av-drawer-page]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Page/DrawerPage.cs
[av-expander]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Expander.cs
[av-fluent]: https://github.com/AvaloniaUI/Avalonia/tree/12.1.2/src/Avalonia.Themes.Fluent/Controls
[av-flyout]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Flyouts/Flyout.cs
[av-grid-splitter]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/GridSplitter.cs
[av-groupbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/GroupBox.cs
[av-hyperlink]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/HyperlinkButton.cs
[av-image]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Image.cs
[av-label]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Label.cs
[av-listbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ListBox.cs
[av-masked-textbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/MaskedTextBox.cs
[av-menu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Menu.cs
[av-menu-flyout]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Flyouts/MenuFlyout.cs
[av-menu-item]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/MenuItem.cs
[av-native-menu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/NativeMenu.cs
[av-notification-card]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Notifications/NotificationCard.cs
[av-notification-manager]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Notifications/WindowNotificationManager.cs
[av-notification-theme]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Themes.Fluent/Controls/NotificationCard.xaml
[av-number-input]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/NumericUpDown/NumericUpDown.cs
[av-pathicon]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/PathIcon.cs
[av-pips-pager]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/PipsPager/PipsPager.cs
[av-progress]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ProgressBar.cs
[av-radio]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/RadioButton.cs
[av-release]: https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.2
[av-scrollbar]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Primitives/ScrollBar.cs
[av-scrollviewer]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ScrollViewer.cs
[av-separator]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Separator.cs
[av-slider]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Slider.cs
[av-split-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/SplitButton/SplitButton.cs
[av-splitview]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/SplitView/SplitView.cs
[av-spring]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Base/Animation/Easings/SpringEasing.cs
[av-switch]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ToggleSwitch.cs
[av-tableview]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/TableView.cs
[av-tabs]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/TabControl.cs
[av-tabstrip]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Primitives/TabStrip.cs
[av-textblock]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/TextBlock.cs
[av-textbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/TextBox.cs
[av-toggle-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Primitives/ToggleButton.cs
[av-tooltip]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/ToolTip.cs
[av-tree]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/TreeView.cs
[av-virtualizing-stack-panel]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/VirtualizingStackPanel.cs
[av-window-decorations]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Controls/Chrome/WindowDrawnDecorations.cs
[gp-accordion]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/accordion.md
[gp-alert]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/alert.md
[gp-alert-dialog]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/alert-dialog.md
[gp-attachment]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/attachment.md
[gp-avatar]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/avatar.md
[gp-badge]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/badge.md
[gp-bubble]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/bubble.md
[gp-button]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/button.md
[gp-calendar]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/calendar.md
[gp-carousel]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/carousel.md
[gp-catalog]: https://github.com/longbridge/gpui-kit/tree/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component
[gp-chart]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/chart.md
[gp-checkbox]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/checkbox.md
[gp-clipboard]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/clipboard.md
[gp-collapsible]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/collapsible.md
[gp-color-picker]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/color-picker.md
[gp-combobox]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/combobox.md
[gp-command]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/command.md
[gp-commit]: https://github.com/longbridge/gpui-kit/commit/32030edcdfad813c83174cf9fea5cf595a46dc46
[gp-data-table]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/data-table.md
[gp-date-picker]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/date-picker.md
[gp-default-theme]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/theme/default-theme.json
[gp-description-list]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/description-list.md
[gp-dialog]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/dialog.md
[gp-dock]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/dock.md
[gp-dropdown_button]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/dropdown_button.md
[gp-editor]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/editor.md
[gp-empty]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/empty.md
[gp-focus-trap]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/focus-trap.md
[gp-form]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/form.md
[gp-group-box]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/group-box.md
[gp-hover-card]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/hover-card.md
[gp-icon]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/icon.md
[gp-image]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/image.md
[gp-input]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/input.md
[gp-kbd]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/kbd.md
[gp-label]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/label.md
[gp-lib]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/lib.rs
[gp-list]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/list.md
[gp-marker]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/marker.md
[gp-menu]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/menu.md
[gp-message]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/message.md
[gp-message-scroller]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/message-scroller.md
[gp-motion]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/theme/motion.rs
[gp-notification]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/notification.md
[gp-number-input]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/number-input.md
[gp-otp-input]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/otp-input.md
[gp-pagination]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/pagination.md
[gp-plot]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/plot.md
[gp-popover]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/popover.md
[gp-popover-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/popover.rs
[gp-progress]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/progress.md
[gp-radio]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/radio.md
[gp-rating]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/rating.md
[gp-resizable]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/resizable.md
[gp-root]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/root.md
[gp-scrollable]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/scrollable.md
[gp-select]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/select.md
[gp-settings]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/settings.md
[gp-sheet]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/sheet.md
[gp-shimmer]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/shimmer.md
[gp-sidebar]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/sidebar.md
[gp-sizing]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/sizing.rs
[gp-skeleton]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/skeleton.md
[gp-slider]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/slider.md
[gp-spinner]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/spinner.md
[gp-spinner-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/spinner.rs
[gp-status-bar]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/status-bar.md
[gp-stepper]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/stepper.md
[gp-styling-motion]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/docs/STYLING-AND-MOTION.md
[gp-switch]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/switch.md
[gp-table]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/table.md
[gp-tabs]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/tabs.md
[gp-tag]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/tag.md
[gp-text-view]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/text-view.md
[gp-textarea]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/textarea.md
[gp-theme]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/theme.md
[gp-theme-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/theme/mod.rs
[gp-title-bar]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/title-bar.md
[gp-toggle]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/toggle.md
[gp-tooltip]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/tooltip.md
[gp-tree]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/tree.md
[gp-virtual-list]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/website/component/virtual-list.md
[gp-separator-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/separator.rs
[gp-link-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/link.rs
[gp-progress-circle-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/progress/progress_circle.rs
[gp-breadcrumb-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/breadcrumb.rs
[gp-native-menu-source]: https://github.com/longbridge/gpui-kit/blob/32030edcdfad813c83174cf9fea5cf595a46dc46/crates/component/src/native_menu/mod.rs
[av-platform-settings]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.2/src/Avalonia.Base/Platform/IPlatformSettings.cs
