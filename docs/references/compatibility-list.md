# GPUI Kit → Avalonia コンポーネント対応表

調査日: 2026-09-11（固定点の更新: 2026-10-03、サードパーティのライブラリと ADR 30 の機能の追加: 2026-10-05、Settings の追加: 2026-10-09）

## 結論

GPUI Kit の見た目を、Avalonia の既存コントロールに適用するテーマとして移植することは可能。配色だけでなく、`ControlTheme` と `ControlTemplate` を差し替えることで、余白、輪郭、内部の配置、状態表示、アニメーションも変更できる。ただし、テーマで扱えるのは**対応する既存コントロールの機能まで**である。

ボタン、入力欄、選択欄、タブ、メニュー、カレンダー、一覧、表、通知などは対象にできる。標準に存在しないコンポーネントのうち、見た目が中心で小さく作れるもの（Badge、Tag、Alert など）は新しいコントロールとして実装する（ADR 19）。既存のコントロールが持たない GPUI Kit の機能（Select の検索、List のセクション、Calendar の複数月の表示など）は、既存のコントロールに付ける添付プロパティと新しいコントロールとして足す（ADR 30）。設定画面（Settings）は、ページ・group・項目の新しいコントロールとして足し、フィールドには標準のコントロールを置く（ADR 40）。テキスト編集エンジンや OS の機能が要るもの、SVG とアニメーション画像の読み込み、keymap からの Action の解決、DataTable の機能、モーダル・コマンドパレット・可視化の仕組みは実装しない。ドッキングは、それを持つサードパーティのライブラリ（Dock.Avalonia）を使う人のために、別パッケージのテーマとして対応する（ADR 28）。タブを閉じる・足す・D&D で並べ替える・ウィンドウへ切り離す操作は、TabStrip / TabControl の添付プロパティとして足す（ADR 33）。対応表の「部分対応」は、既存のコントロールのテーマで扱う範囲を表す。テーマで扱えない機能のうち ADR 30 で足したものは、各行に添付プロパティと新しいコントロールの名前を書き、[ADR 30 で機能を足したもの](#adr-30-で機能を足したもの) にまとめた。

この資料はもともと、移植範囲を決めるためのソース・資料調査として作った。その後、「対応」の 16 行と「部分対応」の 34 行（DataTable は `TableView` と DataGrid の 2 行）をテーマとして、「新規実装」の 20 行（Settings は 2026-10-09 に ADR 40 で追加）を新しいコントロールとして、サードパーティのライブラリの 2 行を別パッケージのテーマとして実装し、GPUI Kit との一致を自動テストで検証した（[実装状況](#実装状況)）。見た目だけのコードを認めた方針の改定（ADR 15）で対象に戻した動き（タブのインジケーター、自然高の reveal など）は、行の説明も改めた。さらに ADR 30 で「対応」「部分対応」の行に機能を足し、各行の対象外を、ADR 30 の後も扱わないものだけに書き直した。あわせて、Avalonia が持つのにテンプレートが描いていなかった機能（ComboBox の `IsEditable`、TextBox と ComboBox のクリアボタン、TextBox の右クリックメニュー、NumericUpDown の前後の内容、`DropDownButton`、表の空の表示など）をテーマで描くようにした。

## 実装状況

2026-10-09 時点。「対応」と「部分対応」の全 50 行、「新規実装」の 20 行、サードパーティのライブラリの 1 行を実装し、ADR 30 で 36 のコンポーネントに機能を足した。GPUI Kit `2c5162f` が描いた参照データと比べて、Light / Dark、各 Variant・サイズ・状態、動きが一致することを自動テストで確かめた。本体のコントロールと ADR 30 の添付プロパティは `UIKitTheme`（`src/AvaloniaUIKit`）、公式の別パッケージのコントロールは別のアセンブリ（`src/AvaloniaUIKit.ColorPicker`、`src/AvaloniaUIKit.DataGrid`。ADR 16）、サードパーティのライブラリのテーマはそのライブラリごとのアセンブリ（`src/AvaloniaUIKit.Dock`。ADR 28）にある。全 6994 件のテストが成功している。検証の方法、許容値、緩和 ID の意味、利用側の約束は [テストと一致検証](../testing.md) にまとめた。

「静止ケース」は参照データの静止状態のケース数（Light / Dark と Aurora Light の合計）。「対象外とした機能」には、ADR 30 の後も扱わない機能を書く。ADR 30 で足した機能は、それを持つ添付プロパティと新しいコントロールの名前を添える（[ADR 30 で機能を足したもの](#adr-30-で機能を足したもの)）。GPUI と動きだけが違うもの（閉じるときのアニメーションなど）は「動きの差」と書く。

### 対応

| GPUI Kit | Avalonia | 静止ケース | 動き（フレームごとの比較） | 固有の緩和 | 対象外とした機能 |
| --- | --- | --- | --- | --- | --- |
| Button | `Button`、`DropDownButton`（dropdown_caret） | 784、128 | – | R12、R27、R28 | –（loading とフォーカスを奪わない押下は `uikit:Buttons`） |
| DropdownButton | `SplitButton` | 150 | – | R12、R28 | –（アクションの半分の loading は `uikit:Buttons.IsLoading`） |
| Toggle | `ToggleButton` | 160 | – | – | – |
| Checkbox | `CheckBox` | 86 | チェック記号のフェード（spring）、途中で戻したとき | R16、R27 | – |
| Radio | `RadioButton` | 86 | チェック記号のフェード（spring） | R27 | – |
| Switch | `ToggleSwitch` | 94 | つまみの移動（spring）、途中で戻したとき | R8 | – |
| NumberInput | `NumericUpDown`、`ButtonSpinner` | 64 | – | R24、R25 | `ButtonSpinnerLocation`（GPUI と同じ [−] 値 [+] に固定）。入力の制限と値で変わる刻みは `uikit:NumberInput` |
| GroupBox | `GroupBox` | 16 | – | – | –（footer は `uikit:GroupBoxes.Footer`） |
| Scrollable / Scrollbar | `ScrollViewer`、`ScrollBar`、`Thumb` | 18 | 表示、2 秒後の消去、つまみの拡大、スクロールでの表示、Scrolling モード、つまみを直接指したときのスライド入場 | – | – |
| Separator | `Separator` | 16 | – | – | –（label は `uikit:Separators.Label`） |
| Link | `HyperlinkButton` | 6 | – | R18 | – |
| Progress | `ProgressBar` | 27 | 値の変化、不定値の繰り返し | R19 | – |
| Spinner | `ProgressBar`（`Theme="{StaticResource UIKitSpinner}"`） | 14 | 回転 | – | easing の変更（`ease()`）。アイコンは `uikit:Spinners.Icon` |
| Tooltip | `ToolTip` | 4 | 表示（フェードとスライド） | R5、R21 | Action からのキー表示の解決。閉じる前の猶予と隣への切り替えスライドは動きの差 |
| Menu / ContextMenu / DropdownMenu | `ContextMenu`、`MenuFlyout`、`MenuItem` | 14 | – | R12、R22、R28 | link 項目 |
| AppMenuBar | `Menu` | 4 | – | R28 | – |

### 部分対応

「対象外とした機能」には、各行の説明で対象外とした機能に加えて、実装で扱えなかったものを書く。

| GPUI Kit | Avalonia | 静止ケース | 動き（フレームごとの比較） | 固有の緩和 | 対象外とした機能 |
| --- | --- | --- | --- | --- | --- |
| ButtonGroup | `StackPanel Classes="button-group"` の `Button` | 86 | – | R27 | –（グループのクラスの受け渡し、選択とクリックの報告は `uikit:ButtonGroup`） |
| ToggleGroup | `ListBox Classes="toggle-group"` | 76 | – | – | –（各トグルを Tab で移り、矢印キーで移らないキー操作は `uikit:ToggleGroup`） |
| Input | `TextBox` | 116 | – | R24、R25 | インライントークン、自動入力のヒント（`content_type`）、IME の未確定の文字の検証、右クリックでのキャレットの移動。検証・マスク・Esc でのクリアは `uikit:Inputs` |
| Textarea | `TextBox`（`AcceptsReturn`） | 42 | – | R25 | 検索 UI、インライントークン、複数カーソル。インデントは `uikit:Inputs.TabSize` |
| InputGroup | `TextBox Classes="group"` | 54 | フォーカスの色（HSLA の補間） | R25 | –（`BlockStart` / `BlockEnd` の行は `uikit:InputGroup`、ボタンの loading は `uikit:Buttons.IsLoading`） |
| Slider | `Slider` | 55 | つまみのリングの表示・消去・途中で戻したとき | R30 | トラックを押している間の active 色（GPUI もつまみが先に動くので描かない）。範囲と対数スケールは `uikit:RangeSlider` |
| Select | `ComboBox` | 90 | 開くとき（スライドとフェード） | R5、R12 | –（ポップアップ内の検索欄、グループ、複数選択は `uikit:Select`） |
| Combobox | `ComboBox Classes="combobox"`、`AutoCompleteBox` | 24 | 開くとき | R5、R12 | チェックの差し替え（`check_icon`）。検索欄、複数選択、footer、`render_trigger` は `uikit:Select`（`combobox` クラス） |
| List | `ListBox` | 24 | – | R29 | `render_initial`、検索中のスピナー、区切りの項目（`ListItem::separator`、`ListSeparatorItem`）。セクション、検索欄、確定、追加取得、読み込み中の表示は `uikit:ListView` |
| Tree | `TreeView` | 22 | – | – | –（平坦化した行の仮想化と GPUI のキー操作は `uikit:Tree`） |
| Calendar | `Calendar` | 55 | – | R7（「今日」の固定） | 年の範囲の変更（`set_year_range`）。複数月の表示、20 年の年グリッド（Avalonia は 12 年）、月ごとに 4〜6 行の週（Avalonia は常に 6 行）、曜日と関数での無効な日は `uikit:CalendarView` |
| DatePicker | `CalendarDatePicker` | 60 | 開くとき | R5、R7、R12 | 時刻付きのプリセット、年の範囲の変更。日付の範囲、プリセット、時刻の同時編集、クリアボタンは `uikit:DateField` |
| TimeField | `TimePicker` | 46 | – | – | –（欄内のセグメントの直接編集は `uikit:TimeField`。`TimePicker` の開いたピッカーは Avalonia のもの） |
| Table | `TableView`（`Theme="{StaticResource UIKitTable}"`） | 26 | – | – | –（宣言的な部品、列の結合、`TableFooter` / `TableCaption` は `uikit:Table`） |
| DataTable | `TableView` | 52 | – | – | ソート、列の移動・固定・最小幅・最大幅、列とセルの選択、多段の列見出し、読み込み中の表示、無限読み込み（DataTable の機能は ADR 30 でも範囲外）。右クリックした行の枠（Avalonia は右ボタンで行を選択する） |
| VirtualList | `ListBox`、`VirtualizingStackPanel` | 22 | – | – | –（`scroll_to_item` の Center は `ItemsScrolling.ScrollToItem`） |
| Tabs / TabBar / Tab | `TabStrip`、`TabControl` | 154 | pill / segmented / underline のインジケーター、途中で戻したとき（`TabControl` も） | R8 | –（prefix と suffix は `uikit:Tabs.Prefix` / `Suffix`、閉じるボタンは `uikit:Tabs.Closable`。溢れたタブのスクロールとタブの一覧のメニューはテーマ） |
| Accordion | `Expander`、`StackPanel Classes="accordion"` | 32 | 開く・閉じる・途中で戻したとき | R8 | –（1 項目だけを開く制御は `uikit:Accordion`） |
| Collapsible | `Expander`（`Theme="{StaticResource UIKitCollapsible}"`） | 16 | 開く・閉じる・即時・途中で戻したとき | R8 | – |
| Carousel | `Carousel`、`PipsPager Classes="carousel"` | 22 | 次・前のページ送り（`uikit:SpringSlide`） | R8 | –（ドラッグ・ホイール・トラックパッドと、複数ページ先への移動・途中での反転・ループの動きは `uikit:Carousels.TracksPointer`、前後のボタンは `uikit:CarouselPrevious` / `uikit:CarouselNext`） |
| Pagination | `PipsPager` | 42 | – | – | –（省略記号付きの番号と、省略したページのメニューは `uikit:Pagination`） |
| Resizable | `GridSplitter` | 30 | pill の hover・離脱・押下・ドラッグ・解放 | R30 | 状態の共有（`with_state`、`adopt_sizes`）。パネルの登録、連鎖するリサイズ、サイズの範囲は `uikit:ResizablePanelGroup` |
| Sidebar | `SplitView`、`DrawerPage` | 22 | icon の折り畳み・展開（左右）、offcanvas の折り畳み・展開 | – | 項目の仮想化（`list()`）。途中で戻したときに GPUI が前の目標へ飛ぶ動きは動きの差。SidebarMenu、項目の suffix（バッジ）、offcanvas の 200ms 後の非表示は `uikit:Sidebar` |
| Sheet | `DrawerPage Classes="sheet"` | 28 | 4 方向の滑り込み | – | –（コードからウィンドウの上に開く使い方は `uikit:Sheet`） |
| Toolbar | `CommandBar` | 50 | – | – | 矢印キーの移動だけを止める `disabled`。任意の要素、伸縮スペーサー、`ToolbarGroup` は `uikit:Toolbar` |
| Label | `TextBlock Classes="label"`、`Label` | 40 | – | R10（解消） | –（検索一致の強調とマスクは `uikit:TextLabel`） |
| Icon | `PathIcon`（109 個の Lucide アイコン） | 154 | – | – | 任意 SVG の読み込み（ADR 30 でも範囲外）。`IconName` は `uikit:Icon` |
| Image | `Image` | 20 | – | R31 | SVG とアニメーション画像（ADR 30 でも範囲外）、`grayscale`、枠より縦長の画像（GPUI が枠に収めない）。URL の取得、読み込み中と失敗時の内容は `uikit:AsyncImage` |
| ProgressCircle | `ProgressBar`（`Theme="{StaticResource UIKitProgressCircle}"`） | 44 | 値の変化、不定値 | – | –（中央の内容は `uikit:ProgressCircles.Content`） |
| Popover | `Flyout`、`FlyoutPresenter` | 38 | 開くとき | R12 | –（閉じるアニメーションは動きの差） |
| Notification | `WindowNotificationManager`、`NotificationCard` | 34 | 入場・退場 | R3、R5、R30 | OS の通知への配信（ADR 30 でも範囲外）。通知ごとの配置、重複排除、重なりの展開と並べ直しは `uikit:NotificationList` |
| TitleBar / WindowBorder | `WindowDrawnDecorations` | 6 | – | R7、R14、R32 | OS が描く装飾、Linux の右クリックのウィンドウメニュー。任意の内容は `uikit:TitleBar` |
| ColorPicker | `ColorPicker`（`UIKitColorPickerTheme`） | 42 | – | – | –（GPUI のパレット・featured 行・HSLA のスライダーと、色なしの値は本体の `uikit:ColorSelect`）。ポップオーバーは挙動テストで確かめる |
| DataTable | `DataGrid`（`UIKitDataGridTheme`） | 56 | – | R28 | 列の選択、多段の列見出し、読み込み中の表示、無限取得（ADR 30 でも範囲外）。セルの選択は選択行の current cell として描く（`cell-selectable`）。DataGrid 本体がトリム非対応なので NativeAOT は保証しない |

### 新規実装

Avalonia に対応するコントロールがないため、新しいコントロールとして作った（ADR 19。Settings は ADR 40）。どれも `UIKitTheme` に含まれ、NativeAOT のギャラリーにも入っている。

| GPUI Kit | Avalonia | 静止ケース | 動き（フレームごとの比較） | 固有の緩和 | 対象外とした機能 |
| --- | --- | --- | --- | --- | --- |
| Badge | `uikit:Badge` | 32 | – | – | – |
| Tag | `uikit:TagLabel` | 82 | – | – | 任意の色の組み合わせ（`TagVariant::Custom`。Background などを直接指定すれば描ける） |
| Alert | `uikit:Alert` | 46 | – | R34 | メッセージの Markdown（TextView） |
| Skeleton | `uikit:Skeleton` | 8 | 明滅 | – | – |
| StatusBar | `uikit:StatusBar` | 9 | – | – | – |
| Breadcrumb | `uikit:Breadcrumb`、`uikit:BreadcrumbItem` | 8 | – | – | – |
| Kbd | `uikit:Kbd` | 16 | – | R22 | Action からのキーの解決 |
| Clipboard | `uikit:Clipboard` | 18 | – | – | 値を関数で渡す `value_fn`（クリック時に `Text` を設定すれば同じ） |
| Rating | `uikit:Rating`、`uikit:RatingStar` | 28 | – | – | – |
| Avatar / AvatarGroup | `uikit:Avatar`、`uikit:AvatarGroup` | 36、10 | – | R33 | URL からの画像の取得 |
| Empty | `uikit:EmptyState` | 8 | – | R34 | 破線の枠 |
| DescriptionList | `uikit:DescriptionList`、`uikit:DescriptionItem`、`uikit:DescriptionSeparator` | 16 | – | – | – |
| Stepper | `uikit:Stepper`、`uikit:StepperItem` | 26 | – | – | – |
| Form / Field | `uikit:Form`、`uikit:FormField` | 12 | – | – | 列の開始・終了位置の指定（`col_start` / `col_end`）、ラベルの文字サイズの変更 |
| HoverCard | `uikit:HoverCard` | 8 | – | R9 | タップで開く `tap_to_open`、`appearance(false)` |
| Shimmer / ShimmerText | `uikit:ShimmerText` | 6 | スイープ、逆向き（Light / Dark） | – | 複数の ShimmerText の位相をアプリの時計でそろえる（GPUI の repeat_synced）、絶対値の帯幅 |
| Marker | `uikit:Marker` | 18 | – | – | 区切り線のスタイル変更（`separator_style`） |
| Bubble | `uikit:Bubble` | 28 | – | – | リアクションに置く Button の自動の丸め |
| Message / MessageGroup | `uikit:Message` | 10 | – | – | MessageGroup（StackPanel の Spacing 8 で同じ）、ヘッダー・フッターの inset の個別指定 |
| Settings / SettingPage / SettingGroup / SettingItem | `uikit:Settings`、`uikit:SettingPage`、`uikit:SettingGroup`、`uikit:SettingItem` | 50 | – | R34 | dropdown フィールドの自動の見た目（`DropDownButton.outline` で同じ見た目を作れるが値は追わない）、説明の Markdown（TextBlock の Inlines で代える）、`sidebar_style` / `header_style` の StyleRefinement、group の一覧の仮想化（GPUI の `list`。全 group を並べる） |

### ADR 30 で機能を足したもの

既存のコントロールのテーマで扱えない GPUI Kit の機能を、添付プロパティと新しいコントロールとして足した（ADR 30）。どれも `UIKitTheme` に含まれ、NativeAOT のギャラリーにも入っている。標準のコントロールのテーマはそのまま残り、機能はアプリが添付プロパティや新しいコントロールを使ったときだけ加わる。テーマの修正で Avalonia の機能を描くようにしたもののうち、別のケースで比べるもの（`DropDownButton`、TextBox の右クリックメニュー）もここに書く。差し込み口の添付プロパティ（GroupBox、Separator、ProgressCircle、Spinner）と `ItemsScrolling` のケースは元のコンポーネントのケースに含まれ、ここにはそのうちの数を書く。

| GPUI Kit | Avalonia | 静止ケース | 動き（フレームごとの比較） | 固有の緩和 | 対象外とした機能 |
| --- | --- | --- | --- | --- | --- |
| Button | `uikit:Buttons.IsLoading` / `LoadingIcon` / `TakesFocusOnPointer`（`Button` とその派生型）、`DropDownButton`（dropdown_caret。テーマ） | 356、128 | – | R12、R27、R28 | – |
| DropdownButton | `SplitButton` の `uikit:Buttons.IsLoading`（アクションの半分） | 72 | – | R12、R28 | – |
| ButtonGroup | `uikit:ButtonGroup` | 126 | – | R27 | – |
| ToggleGroup | `uikit:ToggleGroup` | 88 | – | – | – |
| Input | `TextBox` の `uikit:Inputs.Pattern` / `Validate` / `MaskPattern` / `CleanOnEscape`、右クリックメニュー（テーマ） | 18、8 | – | R12、R22、R24、R25、R28 | インライントークン、自動入力のヒント（`content_type`）、IME の未確定の文字の検証、右クリックでのキャレットの移動 |
| Textarea | `TextBox` の `uikit:Inputs.TabSize` / `HardTabs` | 10 | – | R25 | 検索 UI、インライントークン、複数カーソル |
| InputGroup | `uikit:InputGroup`、`uikit:InputGroupAddon`、`TextBox.group` の Button の `uikit:Buttons.IsLoading` | 76、20 | フォーカスの色 | R25 | アドオンの muted の文字（`InputGroupText`）、アドオンのボタンの色違い |
| NumberInput | `uikit:NumberInput`（`NumericUpDown` の派生） | 46 | – | R24、R25 | – |
| Slider | `uikit:RangeSlider` | 93 | つまみのリング（範囲の終点も） | R30 | – |
| Select | `uikit:Select`、`uikit:SelectGroup` | 118 | 開くとき | R5、R12 | – |
| Combobox | `uikit:Select Classes="combobox"` | 54 | 開くとき | R5、R12 | チェックの差し替え（`check_icon`） |
| List | `uikit:ListView`、`uikit:ListItem`、`uikit:ListSection` | 68 | – | R29 | `render_initial`、検索中のスピナー、区切りの項目 |
| Tree | `uikit:Tree`、`uikit:TreeItem` | 54 | – | – | – |
| Calendar | `uikit:CalendarView` | 113 | – | R7 | 年の範囲の変更（`set_year_range`。今日の前後 50 年） |
| DatePicker | `uikit:DateField` | 114 | 開くとき | R5、R7、R12 | 時刻付きのプリセット、年の範囲の変更 |
| TimeField | `uikit:TimeField` | 80 | – | R16 | – |
| Table | `uikit:Table` と部品（`TableHeader`、`TableBody`、`TableFooter`、`TableRow`、`TableHead`、`TableCell`、`TableCaption`） | 42 | – | – | – |
| VirtualList | `ItemsScrolling.ScrollToItem` | 10 | – | – | – |
| Tabs / TabBar / Tab | `TabStrip` / `TabControl` の `menu` クラス（`uikit:TabsMenuButton`）と溢れたタブのスクロール、`uikit:Tabs.Prefix` / `Suffix`。タブの操作は `uikit:Tabs.Closable` / `NewTabFactory` / `Reorderable` / `DragGroup` / `DetachedWindowFactory`（ADR 33。GPUI に参照がなく、挙動テストで確かめる） | 38 | – | R12、R28 | GPUI の Dock の TabPanel が描く挿入線とドラッグのプレビュー（ドラッグ中はその場で並べ替える）、キーボードでの並べ替え |
| Accordion | `uikit:Accordion` | 40 | 開く・閉じる・途中で戻したとき、1 項目だけを開くとき | R8 | – |
| Carousel | `uikit:Carousels.TracksPointer`（`uikit:CarouselTrack`）、`uikit:CarouselPrevious` / `uikit:CarouselNext` | 38 | 次・前・2 ページ先・ループの折り返し・ドラッグを離した後 | R8、R38 | 1 ページに複数のアイテムを並べる表示（`ViewportFraction` はトラックでは使えない） |
| Pagination | `uikit:Pagination` | 70 | – | R12、R28、R37 | – |
| GroupBox | `uikit:GroupBoxes.Footer` | 6 | – | – | – |
| Resizable | `uikit:ResizablePanelGroup`、`uikit:ResizablePanel` | 60 | pill の hover・離脱・押下・ドラッグ・解放 | R30 | 状態の共有（`with_state`、`adopt_sizes`）、伸びないパネルのスタイル（`flex_none()` など） |
| Sidebar | `uikit:Sidebar`、`uikit:SidebarHeader`、`uikit:SidebarFooter`、`uikit:SidebarGroup`、`uikit:SidebarMenu`、`uikit:SidebarMenuItem`、`uikit:SidebarToggleButton` | 52 | icon の折り畳み・展開（左右）、offcanvas の折り畳み・展開、メニューの折り畳み | – | 項目の仮想化（`list()`）、ラベルだけのスタイル（`label_style`）。途中で戻したときに GPUI が前の目標へ飛ぶ動きは動きの差 |
| Sheet | `uikit:Sheet` | 40 | 4 方向の滑り込み | – | – |
| Toolbar | `uikit:Toolbar`、`uikit:ToolbarGroup`、`uikit:ToolbarSpacer`、`uikit:Toolbar.TakesSize` | 72 | – | – | 矢印キーの移動だけを止める `disabled` |
| Separator | `uikit:Separators.Label` | 8 | – | – | – |
| Label | `uikit:TextLabel` | 56 | – | – | – |
| Icon | `uikit:Icon`、`IconName`（1830 個。テーマが 109 個を持ち、残りはジェネレーターが使うアプリに足す。ADR 38） | 162 | – | – | 任意 SVG の読み込み、サイズを指定しないアイコンを文字の大きさにすること（16px になる） |
| Image | `uikit:AsyncImage`、`ImageLoader` | 28 | – | R31 | SVG とアニメーション画像、`grayscale`、失敗した読み込みの保持（次の画像で読み直す） |
| ProgressCircle | `uikit:ProgressCircles.Content` | 2 | – | – | – |
| Spinner | `uikit:Spinners.Icon` | 6 | – | – | easing の変更（`ease()`） |
| Notification | `uikit:NotificationList`、`uikit:NotificationItem`、`NotificationPlacement` | 60 | 入場・退場、左右中央の入場（フェードだけ） | R3、R5、R30 | OS の通知への配信、Sheet を開いている間の通知の層のずれ。同じ id での置き換えと、`MaxItems` で隠れていたカードの再表示は動きの差 |
| TitleBar | `uikit:TitleBar` | 10 | – | R14、R32 | Linux の右クリックのウィンドウメニュー（`show_window_menu`。Avalonia に公開 API がない）、サーバー側の装飾かどうかによるボタンの出し分け（ウィンドウが装飾に広がっているかで決める） |
| ColorPicker / ColorSelect | `uikit:ColorSelect`、`uikit:ColorSwatch` | 84 | – | R12 | – |

### サードパーティのライブラリ

そのライブラリを使う人のための別パッケージにある（ADR 28）。パッケージは 1 つの版（Dock.Avalonia 12.1.0.6）に固定する。

| GPUI Kit | Avalonia | 静止ケース | 動き（フレームごとの比較） | 固有の緩和 | 対象外とした機能 |
| --- | --- | --- | --- | --- | --- |
| Dock / DockArea / Panel / TabPanel | Dock.Avalonia の `DockControl` と各部品（`UIKitDockTheme`） | 26 | – | R5、R35、R36 | グループのズーム、左右・下のドックの開閉ボタン。浮いたウィンドウとピン留めの帯は GPUI にないので、GPUI の部品で描くが比べていない。Dock.Avalonia が trim 非対応なので NativeAOT は保証しない |

### 共通

- 全コンポーネントに共通の緩和: R1（文字のラスタライズ）、R2（縁の AA）、R3（影）、R4（アイコン）、R7（仮想時計がない）、R9（レイアウトの丸め）、R11（色の量子化）、R13（システムフォント）、R14（参照データは macOS で生成）、R15（参照生成器の時計パッチ）、R17（rem は 16 固定）。
- 静止ケースはすべて Light / Dark の両方で比べる（Tooltip のサイズ違いを除く）。テーマを FluentTheme の上に重ねても同じ見た目になることも確かめている。
- テーマの既定フォントはシステム UI フォント。検証は同梱の Inter で行っている（R13）。
- NativeAOT で publish したギャラリー（`samples/AvaloniaUIKit.AotSmoke`）で、DataGrid と Dock を除く全コントロール（新規実装、ADR 30 のコントロールと添付プロパティ、タブの操作を含む）が警告なしにビルドでき、描画できることを確かめている。
- テーマで書けない見た目と動きは、見た目だけを動かす Behavior と値変換で補った（ADR 15、ADR 17）。一覧は [テストと一致検証](../testing.md#見た目だけのコード) にある。
- 既存のコントロールにない機能は、アプリが設定したときだけ挙動を変える添付プロパティと、新しいコントロールで足した（ADR 30）。一覧は [テストと一致検証](../testing.md#機能を足したコントロールと添付プロパティ) にある。

## 調査対象と判定基準

| 項目 | 基準 |
| --- | --- |
| 移植元 | ワークスペースの **longbridge/gpui-kit**。依頼文の `pgui-kit` はこのリポジトリを指すものとして調査した。 |
| GPUI Kit の固定点 | commit [`2c5162f8c5b0c7fcec066ed53125d304c632bfe2`][gp-commit]。`gpui-component` の manifest は `0.7.0`。リリース版そのものではなく、この checkout を基準とする。初回調査時の `32030edcdfad813c83174cf9fea5cf595a46dc46`（`0.6.1`）から更新した。 |
| コンポーネントの範囲 | [`website/component` の全 79 Markdown ページ][gp-catalog]と、[`crates/component/src/lib.rs` の公開モジュール・再公開][gp-lib]。子部品は親の行にまとめ、資料にない `Breadcrumb`、`Link`、`NativeMenu` なども補足する。 |
| Avalonia の固定点 | 固定点の更新時点の最新安定版 [`12.1.3`][av-release]（初回調査時は `12.1.2`）。同タグのソースと同梱テーマを確認した。11.x や将来版への対応はこの資料では約束しない。 |
| 外観の基準 | GPUI Kit の [`Default Light` / `Default Dark`][gp-default-theme]、サイズ定義、各コンポーネントの描画、モーション定義。全テーマ JSON の読み込み機能や Rust API の互換層は対象外。 |

本資料では「標準」を次のように区別する。公式の別パッケージも対象に含める前提で整理しているが、本体同梱と混同しない。

| 分類 | この移植での扱い |
| --- | --- |
| Avalonia 本体 | `Avalonia.Controls` と標準の描画・レイアウト・アニメーション機構。テーマの基本対象。`Primitives`、`Notifications`、`Chrome` 名前空間の公開型も含む。 |
| 公式の別パッケージ | `Avalonia.Controls.ColorPicker`、`Avalonia.Controls.DataGrid`。既存コントロールへのテーマ適用は可能だが、追加参照が必要なため別表に記載する。 |
| サードパーティのライブラリ | Dock.Avalonia。本体が依存しない別パッケージ（`AvaloniaUIKit.Dock`）で、ライブラリの 1 つの版に固定してテーマを付ける（2026-10-05 に追加。ADR 28）。ライブラリのテンプレートの約束に GPUI の部品を置けないところだけ、パッケージにコントロールを足す（ADR 29）。Tabalonia のテーマ（`AvaloniaUIKit.Tabalonia`）も同じ日に作ったが、ページに並べたタブバーの間でタブが移る不具合が Tabalonia の設計にあったので、タブの操作を本体の添付プロパティにして廃止した（ADR 33）。 |
| 本体・上記のパッケージにないもの | 原則として非対応。見た目が中心で小さく作れるものだけ、新規実装として新しいコントロールを作る（ADR 19）。既存のコントロールが持たない機能は、添付プロパティと新しいコントロールで足す（2026-10-05 に追加。ADR 30）。FluentAvalonia、上記以外の第三者製コントロール、Avalonia Pro の Charts / TreeDataGrid / Markdown 等を導入して対象を広げない。公式サイトに紹介ページがあることだけでは、本体標準とは判定しない。 |

判定は **対応**、**部分対応**、**新規実装**、**非対応** の四つ。前二つは既存のコントロールのテーマ、新規実装は新しいコントロール（2026-10-04 に追加。ADR 19）、最後が対象外となる。

- `Style` は既存プロパティと状態ごとの外観を、`ControlTheme` は型ごとの外観一式を、`ControlTemplate` はコントロール内部の表示構造を変更するものとして使う。
- テンプレート内の `Border`、`Path`、`TextBlock` 等の利用は可能。既存の状態・プロパティ・テンプレート部品に結び付ける。
- 見た目だけを変えるコードは使える（2026-10-03 に改定。ADR 15）。
  - 使えるもの: 状態を持たない値変換（Converter）と、既存のコントロールの状態やイベントを読んで見た目のプロパティだけを動かす添付プロパティ型の Behavior。どちらもテーマのスタイルから適用し、アプリはテーマを追加するだけで使える。
  - 作らないもの: 新規コントロール（2026-10-04 から、小さなコンポーネントに限って新規実装として作る。ADR 19）、コントロールの論理的な状態や操作を変える処理（ポップアップを閉じるのを遅らせる等）、選択・検索・データ管理の機能。後の二つは、2026-10-05 から、アプリが設定する添付プロパティと新しいコントロールでは作る（ADR 30。下の項目）。
  - 条件: NativeAOT で動き、リフレクションを使わず、参照データとの比較テストで検証する。
  - 改定前は「独自のアニメーション処理を作らない」としていたため、ばねの途中反転やスクロール中だけ表示するスクロールバーを対象外にしていた。各行の「対象外」のうち、この条件で表せる見た目・動きは対象に戻す。
- 既存のコントロールにない機能は、添付プロパティと新しいコントロールとして足せる（2026-10-05 に追加。ADR 30）。
  - 既存のコントロールにそのまま足せる機能は、そのコントロールに付ける添付プロパティにし、アプリが設定したときだけ挙動を変える（`uikit:Buttons`、`uikit:Inputs` など）。テンプレートに差し込み口を足すだけのものは、値を置く添付プロパティとテーマのテンプレートにする（`uikit:GroupBoxes.Footer` など）。
  - 構成や操作が既存のコントロールと違うものは、新しいコントロールにする（`uikit:Select`、`uikit:ListView` など）。中の Button、TextBox、ListBox、Popup などはテーマ済みの標準のコントロールを使う。
  - 範囲外: テキスト編集エンジンが要るもの（インライントークン、複数カーソル、TextBox の中の検索）、OS の機能が要るもの（自動入力のヒント、OS の通知）、SVG とアニメーション画像の読み込み、keymap からの Action の解決、DataTable の機能、ADR 19 で範囲外にしたもの。
- `Border` や `ItemsControl` を組み合わせれば描ける、という理由だけで標準に存在すると判定しない。`Badge` や `Breadcrumb` のような専用の構成は、既存のコントロールの対応ではなく、新規実装として扱う。
- アプリによる通常のデータバインディング、コマンド、内容の指定は必要。テーマがアプリのデータ取得や業務処理を代行するものではない。

根拠: [Avalonia の ControlTheme][av-doc-themes]、[本体のコントロール実装][av-controls]、[Fluent の標準テンプレート][av-fluent]。

## Avalonia 本体に対応するもの

### ボタン・入力

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Button][gp-button] | 対応 | [`Button`][av-button]、[`DropDownButton`][av-drop-down-button]、`uikit:Buttons` | 色違い、outline / ghost / link 風の表示、サイズ、アイコンを含む内容、hover / pressed / focus / disabled。`dropdown_caret` のボタンは `DropDownButton` のテーマで描く（Button のクラスがすべて効き、開いている間は selected の見た目）。Custom variant は `Background` などの指定とスタイルで、`toggled`（支援技術に伝える押下状態）は `ToggleButton` で表せる。loading は添付プロパティ `uikit:Buttons.IsLoading`（ADR 30）で足す。押下・Enter・Space・アクセスキー・`IsDefault` / `IsCancel` で Click と Command を出さず、無効の見た目にはせずに 80% にフェードし、アイコンをスピナーにする（`LoadingIcon`）。Avalonia が Click の前に行う ToggleButton の切り替えと Flyout の表示は、アクセスキー・`IsDefault`・`IsCancel` では止まらない。マウスの押下でフォーカスを奪わない GPUI の動作は `uikit:Buttons.TakesFocusOnPointer="False"` で選ぶ（既定は Avalonia のまま。R27）。 |
| ButtonGroup（Button の子部品） | 部分対応 | `Button`、標準 Panel、`uikit:ButtonGroup` | テーマ: 各ボタンと、通常のレイアウトで隣接させたときの外観（`StackPanel Classes="button-group"`）。`uikit:ButtonGroup`（ADR 30。`StackPanel` の派生）は、グループの色・`outline`・`compact`・サイズのクラスを全ボタンに渡し、クリックで `Click`（`SelectedIndices`。単一選択は押したボタン、`Multiple` は押したボタンを足す・外す）を出す。選択はアプリが各ボタンの `selected` クラスに反映する（GPUI もアプリが描き直す）。GPUI のグループの on_click は子の on_click を置き換えるが、ここでは子の `Click` / `Command` も動き、キー操作のクリックも `Click` を出す。 |
| [DropdownButton][gp-dropdown_button] | 対応 | [`SplitButton`][av-split-button] | 主操作とメニューを開く操作が独立した二分割ボタン。名称の似た `DropDownButton` は全体がメニュー起動ボタンなので対応先を分ける（Button の `dropdown_caret` の対応先）。アクションの半分だけの loading（GPUI の `DropdownButton::button(Button::loading(true))`）は `uikit:Buttons.IsLoading`（ADR 30）で足し、メニューの半分は開ける。 |
| [Toggle][gp-toggle] | 対応 | [`ToggleButton`][av-toggle-button] | checked / unchecked / disabled、アイコン、枠線、サイズ。 |
| ToggleGroup（Toggle の子部品） | 部分対応 | [`ListBox`][av-listbox] / `ListBoxItem`、`uikit:ToggleGroup` | テーマ: 標準の選択を使うボタン状の項目表示。`SelectionMode="Multiple,Toggle"` で GPUI と同じく各項目が独立に切り替わる。ListBox は矢印キーで項目を移るので、各トグルが Tab 停止で矢印キーでは何もしない GPUI のキー操作は `uikit:ToggleGroup`（ADR 30。`ToggleButton` を並べる `StackPanel` の派生、`Click` の `Checked`）で足す。Space / Enter でもトグルが切り替わり `Click` を出す（GPUI のグループはポインターのクリックだけを on_click に渡す）。 |
| [Checkbox][gp-checkbox] | 対応 | [`CheckBox`][av-checkbox] | チェック記号、ラベル、枠、状態表示。標準の `IsThreeState` / `IsChecked` を使用する。 |
| [Radio / RadioGroup][gp-radio] | 対応 | [`RadioButton`][av-radio] | 円形マークとラベル。グループ選択は標準の `GroupName` に従い、専用 RadioGroup 型は追加しない。 |
| [Switch][gp-switch] | 対応 | [`ToggleSwitch`][av-switch] | トラック、つまみ、ラベル、checked / disabled、フォーカス表示、つまみの移動。つまみの移動は GPUI と同じばねの式で動かし、途中で反転しても速度を引き継ぐ（`Motion.Spring`）。 |
| [Input][gp-input] | 部分対応 | [`TextBox`][av-textbox]、[`MaskedTextBox`][av-masked-textbox]、`uikit:Inputs` | テーマ: 枠、placeholder、選択色、キャレット、read-only / disabled、`PasswordChar` / `RevealPassword`、左右の内容領域。GPUI の `cleanable` は `clearButton` クラス（Fluent と同じクラス名。単一行・編集可能・空でないときだけ出す）。右クリックメニュー（Cut / Copy / Paste / Select All とショートカット、GPUI の有効条件）はテーマが既定で付ける。見た目は GPUI が Linux で描くメニューで、macOS と Windows の GPUI は OS のメニューを出す。右クリックでキャレットをポインターの位置へ移す動作は Avalonia のまま。`MaskedTextBox` の入力マスクは Avalonia の書式に従う。GPUI の検証（`pattern` / `validate`。編集後の全文で判定）、マスク構文（`9` `A` `#` `*` と数値のマスク）、Esc でのクリアは添付プロパティ `uikit:Inputs`（ADR 30）で足す。IME の未確定の文字は確定したときに検証する。インライントークンと自動入力のヒント（`content_type`）は追加しない（ADR 30 で範囲外）。 |
| [Textarea][gp-textarea] | 部分対応 | `TextBox`、`uikit:Inputs` | `AcceptsReturn`、`TextWrapping`、`MinLines` / `MaxLines` による複数行・高さ制限、スクロール部分、右クリックメニューをテーマ化。Tab / Shift+Tab と Cmd（macOS 以外は Ctrl）+ ] / [ による行のインデントは `uikit:Inputs.TabSize` / `HardTabs`（ADR 30）で足す。検索 UI、インライントークン、複数カーソルなど GPUI の編集エンジンの機能は対象外（ADR 30 で範囲外）。 |
| [InputGroup][gp-input-group] | 部分対応 | `TextBox` の `InnerLeftContent` / `InnerRightContent`、`uikit:InputGroup` / `uikit:InputGroupAddon` | テーマ（`TextBox Classes="group"`）: 入力欄の前後に置く文字・アイコン・ボタンと入力欄を一つの枠で囲む外観。複数行は `AcceptsReturn` の `TextBox` を使い、無効・read-only・検証エラーは標準の状態で表示する。`uikit:InputGroup`（ADR 30）は同じ見た目で、入力行の上下の行（`InputGroupAddon` の `Alignment` が `BlockStart` / `BlockEnd`）、入力にフォーカスがあるときだけのリング、`IsInvalid`、入力の無効化によるグループ全体の無効化、`IsReadOnly`、アドオンの押下で入力にフォーカスすることを足す。中の Button の loading は `uikit:Buttons.IsLoading`、押しても入力のフォーカスを残すのは `TakesFocusOnPointer`。アドオンの muted の文字（`InputGroupText`）の型と、アドオンのボタンの色違いは追加しない。 |
| [NumberInput][gp-number-input] | 対応 | [`NumericUpDown`][av-number-input]、`ButtonSpinner`、`uikit:NumberInput` | 数値入力、増減ボタン、書式、最小値・最大値・刻み幅という既存機能の外観。GPUI の prefix / suffix は `InnerLeftContent` / `InnerRightContent`。入力中の文字の制限と正規化（全角の数字を ASCII にする）、値と刻みの小数の桁を保つ刻み、値によって変わる刻み（`StepBy`）、値をアプリが決める刻み（`StepsValue="False"` と `Step`）は `uikit:NumberInput`（ADR 30。`NumericUpDown` の派生で同じテーマ）で足す。 |
| [Slider][gp-slider] | 部分対応 | [`Slider`][av-slider]、`Track`、`Thumb`、`TickBar`、`uikit:RangeSlider` | 単一値、縦横、トラック、つまみ、目盛りと状態表示。二つのつまみを持つ範囲と対数スケールは `uikit:RangeSlider`（ADR 30）で足す。トラックの押下は近いほうのつまみを動かし、つまみはもう一方のつまみで止まる。Avalonia の `Slider` は値と位置の対応が線形なので、単一値の対数スライダーも `RangeSlider`（`IsRange="False"`）にする。キー操作は GPUI の Slider にもない。 |

### 選択・一覧・日付・表

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Select][gp-select] | 部分対応 | [`ComboBox`][av-combobox] / `ComboBoxItem`、`uikit:Select` | テーマ: 単一選択の閉じた欄、キャレット、候補リスト、選択・無効状態。`IsEditable` の ComboBox は、Select の枠の中の枠なしの入力欄として描く。GPUI の `cleanable` は `clearButton` クラス（値があるときキャレットの位置にクリアボタン）。ポップアップ内の検索欄、グループ（`uikit:SelectGroup`）、`title_prefix`、アイコン、メニューの幅、空の表示、`appearance(false)`（`plain` クラス）、変更の取り消し（on_will_change。`SelectionChanging`）は `uikit:Select`（ADR 30）で足す。見た目はテーマ済みの ComboBox と同じ。無効な行はクリックでも Enter でも選べない（GPUI は見た目だけ無効で、確定できる）。 |
| [Combobox][gp-combobox] | 部分対応 | `ComboBox`、[`AutoCompleteBox`][av-autocomplete]、`uikit:Select Classes="combobox"` | テーマ: 単一選択は `ComboBox`（`combobox` クラス）、入力による標準の候補絞り込みは `AutoCompleteBox` か `IsEditable` の `ComboBox`。GPUI の trigger と別の検索欄という構成、複数選択（トリガーは GPUI と同じく選んだ名前を ", " でつなぐ）、footer、トリガーのテンプレート（`render_trigger`）は `uikit:Select` の `combobox` クラス（ADR 30）で足す。選択済みのチップは GPUI の既定の表示ではなく `render_trigger` でアプリが描くもので、`TriggerTemplate` で同じことができる。チェックの差し替え（`check_icon`）は追加しない。GPUI の Combobox は閉じてもクエリとカーソルを残すが、`uikit:Select` は Select と同じく閉じるたびに戻す。 |
| [List][gp-list] | 部分対応 | `ListBox` / `ListBoxItem`、`uikit:ListView` / `uikit:ListItem` / `uikit:ListSection` | テーマ: 行の色・余白・選択・hover、アプリの `ItemTemplate` に配置する内容。セクションと見出し・末尾、クリックと Enter での確定（`Confirmed`）、Esc での解除（`Cancelled`）、端で折り返す ↑↓、右クリックした行の枠、`selectable(false)`、検索欄、読み込み中のスケルトン、空の表示、`scroll_to_item`、追加取得（`HasMore` / `LoadMore`）は `uikit:ListView`（ADR 30）で足す。行は仮想化する。`render_initial`、検索中の 100ms のスピナー、区切りの項目（`ListItem::separator`、`ListSeparatorItem`）は追加しない。並べ替えの D&D は GPUI の List の機能ではない（ストーリーが行に付けている）。 |
| [Tree][gp-tree] | 部分対応 | [`TreeView`][av-tree] / `TreeViewItem`、`uikit:Tree` / `uikit:TreeItem` | テーマ: 階層行、インデント、展開マーク、選択・無効表示。GPUI と同じく見えている木を行に平坦化して仮想化すること、押下での選択と開閉、↑↓ と → ←、Enter での開閉、右クリックした行の枠、祖先を開いて見せる `reveal_item` は `uikit:Tree`（ADR 30）で足す。項目は `uikit:TreeItem` か、任意のデータに子を返す関数かテンプレート（`ChildrenSelector`、`TreeDataTemplate`）。`context_menu` の builder は Avalonia の `ContextMenu` と `RightClickedItem` で組む。 |
| [Calendar][gp-calendar] | 部分対応 | [`Calendar`][av-calendar]、`CalendarItem`、`CalendarButton`、`CalendarDayButton`、`uikit:CalendarView` | テーマ: 月・年・年代の表示、日セル、今日・選択・無効日。範囲選択は標準 `SelectionMode` の範囲で対応し、期間で無効にする日（GPUI の `RangeMatcher`）は `BlackoutDates` で表せる。複数月の表示、月ごとに 4〜6 行の週、月と年の別々のトグルと 20 年の年グリッド、GPUI の規則による日付の範囲の選択、曜日と関数で無効にする日（`Matcher::DayOfWeek` / `Custom`）は `uikit:CalendarView`（ADR 30）で足す。見た目はテーマ済みの Calendar と同じ。年の範囲は今日の前後 50 年に固定し、`set_year_range` は追加しない。キー操作は GPUI の Calendar にもない。 |
| [DatePicker][gp-date-picker] | 部分対応 | [`CalendarDatePicker`][av-date-picker]、`uikit:DateField` | テーマ: 欄とポップアップ内の Calendar。カレンダーを開く UI なので、ホイール状の標準 `DatePicker` よりこちらが対応する。開始・終了の二日付を持つ範囲、日付のプリセット、`time_precision` による時刻の同時編集、クリアボタン、複数月の表示、Enter と Esc での開閉は `uikit:DateField`（ADR 30。中は `uikit:CalendarView`）で足す。見た目はテーマ済みの CalendarDatePicker と同じ。時刻付きのプリセット、`set_year_range`、ポップアップの外でボタンを離したときに閉じる動作（Avalonia は押下で閉じる）は追加しない。 |
| [TimeField][gp-time-field] | 部分対応 | [`TimePicker`][av-timepicker]、`uikit:TimeField` | テーマ: 時・分・秒・AM/PM を区切った欄、`ClockIdentifier` による 12 / 24 時間制、`UseSeconds` による秒の表示、無効・検証エラーの表示。`TimePicker` の値は標準のポップアップ（`TimePickerPresenter`）で選ぶ。欄内のセグメントを直接選び、数字入力や上下キーで変える GPUI の編集操作は `uikit:TimeField`（ADR 30。GPUI の SegmentEditor の移植）で足す。見た目はテーマ済みの TimePicker の欄と同じ。値のない欄（`Time` が null）は Avalonia だけのもの。 |
| [Table][gp-table] | 部分対応 | [`TableView`][av-tableview]、`TableViewColumnHeader`、`TableViewRow`、`TableViewCell`、`uikit:Table` | テーマ: ヘッダー、セル、罫線、外枠、行の余白。行がないときはヘッダーだけを描く（GPUI の Table に空の表示はない）。`TableView` は Avalonia 12.1 からの本体標準。GPUI の宣言的な部品（`TableHeader`、`TableBody`、`TableFooter`、`TableRow`、`TableHead`、`TableCell`、`TableCaption`）、列の結合（`col_span`）、GPUI の flex の規則による列幅は `uikit:Table`（ADR 30）で足す。行の hover と選択は GPUI の Table にもない。 |
| [DataTable][gp-data-table] | 部分対応 | `TableView` | 表示専用の表、行選択、標準の行仮想化、列幅の調整、行がないときの空の表示（GPUI の `render_empty`）までを利用。ソート、列の移動・固定・最小幅・最大幅、列とセルの選択（GPUI の [`TableSelection`][gp-table-state-source] は 1 つのセルを選び、範囲は選ばない）、多段の列見出し、読み込み中の表示、無限読み込みは `TableView` に追加しない（DataTable の機能は規模が大きいので ADR 30 でも範囲外）。公式別パッケージを使う場合は後述。 |
| [VirtualList][gp-virtual-list] | 部分対応 | `ListBox`、[`VirtualizingStackPanel`][av-virtualizing-stack-panel]、`ItemsScrolling` | 標準の仮想化を維持して行をテーマ化。GPUI の VirtualList は縦か横の一方向だけを仮想化するので、標準の仮想化と同じ範囲になる（行と列の二方向は DataTable の仮想化）。スクロールハンドルの `scroll_to_item` のうち、ListBox の `ScrollIntoView` にない Center は、`ItemsScrolling.ScrollToItem(index, ScrollStrategy)`（ADR 30。`ItemsControl` の拡張メソッド）で足す。 |

### ナビゲーション・レイアウト

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Tabs / TabBar / Tab][gp-tabs] | 部分対応 | [`TabControl`][av-tabs] / `TabItem`、[`TabStrip`][av-tabstrip] / `TabStripItem`、`uikit:Tabs.Prefix` / `Suffix`、`uikit:Tabs.Closable` ほか（ADR 33） | タブと内容を持つ場合は `TabControl`、選択列だけなら `TabStrip`。下線・pill・segment 等の外観。選択に付いていくインジケーターは、見た目だけを動かす Behavior（`Tabs.Indicator`）で GPUI と同じばねで動かす（2026-10-03 の方針改定後）。溢れたタブの横スクロールと、タブの一覧を開く `menu` クラス（GPUI の `menu(true)`。ボタンは `uikit:TabsMenuButton`）はテーマで描き、バーの prefix と suffix は `uikit:Tabs.Prefix` / `Suffix`（ADR 30）で置く。タブを閉じる操作と D&D は TabBar / Tab の機能ではなく、GPUI では Dock の [`TabPanel`][gp-tab-panel-source] が持つ。GPUI のドキュメントは閉じるボタンをタブの suffix に置く例だけを示す。その閉じるボタン（tabs story の closable）と、追加ボタン、ドラッグでの並べ替え、同じグループのバーの間の移動、ウィンドウへの切り離しは `uikit:Tabs.Closable` / `NewTabFactory` / `Reorderable` / `DragGroup` / `DetachedWindowFactory`（ADR 33）で足す。ドラッグ中はその場で並べ替え、GPUI の Dock の挿入線とプレビューは描かない。 |
| [Accordion / AccordionItem][gp-accordion] | 部分対応 | [`Expander`][av-expander]、`uikit:Accordion` | テーマ: 個々の開閉項目、見出し、境界線、矢印。複数の Expander は独立に開閉する。GPUI の既定（`multiple(false)`）の 1 項目だけを開く制御、`Multiple`、`ToggleClick`、`bordered`（`IsBordered`）は `uikit:Accordion`（ADR 30。項目は Expander のまま）で足す。閉じる項目も開く項目と同じフレームからばねで動く。 |
| [Collapsible][gp-collapsible] | 部分対応 | `Expander` | 一つの領域を展開・折り畳みする表示。GPUI の自然高を測定した可逆なばねアニメーション（`MotionReveal`）は、内容を自然な高さで測る `Canvas` と表示専用の値変換（`RevealConverters`）、`Motion.Spring` で再現する（2026-10-03 の方針改定後）。Accordion の各項目も同じ。 |
| [Carousel][gp-carousel] | 部分対応 | [`Carousel`][av-carousel]、`PipsPager`、`uikit:Carousels.TracksPointer`、`uikit:CarouselPrevious` / `uikit:CarouselNext` | テーマ: ページ表示と `PageTransition`、標準の `IsSwipeEnabled` / `ViewportFraction` / `WrapSelection` を使用。ページ送りは `PageSlide` の派生（`uikit:SpringSlide`）で、GPUI と同じく 2 ページを 16px 離してばねで動かす。隣接ページの表示とループも 12.1.3 の既存機能内で扱う。ページを 16px 間隔で並べたトラックをばねで動かす GPUI の動き（複数ページ先へは間のページを通って動く、途中で向きを変える、ループの折り返し）と、マウス・ペン・タッチのドラッグ、ホイールのノッチ、トラックパッドのスクロールとスナップは、添付プロパティ `uikit:Carousels.TracksPointer`（ADR 30。テーマが ItemsPanel を `uikit:CarouselTrack` にする）で足す。前後のボタンは `uikit:CarouselPrevious` / `uikit:CarouselNext`。トラックは `ViewportFraction` と `IsSwipeEnabled` を使わず、1 ページに複数のアイテムを並べる GPUI の表示は扱わない。 |
| [Pagination][gp-pagination] | 部分対応 | [`PipsPager`][av-pips-pager]、`uikit:Pagination` | テーマ: 前後ボタン、選択状態、ページ項目。`TemplateSettings.Pips` に 1 始まりの番号があるので、点を番号表示へ差し替えられる。省略記号付きの番号生成（`visible_pages`）と、省略記号から隠れたページを選ぶメニューは `uikit:Pagination`（ADR 30）で足す。見た目は PipsPager のテーマと同じ。サーバーのページ取得はアプリが行う。 |
| [GroupBox][gp-group-box] | 対応 | [`GroupBox`][av-groupbox]、`uikit:GroupBoxes.Footer` | 見出し、内容、背景、outline / fill、余白・角丸。枠の外に置く GPUI の `footer` は、テンプレートに置く添付プロパティ `uikit:GroupBoxes.Footer`（ADR 30）で足す。 |
| [Resizable][gp-resizable] | 部分対応 | [`GridSplitter`][av-grid-splitter] と `Grid`、`uikit:ResizablePanelGroup` / `uikit:ResizablePanel` | テーマ: 通常の行・列分割、仕切りの太さ・色・hover・ドラッグ表示。パネルの登録（`Size`、`MinSize` / `MaxSize`、`IsVisible`）、GPUI の flex の規則による最初のレイアウト、隣のパネルを順に縮める連鎖のリサイズ、`ResizePanel`、`Resized`、入れ子は `uikit:ResizablePanelGroup`（ADR 30。ハンドルは GridSplitter の派生で同じテーマ）で足す。外部の状態を共有する `with_state` と `adopt_sizes` は追加しない。 |
| [Sidebar][gp-sidebar] | 部分対応 | [`SplitView`][av-splitview]、[`DrawerPage`][av-drawer-page]、`uikit:Sidebar` と部品 | テーマ: 側面の領域、境界線、展開・縮小・overlay。`DrawerPage` なら標準の header / footer 領域も使用できる。SidebarHeader / SidebarFooter / SidebarGroup / SidebarMenu / SidebarMenuItem、アイコンへの折り畳みと右のツールチップ、入れ子の項目、offcanvas で 200ms 後に内容を隠すことは `uikit:Sidebar`（ADR 30）で足す。単独でも `SplitView.Pane` の中でも使え、`Pane` では SplitView の開閉に従う。項目の仮想化（GPUI の `list()`）は追加しない。途中で戻したときに GPUI が前の目標へ飛ぶ動きは描かない（Avalonia の Transition は今の値から動く）。 |
| [Sheet][gp-sheet] | 部分対応 | `DrawerPage`、`uikit:Sheet` | テーマ（`DrawerPage Classes="sheet"`）: 左右上下の引き出し、背景の暗転、標準の開閉・外側クリック・Esc とその外観。GPUI と同じくコードからウィンドウの root layer に開く使い方（`Show(visual)`。ウィンドウに 1 つ、Tab の循環、閉じたら元の要素にフォーカスを戻す、`overlay_closable`）は `uikit:Sheet`（ADR 30）で足す。GPUI `2c5162f` は sheet を 1 つだけ持ち（[`Root`][gp-root-source] の `active_sheet`）、[`resizable`][gp-sheet-source] は保存するだけで使わないので、複数の sheet とドラッグでのサイズ変更は GPUI にもない。`SplitView` 単体と `DrawerPage` の機能を混同しない。 |
| [Toolbar][gp-toolbar] | 部分対応 | [`CommandBar`][av-commandbar]、`CommandBarButton`、`CommandBarToggleButton`、`CommandBarSeparator`、`uikit:Toolbar` | テーマ: 背景・枠のない横並びのコマンド列、アイコン・ラベル付きの ghost 風ボタン、区切り、`Content` 領域の文字・任意の内容、hover / pressed / checked、高さ・余白のサイズ。コマンド列には標準の `ICommandBarElement` だけを置く。矢印キーでの移動は標準の `KeyboardNavigation` / `XYFocus` の設定で表せる範囲に限る。GPUI にないオーバーフローは `OverflowButtonVisibility` で隠す。任意の要素、伸縮スペーサー（`uikit:ToolbarSpacer`）、`ToolbarGroup`、子へのサイズの受け渡し（`uikit:Toolbar.TakesSize`）、端で折り返す ← → のフォーカス移動は `uikit:Toolbar`（ADR 30）で足す。矢印キーの移動だけを止める GPUI の `disabled` は追加しない。 |
| [Scrollable / Scrollbar][gp-scrollable] | 対応 | [`ScrollViewer`][av-scrollviewer]、[`ScrollBar`][av-scrollbar]、`Thumb` | トラック、つまみ、余白、表示条件と遷移。Always / Hover / Scrolling の 3 モード（`AllowAutoHide` と `uikit:Scrollbars.ShowOnHover`）。スクロール後の表示と idle 時間は見た目だけを動かす Behavior（`Scrollbars`）で再現する。 |
| [Separator][gp-separator-source]（公開モジュール） | 対応 | [`Separator`][av-separator]、`uikit:Separators.Label` | 線の色・太さ・余白。メニュー中の区切りも同様。GPUI の `label` は添付プロパティ `uikit:Separators.Label`（ADR 30）で、横の線では末尾、縦の線では中央に置く。 |

### テキスト・画像・フィードバック・メニュー

| GPUI Kit | 判定 | Avalonia の対応先 | テーマで移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Label][gp-label] | 部分対応 | [`TextBlock`][av-textblock]、`SelectableTextBlock`、[`Label`][av-label]、`uikit:TextLabel` | テーマ: 文字サイズ、色、行間、折り返し、配置。GPUI Label は表示テキストが中心なので、単純に同名型へ寄せない。補足の文字（`secondary`）、検索一致の強調（`highlights`。全体か先頭）、マスクは `uikit:TextLabel`（ADR 30。`TextBlock` の派生。Avalonia の `Label` と同名を避けた）で足す。補足の上の一致の色は、GPUI ではハッシュの順で決まるが、TextLabel は常に一致の色にする。 |
| [Link][gp-link-source]（公開モジュール） | 対応 | [`HyperlinkButton`][av-hyperlink] | リンクの色・装飾・hover・pressed。遷移は標準の `NavigateUri` / コマンドに従う。 |
| [Icon][gp-icon] | 部分対応 | [`PathIcon`][av-pathicon]、`Path` / `DrawingImage`、`uikit:Icon` | テンプレートで必要なアイコン形状・色・線幅・サイズをリソース化。Lucide は別のアセットであり、Avalonia に同梱されているとは扱わない。GPUI Kit の共有の `IconName`（`gpui_kit::assets`）の 1830 個（Lucide 1.43.0 の 1818 個と GPUI Kit 独自の 12 個）を `IconName` とし、`UIKit.Icon.*` のジオメトリで描く。テーマはコンポーネントの `IconName` の 106 個に、テーマが使うものを合わせた 109 個を同梱し、残りはパッケージのソースジェネレーターが、アプリの C# と XAML が名前を書いたものだけをアプリに足す（ADR 38。GPUI Kit の既定の `Assets` と `AllAssets` の関係に当たる）。`IconName` で選ぶ API は `uikit:Icon`（ADR 30。`Kind` を持つ PathIcon の派生で、PathIcon のテーマとサイズのクラスが効く）で足す。任意 SVG の読み込み器は追加しない（ADR 30 で範囲外）。サイズを指定しないアイコンは 16px で、GPUI と違い文字の大きさに従わない。 |
| [Image][gp-image] | 部分対応 | [`Image`][av-image]、`uikit:AsyncImage` | 標準が読める画像の配置、拡縮、周囲の余白。`Image` 自体はテンプレートを持たないので `Style` を使う。URL（http / https / file / avares）の非同期の取得、200ms を過ぎても終わらないときの読み込み中の内容、失敗時の内容は `uikit:AsyncImage`（ADR 30。GPUI の `img()`。取得は `IImageLoader`）で足す。SVG とアニメーション画像（ADR 30 で範囲外）、`grayscale` は移植しない。 |
| [Progress][gp-progress] | 対応 | [`ProgressBar`][av-progress] | 横・縦のバー、トラック、確定値／不定値、色・角丸・値の変化。元コントロールの値と範囲の意味を維持する。 |
| [ProgressCircle][gp-progress-circle-source]（Progress の子部品） | 部分対応 | `ProgressBar` の専用テンプレート、[`Arc`][av-arc] | 進捗の値・範囲・不定値は既存の ProgressBar に任せ、円形の描画を差し替える。不定値は標準の Animation で表現可能。確定値は `Percentage × 3.6` を `SweepAngle` に渡す表示専用の値変換（`AffineConverter`。2026-10-03 の方針改定で許可）を使う。新しい進捗管理型は追加しない。円の中央の内容（GPUI の children）は添付プロパティ `uikit:ProgressCircles.Content`（ADR 30）で置く。 |
| [Spinner][gp-spinner] | 対応 | `ProgressBar`（`IsIndeterminate=true`）の専用テーマ、`uikit:Spinners.Icon` | 回転するローディング記号として表現する。Avalonia の同名 `Spinner` / `ButtonSpinner` は数値の増減用であり、ローディングの対応先ではない。回すアイコン（GPUI の `icon`）は添付プロパティ `uikit:Spinners.Icon`（ADR 30）で変える。GPUI の回転は 0.8 秒に固定され、速さを変える公開 API はない。easing の変更（`ease()`）は追加しない。 |
| [Tooltip][gp-tooltip] | 対応 | [`ToolTip`][av-tooltip] | 背景、枠、影、文字、余白、標準の表示遅延。GPUI の Action からキー表示を自動解決する機能は移植しない（ADR 30 でも範囲外）。閉じる前の猶予と隣への切り替えスライドは描かない（動きの差。R21）。 |
| [Popover][gp-popover] | 部分対応 | [`Flyout`][av-flyout] / `FlyoutPresenter` | アンカーに対する配置、`HorizontalOffset` / `VerticalOffset` による間隔、内容、枠・影、開くときの表示。論理的な開閉は標準 Flyout が担当。配置した辺に合わせて描く矢印（`arrow` クラス）は、`Placement` から間隔と矢印の位置を求める値変換（`PlacementConverter`）で描く。閉じるアニメーションを待って Popup を破棄する制御は追加しない（動きの差）。 |
| [Menu / ContextMenu / DropdownMenu][gp-menu] | 対応 | [`ContextMenu`][av-contextmenu]、[`MenuFlyout`][av-menu-flyout]、[`MenuItem`][av-menu-item] | 項目、チェック、サブメニュー、アイコン、ショートカット表示、区切り、hover / disabled。クリック型トリガーには標準 `DropDownButton` も使える。ポインターの位置に開くメニューは `FlyoutPresenterClasses="context"` でトリガーとの間隔をなくす（TextBox の右クリックメニューも同じ）。 |
| AppMenuBar（menu の公開型） | 対応 | [`Menu`][av-menu] | アプリ内に描画するメニューバーと項目。OS のネイティブメニューとは分ける。 |
| [Notification][gp-notification] | 部分対応 | [`WindowNotificationManager`][av-notification-manager] / [`NotificationCard`][av-notification-card]、`uikit:NotificationList` / `uikit:NotificationItem` | テーマ: アプリ内通知の色、アイコン、内容、`Position` の 6 種の位置、標準の自動消去と入退場表示。GPUI の NotificationList は `uikit:NotificationList`（ADR 30）で足す。通知ごとの配置（左右中央を含む 8 種。左右中央はスライドせずフェードだけ）と配置ごとのスタック、新しい順に 3 枚まで重ねてポインターかフォーカスがある間は広げる重なり、GPUI と同じばねでの並べ直し、どれかのスタックを広げている間は止まる 5 秒の自動消去、action（あると既定で消えない）、`Id` / `Key` による重複排除と置き換え、`Remove` / `Clear`、`MaxItems`。カードは `uikit:NotificationItem`（`NotificationCard` の派生。Avalonia の `Notification` と同名を避けた）で、見た目と入退場は WindowNotificationManager のカードと同じ。OS の通知への配信は追加しない（ADR 30 で範囲外）。Sheet を開いている間に通知の層をずらす GPUI の処理は持たない。 |
| [TitleBar][gp-title-bar] / WindowBorder | 部分対応 | [`WindowDrawnDecorations`][av-window-decorations]、`Window.WindowDecorationsTheme`、`uikit:TitleBar` | テーマ: Avalonia が描くタイトルバー・枠のテンプレート、標準のキャプションボタン・状態。OS が描く装飾は対象外。実際に描画される部位は OS ごとに確認が必要。アプリがウィンドウの内容の先頭に置く GPUI の TitleBar は `uikit:TitleBar`（ADR 30）で足す。任意の子を両端に分けて並べ、34px の高さ、グラデーション、下の罫線を描く。Windows と macOS ではバーをウィンドウのタイトルバーの領域（`WindowDecorationProperties.ElementRole`）にして OS が移動と最大化を行い、ほかの OS ではバー自身がドラッグでウィンドウを動かし、ダブルクリックで最大化する。Windows と Linux ではウィンドウが許すキャプションボタンを末尾に置き、macOS では信号機の 80px を空ける。Linux の右クリックのウィンドウメニュー（`show_window_menu`）は Avalonia に公開 API がないので追加しない。WindowBorder（影・枠・リサイズ帯）は WindowDrawnDecorations のテーマが描く。 |

## 公式の別パッケージに対応するもの

これらは本体のテーマと依存関係を区別する。本体だけを対象にする場合は、この節のコントロール用テーマを除外できる。ここでは新しいパッケージ分割や配布方式までは決定しない。

| GPUI Kit | 判定 | 公式の対応先 | テーマで移植する範囲／制約 |
| --- | --- | --- | --- |
| [ColorPicker][gp-color-picker] | 部分対応 | [`Avalonia.Controls.ColorPicker`][av-colorpicker] の `ColorPicker` / `ColorView` / `ColorSpectrum` / `ColorSlider` / `ColorPreviewer` | 色見本、ドロップダウン、パレット、数値欄、周辺のボタンをテーマ化できる。枠付きの欄として描く `ColorSelect` は、同パッケージの `ColorToHexConverter` で 16 進値を表示するテンプレートで表せる。`Color` は未選択を持たないため、その placeholder は扱わない。色のモデル・編集は標準の RGB / HSV 等に従い、このパッケージのテーマには GPUI の HSL 編集 UI や独自パレットを追加しない。本体と対応する版の追加参照が必要。GPUI の ColorPicker と ColorSelect（パレット、featured の行、HSLA のスライダー、16 進の入力、色なしの値と placeholder）は、このパッケージに依存しない本体の `uikit:ColorSelect`（ADR 30。欄は `field` クラス）で足す。 |
| [DataTable][gp-data-table] | 部分対応 | [`Avalonia.Controls.DataGrid` 12.1.2][av-datagrid] | 行・セル・ヘッダー・ソート表示・列リサイズ・列移動・固定列など、DataGrid が持つ機能の外観。行がないときの空の表示、GPUI の `cell_selectable`（`cell-selectable` クラス。DataGrid はクリックで行を選ぶので、選択行の current cell を GPUI の選択したセルとして描く）、`row_header`（`HeadersVisibility="All"`）もテーマで描く。列の選択、多段の列見出し、読み込み中の表示、無限取得、データモデルは追加しない（ADR 30 でも範囲外）。**公式に非推奨**とされているため、新規テーマの基本対応先は `TableView`。DataGrid はこの追加パッケージを利用する場合の対応先として扱う。 |

根拠: [ColorPicker の追加参照と再テンプレート化][av-doc-colorpicker]、[DataGrid の配布条件・非推奨の案内][av-doc-datagrid]。公式の別パッケージがあることと、本体に機能を新設せず使えることは両立するが、必要な依存は明示する。

## 新しいコントロールとして実装するもの

Avalonia に対応するコントロールがないコンポーネントのうち、見た目が中心で小さく作れるものは、新しいコントロールとして `UIKitTheme` に含める（2026-10-04 に決定。ADR 19）。判定は **新規実装**。コントロールはプロパティ・疑似クラス・テンプレートの部品だけを持ち、処理はそのコンポーネント自身の操作に限る。名前は GPUI に合わせ、Avalonia のメンバーとぶつかるものや意味が広すぎるものだけ変える。設定画面の Settings は、ページ・group・項目の構成と検索・選択・リセットを持つ新しいコントロールとして作る（2026-10-09 に決定。ADR 40）。既存のコントロールが持たない機能を足す新しいコントロール（`uikit:Select`、`uikit:ListView` など。2026-10-05 に決定。ADR 30）は、対応する既存のコントロールがあるので、そのコンポーネントの行（[Avalonia 本体に対応するもの](#avalonia-本体に対応するもの)）と [ADR 30 で機能を足したもの](#adr-30-で機能を足したもの) に書く。

| GPUI Kit | 判定 | Avalonia の新しいコントロール | 移植する範囲／対象外 |
| --- | --- | --- | --- |
| [Badge][gp-badge] | 新規実装（第 1 弾） | `Badge`（`ContentControl`） | 内容の右上に重ねる数・点・アイコン、上限、色。 |
| [Tag][gp-tag] | 新規実装（第 1 弾） | `TagLabel`（`Control.Tag` と同名を避ける） | 意味別の色、outline、角丸、サイズ。 |
| [Alert][gp-alert] | 新規実装（第 1 弾） | `Alert` | 種類ごとの色とアイコン、タイトル、banner 表示、閉じるボタン（押したときのイベントだけを出し、隠すのはアプリ）。 |
| [Skeleton][gp-skeleton] | 新規実装（第 1 弾） | `Skeleton` | 読み込み中の代替表示と明滅のアニメーション。 |
| [StatusBar][gp-status-bar] | 新規実装（第 1 弾） | `StatusBar` | 左・中央・右の 3 領域を持つ下部バー。 |
| [Breadcrumb / BreadcrumbItem][gp-breadcrumb-source]（公開モジュール） | 新規実装（第 1 弾） | `Breadcrumb`、`BreadcrumbItem` | 項目と区切り、クリック、無効、現在地。遷移はアプリのコマンドに任せる。 |
| [Kbd][gp-kbd] | 新規実装（第 1 弾） | `Kbd` | キーキャップの表示と GPUI の OS ごとの表記。Action からのキーの解決は対象外。 |
| [Clipboard][gp-clipboard] | 新規実装（第 1 弾） | `Clipboard` | コピーボタンと、コピー後の一時的な完了表示。値はアプリが渡す。 |
| [Rating][gp-rating] | 新規実装（第 1 弾） | `Rating` | 星の表示、hover での予告、クリックでの値の変更、無効、サイズ。 |
| [Avatar / AvatarGroup][gp-avatar] | 新規実装（第 1 弾） | `Avatar`、`AvatarGroup` | 頭文字と名前から決まる色、画像、代わりのアイコン、サイズ、重ねた集合表示と上限。画像の取得は標準の `Image` の範囲。 |
| [Empty][gp-empty] と子部品 | 新規実装（第 1 弾） | `EmptyState` | 空状態の画像・タイトル・説明・操作の配置。 |
| [DescriptionList][gp-description-list] / DescriptionItem | 新規実装（第 2 弾） | `DescriptionList`、`DescriptionItem` | ラベルと値、列数、項目の結合、縦横の配置、枠。 |
| [Stepper][gp-stepper] / StepperItem / StepperTrigger | 新規実装（第 2 弾） | `Stepper`、`StepperItem` | 完了・現在・未到達の表示、区切りの線、縦向き、クリックでの選択。 |
| [Form][gp-form] / Field | 新規実装（第 2 弾） | `Form`、`FormField` | ラベル・説明・必須表示と入力欄の配置、列。検証は標準の `DataValidationErrors` に任せ、フォームの値の管理はしない。 |
| [HoverCard][gp-hover-card] | 新規実装（第 2 弾） | `HoverCard` | hover で開き、カードの上にポインターがある間は開いたままにする開閉と、その遅延。 |
| [Shimmer / ShimmerText][gp-shimmer] | 新規実装（第 2 弾） | `ShimmerText` | 文字の上を流れるハイライトのアニメーション。 |
| [Marker][gp-marker] と子部品 | 新規実装（第 2 弾） | `Marker` | 会話やタイムラインの区切りのアイコン・内容・線、読み込み中の表示。 |
| [Bubble][gp-bubble] と子部品 | 新規実装（第 2 弾） | `Bubble` | メッセージの吹き出しと reaction 領域。 |
| [Message][gp-message] / MessageGroup と子部品 | 新規実装（第 2 弾） | `Message`、`MessageGroup` | アバター・ヘッダー・本文・フッターの配置と左右の寄せ。会話の末尾への追従（MessageScroller）は対象外。 |
| [Settings][gp-settings] / SettingPage / SettingGroup / SettingItem | 新規実装（ADR 40） | `Settings`、`SettingPage`、`SettingGroup`、`SettingItem`、`SettingItemPanel`（項目の配置） | 検索欄付きのサイドバーとページのリサイズ可能な分割、ページのメニューと group の行（`click_to_open`、`default_open` は `IsOpen`）、タイトル・説明・キーワードでの検索と GPUI の選択の規則、group へのスクロール、group の variant（`GroupVariant`、group ごとの `Variant`）、footer、480px 以下のページでの縦並び、項目の縦横の配置と無効、サイズ（項目の中身にサイズのクラスを付ける）。フィールドは GPUI の `SettingField` の型を作らず、標準のコントロールを項目の中身に置く。`DefaultValue` を書くと ToggleButton、TextBox、NumericUpDown、RangeBase、SelectingItemsControl、`uikit:Select` の値を型ごとの分岐で追い（リフレクションなし）、既定と違う間はページにリセットボタンを出し、押すと検索で見えている項目を既定に戻す。ほかの中身は `IsModified` と `Reset` / `ResetCommand`（GPUI の `on_reset`）。 |

## サードパーティのライブラリに対応するもの

Avalonia に対応先のない機能のうち、それを持つ広く使われたライブラリがあるものは、そのライブラリを使う人のためのテーマを別パッケージにする（2026-10-05 に決定。ADR 28）。本体のテーマはライブラリに依存しない。パッケージはライブラリの 1 つの版に固定し、その版のテンプレートを GPUI の見た目に書き換える。ライブラリのテンプレートの約束に GPUI の部品を置けないところだけ、パッケージに新しいコントロールを足す（ADR 29）。

| GPUI Kit | 判定 | ライブラリ | テーマで移植する範囲／制約 |
| --- | --- | --- | --- |
| [Dock][gp-dock] / DockArea / Panel / TabPanel | 部分対応 | [Dock.Avalonia][td-dock] 12.1.0.6 の `DockControl` と各部品（`AvaloniaUIKit.Dock`） | タブバーとツールバーのメニュー、1 つだけのパネルのタイトルバー、場所を取らない分割のハンドル、ドラッグ中のドロップ先（GPUI と同じ 35% / 30% / 35% の区分）とプレビュー、閉じるボタン。ドッキング、浮いたウィンドウ、ピン留め、配置の保存・復元は Dock.Avalonia のもの。Dock.Avalonia が trim 非対応なので NativeAOT は保証しない。 |

## 対応する標準コンポーネントがないもの

以下は非対応。モーダル、コマンドパレット、編集エンジン、可視化などの仕組みそのものが要り、新しいコントロールを足しても小さく収まらない（ADR 19。ADR 30 でも範囲外）。対応済みのボタン・文字等をアプリ側で使った結果として外観の一部が揃うことは、ここでのコンポーネント対応には数えない。

| GPUI Kit | 非対応とする理由 |
| --- | --- |
| [Dialog][gp-dialog] / [AlertDialog][gp-alert-dialog] と子部品 | 同一画面内のモーダル、背景 overlay、標準アクションを備えた対応コントロールがない。`Window.ShowDialog` は別ウィンドウであり、Popover 用 Popup も同等のモーダル機構ではない。 |
| [Attachment][gp-attachment] と子部品 | 添付ファイルの preview・metadata・actions・状態表示という専用の構成がない。アップロード状態の管理もテーマの範囲外。 |
| [Command][gp-command] / CommandGroup / CommandItem | コマンドパレットとしての検索・項目管理・キー表示を持つ標準型がない。標準 `CommandBar` はツールバーであり対応先ではない。 |
| [Editor][gp-editor] | シンタックスハイライト、LSP、折り畳み、補完、巨大テキストの編集エンジンは TextBox のテーマでは追加できない。 |
| [MessageScroller][gp-message-scroller] | 会話の末尾追従・アンカー保持を管理する専用型がない。通常の ScrollViewer のテーマに追従処理は追加しない。 |
| [OtpInput][gp-otp-input] | 複数桁に分離した入力欄、貼り付け時の配分、桁間移動の標準型がない。MaskedTextBox と同一視しない。 |
| [Questionnaire][gp-questionnaire] と子部品 | 設問の順序、回答・検証状態、前後の移動、選択肢のショートカットを管理する標準型がない。含まれる RadioButton・CheckBox・TextBox・Button だけがテーマの対象になる。 |
| [Speech][gp-speech] | 音声の取り込み・認識と入力レベルの波形表示を持つ標準型がない。開始・停止ボタンの外観を ToggleButton 等で揃えても、録音・認識処理はテーマで追加しない。 |
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
| Carousel のページ切り替え | 対応 | `PageTransition` に指定する `PageSlide` の派生（`uikit:SpringSlide`）で、GPUI と同じばねで動かす。複数ページ先への移動、途中での反転、ループの折り返し、ドラッグを離した後は、ページを並べたトラック（`uikit:CarouselTrack`。`uikit:Carousels.TracksPointer`、ADR 30）を同じばねで動かす。 |
| Expander / Sheet / Sidebar の開閉 | 対応（2026-10-03 の方針改定後） | DrawerPage / SplitView の既存状態・テンプレートに Transition とキーフレームを付ける。Expander の自然高の reveal は下の行。 |
| Popup / Flyout / Menu / Tooltip の入退場 | 部分対応 | 内容が生存する間の表示開始アニメーションは可能。非表示・破棄後は描画できないので、閉じるアニメーションのために独自の表示寿命管理を追加しない。 |
| Notification の入退場 | 対応範囲あり | `NotificationCard` は `IsClosing` / `IsClosed` を持ち、標準テーマがアニメーションと閉じる完了を結び付けている。この契約を維持して外観と時間を変更する。 |
| 途中で反転しても速度を維持する spring | 対応（2026-10-03 の方針改定後） | Avalonia の `SpringEasing` は速度を持たないので、見た目だけを動かす Behavior（`Motion.Spring`）で GPUI と同じ式を使う。 |
| 選択タブを追いかける下線 | 対応（2026-10-03 の方針改定後） | 選択項目の位置と幅を読んでインジケーターだけを動かす Behavior（`Tabs.Indicator`）。 |
| 通知の重なり・並べ直し | 対応（ADR 30） | 通知の並びと寿命の管理が必要で、見た目だけのコードの範囲を超えるので、`uikit:NotificationList` で足した。内部の `NotificationStack` が GPUI の ToastStack と同じばねで、カードの位置・幅・高さ・不透明度を毎フレーム置く。 |
| 自然高を計測し、レイアウト高も滑らかに変える reveal | 対応（2026-10-03 の方針改定後） | `Height=Auto` と数値の遷移だけでは GPUI の `MotionReveal` に相当しないので、内容を自然な高さで測る `Canvas` と値変換（`RevealConverters`）で、高さ ＝ 自然高 × ばねの値にする。 |
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
6. 円形 Progress は `PART_Indicator` 等の既存契約、Percentage の更新、0 / 100 %・任意の Minimum / Maximum を確認する。標準側の横幅計算と円弧の配置が干渉しないテンプレートを検証する。確定値の円形表示には表示専用の値変換を使う（2026-10-03 の方針改定で許可）。

## 根拠の参照方法

各 GPUI 名のリンクは調査 commit のドキュメント、Avalonia 型名のリンクは `12.1.3` のソースを指す。別リポジトリの `Avalonia.Controls.DataGrid` には `12.1.3` がないため、本体 `12.1.0` 以上に依存する最新版 `12.1.2` を指す。サードパーティのライブラリのリンクは、パッケージが固定した版のタグを指す。機能の有無はこの固定ソースを優先し、更新される公式資料とは区別する。対象版または「標準」の範囲が変わった場合は、とくに TableView、DrawerPage、Carousel、ComboBox、ウィンドウ装飾、公式別パッケージを再判定する。

[av-arc]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Shapes/Arc.cs
[av-autocomplete]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/AutoCompleteBox/AutoCompleteBox.cs
[av-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Button.cs
[av-calendar]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Calendar/Calendar.cs
[av-carousel]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Carousel.cs
[av-checkbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/CheckBox.cs
[av-colorpicker]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls.ColorPicker/ColorPicker/ColorPicker.cs
[av-combobox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ComboBox.cs
[av-commandbar]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/CommandBar/CommandBar.cs
[av-contextmenu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ContextMenu.cs
[av-controls]: https://github.com/AvaloniaUI/Avalonia/tree/12.1.3/src/Avalonia.Controls
[av-datagrid]: https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid/blob/12.1.2/src/Avalonia.Controls.DataGrid/DataGrid.cs
[av-date-picker]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/CalendarDatePicker/CalendarDatePicker.cs
[av-drop-down-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/DropDownButton.cs
[av-doc-animation]: https://docs.avaloniaui.net/docs/graphics-animation/keyframe-animations
[av-doc-colorpicker]: https://docs.avaloniaui.net/controls/input/selectors/colorpicker
[av-doc-datagrid]: https://docs.avaloniaui.net/controls/data-display/structured-data/datagrid
[av-doc-themes]: https://docs.avaloniaui.net/docs/styling/control-themes
[av-doc-transitions]: https://docs.avaloniaui.net/docs/graphics-animation/control-transitions
[av-drawer-page]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Page/DrawerPage.cs
[av-expander]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Expander.cs
[av-fluent]: https://github.com/AvaloniaUI/Avalonia/tree/12.1.3/src/Avalonia.Themes.Fluent/Controls
[av-flyout]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Flyouts/Flyout.cs
[av-grid-splitter]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/GridSplitter.cs
[av-groupbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/GroupBox.cs
[av-hyperlink]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/HyperlinkButton.cs
[av-image]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Image.cs
[av-label]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Label.cs
[av-listbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ListBox.cs
[av-masked-textbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/MaskedTextBox.cs
[av-menu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Menu.cs
[av-menu-flyout]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Flyouts/MenuFlyout.cs
[av-menu-item]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/MenuItem.cs
[av-native-menu]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/NativeMenu.cs
[av-notification-card]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Notifications/NotificationCard.cs
[av-notification-manager]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Notifications/WindowNotificationManager.cs
[av-notification-theme]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Themes.Fluent/Controls/NotificationCard.xaml
[av-number-input]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/NumericUpDown/NumericUpDown.cs
[av-pathicon]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/PathIcon.cs
[av-pips-pager]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/PipsPager/PipsPager.cs
[av-progress]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ProgressBar.cs
[av-radio]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/RadioButton.cs
[av-release]: https://github.com/AvaloniaUI/Avalonia/releases/tag/12.1.3
[av-scrollbar]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Primitives/ScrollBar.cs
[av-scrollviewer]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ScrollViewer.cs
[av-separator]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Separator.cs
[av-slider]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Slider.cs
[av-split-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/SplitButton/SplitButton.cs
[av-splitview]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/SplitView/SplitView.cs
[av-spring]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Base/Animation/Easings/SpringEasing.cs
[av-switch]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ToggleSwitch.cs
[av-tableview]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/TableView.cs
[av-tabs]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/TabControl.cs
[av-tabstrip]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Primitives/TabStrip.cs
[av-textblock]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/TextBlock.cs
[av-textbox]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/TextBox.cs
[av-timepicker]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/DateTimePickers/TimePicker.cs
[av-toggle-button]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Primitives/ToggleButton.cs
[av-tooltip]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/ToolTip.cs
[av-tree]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/TreeView.cs
[av-virtualizing-stack-panel]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/VirtualizingStackPanel.cs
[av-window-decorations]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Controls/Chrome/WindowDrawnDecorations.cs
[gp-accordion]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/accordion.md
[gp-alert]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/alert.md
[gp-alert-dialog]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/alert-dialog.md
[gp-attachment]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/attachment.md
[gp-avatar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/avatar.md
[gp-badge]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/badge.md
[gp-bubble]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/bubble.md
[gp-button]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/button.md
[gp-calendar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/calendar.md
[gp-carousel]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/carousel.md
[gp-catalog]: https://github.com/longbridge/gpui-kit/tree/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component
[gp-chart]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/chart.md
[gp-checkbox]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/checkbox.md
[gp-clipboard]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/clipboard.md
[gp-collapsible]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/collapsible.md
[gp-color-picker]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/color-picker.md
[gp-combobox]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/combobox.md
[gp-command]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/command.md
[gp-commit]: https://github.com/longbridge/gpui-kit/commit/2c5162f8c5b0c7fcec066ed53125d304c632bfe2
[gp-data-table]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/data-table.md
[gp-date-picker]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/date-picker.md
[gp-default-theme]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/theme/default-theme.json
[gp-description-list]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/description-list.md
[gp-dialog]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/dialog.md
[gp-dock]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/dock.md
[gp-dropdown_button]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/dropdown_button.md
[gp-editor]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/editor.md
[gp-empty]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/empty.md
[gp-focus-trap]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/focus-trap.md
[gp-form]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/form.md
[gp-group-box]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/group-box.md
[gp-hover-card]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/hover-card.md
[gp-icon]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/icon.md
[gp-image]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/image.md
[gp-input]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/input.md
[gp-input-group]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/input-group.md
[gp-kbd]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/kbd.md
[gp-label]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/label.md
[gp-lib]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/lib.rs
[gp-list]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/list.md
[gp-marker]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/marker.md
[gp-menu]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/menu.md
[gp-message]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/message.md
[gp-message-scroller]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/message-scroller.md
[gp-motion]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/theme/motion.rs
[gp-notification]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/notification.md
[gp-number-input]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/number-input.md
[gp-otp-input]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/otp-input.md
[gp-pagination]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/pagination.md
[gp-plot]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/plot.md
[gp-popover]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/popover.md
[gp-popover-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/popover.rs
[gp-progress]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/progress.md
[gp-questionnaire]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/questionnaire.md
[gp-radio]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/radio.md
[gp-rating]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/rating.md
[gp-resizable]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/resizable.md
[gp-root]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/root.md
[gp-scrollable]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/scrollable.md
[gp-select]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/select.md
[gp-settings]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/settings.md
[gp-sheet]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/sheet.md
[gp-shimmer]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/shimmer.md
[gp-sidebar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/sidebar.md
[gp-sizing]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/sizing.rs
[gp-skeleton]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/skeleton.md
[gp-slider]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/slider.md
[gp-speech]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/speech.md
[gp-spinner]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/spinner.md
[gp-spinner-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/spinner.rs
[gp-status-bar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/status-bar.md
[gp-stepper]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/stepper.md
[gp-styling-motion]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/docs/STYLING-AND-MOTION.md
[gp-switch]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/switch.md
[gp-table]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/table.md
[gp-tabs]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/tabs.md
[gp-tag]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/tag.md
[gp-text-view]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/text-view.md
[gp-textarea]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/textarea.md
[gp-theme]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/theme.md
[gp-theme-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/theme/mod.rs
[gp-time-field]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/time-field.md
[gp-title-bar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/title-bar.md
[gp-toggle]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/toggle.md
[gp-toolbar]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/toolbar.md
[gp-tooltip]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/tooltip.md
[gp-tree]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/tree.md
[gp-virtual-list]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/website/component/virtual-list.md
[gp-separator-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/separator.rs
[gp-link-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/link.rs
[gp-progress-circle-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/progress/progress_circle.rs
[gp-breadcrumb-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/breadcrumb.rs
[gp-native-menu-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/native_menu/mod.rs
[gp-root-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/root.rs
[gp-sheet-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/sheet.rs
[gp-tab-panel-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/dock/tab_panel.rs
[gp-table-state-source]: https://github.com/longbridge/gpui-kit/blob/2c5162f8c5b0c7fcec066ed53125d304c632bfe2/crates/component/src/table/state.rs
[av-platform-settings]: https://github.com/AvaloniaUI/Avalonia/blob/12.1.3/src/Avalonia.Base/Platform/IPlatformSettings.cs
[td-dock]: https://github.com/wieslawsoltes/Dock/tree/v12.1.0.6
