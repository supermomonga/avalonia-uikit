# テストと一致検証

UIKitTheme が GPUI Kit と同じ見た目・動きになっていることを、GPUI Kit 自身が描いた参照データとの比較で検証する。この文書は、検証の仕組み、許容値、許容値を緩めた理由（緩和 ID）、テーマの利用側に求める約束をまとめる。

- 移植元: gpui-kit [`2c5162f8c5b0c7fcec066ed53125d304c632bfe2`](https://github.com/longbridge/gpui-kit/tree/2c5162f8c5b0c7fcec066ed53125d304c632bfe2)（gpui-pre 0.3.7）
- 移植先: Avalonia 12.1.3（別パッケージは `Avalonia.Controls.ColorPicker` 12.1.3、`Avalonia.Controls.DataGrid` 12.1.2）、.NET 10、TUnit 1.72.16
- サードパーティのライブラリ（ADR 28）: Tabalonia 12.0.0、Dock.Avalonia 12.1.0.6
- 参照データ: `goldens/gpui-2c5162f/`（6396 ケース。うち動き 98、Aurora Light 124。PNG、Scene JSON、トークン）

## コマンド

| 目的 | コマンド | 備考 |
| --- | --- | --- |
| 全テスト | `scripts/verify.sh` | `dotnet build tests/AvaloniaUIKit.Tests && dotnet run --no-build --project tests/AvaloniaUIKit.Tests` と同じ。macOS 以外でも動く。 |
| 一部だけ | `scripts/verify.sh --treenode-filter "/*/*/ButtonTests/*"` | クラス名で絞る。 |
| 許容値の校正 | `AVALONIA_UIKIT_CALIBRATE=1 scripts/verify.sh` | `tests/artifacts/pixel-stats.csv`（領域ごとの n / max / mean / bias）、`ink-mass.csv`、`border-mass.csv`（枠線の角と辺ごと）を書き出す。 |
| 参照データの再生成 | `scripts/generate-goldens.sh [--only <id 接頭辞>]` | macOS（Metal）専用。`reference/vendor/` を作り直し、生成後に 2 回描画して一致を確かめる。`Palettes.g.cs`、`Lucide.g.axaml`、`IconName.g.cs`、サイトのテーマ（`sites/app/lib/themes.g.json`、`sites/app/styles/themes.g.css`）も再生成する。色だけなら `reference tokens` で足りる。全体の生成が途中で失敗すると `goldens/` の一部が消えるので、`git checkout goldens` で戻す。 |
| NativeAOT | `scripts/aot-smoke.sh` | ギャラリーを NativeAOT で publish し（trim / AOT 警告はエラー）、`--smoke` で Light / Dark を描画して終了する。 |

失敗したケースは `tests/artifacts/<ケース ID>/` に `gpui.png`、`avalonia.png`、`diff.png`、`mask.png`（領域の分類）、`report.txt` を出力する。

## 参照データの作り方

`reference/` は GPUI Kit を固定コミットのまま使う Rust 製の生成器で、GPUI Kit を変更しない。

1. `reference/scripts/vendor.sh` が gpui-kit を `git archive` で、gpui-pre 0.3.7 を crates.io から（Cargo.lock のチェックサムを照合して）`reference/vendor/` に展開する。
2. 時刻の読み取りだけを差し替えるパッチを当てる（R15）。
   - gpui-pre の `elements/animation.rs` と gpui-base の `scrollbar.rs` の `Instant::now()` を、テスト用 executor の時計に置き換える。
   - gpui-pre の `elements/img.rs` が読み込み中の内容を出すまでの 200ms（`LOADING_DELAY`）も、同じ時計で測る。GIF のフレームの時計は変えない。
   - Calendar の「今日」（`Local::now()`）を `test_clock::today()` に置き換え、ケースが日付を固定する。
   - 置き換え漏れがあれば止まる。描画結果は変わらない。
3. `HeadlessAppContext` と Metal の headless レンダラで、`cases/*.toml` に定義した組み合わせを描く。フォントは同梱の Inter（`assets/fonts/inter/`）、スケールは 2。状態は GPUI の入力（hover、マウス押下、Tab、クリック、右クリック、ドラッグ、ホイール、キー）と `advance_clock` で作る。ケースの終わりにドラッグを止め、次のケースに持ち越さない。
4. 各ケースについて、PNG と Scene（quad、影、下線、スプライト、パス、画像）の JSON、要素の bounds を書き出す。quad の塗りは単色と 2 色の線形グラデーションを書き出す。トークン（解決済みの色、コンポーネントが描画時に作る派生色、トークンの背景）は、Default Light / Default Dark が `tokens/gpui-theme.json`、同梱のテーマが `tokens/gpui-themes.json` になり、どちらも `Palettes.g.cs` になる（ADR 26）。
5. アイコンは GPUI Kit の Lucide SVG を線から塗りの輪郭に変換して `Lucide.g.axaml` に書き出す。テーマが使うアイコン（`reference/src/icons.rs` の `ICONS`）に続けて、GPUI Kit の `IconName` の全 106 個（`crates/assets/default-icons.txt`）を重複なしで書き、`uikit:Icon` の列挙 `IconName.g.cs` も生成する。不透明度の付いた部分（二色アイコンの薄い半分）は `<名前>.Faint` の別のジオメトリにする。

ケース ID は `<コンポーネント>/<グループ>.<組み合わせ>/<状態>/<テーマ>` の形（例: `button/outline.primary.small/hover/dark`）。動きのケースは `<ID>/<経過 ms>` のフレーム列を持つ。

状態の語彙（`cases/*.toml` の `states`、`trigger`）:

| 語 | 操作 |
| --- | --- |
| `normal`、`disabled` | そのまま（disabled はビルダーのフラグ） |
| `hover`、`pressed`、`click`、`right-click` | 要素の中心（`pointer_x` / `pointer_y` で比率を変える）への移動、押下、クリック |
| `focus` | Tab |
| `activate` | ウィンドウをアクティブにする（GPUI はアクティブなときだけキャレットと選択を描く） |
| `leave` | 要素の外へ移動 |
| `tooltip` | hover して 600ms 待つ |
| `at-X-Y`、`click-at-X-Y`、`right-click-at-X-Y`、`pressed-at-X-Y` | ウィンドウ座標への移動・クリック・押下 |
| `drag-at-X-Y`、`release` | 左ボタンを押したままの移動、最後の位置での解放 |
| `wheel-at-X-Y` | その位置で 40px 下へスクロール |
| `wait-Nms` | 時計を進める |
| `key-<キー>` | キー入力。修飾キーのない 1 文字のキー（`key-a`、`key-.`）は、その文字も入力する（GPUI は `dispatch_keystroke` でフォーカスのある入力欄に、Avalonia は `KeyTextInput` で渡す）。記号は `.` `,` `[` `]` を打てる |

状態は `+` でつなぐ（例: `click+wait-200ms+key-escape`）。

## テストの構成

6985 件。macOS arm64 での最新の実行結果は全件成功し、全体で約 13 分かかる。テストの時刻はすべて仮想時計で進める（[時刻](#時刻)）。

| テスト | 件数 | 内容 |
| --- | --- | --- |
| `*_matches_gpui`（コンポーネント別 105 クラス） | 6478 | 静止状態の全ケース（TabControl は Tabs のケースをもう一度使い、TabBar のケースは TabStrip と TabControl の両方で、Select の閉じた欄のケースは編集可能な ComboBox でも使う）。構造と画素を比較する。 |
| `MotionTests`、`TabControl_moves_as_gpui` | 98、4 | 動きを GPUI が記録した時刻ごとに描画し、フレームを比較する。 |
| `TokenTests` | 40 | トークンの完全一致と過不足。Default Light / Default Dark と同梱の 36 テーマのそれぞれ、テーマのバリアントの継承。 |
| `BehaviorTests`、`ControlBehaviorTests` | 24、34 | 時間・入力・無効状態の挙動。後者は新しいコントロール（ADR 19）の操作と、GPUI の表記・色の計算。 |
| `ThemeFixBehaviorTests` | 16 | テーマで描くようにした Avalonia の機能（クリアボタン、右クリックメニュー、編集可能な ComboBox など）。 |
| `ButtonsBehaviorTests`、`InputsBehaviorTests`、`SelectBehaviorTests`、`ListsBehaviorTests`、`DatesBehaviorTests`、`DisplayBehaviorTests`、`NavigationBehaviorTests`、`TableColorBehaviorTests`、`LayoutBehaviorTests`、`ShellBehaviorTests` | 19、50、27、17、50、16、21、24、19、20 | ADR 30 のコントロールと添付プロパティの操作と、GPUI のテストと同じ例の計算。 |
| `TabaloniaBehaviorTests`、`DockBehaviorTests` | 6、3 | サードパーティのライブラリ（ADR 28）の操作が、テーマの部品を通して効くこと。 |
| `FluentLayeringTests` | 19 | FluentTheme の上に重ねても見た目が変わらないこと。 |

コンポーネント別の静止ケース数（括弧内は動きのケース数）:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Button → Button | 784 | 10 色 × 4 サイズ × 5 状態、outline、selected、compact、rounded、アイコンのみ、Aurora Light の 8 色 × outline × 4 状態 |
| Toggle → ToggleButton | 160 | ghost / outline × 4 サイズ × checked、ラベル / アイコン |
| Icon → PathIcon | 154 | 26 個のアイコン × サイズ、色の継承、色、回転 |
| DropdownButton → SplitButton | 150 | 色 × サイズ、outline、各部の hover / 押下 / フォーカス、selected、メニューを開いた状態 |
| Button::dropdown_caret → DropDownButton | 128 | 5 色 × 4 サイズ × normal / disabled、3 色 × outline × hover / 押下 / focus、selected、幅指定、開いたメニュー |
| Input → TextBox | 116 | 4 サイズ、値 / placeholder / 読み取り専用 × focus / disabled、前後のアイコン、マスク、選択範囲、`clearButton`（4 サイズ × normal / focus / disabled、ボタンの hover とクリック、空・読み取り専用、mask toggle と suffix との並び） |
| Tabs / TabBar → TabStrip、TabControl | 110（4） | 4 種類 × サイズ、hover、無効なタブ、アイコン、インジケーターの移動、Aurora Light |
| Switch → ToggleSwitch | 94（2） | 4 サイズ × checked × hover / focus / disabled、ラベルなし、Aurora Light |
| Select → ComboBox | 90（1） | 閉じた欄の状態、placeholder、開いた一覧の hover / 無効な行、開く動き、`clearButton`（4 サイズ × normal / disabled、ボタンの hover、focus、値なし） |
| ButtonGroup → StackPanel.button-group の Button | 86 | 横 / 縦、サイズ、outline、先頭・中間・末尾の hover / 押下 |
| Checkbox → CheckBox | 86（2） | 4 サイズ × checked × hover / focus / disabled、ラベルなし、Aurora Light |
| Radio → RadioButton | 86（1） | 同上 |
| ToggleGroup → ListBox.toggle-group | 76 | ラベル / アイコン、segmented、checked と unchecked の hover / クリック、disabled |
| NumberInput → NumericUpDown | 64 | 4 サイズ × disabled、増減ボタンの hover / 押下、文字の prefix・suffix、アイコンの prefix とボタンの suffix × normal / disabled |
| DatePicker → CalendarDatePicker | 60（1） | 欄の状態、開いたカレンダーと hover、開く動き |
| DataTable → DataGrid（別パッケージ） | 56 | 4 サイズ、ソート、行の hover / 選択、リサイズのヘアライン、stripe、borderless、行なし、`cell-selectable`（クリック、hover、キー移動、stripe、行見出し） |
| InputGroup → TextBox.group | 54（1） | アイコン、前後の文字、ボタン、invalid、フォーカスの色の動き |
| Calendar → Calendar | 55 | 選択日、今日、日の hover / 押下 / クリック、選択できない日、未選択、月曜始まり、月の一覧 × サイズ、Aurora Light |
| Slider → Slider | 55（3） | 値 0 / 40 / 100 × hover / 押下 / focus、トラックの押下、ドラッグ、逆向き、縦、disabled、リングの動き、Aurora Light |
| DataTable → TableView | 52 | 4 サイズ、行の hover / 選択、stripe、borderless、固定列、行が埋まる / 埋まらない、行なし（3 サイズ、stripe） |
| Toolbar → CommandBar | 50 | 4 サイズ、項目の hover / 押下 / focus、矢印キー、disabled、内容 |
| TimeField → TimePicker | 46 | サイズ、書式（12 / 24 時間、秒）、focus、invalid、hover |
| ProgressCircle → ProgressBar（UIKitProgressCircle） | 44（2） | 4 サイズ × 値、大きさ指定、値の変化、不定値、中央の内容（`uikit:ProgressCircles.Content`） |
| ColorPicker / ColorSelect → ColorPicker（別パッケージ） | 42 | スウォッチ 4 サイズ、ラベル、ツールチップ、field 4 サイズ、focus、値 |
| Pagination → PipsPager | 42 | サイズ、compact、両端、ページの hover / 押下 / クリック、disabled |
| Textarea → TextBox | 42 | 行数、placeholder、折り返し、読み取り専用、自動の高さ |
| Label → TextBlock.label、Label | 40 | サイズ、補足、太さ、揃え、折り返し、色 |
| Popover → Flyout | 38（1） | 8 つのアンカー、複数行、offset、plain、閉じる操作、開く動き |
| Notification → WindowNotificationManager、NotificationCard | 34（2） | 4 種類とアイコンなし、タイトル、hover と閉じるボタン、折り返し、配置、入退場 |
| Accordion → Expander | 32（3） | card / borderless、disabled、アイコン、hover / focus、開閉と途中の反転 |
| Resizable → GridSplitter | 30（5） | 横 / 縦 × hover / ドラッグ、pill の動き |
| Sheet → DrawerPage.sheet | 28（4） | 4 方向、大きさ、タイトルなし、フッター、閉じる操作、滑り込み |
| Table → TableView（UIKitTable） | 26 | サイズ、枠付き、stripe、固定幅、行の hover / クリック、行なし（枠なし / 枠付き） |
| Combobox → ComboBox.combobox、AutoCompleteBox | 24（1） | 閉じた欄、placeholder、開いた一覧の hover / キー、閉じる操作 |
| List → ListBox | 24 | 行の hover / 選択 / キー操作、未選択、スクロール、空 |
| Progress → ProgressBar | 27（2） | 4 サイズ × 値 0 / 40 / 100、Aurora Light |
| Carousel → Carousel、PipsPager.carousel | 22（2） | ナビゲーションの hover / 押下、サイズ、focus、ページ送り |
| Sidebar → SplitView、DrawerPage | 22（5） | icon / offcanvas / none、左右、既定の幅、DrawerPage、開閉 |
| Tree → TreeView | 22 | 行の hover / クリック / キー、選択、角丸 |
| VirtualList → ListBox | 22 | 可変・均一の行の高さ、スクロール、深い位置、`ItemsScrolling.ScrollToItem`（Center / Top / Bottom、見えている行、可変の行で Center） |
| img() → Image | 20 | ObjectFit 5 種、小さい画像、角丸、元の大きさ |
| Collapsible → Expander（UIKitCollapsible） | 16（4） | 開閉、内容が上、hover / focus、開閉と途中の反転 |
| Scrollbar → ScrollViewer | 18（7） | Always / Hover / Scrolling モード、つまみの hover、Aurora Light |
| GroupBox | 16 | normal / fill / outline、タイトルの有無、footer（`uikit:GroupBoxes.Footer`）× normal / fill / outline |
| Separator | 16 | 横 / 縦 × 実線 / 破線、label（`uikit:Separators.Label`）の横 / 縦 × 実線 / 破線 |
| Spinner → ProgressBar（UIKitSpinner） | 14（1） | 4 サイズ、アイコン（`uikit:Spinners.Icon` の LoaderCircle）× 3 サイズ |
| DropdownMenu → MenuFlyout | 10 | 開いた状態、各項目の hover、サブメニュー |
| Input の右クリックメニュー → TextBox の ContextFlyout | 8 | 選択なし・選択の中・読み取り専用、項目の hover |
| Link → HyperlinkButton | 6 | normal / hover / pressed |
| TitleBar → WindowDrawnDecorations | 6 | バー、タイトル、hover |
| AppMenuBar → Menu | 4 | 通常、項目の hover |
| ContextMenu | 4 | 開いた状態、項目の hover |
| Tooltip → ToolTip | 4（1） | 表示後、サイズ |
| 背景（Window） | 2 | Light / Dark |

新しいコントロール（ADR 19）の静止ケース数:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Tag → TagLabel | 82 | 6 色 × outline、19 のパレット色、4 サイズ、rounded-full、hover |
| Alert → Alert | 46 | 5 種類、タイトル、4 サイズ、banner、アイコン、折り返し、閉じるボタンの hover / 押下 |
| Avatar → Avatar | 36 | 頭文字 × 4 サイズ、12 色のうち 8 名分、1 語、代わりのアイコン、画像 |
| Badge → Badge | 32 | 数 / 点 / アイコン × 3 サイズ、2 桁、上限、色 |
| Rating → Rating | 28 | 値、サイズ、上限 10、色、hover の予告とクリック、disabled |
| Bubble → Bubble | 28 | 7 種類、start / end、折り返し、リアクションの上下・左右 |
| Stepper → Stepper | 26 | 選択、4 サイズ、アイコン、中央寄せ、縦、hover / 押下 / disabled、Aurora Light |
| Clipboard → Clipboard | 18 | 4 サイズ、hover / 押下、コピー後と 2 秒後 |
| Marker → Marker | 18 | plain + アイコン、separator × 3 揃え、border、揃え、spinner、shimmer |
| DescriptionList → DescriptionList | 16 | 横 × 3 サイズ、縦、枠なし、2 列とラベル幅、区切り |
| Kbd → Kbd | 16 | 修飾キーのないキー 6 種、outline |
| Form / Field → Form / FormField | 12 | 縦 × 3 サイズ、横、ラベル幅、2 列と結合とフッター |
| AvatarGroup → AvatarGroup | 10 | 2 サイズ、上限、省略記号 |
| Message → Message | 10 | アバター・ヘッダー・フッター × start / end、複数の吹き出し、文字だけ、filled |
| Skeleton → Skeleton | 8（1） | 帯、secondary、円、角丸、明滅 |
| StatusBar → StatusBar | 9 | 3 領域、左だけ、右だけ、中央だけ、Aurora Light |
| Breadcrumb → Breadcrumb | 8 | 項目、1 つ、無効、長い列 |
| Empty → EmptyState | 8 | 全部、ヘッダーだけ、枠なしの画像、タイトルだけ |
| HoverCard → HoverCard | 8 | 500ms では閉じている、700ms で開く、左右の揃え |
| ShimmerText → ShimmerText | 6（4） | 色、小さい文字、1 回のスイープ（逆向き） |

機能を足したコントロールと添付プロパティ（ADR 30）の静止ケース数。元のコンポーネントのケースをそのまま写したグループ（`select.toml` を写した `uikit-select.toml` など）を含む:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Button::loading → Button の `uikit:Buttons.IsLoading` | 356 | 10 色 × 4 サイズ（ラベルだけ（normal / hover）、アイコンとラベル）、アイコンのみ 3 色 × 4 サイズ × normal / hover、outline 8 色 × 2 サイズ、押下 / クリック / focus / 300ms 後の回転、disabled、selected（normal / hover）、loading_icon（normal / 300ms 後） |
| Icon → `uikit:Icon` | 162 | Icon と同じアイコン × サイズ、サイズなし、色、回転、二色のアイコン、全 IconName の格子（16px と 24px） |
| ButtonGroup → `uikit:ButtonGroup` | 126 | buttongroup の全ケース（色・サイズ・outline・compact をグループに付ける）、選択されたボタン × 3 色 × outline × normal / hover、2 つ選択、クリックで選ぶ（単一 / multiple × 1 回 / 2 回） |
| Select → `uikit:Select` | 118（1） | select の全ケース、閉じたトリガーで ↑↓、検索欄 × 4 サイズ、クエリ（一致、カーソル移動、一致なし、Enter、Esc の後に開き直す）、グループとその hover / キー、グループの検索、クリアボタン（hover / 押下後）、title_prefix、menu_width、icon、独自の空の表示、appearance(false) |
| DatePicker → `uikit:DateField` | 114（1） | datepicker の全ケースと開く動き、範囲（欄と開いた状態）、2 か月の範囲、クリックでの範囲選択、プリセット（単一・範囲）、クリアボタン（値の有無 × normal / focus / disabled、hover、クリック）、時刻（24 / 12 時間、秒 × small / large、日を選んで開いたまま・同じ日で閉じる）、日の選択、2 か月、plain |
| Calendar → `uikit:CalendarView` | 113 | calendar の全ケース、4・5 週の月、前後の月送り、2 か月 × 3 サイズ、3 か月、無効な曜日と hover、範囲 × 3 サイズ、無効な週末をまたぐ範囲、月をまたぐ範囲、クリックでの範囲選択とやり直し、年グリッド × 3 サイズ、年・月のトグルとページ送りと選択 |
| Slider → `uikit:RangeSlider` | 93（4） | slider の全ケースとリングの動き（単一値、`IsRange=False`）、範囲の始点 / 終点のつまみの hover / 押下、トラックの押下で近いつまみ（30% / 80%）、始点のドラッグと終点で止まる、reverse を無視、縦、disabled、Aurora Light、対数スケール（単一 10 / 100、範囲 10..100）、終点のつまみのリングの動き |
| ToggleGroup → `uikit:ToggleGroup` | 88 | togglegroup の全ケース、Tab で 1 つ目 / 2 つ目、1 つ目で →（リングなし、何も変わらない） |
| ColorPicker / ColorSelect → `uikit:ColorSelect` | 84 | スウォッチ 4 サイズ × focus、ラベル、ツールチップ、field 4 サイズ × focus、値、色なし（スウォッチ × 2 サイズ × hover、placeholder × 4 サイズ、アプリの placeholder）、アイコン（ラベルあり・なし）、開いたポップオーバー（スウォッチ、field、色なし）、パレット・featured の色を指す / 押す、色なしでの最初のプレビュー、クリックでの確定、HSLA タブ（色あり・なし） |
| TimeField → `uikit:TimeField` | 80 | timefield の全ケース、セグメントの移動（→、←、Tab）、入力（2 桁、25 → 05、保留中の 1、↑↓、Backspace、分の ↓）、秒への Tab と入力、12 時間の AM/PM と p、12 → 0 時、クリックで分を選ぶ |
| InputGroup → `uikit:InputGroup` | 76（1） | input-group の全ケースとフォーカスの色の動き、上の行 × 2 サイズ × 通常 / focus / disabled、下の行（カウンターと末尾の Send）× invalid × 通常 / focus、アドオンの押下でフォーカス |
| DropdownButton の loading → SplitButton の `IsLoading` | 72 | default / primary / ghost × outline × normal / 左の hover / 左の押下 / 右の hover / focus、ラベルだけ × 2 サイズ、メニューを開いた状態 |
| Toolbar → `uikit:Toolbar` | 72 | toolbar の全ケース、ToolbarGroup × 3 サイズ × normal / グループ内で →、spacer（文字と 288px の幅で末尾へ）× 3 サイズ、← で末尾へ・→ で先頭へ折り返し |
| Pagination → `uikit:Pagination` | 70 | pagination の全ケース、10 ページの省略記号 × 現在ページ 3 通り、サイズ、disabled、visible_pages(7)、長いページ番号、省略記号のメニュー（hover、開く、項目の hover、項目のクリック、前の省略記号） |
| List → `uikit:ListView` | 68 | List と同じ行の hover / 選択 / キー操作、未選択、スクロール、空、右クリックの枠とその解除、selectable(false)、セクション（見出しと末尾、hover、キーでの移動と折り返し、scroll_to_item）、読み込み中、検索欄（フォーカス、クエリ、一致なし）、確定行のチェック、scroll_to_item（Center / Top） |
| Resizable → `uikit:ResizablePanelGroup` | 60（5） | resizable の全ケースと pill の動き、3 つのパネルの連鎖リサイズ（最小を越えて広げる・狭める）、size_range の両端、隠したパネル、入れ子 |
| Notification → `uikit:NotificationList`、`uikit:NotificationItem` | 60（3） | 4 種類と種類なし、タイトルと閉じるボタンの hover、折り返し、配置 5 種、左右中央 2 種、2〜4 枚の重なり、上下での展開、action とその hover、種類なしのアイコン（タイトルあり・なし）。動きは入場、退場、左右中央の入場 |
| Label → `uikit:TextLabel` | 56 | Label と同じサイズ・補足・太さ・揃え・折り返し・色、一致（複数・なし）、Prefix（先頭・先頭でない）、重なる一致、補足付きの label の中の一致、マスク（補足と一致あり・なし） |
| Combobox → `uikit:Select.combobox` | 54（1） | combobox の全ケース、複数選択（閉じた状態、開いた状態、行のクリックで追加・解除）、複数選択の検索と Enter、footer とその hover、render_trigger × 2 サイズ × focus、プレースホルダー、on_will_change で 2 件まで、複数選択のクリア |
| Tree → `uikit:Tree` | 54 | Tree と同じ行の hover / クリック / キー、選択、角丸、右クリックの枠とその解除、クリックとキー（→ ← ↑↓）での開閉と移動、3 段の木、reveal_item（深い木で Center、202 行の木で Center / Top / Bottom） |
| Sidebar → `uikit:Sidebar` | 52（6） | sidebar の全ケースと開閉の動き、ヘッダーとフッターの余白、SidebarToggleButton、サイドバーのストーリーのメニュー（hover、無効な項目、サブ項目、ヘッダーとフッターの hover、キャレットでの開閉、click_to_open / click_to_toggle、アイコンへの折り畳みと右のツールチップ）、メニューの折り畳みの動き |
| NumberInput → `uikit:NumberInput` | 46 | number の全ケース、入力の制限（英字の拒否、先頭の小数点）、刻みの精度（0.1 ± 0.2）、max でのクランプと端で何もしない、数値マスクと刻み 100 |
| Table → `uikit:Table` | 42 | 4 サイズ、枠付き × 3 サイズ、stripe、固定幅、ポインター（hover・選択なし）、フッター、キャプション × 3 サイズ × 枠、2 段の見出し、ストーリーの表（w(150)、2 列の見出し、2 行の支払方法、3 列の合計、キャプション）× 2 サイズ |
| Accordion → `uikit:Accordion` | 40（4） | accordion の全ケースと 3 つの動き、1 項目だけ開く（2 サイズ × normal / 2 つ目のクリック後）、その動き（開く項目と閉じる項目が同時に動く） |
| Sheet → `uikit:Sheet` | 40（4） | sheet の全ケースと滑り込み、ウィンドウの半分の大きさ、閉じないオーバーレイと透明なオーバーレイの押下、スクロールした本体とスクロールバー |
| Carousel → Carousel の `uikit:Carousels.TracksPointer`、`uikit:CarouselPrevious` / `CarouselNext` | 38（5） | carousel の全ケース、ループ、縦、ドラッグ中と離した後、トラックパッドのスクロール中とスナップ後。動きは次、前、2 ページ先、ループの折り返し、ドラッグを離した後 |
| Tabs / TabBar → TabStrip、TabControl の `menu`、`uikit:Tabs.Prefix` / `Suffix` | 38 | prefix と suffix × 5 種類、menu の閉と開 × Tab / Underline、メニューの無効なタブ、prefix・menu・suffix の同時（開いた状態も）× Tab / Pill、溢れたタブ × 5 種類 |
| Input / Textarea → TextBox の `uikit:Inputs` | 28 | 数値マスク（3 桁区切り、小数 2 桁、削除後の再区切り）、パターンマスク（placeholder、区切りの自動入力、英字の拒否）、全文の検証（英字の拒否）、Esc（clean_on_escape あり・なし）、Textarea の選択行のインデント（2 回、戻す）、キャレット位置のインデントと行のアウトデント |
| img() → `uikit:AsyncImage` | 28 | Image と同じ ObjectFit 5 種・小さい画像・角丸・元の大きさ、失敗（fallback）、読み込み中（150ms では何もなく、250ms で loading の内容） |
| InputGroupButton::loading → TextBox.group の Button の `IsLoading`、`TakesFocusOnPointer` | 20 | アイコンのボタン × 2 サイズ × normal / hover / disabled、ラベルのボタン、Tab で入力欄にフォーカスしてボタンをクリック / 押下（入力欄のリングが残る） |
| TitleBar → `uikit:TitleBar` | 10 | バー、タイトル、hover、子（タイトルと小さな ghost のアイコンボタン）とボタンの hover |

GroupBox の footer、Separator の label、Spinner のアイコン、ProgressCircle の中央の内容、VirtualList の `ItemsScrolling.ScrollToItem` は、元のコンポーネントのケースファイルにグループを足し、上の表の数に含めた。

サードパーティのライブラリ（ADR 28、別パッケージ）の静止ケース数:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Tabs / TabBar → Tabalonia の TabsControl | 172（3） | 5 種類 × 4 サイズ、hover、無効なタブ、アイコン、閉じるボタンとその hover、前後のボタン、メニューを開いた状態、インジケーターの移動、Aurora Light |
| Dock → Dock.Avalonia の DockControl | 26 | 配置、閉じるボタンとその hover / 押下、タブの hover、分割の hover / 押下 / ドラッグ、タブのドラッグ中（グループの中央と左の 3 分の 1、タブ、タブバーの空き） |

静止ケースは Light と Dark の両方を持つ（Tooltip のサイズ違いを除く）。動きは時間の比較が目的なので Light だけで行う。

同梱のテーマで見た目が変わるのは色だけで、色はトークンの比較で全テーマを確かめる。それに加えて、Aurora Light が塗るグラデーション（ADR 26）を、グラデーションの塗りを持つコンポーネントの 14 のケースファイルで GPUI の描画と比べる。ケースのテーマはテーマの slug で書く（`themes = ["aurora-light"]`）。

## 比較の 4 層

### 1. トークン

`UIKit.*` の色リソースが、GPUI Kit が Default Light / Default Dark と同梱の 36 テーマのそれぞれで解決した値と 8bit で完全一致すること。グラデーションは角度と 2 つの停止点（色と位置）まで一致すること。GPUI にない色がテーマに定義されていないこと。

### 2. 構造

GPUI の Scene と Avalonia の可視ツリーを、どちらも次の図形に正規化して 1 対 1 で照合する。対応の付かない図形が残れば失敗する。文字・アイコン・下線・パスは色の集合で照合する。

| 図形 | GPUI | Avalonia |
| --- | --- | --- |
| Fill（塗り） | 単色の quad | Border、ContentPresenter、Panel の背景、Rectangle |
| Band（枠線） | quad の枠 | Border などの枠（描かれる太さはレイアウトの丸め後の値） |
| Gradient | 2 色の線形グラデーションの quad | 2 色の `LinearGradientBrush`（始点色・終点色・角度） |
| Shadow | 影 | `BoxShadow` |
| Image | ポリクロームのスプライト | `Image` が実際に描く矩形と角丸 |

正規化の規則（ADR 18）:

- 角丸のない四角の、片側だけの枠は、その辺の帯（Fill）として扱う（GPUI の行罫線は quad の下枠、DataGrid の罫線は 1px の Rectangle）。
- GPUI の枠の太さは、各辺とも箱の幅・高さの半分までとして読む。GPUI のシェーダーは点のある象限の辺の太さを使うので、枠は箱の中心線までしか塗られない（メニューの区切り線は、2px の箱に 2px の下枠で、見えるのは 1px）。画素の領域と枠線の量にも同じ値を使う（ADR 25）。
- 完全に切り取られた図形（閉じた reveal の中身など）は数えない。GPUI は描かない。
- インクの色は、ウィンドウの外の要素を数えない。文字は行ごとのインクの帯（行の幅 × フォントの ascent + descent）で判定する。TextBlock の箱は文字より広く高いため。
- Rectangle はインクではなく箱として比べる。
- 不透明度の低い PathIcon（二色アイコンの薄い半分）は、同じ色のより濃い GPUI のスプライトの一部として認める。GPUI では 1 枚のスプライトのマスクの中にある。
- GPUI の path の色（ProgressCircle の弧など）もインクとして比べる。Avalonia の TextPresenter の選択範囲もインクとして数える。高さ 0 の Shape（破線の Separator）は数え、ジオメトリが空の Shape（掃引 0 の弧）は数えない。

| 項目 | 許容値 | 緩和 |
| --- | --- | --- |
| 色 | 各チャンネル ±1/255 | R11 |
| 位置・寸法・角丸 | ±0.26 論理 px（動きの途中フレームは ±0.51） | R9 |
| 枠線の太さ | ±0.001 px | – |
| 中央に置いた箱の横位置 | ±0.51 論理 px（1 デバイス px） | R9 |
| 文字を含むコンポーネントの高さ | GPUI より 1 論理 px 未満だけ低くてよい | R9 |
| spread 付きの影の角丸 | ±2.01 px | R3 |
| クリップされた図形の角丸 | クリップ側の角丸でよい | R19 |
| 5% 未満の線とインク | 構造では比べず、画素だけで比べる | R30 |

### 3. 画素

GPUI の Scene から各デバイス画素を領域に分類し、領域ごとの許容値で比べる。差は最大チャンネル差（1/255 単位）。

| 領域 | 対象 | 許容値 | 実測の最大（静止ケース） | 緩和 |
| --- | --- | --- | --- | --- |
| Flat | 縁から離れた平坦部 | 最大 2 | 2 | R11（半透明の色が別の塗りの上に重なると、アルファと合成で 2 回量子化される） |
| Edge | 輪郭から 1.5 デバイス px 以内。クリップが図形を切る位置も含む | 最大 64、平均 3 | 61、1.22 | R2、R9 |
| Ink | 文字・アイコン・下線・パス | 平均 18 | 15.4 | R1、R4、R18 |
| Ink（量） | インクの総量の比 | 0.6〜1.6 倍 | 0.75〜1.10（動きのフレームでは 0.65 から） | R1、R4 |
| 枠線（量） | 枠線ごと、4 つの角と 4 つの辺ごとのインク量の比 | 0.6〜1.6 倍 | 0.69〜1.32 | R2、R9 |
| Shadow | 影の広がり | 最大 12 | 11 | R3 |
| Image | 画像の内側 | 平均 3 | 0.19 | – |
| ImageEdge | 画像の縁から 2.5 デバイス px | 平均 12 | 6.3 | R31 |
| Gradient | グラデーションの内側 | 最大 5、平均 1.25 | 5、1.15 | R32 |

- Edge、Ink、Image、ImageEdge は、1 デバイス px ずれた位置との差のうち最小のものを使う（R9）。
- テストは緩和を挙げて画素を比べない範囲（Excluded 領域）を指定できる。使っているのは R33 の AvatarGroup の省略記号と、R22 の右クリックメニューのショートカット（ショートカットの文字から項目の右端まで。`InputMenuTests`）だけ。構造はその範囲でも比べる。
- インク量は、各インク画素がその下の塗りからどれだけ離れているかの総和。文字が少し太い・細いのは許し、文字・アイコン・線が欠けたり余計に描かれたりしたら検出する。GPUI 側の量が 200 未満のケースでは調べない。破線の Separator が描かれていなかった不具合は、この検査で見つかった（比 0）。
- 枠線の量は、GPUI の枠付きの quad ごとに、帯（外形と内縁の間と、その 1.5 デバイス px の AA）の画素を 4 つの角と 4 つの辺に分けて、インク量と同じ方法で数える（ADR 25）。Edge 領域は近傍の画素と比べるので、細い枠が子の背景に塗りつぶされたり消えたりしても通ってしまう。この検査ではその欠けを検出する。GPUI 側の量が 200 未満の部分は調べない。Accordion のカードの四隅が項目の背景に塗りつぶされていた不具合（比 0.41〜0.45）と、メニューの区切り線が 2px だった差（比 2.0）は、この検査で見つかった。
  - GPUI がクリップごとに分けて描いた同じ枠は 1 つにまとめる。位置が 1 デバイス px ずれても帯から外れないよう、クリップは 1.5 デバイス px 広げて数える（R9）。
  - クリップが辺を切る位置は丸めが分かれ、細い線が 2 倍にも 0 にもなる。その辺と両端の角は数えない（R9）。
  - Ink 領域と Excluded 領域の画素は数えない。

ケース単位で許容値を変えているのは、動きの途中フレーム（`Motion/MotionTolerance.cs`）と、半透明のカードを描く Dock のケース、重なった通知のカードだけ:

| ケース | 変更 | 理由 |
| --- | --- | --- |
| 不定値 Progress の途中で、GPUI のバーが角丸より細いフレーム | Flat 64、Edge 128 / 平均 6 | R19 |
| Tooltip、Select / Combobox / DatePicker のポップアップ（`uikit:Select`、`uikit:DateField` も）、Notification のカード（`uikit:NotificationItem` も）がフェード中のフレーム | Flat 13、Ink 平均 24、枠線の量は比べない（半透明の背景の下に透ける影のほうが、フェード中の枠より濃い） | R5 |
| Dock のタブのドラッグ中（不透明度 75% のプレビューのカード） | Flat 13、枠線の量は比べない | R5 |
| `uikit:NotificationList` の 2 枚以上の重なり（stack、expanded、bottom のグループ） | Shadow 最大 14 | R3（重なったカードの影で近似の差が足し合わさる。実測 13。1 枚の影は 12 以内） |

### 4. 動き

GPUI 側は仮想時計で 1 フレームずつ記録する（R15）。Avalonia 側も仮想時計で動かす。動きの始まりの状態を作ってトリガーを操作した後、時計を GPUI が記録した各時刻まで進めて描画し、そのフレームを構造と画素で比べる。描くのは Avalonia 自身のアニメーターなので、テーマが宣言した Transition や Animation が実際にどう動くかをそのまま確かめられる。位置は GPUI が動く端をデバイス px に丸めるので ±0.51 論理 px まで認める（R8、R9）。

| 動き | GPUI | Avalonia |
| --- | --- | --- |
| Checkbox / Radio のチェック、途中で戻したとき | spring_control で不透明度 | `Motion.Spring`（GPUI と同じばねの式で、速度を引き継ぐ） |
| Switch のつまみ、途中で戻したとき | spring_move で位置 | `Motion.Spring`（Canvas.Left の変化をばねで見せる） |
| Progress の値 | 180ms、easing_move | Width の Transition、SplineEasing(0.2,0,0,1)。最初に収まる幅は動かさない |
| 不定値 Progress | 1 秒周期の左右端 | 幅のキーフレーム + KeySpline |
| ProgressCircle の値、不定値 | 180ms の弧、回転する弧 | SweepAngle の Transition、弧のキーフレーム |
| Spinner | 0.8 秒で 1 回転 | RotateTransform のキーフレーム |
| Tooltip の表示 | 500ms 待ってから 150ms、ease-out-cubic でフェードと 4px | ShowDelay と、Opacity / TranslateTransform のアニメーション |
| スクロールバーの表示 | 300ms、linear | Opacity の Transition |
| スクロールバーの消去 | 2 秒待ってから 500ms、ease-in-cubic でフェードと 16px のスライド | ScrollBar の `HideDelay` と、Opacity / Track の RenderTransform の Transition |
| スクロールで表示（Hover / Scrolling モード） | スクロールから 2 秒表示して消える。Scrolling モードは帯の上で動かしたポインターがある間は残る | `Scrollbars` の Behavior（`Scrollbars.State`） |
| つまみを直接指したときの入場 | 300ms、ease-out-cubic で端から滑り込みながらフェード | `Scrollbars.State=SlidingIn` の Transition |
| つまみの拡大 | 300ms、ease-out-cubic で 6→8px | Width の Transition |
| Select / Combobox / DatePicker のポップアップ（`uikit:Select`、`uikit:DateField` も） | 150ms で 8px 下へスライドしながらフェード、リングと影は 4 乗 | キーフレーム（DatePicker と DateField は Margin でレイアウトを動かす） |
| InputGroup の色（`uikit:InputGroup` も） | HSLA の補間 | `Motion.ColorTransition` |
| Tabs のインジケーター、pill の文字色（Tabalonia も） | spring_move で位置と幅、色のフェード | `Tabs.Indicator`、`Tabs.SelectionFade` |
| Slider のつまみのリング（`uikit:RangeSlider` はつまみごと） | spring_control | `Motion.Spring` |
| loading のスピナー（Button、SplitButton、InputGroup のボタン） | Spinner と同じ 0.8 秒で 1 回転、ease_in_out | ボタンのアイコンの RotateTransform のキーフレーム（QuadraticEaseInOut）。静止ケースの `wait-300ms` で比べる |
| Accordion / Collapsible の開閉 | 自然な高さ × ばね（MotionReveal） | Canvas が内容を測り、`RevealConverters` で高さを掛ける |
| 1 項目だけを開く Accordion（`uikit:Accordion`） | 開く項目と閉じる項目が同じフレームから spring_control で動く | Expander の reveal のまま。`Accordion` が、開いた項目の `IsExpanded` の変化で他の項目を同期で閉じる |
| Notification の入退場（`uikit:NotificationItem` も） | 96px のスライドと 400ms のフェード、退場 200ms。左右中央の配置はスライドせずフェードだけ | キーフレーム、`Notifications.FromBottom` で向きを選ぶ。左右中央は `Notifications.Slides` が false の `:fades` でフェードだけのキーフレーム |
| 通知の重なりと展開（`uikit:NotificationList`） | ToastStack のばね（400ms、臨界減衰。位置と幅は ε 0.1px）で各カードの位置・幅・高さと不透明度 | 内部の `NotificationStack` が GPUI と同じ式（`Spring`）で毎フレーム置く。時計は Animation で進める |
| Resizable の pill（`uikit:ResizablePanelGroup` も） | 120ms で長さと濃さ | Transition、`Splitters.IsDragging`。`uikit:ResizablePanelGroup` のハンドルは GridSplitter の派生で同じテーマ |
| Carousel のページ送り | spring_move で 2 ページが 16px 離れて動く | `uikit:SpringSlide`（PageSlide の派生） |
| Carousel のトラック（`uikit:Carousels.TracksPointer`） | spring_move（ε 0.5）でトラックのオフセット。複数ページ先、ループの折り返し、ドラッグを離した後も同じばね。ループしないトラックは行き過ぎを端で止める | `uikit:CarouselTrack` が `Spring` を自分の時計で進める（`Motion.Spring` と同じ式で、速度を引き継ぐ） |
| Sidebar の開閉（`uikit:Sidebar` も） | 200ms、ease-in-out-cubic でクリップの幅、中身は即時 | PART_PaneRoot の Width の Transition。`uikit:Sidebar` は `PART_Clip` の Width の Transition（`Motion.SettledTransitions`）で、offcanvas は 200ms 後に面を隠す |
| Sheet の表示（`uikit:Sheet` も） | 150ms、linear で 100px 滑り込む | キーフレーム（`uikit:Sheet` は `:open` で） |
| Skeleton の明滅 | 2 秒周期、bounce(ease_in_out) で 1 → 0.5 → 1 | 1 秒の Alternate のキーフレーム、QuadraticEaseInOut（テンプレートの Border を動かす） |
| ShimmerText のスイープ | 2 秒、linear。12 層の文字を帯で切り抜く | `ShimmerText.Phase` を Animation で動かし、帯をクリップにする |

### 時刻

Avalonia には時刻を指定する公開 API がないので、テストに限り内部に手を入れて仮想時計にする（`Infrastructure/VirtualTime.cs`、ADR 14）。テーマ本体はリフレクションを使わない。

- Transition と Animation は、継承されるプロパティ `Animatable.Clock` の時計で動く。テストの各ウィンドウに、テストが進めたときだけ時刻が進む時計を設定する（`UnsafeAccessor` で内部の `ClockBase` を作り、`Pulse` を呼ぶ）。
- `DispatcherTimer`（Tooltip の表示遅延、スクロールバーの消去遅延、キャレットの点滅）は Dispatcher の時刻で動く。その時刻の取得元を仮想時計に差し替え、ディスパッチャーのループ自身のストップウォッチを止め、時刻を進めるたびに期限の来たタイマーを実行する。
- 時刻は 1ms ずつ進める。タイマーで始まった動きも、GPUI と同じ時刻から始まる。
- Avalonia の内部の名前（`Dispatcher._timeProvider`、`Dispatcher._impl`、`Dispatcher.PromoteTimers`、`ManagedDispatcherImpl._clock`、`ClockBase`、`Animatable.Clock`）に依存する（R7）。Avalonia の更新で変わった場合は、起動時の例外で分かる。
- 同じ扱いのテスト専用の措置: Calendar の「今日」（`CalendarDayButton.IsToday` の setter）、ヘッドレスのウィンドウが作らない描画装飾（`WindowDrawnDecorations` の `ApplyTemplate`、`RenderScaling`、`EnabledParts`。`Rendering/DecorationsHost.cs`）、ショートカットの macOS の表記（下）。
- `uikit:CalendarView` と `uikit:DateField` は、作られたときに `DateTime.Today` を読む（GPUI の CalendarState::new と同じ）。テストは内部のプロパティ `Today` で「今日」を固定する（`InternalsVisibleTo`）。
- 参照データは macOS で描くので、メニューのショートカットは ⌘ ⌃ ⌥ ⇧ で書かれる。テストのアプリは Avalonia.Native が登録するのと同じ `KeyGestureFormatInfo` を登録する（`TestApp.UseMacKeyNotation`。AvaloniaLocator の登録 API は参照アセンブリにないので、内部の `_registry` に `UnsafeAccessor` で入れる）。ヘッドレスのままでは「Ctrl+X」と書き、メニューが GPUI より広くなる。

テストのアプリは en-US のカルチャで動く（GPUI の月名・曜日名は英語）。ウィンドウは表示の前にスケール 2 に設定する。

### 挙動

| テスト | 内容 |
| --- | --- |
| Tooltip の遅延 | 499ms では開かず、500ms で開く（GPUI と同じ）。 |
| ポインター押下とフォーカスリング | Button / CheckBox / Radio はクリックでリングを出さず、Tab で出す（R27）。 |
| Switch | クリック後にリングを出す。つまみをドラッグするとすぐにポインターに付いていく。 |
| 無効な Button | クリックを無視する。 |
| メニューの矢印キー | 矢印キーで選んだ項目が hover と同じ見た目になる（R28）。 |
| AutoCompleteBox | 候補が Select と同じポップアップで開く。 |
| VirtualList | 見えている行だけを作る。 |
| Pagination | フォーカスしたページのリングが欠けない。表示数を超えたページはまるごとスクロールする。 |
| Tabs | キーボードのフォーカスでだけリングを出す。 |
| Notification | 閉じたカードは退場の後に取り除かれる。 |
| DataTable（TableView、DataGrid） | キー入力の後だけリングを出す。列を狭めても文字は揃えた側から溢れる。行が本体を埋めたかどうかを追う。 |
| Carousel | キーボードのフォーカスでだけリングを出す。 |
| TitleBar のボタン | 34 × 33 の大きさと 14px のアイコン、hover の色、最大化中の restore（macOS のゴールデンには出ない）。 |
| ColorPicker | ポップオーバーが 288px でスウォッチの 4px 下に開き、パレットと 16 進の欄を持つ。 |
| Clipboard | クリックで文字をコピーしてチェックを出し、その間のクリックは無視し、2 秒で戻る。 |
| Alert | 閉じるボタンは `CloseRequested` を出すだけで、Alert は残る（GPUI の on_close と同じ）。 |
| Rating | クリックで値を変える。埋まった星のクリックは 1 つ前まで戻す。disabled は無視する。 |
| Breadcrumb | 項目のクリック。無効な項目は反応しない。 |
| AvatarGroup | 上限までを表示し、超えたら省略記号を出す。 |
| HoverCard | 599ms では開かず 600ms で開く。カードの上にポインターがある間は開いたまま、離れて 300ms で閉じる。 |
| Stepper | 指標・ラベルのクリックで選択する。disabled は無視する。 |
| Tabalonia | ドラッグしたタブにインジケーターが付いていく。閉じるボタンと追加ボタン、メニューでの選択が効く。溢れたタブはスクロールし、選択したタブが見える。キーボードのフォーカスでだけリングを出す。 |
| Dock | ツールバーのメニューの Close がグループのパネルを閉じる。タブの閉じるボタンがドキュメントを閉じる。グループの左の 3 分の 1 へのドロップが左に分割する。 |
| DropDownButton | キャレットがあり、クリックでフライアウトを開き、開いている間は selected の見た目（`:flyout-open`）。 |
| クリアボタン | TextBox の `clearButton` はクリックで文字を消してフォーカスし、空・読み取り専用・無効・複数行では出ない。並びは mask toggle、クリアボタン、`InnerRightContent` の順。ComboBox の `clearButton` は選択を外し、リストは開かない。 |
| 右クリックメニュー | ポインターの位置に Cut / Copy / Paste / 区切り / Select All（ショートカット付き）を開き、GPUI と同じ条件（選択の有無、読み取り専用、マスク）で項目を有効にし、各項目が効く。複数行の TextBox、AutoCompleteBox、NumericUpDown、編集可能な ComboBox、CalendarDatePicker の欄も同じメニューを持つ。 |
| 編集可能な ComboBox | Tab でリングを出し、入力した文字の項目を選び、F4 / Alt+↓ で開く（Enter では開かない）。 |
| テーマの差し込み口 | NumericUpDown は prefix・値・suffix の順に並べ、増減ボタンが効く。Separator の label と GroupBox の footer は設定したときだけ出る。 |
| 空の表 | TableView と DataGrid は行がないときだけ空の表示を出し、行の追加と削除に従う。UIKitTable は出さない。DataGrid の `cell-selectable` は押したセルだけに選択色を付け、← → ↓ でセルが動く。 |
| loading のボタン | ポインター、Enter、Space、`IsDefault` の Enter を無視し、Click も Command も出さない。無効にはならず、Tab でフォーカスできる。解除すると元どおり動く。SplitButton はアクションの半分だけを止め、メニューは開く。 |
| フォーカスを奪わないボタン | `TakesFocusOnPointer="False"` のボタンを押しても TextBox のフォーカスと選択が残り、Click は出る。Tab ではフォーカスしてリングを出す。InputGroup のボタンも入力欄のフォーカスを残す。 |
| ButtonGroup、ToggleGroup | クリックが報告する選択（単一は押したボタン、`Multiple` は押したボタンを足す・外す）とトグルの状態。無効なグループは報告しない。グループのクラスが子に渡り、外すと消え、子自身のクラスは残る。ToggleGroup は Tab で次のトグルへ移り、矢印キーでは移らない。 |
| Accordion | 1 項目だけのモードはクリックでもコードでも 1 項目だけを開き、`ToggleClick` が開いた項目を報告する。`Multiple` は独立に開く。無効な Accordion はクリックを無視する。 |
| Toolbar | ← → でフォーカスが移り、端で折り返す（TextBox は矢印キーをキャレットに使う）。サイズのクラスが子に渡る（Button は ghost と compact、ToggleButton はサイズだけ、`TakesSize="False"` と Separator には渡らない、ToolbarGroup は受け取って子に渡す、large は medium）。`ToolbarSpacer` が残りの幅を等分する。 |
| uikit:Inputs | `Pattern` / `Validate` は編集後の全文で編集を拒否し、キャレットも動かない。アプリが入れた通らない文字列は編集できる。NumberMask の 3 桁区切りと GPUI の式のキャレット、`UnmaskValue`、PatternMask の placeholder と区切り、GPUI の mask_pattern.rs のテストの例。`CleanOnEscape` は Esc で消し、Undo で戻る。 |
| インデント | Tab はキャレット位置に、選択があれば選択の掛かる行の先頭に入れ、Shift+Tab は行頭のインデントを外す。Cmd / Ctrl + ] / [ は行単位。`HardTabs` はタブ文字。`TabSize` が 0 か読み取り専用なら、Tab はフォーカスを移す。 |
| NumberInput | 文字の制限と全角の数字、GPUI の test_step_value の全例、↑↓ の精度、`StepBy`。`StepsValue="False"` は ↑↓ とボタンで `Step` だけを出す。 |
| InputGroup | アドオンの押下で入力にフォーカスし、アドオンの中のボタンは自分のクリックを受ける（リングは入力のフォーカスにだけ付く）。入力を無効にするとグループとボタンが無効になる。`IsReadOnly` ではボタンは使える。入力の検証エラーと `IsInvalid` で `:invalid`。 |
| RangeSlider | トラックの押下で近いつまみが動き、範囲ではトラックからのドラッグは続かない（単一値は続く）。ドラッグはもう一方のつまみで止まり、値は Step に丸め、つまみはポインターの位置に置く。`Changed` は動くたび、`Released` は 1 回。対数の対応（1..1000 の 1/3 が 10、`Minimum` 0 は線形）。縦は下から数える。 |
| uikit:Select | 検索は大文字小文字を区別しない部分一致で、先頭の一致にカーソルを置き、一致なしは空の表示。100ms のスピナーの後にクリアボタン。グループは行か見出しの一致で残る。閉じたトリガーで ↑↓ Enter が開き、カーソルは両端で折り返して見出しを飛ばす。Enter で確定、Esc で閉じ、閉じるたびにクエリを消す。複数選択はクリックと Enter で切り替え、開いたまま。`SelectionChanging` で取り消し・差し替え。無効な行は選べない。1000 行で見えている行だけを作る。 |
| ListView | クリックと Enter で選択して確定し（Cmd / Ctrl で secondary）、Esc で選択を外す。↑↓ は端で折り返し、見出し・末尾・空のセクションを飛ばす。右ボタンは行に枠を付けるだけで選択しない。`selectable(false)` は何も選ばない。検索欄、読み込み中、末尾から `LoadMoreThreshold` 行での 1 回の `LoadMore`、`scroll_to_item`。10000 行で見えている行だけを作る。 |
| Tree | 押下で選択してフォルダを開閉する（無効な行は無視）。↑↓ は端で折り返し、→ で開き ← で閉じ、Enter で開閉する。右ボタンの印はフォルダの開閉で消える。`reveal_item` と `SelectedItem` は祖先を開いて中央へスクロールする。10100 行の木で見えている行だけを作る。任意のデータ（`ChildrenSelector`、`FuncTreeDataTemplate`、`DisabledSelector`）。 |
| CalendarView | 範囲は 2 日目で完成し、開始より前の日や完成した範囲へのクリックでやり直す。`Selected` は完成した値だけ。3 種類の無効な日はクリックに反応しない。別の月の日でその月に移る。月・年のグリッドで選ぶと日に戻り、年のページは両端で止まる。 |
| TimeField | GPUI の SegmentEditor のテスト（入力と前進、25 のやり直し、繰り上がりなしの増減、端で止まる移動、12 時間）。編集だけを報告し、Tab はセグメントを進めてから欄を出る。クリックでセグメントを選ぶ。値のない欄は「--」で、数字から始まる。 |
| DateField | 日を選ぶと閉じて報告し、範囲は 2 日目で報告する。時刻を編集するときは開いたままで、同じ日で閉じる。クリアボタンはフォーカスを取らずに消す。Delete / Backspace、Enter / Escape、プリセット、無効な欄は開かない、`:error` の枠、表示の書式（GPUI の display_format）。 |
| TextLabel | 文字・補足・一致・マスクの変更ごとに run を作り直し、`Text` は label の文字のまま。マスクは文字ごとに • で、補足の色も一致もない。 |
| AsyncImage | 199ms では何も出さず、200ms で読み込み中の内容を出し、届いたら画像にする。150ms で終わった読み込みは読み込み中の内容を出さない。失敗は fallback を出して再試行せず、`Source` を変えると読み直し、古い読み込みの結果は捨てる。既定のローダーはソースごとに画像を共有し、失敗は保持しない。 |
| uikit:Icon | 全 `IconName` のジオメトリがテーマにあり、`Kind` が形を決め、ボタンのサイズのクラスが中の `uikit:Icon` にも効く。二色のアイコンが薄い半分を描く。 |
| ItemsScrolling | ListBox の 1000 行で Center は中央へ（末尾では端で止まる）、Top / Bottom は見えていない行を近い端へ動かし、見えている行は動かさない。最初のレイアウトの前の呼び出しも効く。 |
| uikit:Pagination | 番号と省略記号の並び（GPUI の calculate_items と ellipsis_menu_pages の例）、`request_page` の条件と `PageChanged`、ページ・前・次のクリック、100 ページの長いメニュー（240px でスクロール。R37）、compact。 |
| Carousel のトラックとボタン | 前後のボタンは端で無効（`WrapSelection` なら有効）で、クリック後にリングなしで carousel にフォーカスする。ドラッグは 2px でロックして付いていき、離すと最も近いページを選び、ページ内のボタンをクリックしない。ホイールのノッチとバースト、トラックパッドのスクロールとスナップ（R38）。縦の carousel は端のノッチを親へ渡す。ループは最後から最初へ進んで周回に戻る。 |
| Tabs の menu と溢れたタブ | 一覧のメニューは選択中のタブにチェックを付け、無効なタブを無効にし、選んだタブを選択する（TabStrip、TabControl）。アイコンのタブはアイコンで一覧する。溢れたタブはスクロールして選択したタブを見せ（prefix は動かない）、行のクリップがフォーカスリングを残す。 |
| uikit:Table | 行の幅を `ColSpan` で分け、パディングを引いた basis の比で縮める（2 列の見出し 223.5 と下の 115 + 115）。1 列 100px より細くしない。`Width` の列は縮まず、収まる幅は伸ばさない。 |
| uikit:ColorSelect | ポップオーバーが 288px でトリガーの 4px 下に開き、featured の 12 色が 18px に縮み、パレットが 99 色。色を指すとプレビューし（値は変えない）、クリックで確定して閉じる。16 進の欄は HEX_PATTERN だけを受け付け、Enter で確定して閉じ、トリガーにフォーカスを戻す。スライダーは確定して開いたまま。disabled は開かず、開いている間に disabled になると閉じる。 |
| ResizablePanelGroup | サイズのないパネルが残りを等分する。ドラッグの連鎖と解放時の 1 回の報告、2px のしきい値、`ResizePanel`、隠したパネル（戻すと元の長さ）、グループの長さの変化での比の伸縮、パネルの追加と削除、ハンドルの矢印キー。 |
| Sheet | 開くと面にフォーカスを移し、Tab がシートの中で循環し、Esc で閉じて元に戻す。ウィンドウに 1 つで、差し替えは戻すフォーカスを引き継ぐ。閉じられるオーバーレイは左ボタンで閉じる（右ボタンでは閉じない）。 |
| Sidebar | 折り畳むと部品が `:collapsed` になり、ラベル・グループ名・サブメニューが隠れ、ツールチップが出る。offcanvas は 200ms 後に内容を隠す。項目の `Click`、`Command`、キャレット、click_to_open / click_to_toggle、無効な項目。`SplitView.Pane` の中では SplitView に従う。 |
| NotificationList | 入場から 5.4 秒で閉じ、退場 200ms の後に消える。action があると残る（`AutoHide` で変える）。ポインターかフォーカスがある間は止まり、離れると残りから再開する。同じ `Id` と `Key` は退場なしに置き換えて最新にする。`Remove` と `Clear`。`MaxItems` を超えた古いものは隠れて待つ。配置ごとのスタック、GPUI の stack_geometry のテストの値、新しいカードで古いカードがばねで 14px 下がって狭くなること、4 枚目は畳んでいる間描かないこと。クリックは `Click` を処理するときだけ閉じ、中クリックと閉じるボタンは常に閉じる。TopLevel 用のリストはアドーナー層に置かれ、カードの外の入力を通す。 |
| uikit:TitleBar | キャプションボタンの 34 × 33 と 14px のアイコン、hover の色、役割、最小化・最大化 / 復元・閉じる、ウィンドウが許すボタンだけ、装飾に広がっていないウィンドウでは出ないこと。子が両端に分かれて縦の中央に並ぶ。バーの役割（Windows / macOS は TitleBar、ほかは None）、フォーカスできる子は User。バーのダブルクリックで最大化 / 復元する。OS の移動と最大化は実機では確かめていない。 |
| 計算 | Avatar の頭文字（GPUI の extract_text_initials）、Kbd の表記（GPUI の test_format、macOS と他の OS）、DescriptionList の行の分け方（GPUI の test_group_item_rows）、TextLabel の一致の範囲（GPUI の test_highlight_ranges）、ColorSelect の 16 進（GPUI の parse_hex / hex_string のテストの例）、月の週の数（GPUI の test_days）。 |

## 見た目だけのコード

テーマで書けない見た目と動きは、ADR 15 の範囲のコードで補う。どれもテーマのスタイルから適用し、アプリはテーマを追加するだけで使える（差し込み口だけは、アプリが内容を置く）。リフレクションは使わない。

| 種類 | 名前 | 用途 |
| --- | --- | --- |
| Behavior | `Motion.Spring` / `SpringTarget` / `SpringValue` / `SpringsCanvasLeft` / `SpringTravel` | GPUI と同じばね（速度を引き継ぐ） |
| | `Motion.SettledTransitions` | 最初のレイアウトの後に Transition を付ける |
| | `Motion.ColorTransition`（`ColorTransition`） | HSLA での色の補間 |
| | `Scrollbars.*` | Hover / Scrolling モード、スクロールでの表示、つまみの hover |
| | `Selects.TracksCursor` / `CursorItem` | ポップアップを開いている間、カーソルの項目を欄に表示する |
| | `Tabs.Indicator` / `SelectionFade` | 選択に付いていくインジケーター、pill の文字色 |
| | `Splitters.TracksDrag` / `IsDragging` | 押して動かしたらドラッグ中 |
| | `Tables.TracksKeyboardFocus` / `ShowsFocusRing` | キー入力の後だけのリング（TableView、DataGrid） |
| | `Tables.TracksFill` / `IsFilled` | 行が本体を埋めたら最後の罫線を消す |
| | `CalendarGrid.Columns` | 月のグリッドを 3 列に並べ替え、列の端をデバイス px に丸める |
| | `TextLines.RoundsWidthUp` | 文字の幅を論理 px に切り上げる（GPUI と同じ。ちょうど整数の幅も 1px 大きくする） |
| | `TextLines.CentersTallGlyphs` | 行より高い文字を行の中央に置く |
| | `TextLines.StartsTrimmedText` | 省略記号で切った文字を左に寄せる（GPUI と同じ） |
| | `DragTabs.FollowsDrag`（Tabalonia） | ドラッグ中のタブにインジケーターをばねなしで付ける |
| | `DockSplitters.Straddles`（Dock） | 分割のハンドルを両側のグループの上に重ね、場所を取らない |
| | `DockTargets.MarksTab`（Dock） | 挿入先のタブの区切り線を、レイアウトを変えずに消す |
| | `uikit:SpringSlide` | Carousel のページ送り |
| 差し込み口（ADR 30） | `GroupBoxes.Footer`、`Separators.Label`、`Spinners.Icon`、`ProgressCircles.Content`、`Tabs.Prefix` / `Suffix` | アプリが置き、テーマのテンプレートが描く内容（GPUI の footer、label、icon、children、prefix / suffix）。挙動は変えない |
| 値の受け渡し（ADR 17） | `Tables.CellPadding` / `CellVerticalAlignment` / `RowHeight` / `ShowsResizeHandles`、`Notifications.FromBottom` / `Slides` | コードで作られる子に、親の見た目を渡す |
| 値 | `TextMenus.SelectAllGesture`（internal） | 右クリックメニューの Select All に出すプラットフォームのショートカット（Cut / Copy / Paste は `TextBox.CutGesture` などを使う） |
| Converter | `AffineConverter`、`ThicknessWhenConverter`、`ThicknessFilterConverter`、`AboveConverter`、`FirstNonNullConverter` | 数値・余白の変換 |
| | `PlacementConverter` | Popover の配置から間隔と矢印 |
| | `OverflowClipConverter` | 溢れたタブの行のクリップを、隠れた内容のない辺だけ広げる（フォーカスリングを残す） |
| | `RevealConverters`、`FadedShadowConverter` | 開閉の高さ、影のフェード |
| | `RoundedClipConverter` | Image の角丸 |
| | `CalendarConverters`、`TimeConverters`、`TableConverters` | 月名・年・時刻の表示、縞の詰め物行 |
| | `ColorConverters`、`ColorPickerConverters`（ColorPicker） | GPUI の単精度の HSL での darken / lighten と 16 進（ColorPicker のパッケージは本体の `ColorConverters` を使う） |
| | `LinearThicknessConverter`、`AvatarConverters`、`StepperConverters`、`FormConverters` | Badge のずれ、円の角丸、Stepper の線、Form の間隔 |
| | `DockConverters`（Dock） | タブバーとタイトルバーの切り替え、Dock が半透明にするドロップ先の塗りの表示 |

新しいコントロール（ADR 19、`src/AvaloniaUIKit/Controls/`）は、プロパティ・疑似クラス・テンプレートの部品と、そのコンポーネント自身の操作だけを持つ: `Badge`、`TagLabel`、`Alert`、`Skeleton`、`StatusBar`、`Breadcrumb` / `BreadcrumbItem`、`Kbd`、`Clipboard`、`Rating` / `RatingStar`、`Avatar`、`AvatarGroup`、`EmptyState`、`DescriptionList` / `DescriptionItem` / `DescriptionSeparator`、`Stepper` / `StepperItem`、`Form` / `FormField`、`HoverCard`、`ShimmerText`、`Marker`、`Bubble`、`Message`。補助として、GPUI の色の選び方（`FxHash`: rustc-hash 2.1 の FxHasher、`OkLab`: mix_oklab）と、長さを分け合うパネル（`DescriptionRowPanel`、`StepperPanel`、`FormPanel`、`MarkerPanel`。辺をデバイス px に丸める `LayoutSnap`）を持つ。

サードパーティのライブラリのパッケージ（ADR 28）は、ライブラリのテンプレートの約束が GPUI の部品を置けないところだけコントロールを足す（ADR 29）。最初に Tabalonia のパッケージに足した `TabsMenuButton`（タブを並べた一覧を開く、タブバーの末尾のボタン）は、本体の TabStrip / TabControl の `menu` クラスも使うので、本体（`src/AvaloniaUIKit/Controls/`、名前空間は同じ `AvaloniaUIKit`）に移した。

## 機能を足したコントロールと添付プロパティ

既存のコントロールにない機能は、アプリが設定したときだけ挙動を変える添付プロパティと、新しいコントロールで足す（ADR 30、`src/AvaloniaUIKit/Controls/`）。テーマから自動で当たるコードは、上の見た目だけのコードのまま。新しいコントロールの中の Button、TextBox、ListBox、Popup などはテーマ済みの標準のコントロールを使い、項目の文字や子は関数（`Func<object?, string>` など）か、コンパイル済みのバインディングとテンプレートで受け取る。リフレクションは使わない。

- 挙動を変える添付プロパティ: `Buttons.IsLoading` / `LoadingIcon` / `TakesFocusOnPointer`、`Inputs.Pattern` / `Validate` / `MaskPattern` / `CleanOnEscape` / `TabSize` / `HardTabs`（マスクの型 `MaskPattern` / `PatternMask` / `NumberMask`、`Inputs.UnmaskValue`）、`Toolbar.TakesSize`、`Carousels.TracksPointer`。拡張メソッド `ItemsScrolling.ScrollToItem`（`ScrollStrategy`。ListView と Tree も使う）。
- 新しいコントロール: `ButtonGroup`、`ToggleGroup`、`Accordion`、`Toolbar` / `ToolbarGroup` / `ToolbarSpacer`、`InputGroup` / `InputGroupAddon`、`NumberInput`（`NumericUpDown` の派生）、`RangeSlider`、`Select` / `SelectGroup`（`SelectTriggerContext`、`SelectionChangingEventArgs`）、`ListView` / `ListItem` / `ListSection`（`IndexPath`）、`Tree` / `TreeItem`、`CalendarView`、`DateField`、`TimeField`、`Table` と部品（`TableHeader`、`TableBody`、`TableFooter`、`TableRow`、`TableHead`、`TableCell`、`TableCaption`）、`ColorSelect` / `ColorSwatch`、`Pagination`、`CarouselPrevious` / `CarouselNext`、`CarouselTrack`（Carousel の ItemsPanel）、`TabsMenuButton`、`TextLabel`、`AsyncImage`（`IImageLoader`、`ImageLoader`）、`Icon`（生成した `IconName`）、`ResizablePanelGroup` / `ResizablePanel`、`Sheet`、`Sidebar` と部品（`SidebarHeader`、`SidebarFooter`、`SidebarGroup`、`SidebarMenu`、`SidebarMenuItem`、`SidebarToggleButton`。折り畳みは継承される `Sidebar.IsIconCollapsed` で部品に渡す）、`NotificationList` / `NotificationItem`（`NotificationCard` の派生、`NotificationPlacement`）、`TitleBar`。
- テンプレートの部品: `SelectList` / `SelectListItem` / `SelectGroupHeader`、`TreeEntry`、`CalendarViewItem`、`RangeSliderTrack`、`InputGroupPanel` / `InputGroupAddonPanel`、`TableRowPanel`。
- 内部の補助: `GroupClasses`（グループのクラスを子に渡し、渡したものを外す）、`ToolbarLayout`、`ListRows`（ListView と Tree の行の仮想化）、`CalendarRow` / `CalendarPickerGrid`、`GpuiHsla` / `GpuiColor`（GPUI の単精度の HSL と 16 進、パレット）、`SwatchRowPanel` / `StripPanel`（ColorSelect の行と色帯）、`ResizablePanelHandle`（GridSplitter の派生）、`NotificationStack`（配置ごとの通知の重なり）、`TitleBarPanel`（両端に分ける行）。辺をデバイス px に丸める `LayoutSnap` に、gpui-pre と同じく中点を 0 の側へ丸める `GpuiEdge` / `GpuiRuns` を足した（`uikit:Table`、`uikit:ColorSelect`）。

## 緩和の一覧

100% の一致が原理的にできない箇所だけ、理由を付けて許容値を緩めている。コード中のコメントにも同じ ID を書いている。

| ID | 内容 | 扱い |
| --- | --- | --- |
| R1 | グリフのラスタライズが異なる（GPUI は CoreText、Avalonia は Skia + HarfBuzz）。同じ Inter でも濃さと AA が違う。 | Ink 領域の平均とインク量の比で比べる。 |
| R2 | 図形の縁の AA が異なる（GPUI は SDF、Skia は解析的 AA）。 | Edge 領域を別の許容値で比べる。 |
| R3 | 影のぼかしの近似が異なる。spread 付きの影の角丸は、GPUI では要素のまま、Skia では spread 分だけ大きくなる。 | σ を Blur に変換（σ = 0.288675 × Blur + 0.5）し、Shadow 領域の最大値と影の角丸を緩める。角丸を保ちたい影（Notification）は、角丸を広げた別の Border で落とす。 |
| R4 | SVG アイコンのラスタライズが異なる（resvg と Skia の Path）。 | R1 と同じく Ink 領域で比べる。 |
| R5 | 不透明度のかけ方が異なる。GPUI は図形ごと、Avalonia はグループ全体にかける。重なった図形がフェード中だけ違って見える。 | フェード中のフレームと、半透明のまま描くカード（Dock のドラッグのプレビュー）だけ Flat を 13 に緩め（理論上の最大 12.75）、枠線の量を比べない。 |
| R6 | spring の途中で目標が変わったときの速度の引き継ぎ。Avalonia の Transition は速度 0 から始まる。 | 解消済み。`Motion.Spring` が GPUI と同じ式で速度を引き継ぐ。途中で戻す動きも比べる。 |
| R7 | Avalonia に時刻を指定する公開 API がない。 | テストに限り、内部の時計と Dispatcher の時刻を差し替える（[時刻](#時刻)）。Avalonia の内部に依存する。 |
| R8 | spring は ε 以内で止まる。止まるかどうかを調べる時刻が、GPUI は描画のたび、`Motion.Spring` は 1ms ごとで異なる。 | 差は ε（つまみで 0.1px）以内。動きのフレームの位置の許容値 ±0.51px に含める。 |
| R9 | レイアウトの丸めが異なる。GPUI は文字の幅と高さを論理 px に切り上げ、端を最近傍のデバイス px に丸める。Avalonia はデバイス px に丸める（大きさは切り上げ）。 | 文字の幅の切り上げは `TextLines.RoundsWidthUp` で再現し、幅は ±0.26px で一致する。ちょうど整数の幅の文字は 1px 大きくする（GPUI の macOS のテキストは行の最初の run をフォントサイズの次の浮動小数で組むので、幅がわずかに増えて次の px に切り上がる）。位置 ±0.26px、中央に置いた箱の横位置（GPUI は文字を中央に置いてから幅を切り上げるので、半 px の丸めの向きが分かれる。Popover、Calendar の月、ColorPicker のツールチップ）、折り返した文字の高さ、Edge / Ink の近傍比較、クリップの切り口を Edge にする。枠線の量は、クリップを 1.5 デバイス px 広げて数え、クリップが切る辺を数えない。 |
| R10 | 行の高さがフォント本来の高さより小さいときの文字の寄せ方（GPUI は中央、Avalonia は上）。 | 解消済み。`TextLines.CentersTallGlyphs` が文字を中央に寄せる。 |
| R11 | 色の量子化（GPUI は float の HSLA、Avalonia は 8bit）。 | 色と Flat 領域で ±1/255。 |
| R12 | ポップアップを画面内に収める処理が異なる。 | 画面端にかからない位置のケースだけを比べる。 |
| R13 | テーマの既定フォントはシステム UI フォント。 | 検証はすべて同梱の Inter で行う。システムフォントでの一致は保証しない。 |
| R14 | 参照データは macOS（Metal）でしか作れない。 | 許容値は macOS arm64 で校正した。ほかの OS でもテストは動くが、校正はしていない。macOS で GPUI が描かないもの（TitleBar のボタン）は Avalonia 側の検査で確かめる。 |
| R15 | 参照の生成器は時刻と「今日」の読み取りだけにパッチを当てている。 | 描画には影響しない。`vendor.sh` が置き換え漏れを検査する。 |
| R16 | Avalonia にだけある状態（CheckBox の不確定状態、編集中のセルなど）。 | 比較しない。読める見た目であることだけを保つ。 |
| R17 | rem は 16px に固定。 | GPUI の rem を変える設定は対象外。 |
| R18 | 下線の位置と太さ。GPUI は descent の 0.618 倍、Avalonia はフォントの値を使う。 | Ink 領域として比べる。 |
| R19 | クリップされた図形の輪郭はクリップ側になる。不定値 Progress で角丸より細いバーは、GPUI では幅 2r の pill、Avalonia では 2 つの丸い端が重なった形になる。 | 構造比較でクリップ側の角丸を認め、該当フレームの画素の許容値を緩める。 |
| R20 | スクロールバーの Scrolling モード（スクロール中だけ表示）と、Hover モードで隠れたバーのつまみを直接指したときのスライド入場（SlideAndFade）。 | 解消済み。`Scrollbars` の Behavior がスクロールと帯の上のポインターを追い、表示状態を 1 つのプロパティにまとめてテーマに渡す。 |
| R21 | Tooltip の閉じる前の猶予と、隣への切り替えスライド。 | 対象外。 |
| R22 | ショートカットの表記は OS ごとに異なる。修飾キーの記号（⌘ ⇧ など）は同梱の Inter になく、描画系ごとのフォールバックのフォントで描かれる。 | 修飾キーのないショートカット（F5）とキー（Kbd）だけを画素で比べる。TextBox の右クリックメニューは、ショートカットの文字から項目の右端までを画素で比べない（Avalonia は「⌘+X」、GPUI は「⌘X」と書く。色は比べる）。Kbd の表記は GPUI のテストと同じ例で、macOS と他の OS の両方を単体テストで確かめる。 |
| R23 | 欠番。 | – |
| R24 | テーマが持たないコントロール側の処理。NumericUpDown はボタンの押下やフォーカスでテキストを選択し、TextBox は Tab で全選択する。GPUI はしない。 | テストで選択を戻してから比べる。文字を打つケース（`uikit:Inputs`、`uikit:NumberInput`）は、選択を外してキャレットを先頭に戻してから打つ。 |
| R25 | キャレットの点滅の位相。 | キャプチャではキャレットを隠す。 |
| R26 | 欠番。 | – |
| R27 | ポインター押下時のフォーカス。GPUI はフォーカスを移さない。Avalonia は移すが `:focus-visible` にはしない。 | どちらもリングは出ない。挙動テストで確かめる。Button と SplitButton は `uikit:Buttons.TakesFocusOnPointer="False"` で GPUI と同じくフォーカスを移さない（アプリが選ぶ。既定は Avalonia のまま）。 |
| R28 | Avalonia のコントロールが開いたときやフォーカスを得たときに先頭を選ぶ（メニューの先頭の項目、DataGrid の先頭行）。GPUI は何も選ばない。 | テーマは選択にも hover と同じ見た目を付ける。テストは開いた直後・フォーカス直後の選択を外してから比べる。 |
| R29 | スクロールバーの帯の上のポインター。GPUI は帯の下の行に hover を付けるが、Avalonia では帯の ScrollBar がポインターを受ける。 | 一覧のケースはポインターを帯にかけない。 |
| R30 | GPUI は 0 でない線を少なくとも 1 デバイス px に広げる（snap_stroke）。Avalonia のレイアウトの丸めは半デバイス px 未満の線を消す。消えかけの線とインクは描くかどうかが分かれる。 | 不透明度 5% 未満の線とインクは構造では比べず、画素だけで比べる。 |
| R31 | GPUI はアトラスの透明な隣接画素と補間して拡大した画像の縁を半ソース画素ぶん薄める。Avalonia は端を伸ばす。 | 画像の縁 2.5 デバイス px を ImageEdge 領域として平均 12 まで認める。 |
| R32 | GPUI のシェーダーはグラデーションをディザする。Skia はしない。1 つの三角分布のノイズで各色を ±2/255、アルファを ±3/255 動かすので、不透明なグラデーションも最大 3/255 透けて下の色が混ざる。差は最大で 2 + 3 × 下との色の差（≦ 5）、平均で (4 + 3 × 下との色の差) / 6（≦ 1.17）になる。 | グラデーションの内側を Gradient 領域として最大 5、平均 1.25 まで認める。 |
| R33 | 同梱の Inter にない文字（AvatarGroup の省略記号「⋯」、U+22EF）は、描画系がそれぞれのフォールバックのフォントで描く。 | その文字を含むアバターの輪の内側だけ画素を比べない（`VisualAssert.Matches` の `excluded`）。色と、塗り・輪の形は比べる。 |
| R34 | GPUI は折り返した行の末尾の空白も含めて行を中央に寄せ、折り返す位置を決める。Avalonia は末尾の空白を数えない。 | 中央寄せの文字（EmptyState）は 1 行に収まるケースで比べる。折り返す位置が空白 1 つ分の差で変わる幅（Alert の small）はケースの幅を変えて避ける。 |
| R35 | Dock のドラッグのプレビューの位置。Dock.Avalonia はデスクトップでは別のウィンドウに出し、ウィンドウのない描画（managed）ではレイヤーの原点に置いたままにする（Dock 12.1.0.6 が Avalonia 12 の可視ツリーの根を TopLevel と見なすため）。GPUI はタブの角をドラッグの始まりからポインターと一緒に動かし、ドロップ先の上に描く。 | テストがプレビューを OverlayLayer に移し、GPUI と同じ位置に置く（`Adapters.Dock.cs` の `PlaceDockDragPreview`）。位置は Dock のもの、カードの見た目はテーマのもの。 |
| R36 | Dock.Avalonia はタブを押した時点で選択し、ドロップ先の操作を 2 回目の移動で決め、タブバーの中でのドラッグを並べ替えに使わない。GPUI は選択を変えずにドラッグし、最初の移動で決める。 | ドロップのケースは選択中のタブをドラッグし、タブバーの外へ一度出し、ドロップ先の上で 2 回動かす。 |
| R37 | GPUI のスクロールするポップアップメニュー（`scrollable(true)` と `max_h`）は、項目があふれると Taffy の flex_shrink で項目を文字の高さ（14px の phi = 22.65px）まで縮める（9 項目なら 24px）。Avalonia の MenuFlyoutPresenter は 26px のまま。 | `uikit:Pagination` の省略記号のメニューは、8 ページ以下のものだけを画素で比べる。あふれるメニュー（最大 100 ページ、240px でスクロール）は挙動テストで確かめる。 |
| R38 | Avalonia のホイールイベントには、差分が行（ホイール）かピクセル（トラックパッド）かの区別も touch phase もない。 | `uikit:CarouselTrack` は整数の行の差分をノッチ（GPUI の ScrollDelta::Lines）、それ未満をトラックパッドのピクセル（1 行 50px）とし、28ms 入力がなければジェスチャーを終える（GPUI の SCROLL_EVENT_SEPARATION。GPUI も phase のない環境ではこれを使う）。描画のケースはこの規則のもとで GPUI と一致する。macOS の Avalonia.Native はホイールの差分を 5 で割るので、ノッチがトラックパッドとして扱われることがある。 |

### まだ比べていない差

どのケースにも出ないが、GPUI と違うと分かっているもの。

- ScrollViewer のつまみの長さ。GPUI（gpui-base の scrollbar.rs の ThumbGeometry）は長さを container / content × container（最小 48）とし、両端の inset 4 を引いて描き、移動量は container − 長さ。テーマは Track の Margin が 0,4 なので、（container − 8）× 比（最小 40）になる。最小に当たるときは一致し、つまみが 48 を超えるときと、比が 40 / (container − 8) と 48 / container の間のときに数 px ずれる。今のケースはどれもつまみが最小になる長さにしている。
- 長さを分け合うパネルの辺の丸め。`LayoutSnap.Edge` は中点を 0 から遠い側に丸め、gpui-pre（`round_half_toward_zero`）は 0 の側に丸める。CalendarView の行と月・年のグリッド、DescriptionList、Stepper、Form、RangeSlider が使い、中点に当たるケースはない。`uikit:Table` と `uikit:ColorSelect` は GPUI と同じ `LayoutSnap.GpuiEdge` を使う。

## 利用側の約束

GPUI と同じ見た目・挙動にするため、アプリ側で次の設定をする。

| 項目 | 設定 | 理由 |
| --- | --- | --- |
| サブメニューの表示 | `DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero` | GPUI は hover するとすぐにサブメニューを開く。 |
| MenuFlyout の配置 | `Placement="BottomEdgeAlignedLeft"`（SplitButton は `BottomEdgeAlignedRight`） | テーマが GPUI のトリガーとの間隔 4px を付ける。ContextMenu はポインター位置に開く。 |
| フォント | `UIKit.FontFamily` リソースを差し替える | 既定はシステム UI フォント（GPUI の `.SystemUIFont` と同じ）。検証と同じ Inter にする場合は差し替える。 |
| NumericUpDown | `ButtonSpinnerLocation` は効かない | GPUI と同じく [−] 値 [+] の順に固定。 |
| NumericUpDown の前後の内容 | `InnerLeftContent` / `InnerRightContent` | GPUI の NumberInput の prefix / suffix。 |
| DropDownButton | `MenuFlyout Placement="BottomEdgeAlignedLeft"`。クラスは Button と同じ | GPUI の dropdown_caret のボタンは左下にメニューを開く。 |
| スクロールバーの表示モード | `ScrollViewer.AllowAutoHide` と `uikit:Scrollbars.ShowOnHover` | False / – が Always、True / True（既定）が Hover、True / False が Scrolling。 |
| ButtonGroup | `StackPanel Classes="button-group"`、Spacing 0 | 中の Button の角と境界を GPUI のグループにする。 |
| ToggleGroup | `ListBox Classes="toggle-group"`、`SelectionMode="Multiple,Toggle"`（単一選択なら `Single`） | 選択の規則はアプリが決める。`segmented` で角をつなぐ。各トグルを Tab で移る GPUI のキー操作にするなら `uikit:ToggleGroup`。 |
| Label | `TextBlock Classes="label"`、補足は `Run Classes="secondary"` | TextBlock の既定テーマは置かない（全テンプレートの文字に効くため）。 |
| Input | パスワードは `PasswordChar="•"`、表示の切り替えは `revealPasswordButton` クラス | GPUI のマスク文字と mask_toggle。 |
| クリアボタン | `TextBox Classes="clearButton"`、`ComboBox Classes="clearButton"` | GPUI の cleanable。Fluent と同じクラス名。 |
| 右クリックメニュー | TextBox の既定の `ContextFlyout`。文言はリソース `UIKit.Strings.Cut` / `Copy` / `Paste` / `SelectAll` で変え、別のメニューにするなら `ContextFlyout` を設定する | GPUI の Input のメニュー。 |
| 編集可能な Select | `ComboBox IsEditable="True"`（入力した文字の項目を選び、F4 / Alt+↓ で開く） | Avalonia の規則。 |
| InputGroup | `TextBox Classes="group"` と `InnerLeftContent` / `InnerRightContent` | 前後の内容と入力欄を 1 つの枠にする。 |
| List | 行の内容は `ItemTemplate`。キー操作の後はポインターを動かすまで hover を描き直さない（GPUI） | ケースは先にポインターを外す。 |
| Combobox | 入力で絞り込むなら `AutoCompleteBox` か `ComboBox IsEditable="True"`、選ぶだけなら `ComboBox Classes="combobox"`。検索欄・複数選択・footer は `uikit:Select Classes="combobox"` | – |
| Popover | GPUI のアンカーと Placement の対応（TopLeft = BottomEdgeAlignedLeft、TopCenter = Bottom、TopRight = BottomEdgeAlignedRight、BottomLeft = TopEdgeAlignedLeft、BottomCenter = Top、BottomRight = TopEdgeAlignedRight、LeftCenter = Right、RightCenter = Left）、`PlacementConstraintAdjustment="SlideX, SlideY"`、offset は `HorizontalOffset` / `VerticalOffset` に n − 4 | テーマが 4px の間隔を付ける。GPUI は反転しない。`FlyoutPresenterClasses` に `arrow` / `plain`。 |
| Tabs | `TabStrip` / `TabControl` に `outline` `pill` `segmented` `underline`、アイコンだけのタブに `icon-only`。タブの一覧のメニューは `menu`、バーの前後の内容は `uikit:Tabs.Prefix` / `uikit:Tabs.Suffix` | GPUI の menu(true)、prefix、suffix。 |
| Toolbar | `CommandBar` の `DefaultLabelPosition` は Right のまま、サイズは `xsmall`（既定 small）`medium` | GPUI の toolbar は small。 |
| Accordion | `StackPanel Classes="accordion"` に Expander を並べ、枠は `Border Classes="accordion"` | 項目の区切りと外枠。 |
| Collapsible | `Theme="{StaticResource UIKitCollapsible}"`、動きは `reveal` クラス | GPUI の motion_id。 |
| Notification | `WindowNotificationManager(topLevel)`、`MaxItems = 10`、期限 5.4 秒、文字列だけの通知は `classes: ["plain"]` | GPUI の上限と自動で消えるまでの時間。 |
| Resizable | `GridSplitter` の `ResizeDirection` を `Columns` / `Rows` で明示し、1px の定義に置く。GPUI と同じ配置にするなら 2 枚目のセルの先頭に `PreviousAndCurrent`、各パネルに MinWidth 100 | テーマは向きを ResizeDirection で決める。 |
| Image | ObjectFit = Stretch: Fill = Fill、Contain = Uniform、Cover = UniformToFill、ScaleDown = Uniform + `StretchDirection="DownOnly"`、None = None + 左上寄せ。角丸は `rounded` `rounded-lg` `rounded-full` | – |
| Calendar / DatePicker | DatePicker は `SelectedDateFormat="Custom"`、`CustomDateFormatString="yyyy/MM/dd"`、`PlaceholderText="Select date"`。サイズのクラスはポップアップのカレンダーにも効く | GPUI の書式と placeholder。 |
| TimeField | `TimePicker` の `ClockIdentifier`、`UseSeconds` | 閉じた欄が GPUI の TimeField。開いたピッカーは Avalonia のもの。 |
| Table | `TableView Theme="{StaticResource UIKitTable}"`、枠付きは `BorderThickness="1" CornerRadius="5.5"`、右寄せは列の `HorizontalContentAlignment` | 既定のテーマは DataTable。 |
| DataTable（TableView） | サイズ `xsmall` `small` `large`、`stripe`、`borderless`。列幅はピクセルで | GPUI の列はピクセル幅。 |
| DataTable（DataGrid） | `UIKitDataGridTheme` を追加、`CanUserResizeColumns="True"`、表示だけなら `IsReadOnly="True"`、右寄せの列は `CellStyleClasses="text-right"` と右寄せの見出し | DataGrid の既定はリサイズ不可、クリックで編集に入る。 |
| DataGrid のセル選択 | `Classes="cell-selectable"`、行見出しは `HeadersVisibility="All"` | GPUI の cell_selectable / row_header。選択行の current cell を選択したセルとして描く。 |
| Carousel | `Focusable="True"`（GPUI はタブ停止）。前後のボタンは `uikit:CarouselPrevious` / `uikit:CarouselNext` に `Carousel="{Binding #名前}"` を付けて carousel と同じ Panel のセルに置くか、`Button Classes="outline icon-only rounded-full"` を 16px 外側に置く。ページ番号は `PipsPager Classes="carousel"` を 16px 下に置き、`SelectedPageIndex` と `SelectedIndex` を双方向に | テーマはフォーカス可能性を変えない。前後のボタンはテーマが 16px 外側・中央に置き、縦の carousel（`uikit:SpringSlide Orientation="Vertical"`）では上と下に置く。 |
| Sidebar | Icon = `SplitView` の `CompactInline`、Offcanvas = `Inline`、折り畳めない = `Inline` + `IsPaneOpen="True"`。`DrawerPage` なら `Locked` / `CompactInline` / `Split` | 幅は OpenPaneLength（既定 255）。 |
| Sheet | `DrawerPage Classes="sheet" DrawerBehavior="Flyout"`、暗転なしは `BackdropBrush="{x:Null}"`、GPUI の 34px のタイトルバーの下に出すなら `UIKit.Sheet.Margin` | – |
| TitleBar | `UIKit.TitleBar.Padding`（既定 12。`uikit:TitleBar` も使う） | WindowDrawnDecorations では、Avalonia が装飾を描く OS（Windows の拡張、X11、Wayland）でだけ使われる。 |
| ColorPicker | `UIKitColorPickerTheme` を追加、ColorSelect は `Classes="field"` | パレットは標準の FluentColorPalette（`Palette` で変える）。GPUI のパレットと HSLA にするなら、追加のパッケージの要らない `uikit:ColorSelect`。 |
| Tabalonia | `UIKitTabaloniaTheme` を追加（Tabalonia のテーマは入れない）、種類とサイズは Tabs と同じクラス、一覧は `menu`。`ItemsSource` は変更できるリスト | タブは `TabItemWidth` の 1 つの幅に並ぶ（GPUI は文字の幅）。ウィンドウのない環境では `EnableTabDetaching="False"`。 |
| Dock | `UIKitDockTheme` を追加（Dock のテーマは入れない）。ウィンドウのない環境では `DockSettings.UseManagedWindows` か `FloatingWindowHostMode="Managed"` | Dock 12.1.0.6 は managed のドラッグのプレビューをレイヤーの原点に置く（R35）。trim するなら分割のモデルのプロパティを残す（`DynamicDependency`）。 |
| Badge | `Count` / `Maximum` / `IsDot` / `Icon`、色は `BadgeBackground`、サイズは `small` `large` | 色を Background にすると中身の背景と区別できない。 |
| TagLabel | 色はクラス（`primary` 既定、`secondary` `danger` `success` `warning` `info`、19 のパレット色）、`outline`、`xsmall` `small` `large`、`rounded-full` | GPUI の Tag。Avalonia の `Control.Tag` と同名になるため改名。 |
| Alert | 種類は `info` `success` `warning` `error`、`banner`、サイズのクラス。`IsClosable` と `CloseRequested` で隠すのはアプリ | GPUI の on_close も隠さない。 |
| Skeleton | 大きさと角丸はアプリが指定、`secondary` | GPUI と同じく形は持たない。 |
| StatusBar | `Left` / `Content` / `Right` に 1 要素ずつ、複数なら `StackPanel` の Spacing 8 | GPUI の各領域は gap 8 の行。 |
| Breadcrumb | 項目は `BreadcrumbItem`（Button。`Click` / `Command`） | 遷移はアプリ。 |
| Kbd | `Gesture`（KeyGesture）。表記は実行中の OS に合わせる | Action からの解決は対象外。 |
| Clipboard | `Text` に値、完了は `Copied` | – |
| Rating | `Value`（双方向）、`Maximum`、色は `ActiveBrush`、サイズのクラス | – |
| Avatar / AvatarGroup | `UserName` / `Source` / `Placeholder`、サイズのクラス（他の大きさも円のまま）。AvatarGroup のサイズのクラスは中のアバターに渡る、`Limit` / `ShowsEllipsis` | – |
| EmptyState | `Media` / `Title` / `Description` / `Actions`、アイコンの枠は `icon-media` | GPUI の Empty。破線の枠は Avalonia の Border で描けないので持たない。 |
| DescriptionList | 項目は `DescriptionItem`（`Label` / `Value` / `Span`）と `DescriptionSeparator`、`Columns`、`Orientation`、`IsBordered`、`LabelWidth` | – |
| Stepper | `SelectedIndex`（双方向、クリックで変わる）、`Orientation`、`CentersSteps`、項目の `Icon`、サイズのクラス | – |
| Form | `FormField`（`Label` / `IsRequired` / `Description` / `ColumnSpan`、中身が入力）、`Columns`、`LabelOrientation`、`LabelWidth`、`Footer`、サイズのクラスは間隔 | 入力のサイズは入力のクラス。検証は DataValidationErrors。 |
| HoverCard | 中身がトリガー、`Card`、`Placement`（GPUI のアンカーの対応は Popover と同じ）、`OpenDelay` / `CloseDelay` | – |
| ShimmerText | `Text`、`Duration`、`Repeats`、`IsReversed`、`Spread`、`HighlightColor`。文字の大きさと色はアプリ | GPUI は繰り返しの位相をアプリの時計で全体にそろえる。ここでは要素ごと。 |
| Marker | `separator` / `border`、揃えは `HorizontalContentAlignment`、`Icon`、`IsLoading` と `shimmer` | – |
| Bubble / Message | Bubble は種類のクラスと `start` / `end`、`Reaction` と `reaction-top` / `reaction-start`。Message の項目が吹き出し、`Avatar` / `Header` / `Footer`、`end` | 会話の末尾への追従（MessageScroller）は対象外。 |
| Button の loading | `uikit:Buttons.IsLoading`、スピナーの形は `LoadingIcon`。スピナーになるのは内容の `PathIcon`、または内容のパネルの最初の子の `PathIcon` | GPUI の icon に当たる部品を内容から決める。後ろのアイコン（キャレットなど）は回らない。 |
| アイコンとラベルのボタン | 横の `StackPanel`、Spacing は xsmall / small で 4、それ以外は 8。`TextBlock` に `VerticalAlignment="Center"` | GPUI はラベルをボタンの中央に置く。StackPanel の既定（Stretch）では large で文字が 1.5px 上に寄る。 |
| 押下でフォーカスを奪わないボタン | `uikit:Buttons.TakesFocusOnPointer="False"`（継承。TextBox、パネル、ウィンドウに付けられる） | GPUI のボタンは mouse down で `prevent_default` する。既定は Avalonia のまま（R27）。 |
| uikit:ButtonGroup | 色・`outline`・`compact`・サイズのクラスはグループに付け、ボタンには付けない。選択は `Click` の `SelectedIndices` から各ボタンの `selected` クラスに反映する | 子にも同じ種類のクラスがあると、どちらが勝つかはテーマの順で決まる（GPUI はグループが上書きする）。 |
| uikit:ToggleGroup | 子は `ToggleButton`。`outline`・サイズ・`segmented` はグループに付ける。状態は各トグルの `IsChecked` | 同上。 |
| uikit:Accordion | 項目は `Expander`。1 項目だけを開くのが既定で、`Multiple="True"` で独立に開く。枠なしは `IsBordered="False"`、サイズのクラスは Accordion に付ける。アイコンは Header の内容に置く | GPUI の `multiple(false)`、`bordered(true)` が既定。 |
| uikit:Toolbar | サイズは `xsmall`、なし（small）、`medium`（`large` も medium）。アイコンだけのボタンは `icon-only`、区切りは `Separator Classes="vertical" Height="20"`、伸縮は `uikit:ToolbarSpacer`（ツールバーに幅が要る）。自分のサイズと色のまま置くコントロールは `uikit:Toolbar.TakesSize="False"`。`ToolbarGroup` の間隔は `Spacing`、名前は `AutomationProperties.Name` | GPUI の `child` / `content` の区別を添付プロパティで表す。 |
| Input の検証とマスク | 単一行の TextBox に `uikit:Inputs.Pattern` / `MaskPattern` / `CleanOnEscape`。`Validate` はコードから。NumberMask はプロパティ要素（`<uikit:Inputs.MaskPattern><uikit:NumberMask Separator="," Fraction="2" /></uikit:Inputs.MaskPattern>`） | GPUI の InputState の規則。アプリが入れた Text はマスクするだけで、拒否しない。 |
| Textarea のインデント | 複数行の TextBox に `uikit:Inputs.TabSize="2"`（GPUI の既定）、タブ文字なら `HardTabs="True"` | 設定しなければ Tab はフォーカスを移す。 |
| uikit:InputGroup | 中に TextBox を 1 つと `uikit:InputGroupAddon`（`Alignment`）を並べる。行の末尾に置く項目は `HorizontalAlignment="Right"`。サイズのクラスはグループに付ける | 入力は枠なしになり、グループのサイズに従う。中の Button は InputGroupButton の見た目に固定。 |
| uikit:NumberInput | 値によって変わる刻みは `StepBy`、アプリが値を決めるなら `StepsValue="False"` と `Step` | NumericUpDown の `Spinned` は出ない。NumberMask の `Separator` はカルチャの桁区切りと同じにする（値への変換は NumericUpDown）。 |
| uikit:RangeSlider | 範囲は `StartValue` / `EndValue`。単一値の対数スライダーは `IsRange="False"` と `EndValue`。`Scale="Logarithmic"` では `Minimum` を 0 より大きくする | Avalonia の Slider は対数にできない。 |
| uikit:Select | 要素はデータ（文字列か、`TextSelector` で文字を返すオブジェクト。Control は不可）、グループは `uikit:SelectGroup`、無効な要素は `ItemEnabledSelector`。Combobox の見た目は `combobox` クラス、複数選択は `SelectionMode="Multiple"`、appearance(false) は `plain`。行の内容は `ItemTemplate`、トリガー全体は `TriggerTemplate`（`uikit:SelectTriggerContext`） | 文字列化と絞り込みは関数で受け取る（リフレクションなし）。GPUI の render / render_trigger。 |
| uikit:ListView | 項目は `ItemsSource`（または子要素）と `ItemTemplate`。セクションは `uikit:ListSection`（`Header` / `Footer`）か、`SectionItemsSelector` とテンプレート。無効な行は `DisabledSelector`、検索は `IsSearchable` と `SearchFilter`、追加取得は `HasMore` と `LoadMore`（追加し終えたら `IsLoadingMore` を False に戻す）。確定行のチェックは `uikit\|ListItem` のスタイルで `CheckIcon` と `IsConfirmed` | 行の高さはそろえる（GPUI も 1 つの行・見出し・末尾を測って全体に使う）。見出し・末尾の高さが行と違うと、遠くへのスクロールの途中とスクロールバーのつまみが推定になる。 |
| uikit:Tree | `uikit:TreeItem`（`Label`、子、`IsExpanded`、`IsDisabled`）か、任意のデータに `ChildrenSelector` か `TreeDataTemplate`、`DisabledSelector`。行の中身は `ItemTemplate` | 項目は Equals で区別する（GPUI は id）。 |
| uikit:CalendarView | `Date` / `EndDate`（`IsRange`）、`NumberOfMonths`、`DisplayDate` / `DisplayMode`、無効な日は `DisabledDaysOfWeek="Sunday, Saturday"` / `DisabledDates`（`uikit:DateRange`）/ `DisabledMatcher`、サイズのクラス | 「今日」は作成時に読む。年のグリッドは今日の前後 50 年。 |
| uikit:DateField | 範囲は `IsRange`、時刻は `TimePrecision`（と `HourCycle`、`DefaultTime`）、`Presets`、`IsCleanable`、`NumberOfMonths`、`plain`。書式は既定で GPUI の `yyyy/MM/dd`（`DateFormat` で変える）、`PlaceholderText` の既定は「Select date」 | 「Time」のラベルはリソース `UIKit.DateField.TimeLabel`。 |
| uikit:TimeField | `Time`（`TimeSpan?`）、`Precision`、`HourCycle`、検証は `DataValidationErrors` | 編集の通知は `Changed`、値の変化はバインディング。 |
| uikit:Table | 部品（`TableHeader` / `TableBody` / `TableFooter` / `TableCaption`）を書いた順に並べる。セルは `TableHead` / `TableCell`、`ColSpan`、GPUI の `w()` は `PreferredWidth`（縮む）、縮まない列は `Width`、揃えは `HorizontalContentAlignment`。サイズ `xsmall` `small` `large`、`stripe`、枠は `BorderThickness="1" CornerRadius="5.5"` | GPUI と同じく行ごとの flex なので、列をそろえるには各行の ColSpan の和と PreferredWidth をそろえる。 |
| uikit:ColorSelect | `Color`（null は色なし）、欄は `Classes="field"`、`Label`、`Icon`、`PlaceholderText`、`FeaturedColors`、`ActiveTab`、`Placement`（アンカーの対応は Popover と同じ） | 追加のパッケージは要らない。 |
| VirtualList のスクロール | `listBox.ScrollToItem(index, ScrollStrategy.Center)` | Center 以外は GPUI の VirtualList と同じく、見えていなければ近い端へ動かす。ListBox の `ScrollIntoView` は近い端だけ。 |
| uikit:Pagination | `TotalPages`、`CurrentPage`（1 始まり、双方向）、`VisiblePages`（既定 5）、`PageChanged`。サイズのクラスと `compact` | GPUI の current_page / total_pages / visible_pages / on_click。 |
| Carousel のポインター入力 | `uikit:Carousels.TracksPointer="True"`。`ItemsPanel` はテーマに任せる。ループは `WrapSelection="True"` | テーマが ItemsPanel を `uikit:CarouselTrack` にする。`ViewportFraction` と `IsSwipeEnabled` は使われない。 |
| 差し込み口 | GroupBox の footer は `uikit:GroupBoxes.Footer`、Separator の label は `uikit:Separators.Label`（文字列）、Spinner のアイコンは `uikit:Spinners.Icon="{StaticResource UIKit.Icon.LoaderCircle}"` など、ProgressCircle の中身は `uikit:ProgressCircles.Content` | – |
| uikit:ResizablePanelGroup | パネルは `uikit:ResizablePanel`（`Size`、`MinSize`、`MaxSize`、`IsVisible`）。後からの大きさの変更は `ResizePanel` で（`Size` は初期サイズ） | GPUI の `size` は初期サイズ。 |
| uikit:Sheet | `new Sheet { ... }.Show(visual)` で出す（XAML に置かない）。タイトルバーの下に出すなら `UIKit.Sheet.Margin` | ウィンドウの OverlayLayer に置くため。 |
| uikit:Sidebar | 幅は `ExpandedWidth`（`Width` を設定しない）。SplitView の中では `Pane` に置く。折り畳んだときのヘッダー・フッターの中身は、アプリが `:collapsed`（`uikit\|SidebarHeader:collapsed ...`）や `Sidebar.IsIconCollapsed` で切り替える | 幅は折り畳みで動くため。GPUI のストーリーもヘッダーの中身を自分で切り替える。 |
| TextLabel | `Text`、`Secondary`、`Highlights` と `HighlightsMatch`、`IsMasked`。大きさ・太さ・色・揃えは TextBlock のプロパティ | 一致は補足の上でも一致の色（GPUI はハッシュの順で決まる）。 |
| AsyncImage | `Source` は絶対 URI（http / https / file / avares）。`Loading` / `Fallback` に内容、ObjectFit は Image と同じ `Stretch` の対応。別の読み方は `Loader` に `IImageLoader` | 読み込みはアタッチしたときに始まる（GPUI は最初のレイアウト）。失敗した読み込みは保持しないので、同じソースの次の画像で読み直す。SVG とアニメーション画像は読めない。 |
| uikit:Icon | `uikit:Icon Kind="..."`、サイズと色は PathIcon と同じクラスと `Foreground` | サイズを指定しなければ 16px（GPUI は文字の大きさ）。 |
| uikit:NotificationList | `new NotificationList(topLevel)`（アドーナー層）か、内容の上に置く。`Show(new NotificationItem(type, message) { Title = … })`。既定の配置は `Placement`（TopRight）、上限は `MaxItems`（10）、端との間隔は `Padding`（16,50,16,16）、幅は `ItemWidth`（382） | GPUI の NotificationSettings の既定値。上の 50px は 34px のタイトルバーの下。 |
| uikit:NotificationItem | `Type`（null で種類なし）、種類なしなら `Icon`、`Action`（Button は small になる。押して閉じるなら `NotificationCard.CloseOnClick`）、`AutoHide`（null は Action がなければ自動で消える）、置き換えと削除は `Id` / `Key`、クリックで閉じるなら `Click` を処理する | GPUI の Notification のビルダー。 |
| uikit:TitleBar | Window に `ExtendClientAreaToDecorationsHint="True"`。Windows と Linux では `WindowDecorations="BorderOnly"`（Avalonia が描くタイトルバーとボタンを出さない。例 `{OnPlatform BorderOnly, macOS=Full}`）、macOS は Full（信号機）。Linux では枠と影が内容の周りに描かれるので、内容を `WindowDecorationMargin` の内側に置く。フォーカスできない操作部品には `WindowDecorationProperties.ElementRole="User"` を付ける。ボタンの表示は `ShowsCaptionButtons` | GPUI の TitleBar::window_options に当たる設定。ドラッグ、ダブルクリック、ウィンドウの役割はヘッドレスの挙動テストでだけ確かめている。 |

Tooltip の表示遅延（500ms）、間隔（300ms）、配置（上）は、テーマがすべてのコントロールに設定する。

## 許容値を変えるとき

1. `AVALONIA_UIKIT_CALIBRATE=1` で全テストを流し、`tests/artifacts/pixel-stats.csv`、`ink-mass.csv`、`border-mass.csv` の分布を確認する。
2. 実測の最大値に余裕を少し足した値にする。理由のない緩和はしない。
3. 新しい緩和には ID を付け、この表とコードのコメントに書く。
