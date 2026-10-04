# テストと一致検証

UIKitTheme が GPUI Kit と同じ見た目・動きになっていることを、GPUI Kit 自身が描いた参照データとの比較で検証する。この文書は、検証の仕組み、許容値、許容値を緩めた理由（緩和 ID）、テーマの利用側に求める約束をまとめる。

- 移植元: gpui-kit [`2c5162f8c5b0c7fcec066ed53125d304c632bfe2`](https://github.com/longbridge/gpui-kit/tree/2c5162f8c5b0c7fcec066ed53125d304c632bfe2)（gpui-pre 0.3.7）
- 移植先: Avalonia 12.1.3（別パッケージは `Avalonia.Controls.ColorPicker` 12.1.3、`Avalonia.Controls.DataGrid` 12.1.2）、.NET 10、TUnit 1.72.16
- 参照データ: `goldens/gpui-2c5162f/`（3508 ケース。うち動き 60、Aurora Light 106。PNG、Scene JSON、トークン）

## コマンド

| 目的 | コマンド | 備考 |
| --- | --- | --- |
| 全テスト | `scripts/verify.sh` | `dotnet build tests/AvaloniaUIKit.Tests && dotnet run --no-build --project tests/AvaloniaUIKit.Tests` と同じ。macOS 以外でも動く。 |
| 一部だけ | `scripts/verify.sh --treenode-filter "/*/*/ButtonTests/*"` | クラス名で絞る。 |
| 許容値の校正 | `AVALONIA_UIKIT_CALIBRATE=1 scripts/verify.sh` | `tests/artifacts/pixel-stats.csv`（領域ごとの n / max / mean / bias）、`ink-mass.csv`、`border-mass.csv`（枠線の角と辺ごと）を書き出す。 |
| 参照データの再生成 | `scripts/generate-goldens.sh [--only <id 接頭辞>]` | macOS（Metal）専用。`reference/vendor/` を作り直し、生成後に 2 回描画して一致を確かめる。`Palettes.g.cs`、`Lucide.g.axaml`、サイトのテーマ（`sites/app/lib/themes.g.json`、`sites/app/styles/themes.g.css`）も再生成する。色だけなら `reference tokens` で足りる。全体の生成が途中で失敗すると `goldens/` の一部が消えるので、`git checkout goldens` で戻す。 |
| NativeAOT | `scripts/aot-smoke.sh` | ギャラリーを NativeAOT で publish し（trim / AOT 警告はエラー）、`--smoke` で Light / Dark を描画して終了する。 |

失敗したケースは `tests/artifacts/<ケース ID>/` に `gpui.png`、`avalonia.png`、`diff.png`、`mask.png`（領域の分類）、`report.txt` を出力する。

## 参照データの作り方

`reference/` は GPUI Kit を固定コミットのまま使う Rust 製の生成器で、GPUI Kit を変更しない。

1. `reference/scripts/vendor.sh` が gpui-kit を `git archive` で、gpui-pre 0.3.7 を crates.io から（Cargo.lock のチェックサムを照合して）`reference/vendor/` に展開する。
2. 時刻の読み取りだけを差し替えるパッチを当てる（R15）。
   - gpui-pre の `elements/animation.rs` と gpui-base の `scrollbar.rs` の `Instant::now()` を、テスト用 executor の時計に置き換える。
   - Calendar の「今日」（`Local::now()`）を `test_clock::today()` に置き換え、ケースが日付を固定する。
   - 置き換え漏れがあれば止まる。描画結果は変わらない。
3. `HeadlessAppContext` と Metal の headless レンダラで、`cases/*.toml` に定義した組み合わせを描く。フォントは同梱の Inter（`assets/fonts/inter/`）、スケールは 2。状態は GPUI の入力（hover、マウス押下、Tab、クリック、右クリック、ドラッグ、ホイール、キー）と `advance_clock` で作る。ケースの終わりにドラッグを止め、次のケースに持ち越さない。
4. 各ケースについて、PNG と Scene（quad、影、下線、スプライト、パス、画像）の JSON、要素の bounds を書き出す。quad の塗りは単色と 2 色の線形グラデーションを書き出す。トークン（解決済みの色、コンポーネントが描画時に作る派生色、トークンの背景）は、Default Light / Default Dark が `tokens/gpui-theme.json`、同梱のテーマが `tokens/gpui-themes.json` になり、どちらも `Palettes.g.cs` になる（ADR 26）。
5. アイコンは GPUI Kit の Lucide SVG を線から塗りの輪郭に変換して `Lucide.g.axaml` に書き出す。不透明度の付いた部分（二色アイコンの薄い半分）は `<名前>.Faint` の別のジオメトリにする。

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
| `key-<キー>` | キー入力 |

状態は `+` でつなぐ（例: `click+wait-200ms+key-escape`）。

## テストの構成

3739 件。macOS arm64 での最新の実行結果は全件成功し、全体で約 5 分かかる。テストの時刻はすべて仮想時計で進める（[時刻](#時刻)）。

| テスト | 件数 | 内容 |
| --- | --- | --- |
| `*_matches_gpui`（コンポーネント別 72 クラス） | 3558 | 静止状態の全ケース（TabControl は Tabs のケースをもう一度使う）。構造と画素を比較する。 |
| `MotionTests`、`TabControl_moves_as_gpui` | 64 | 動きを GPUI が記録した時刻ごとに描画し、フレームを比較する。 |
| `TokenTests` | 40 | トークンの完全一致と過不足。Default Light / Default Dark と同梱の 36 テーマのそれぞれ、テーマのバリアントの継承。 |
| `BehaviorTests`、`ControlBehaviorTests` | 24、34 | 時間・入力・無効状態の挙動。後者は新しいコントロール（ADR 19）の操作と、GPUI の表記・色の計算。 |
| `FluentLayeringTests` | 19 | FluentTheme の上に重ねても見た目が変わらないこと。 |

コンポーネント別の静止ケース数（括弧内は動きのケース数）:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Button → Button | 784 | 10 色 × 4 サイズ × 5 状態、outline、selected、compact、rounded、アイコンのみ、Aurora Light の 8 色 × outline × 4 状態 |
| Toggle → ToggleButton | 160 | ghost / outline × 4 サイズ × checked、ラベル / アイコン |
| Icon → PathIcon | 154 | 37 個のアイコン、サイズ、色の継承、回転 |
| DropdownButton → SplitButton | 150 | 色 × サイズ、outline、各部の hover / 押下 / フォーカス、selected、メニューを開いた状態 |
| Tabs / TabBar → TabStrip、TabControl | 110（4） | 4 種類 × サイズ、hover、無効なタブ、アイコン、インジケーターの移動、Aurora Light |
| Switch → ToggleSwitch | 94（2） | 4 サイズ × checked × hover / focus / disabled、ラベルなし、Aurora Light |
| ButtonGroup → StackPanel.button-group の Button | 86 | 横 / 縦、サイズ、outline、先頭・中間・末尾の hover / 押下 |
| Checkbox → CheckBox | 86（2） | 4 サイズ × checked × hover / focus / disabled、ラベルなし、Aurora Light |
| Radio → RadioButton | 86（1） | 同上 |
| Input → TextBox | 80 | 4 サイズ、値 / placeholder / 読み取り専用 × focus / disabled、前後のアイコン、マスク、選択範囲 |
| ToggleGroup → ListBox.toggle-group | 76 | ラベル / アイコン、segmented、checked と unchecked の hover / クリック、disabled |
| Select → ComboBox | 68（1） | 閉じた欄の状態、placeholder、開いた一覧の hover / 無効な行、開く動き |
| DatePicker → CalendarDatePicker | 60（1） | 欄の状態、開いたカレンダーと hover、開く動き |
| InputGroup → TextBox.group | 54（1） | アイコン、前後の文字、ボタン、invalid、フォーカスの色の動き |
| Calendar → Calendar | 55 | 選択日、今日、日の hover / 押下 / クリック、選択できない日、未選択、月曜始まり、月の一覧 × サイズ、Aurora Light |
| Slider → Slider | 55（3） | 値 0 / 40 / 100 × hover / 押下 / focus、トラックの押下、ドラッグ、逆向き、縦、disabled、リングの動き、Aurora Light |
| Toolbar → CommandBar | 50 | 4 サイズ、項目の hover / 押下 / focus、矢印キー、disabled、内容 |
| TimeField → TimePicker | 46 | サイズ、書式（12 / 24 時間、秒）、focus、invalid、hover |
| DataTable → TableView | 44 | 4 サイズ、行の hover / 選択、stripe、borderless、固定列、行が埋まる / 埋まらない |
| ColorPicker / ColorSelect → ColorPicker（別パッケージ） | 42 | スウォッチ 4 サイズ、ラベル、ツールチップ、field 4 サイズ、focus、値 |
| Pagination → PipsPager | 42 | サイズ、compact、両端、ページの hover / 押下 / クリック、disabled |
| ProgressCircle → ProgressBar（UIKitProgressCircle） | 42（2） | 4 サイズ × 値、大きさ指定、値の変化、不定値 |
| Textarea → TextBox | 42 | 行数、placeholder、折り返し、読み取り専用、自動の高さ |
| Label → TextBlock.label、Label | 40 | サイズ、補足、太さ、揃え、折り返し、色 |
| Popover → Flyout | 38（1） | 8 つのアンカー、複数行、offset、plain、閉じる操作、開く動き |
| DataTable → DataGrid（別パッケージ） | 36 | 4 サイズ、ソート、行の hover / 選択、リサイズのヘアライン、stripe、borderless |
| Notification → WindowNotificationManager、NotificationCard | 34（2） | 4 種類とアイコンなし、タイトル、hover と閉じるボタン、折り返し、配置、入退場 |
| Accordion → Expander | 32（3） | card / borderless、disabled、アイコン、hover / focus、開閉と途中の反転 |
| Resizable → GridSplitter | 30（5） | 横 / 縦 × hover / ドラッグ、pill の動き |
| NumberInput → NumericUpDown | 28 | 4 サイズ × disabled、増減ボタンの hover / 押下 |
| Sheet → DrawerPage.sheet | 28（4） | 4 方向、大きさ、タイトルなし、フッター、閉じる操作、滑り込み |
| Combobox → ComboBox.combobox、AutoCompleteBox | 24（1） | 閉じた欄、placeholder、開いた一覧の hover / キー、閉じる操作 |
| List → ListBox | 24 | 行の hover / 選択 / キー操作、未選択、スクロール、空 |
| Progress → ProgressBar | 27（2） | 4 サイズ × 値 0 / 40 / 100、Aurora Light |
| Carousel → Carousel、PipsPager.carousel | 22（2） | ナビゲーションの hover / 押下、サイズ、focus、ページ送り |
| Sidebar → SplitView、DrawerPage | 22（5） | icon / offcanvas / none、左右、既定の幅、DrawerPage、開閉 |
| Table → TableView（UIKitTable） | 22 | サイズ、枠付き、stripe、固定幅、行の hover / クリック |
| Tree → TreeView | 22 | 行の hover / クリック / キー、選択、角丸 |
| img() → Image | 20 | ObjectFit 5 種、小さい画像、角丸、元の大きさ |
| Collapsible → Expander（UIKitCollapsible） | 16（4） | 開閉、内容が上、hover / focus、開閉と途中の反転 |
| Scrollbar → ScrollViewer | 18（7） | Always / Hover / Scrolling モード、つまみの hover、Aurora Light |
| VirtualList → ListBox | 12 | 可変・均一の行の高さ、スクロール、深い位置 |
| DropdownMenu → MenuFlyout | 10 | 開いた状態、各項目の hover、サブメニュー |
| GroupBox | 10 | normal / fill / outline、タイトルの有無 |
| Separator | 8 | 横 / 縦 × 実線 / 破線 |
| Spinner → ProgressBar（UIKitSpinner） | 8（1） | 4 サイズ |
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

静止ケースは Light と Dark の両方を持つ（Tooltip のサイズ違いを除く）。動きは時間の比較が目的なので Light だけで行う。

同梱のテーマで見た目が変わるのは色だけで、色はトークンの比較で全テーマを確かめる。それに加えて、Aurora Light が塗るグラデーション（ADR 26）を、グラデーションの塗りを持つ 11 コンポーネントで GPUI の描画と比べる。ケースのテーマはテーマの slug で書く（`themes = ["aurora-light"]`）。

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
- テストは緩和を挙げて画素を比べない範囲（Excluded 領域）を指定できる。使っているのは R33 の AvatarGroup の省略記号だけ。構造はその範囲でも比べる。
- インク量は、各インク画素がその下の塗りからどれだけ離れているかの総和。文字が少し太い・細いのは許し、文字・アイコン・線が欠けたり余計に描かれたりしたら検出する。GPUI 側の量が 200 未満のケースでは調べない。破線の Separator が描かれていなかった不具合は、この検査で見つかった（比 0）。
- 枠線の量は、GPUI の枠付きの quad ごとに、帯（外形と内縁の間と、その 1.5 デバイス px の AA）の画素を 4 つの角と 4 つの辺に分けて、インク量と同じ方法で数える（ADR 25）。Edge 領域は近傍の画素と比べるので、細い枠が子の背景に塗りつぶされたり消えたりしても通ってしまう。この検査ではその欠けを検出する。GPUI 側の量が 200 未満の部分は調べない。Accordion のカードの四隅が項目の背景に塗りつぶされていた不具合（比 0.41〜0.45）と、メニューの区切り線が 2px だった差（比 2.0）は、この検査で見つかった。
  - GPUI がクリップごとに分けて描いた同じ枠は 1 つにまとめる。位置が 1 デバイス px ずれても帯から外れないよう、クリップは 1.5 デバイス px 広げて数える（R9）。
  - クリップが辺を切る位置は丸めが分かれ、細い線が 2 倍にも 0 にもなる。その辺と両端の角は数えない（R9）。
  - Ink 領域と Excluded 領域の画素は数えない。

ケース単位で許容値を変えているのは、動きの途中フレームだけ（`Motion/MotionTolerance.cs`）:

| ケース | 変更 | 理由 |
| --- | --- | --- |
| 不定値 Progress の途中で、GPUI のバーが角丸より細いフレーム | Flat 64、Edge 128 / 平均 6 | R19 |
| Tooltip、Select / Combobox / DatePicker のポップアップ、Notification のカードがフェード中のフレーム | Flat 13、Ink 平均 24、枠線の量は比べない（半透明の背景の下に透ける影のほうが、フェード中の枠より濃い） | R5 |

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
| Select / Combobox / DatePicker のポップアップ | 150ms で 8px 下へスライドしながらフェード、リングと影は 4 乗 | キーフレーム（DatePicker は Margin でレイアウトを動かす） |
| InputGroup の色 | HSLA の補間 | `Motion.ColorTransition` |
| Tabs のインジケーター、pill の文字色 | spring_move で位置と幅、色のフェード | `Tabs.Indicator`、`Tabs.SelectionFade` |
| Slider のつまみのリング | spring_control | `Motion.Spring` |
| Accordion / Collapsible の開閉 | 自然な高さ × ばね（MotionReveal） | Canvas が内容を測り、`RevealConverters` で高さを掛ける |
| Notification の入退場 | 96px のスライドと 400ms のフェード、退場 200ms | キーフレーム、`Notifications.FromBottom` で向きを選ぶ |
| Resizable の pill | 120ms で長さと濃さ | Transition、`Splitters.IsDragging` |
| Carousel のページ送り | spring_move で 2 ページが 16px 離れて動く | `uikit:SpringSlide`（PageSlide の派生） |
| Sidebar の開閉 | 200ms、ease-in-out-cubic でクリップの幅、中身は即時 | PART_PaneRoot の Width の Transition |
| Sheet の表示 | 150ms、linear で 100px 滑り込む | キーフレーム |
| Skeleton の明滅 | 2 秒周期、bounce(ease_in_out) で 1 → 0.5 → 1 | 1 秒の Alternate のキーフレーム、QuadraticEaseInOut（テンプレートの Border を動かす） |
| ShimmerText のスイープ | 2 秒、linear。12 層の文字を帯で切り抜く | `ShimmerText.Phase` を Animation で動かし、帯をクリップにする |

### 時刻

Avalonia には時刻を指定する公開 API がないので、テストに限り内部に手を入れて仮想時計にする（`Infrastructure/VirtualTime.cs`、ADR 14）。テーマ本体はリフレクションを使わない。

- Transition と Animation は、継承されるプロパティ `Animatable.Clock` の時計で動く。テストの各ウィンドウに、テストが進めたときだけ時刻が進む時計を設定する（`UnsafeAccessor` で内部の `ClockBase` を作り、`Pulse` を呼ぶ）。
- `DispatcherTimer`（Tooltip の表示遅延、スクロールバーの消去遅延、キャレットの点滅）は Dispatcher の時刻で動く。その時刻の取得元を仮想時計に差し替え、ディスパッチャーのループ自身のストップウォッチを止め、時刻を進めるたびに期限の来たタイマーを実行する。
- 時刻は 1ms ずつ進める。タイマーで始まった動きも、GPUI と同じ時刻から始まる。
- Avalonia の内部の名前（`Dispatcher._timeProvider`、`Dispatcher._impl`、`Dispatcher.PromoteTimers`、`ManagedDispatcherImpl._clock`、`ClockBase`、`Animatable.Clock`）に依存する（R7）。Avalonia の更新で変わった場合は、起動時の例外で分かる。
- 同じ扱いのテスト専用の措置: Calendar の「今日」（`CalendarDayButton.IsToday` の setter）、ヘッドレスのウィンドウが作らない描画装飾（`WindowDrawnDecorations` の `ApplyTemplate`、`RenderScaling`、`EnabledParts`。`Rendering/DecorationsHost.cs`）。

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
| 計算 | Avatar の頭文字（GPUI の extract_text_initials）、Kbd の表記（GPUI の test_format、macOS と他の OS）、DescriptionList の行の分け方（GPUI の test_group_item_rows）。 |

## 見た目だけのコード

テーマで書けない見た目と動きは、ADR 15 の範囲のコードで補う。どれもテーマのスタイルから適用し、アプリはテーマを追加するだけで使える。リフレクションは使わない。

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
| | `TextLines.RoundsWidthUp` | 文字の幅を論理 px に切り上げる（GPUI と同じ） |
| | `TextLines.CentersTallGlyphs` | 行より高い文字を行の中央に置く |
| | `uikit:SpringSlide` | Carousel のページ送り |
| 値の受け渡し（ADR 17） | `Tables.CellPadding` / `CellVerticalAlignment` / `RowHeight` / `ShowsResizeHandles`、`Notifications.FromBottom` | コードで作られる子に、親の見た目を渡す |
| Converter | `AffineConverter`、`ThicknessWhenConverter`、`ThicknessFilterConverter`、`AboveConverter`、`FirstNonNullConverter` | 数値・余白の変換 |
| | `PlacementConverter` | Popover の配置から間隔と矢印 |
| | `RevealConverters`、`FadedShadowConverter` | 開閉の高さ、影のフェード |
| | `RoundedClipConverter` | Image の角丸 |
| | `CalendarConverters`、`TimeConverters`、`TableConverters` | 月名・年・時刻の表示、縞の詰め物行 |
| | `ColorPickerConverters`（ColorPicker） | GPUI の darken / lighten と 16 進 |
| | `LinearThicknessConverter`、`AvatarConverters`、`StepperConverters`、`FormConverters` | Badge のずれ、円の角丸、Stepper の線、Form の間隔 |

新しいコントロール（ADR 19、`src/AvaloniaUIKit/Controls/`）は、プロパティ・疑似クラス・テンプレートの部品と、そのコンポーネント自身の操作だけを持つ: `Badge`、`TagLabel`、`Alert`、`Skeleton`、`StatusBar`、`Breadcrumb` / `BreadcrumbItem`、`Kbd`、`Clipboard`、`Rating` / `RatingStar`、`Avatar`、`AvatarGroup`、`EmptyState`、`DescriptionList` / `DescriptionItem` / `DescriptionSeparator`、`Stepper` / `StepperItem`、`Form` / `FormField`、`HoverCard`、`ShimmerText`、`Marker`、`Bubble`、`Message`。補助として、GPUI の色の選び方（`FxHash`: rustc-hash 2.1 の FxHasher、`OkLab`: mix_oklab）と、長さを分け合うパネル（`DescriptionRowPanel`、`StepperPanel`、`FormPanel`、`MarkerPanel`。辺をデバイス px に丸める `LayoutSnap`）を持つ。

## 緩和の一覧

100% の一致が原理的にできない箇所だけ、理由を付けて許容値を緩めている。コード中のコメントにも同じ ID を書いている。

| ID | 内容 | 扱い |
| --- | --- | --- |
| R1 | グリフのラスタライズが異なる（GPUI は CoreText、Avalonia は Skia + HarfBuzz）。同じ Inter でも濃さと AA が違う。 | Ink 領域の平均とインク量の比で比べる。 |
| R2 | 図形の縁の AA が異なる（GPUI は SDF、Skia は解析的 AA）。 | Edge 領域を別の許容値で比べる。 |
| R3 | 影のぼかしの近似が異なる。spread 付きの影の角丸は、GPUI では要素のまま、Skia では spread 分だけ大きくなる。 | σ を Blur に変換（σ = 0.288675 × Blur + 0.5）し、Shadow 領域の最大値と影の角丸を緩める。角丸を保ちたい影（Notification）は、角丸を広げた別の Border で落とす。 |
| R4 | SVG アイコンのラスタライズが異なる（resvg と Skia の Path）。 | R1 と同じく Ink 領域で比べる。 |
| R5 | 不透明度のかけ方が異なる。GPUI は図形ごと、Avalonia はグループ全体にかける。重なった図形がフェード中だけ違って見える。 | フェード中のフレームだけ Flat を 13 に緩め（理論上の最大 12.75）、枠線の量を比べない。 |
| R6 | spring の途中で目標が変わったときの速度の引き継ぎ。Avalonia の Transition は速度 0 から始まる。 | 解消済み。`Motion.Spring` が GPUI と同じ式で速度を引き継ぐ。途中で戻す動きも比べる。 |
| R7 | Avalonia に時刻を指定する公開 API がない。 | テストに限り、内部の時計と Dispatcher の時刻を差し替える（[時刻](#時刻)）。Avalonia の内部に依存する。 |
| R8 | spring は ε 以内で止まる。止まるかどうかを調べる時刻が、GPUI は描画のたび、`Motion.Spring` は 1ms ごとで異なる。 | 差は ε（つまみで 0.1px）以内。動きのフレームの位置の許容値 ±0.51px に含める。 |
| R9 | レイアウトの丸めが異なる。GPUI は文字の幅と高さを論理 px に切り上げ、端を最近傍のデバイス px に丸める。Avalonia はデバイス px に丸める（大きさは切り上げ）。 | 文字の幅の切り上げは `TextLines.RoundsWidthUp` で再現し、幅は ±0.26px で一致する。位置 ±0.26px、中央に置いた箱の横位置（GPUI は文字を中央に置いてから幅を切り上げるので、半 px の丸めの向きが分かれる。Popover、Calendar の月、ColorPicker のツールチップ）、折り返した文字の高さ、Edge / Ink の近傍比較、クリップの切り口を Edge にする。枠線の量は、クリップを 1.5 デバイス px 広げて数え、クリップが切る辺を数えない。 |
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
| R22 | ショートカットの表記は OS ごとに異なる。修飾キーの記号（⌘ ⇧ など）は同梱の Inter になく、描画系ごとのフォールバックのフォントで描かれる。 | 修飾キーのないショートカット（F5）とキー（Kbd）だけを画素で比べる。Kbd の表記は GPUI のテストと同じ例で、macOS と他の OS の両方を単体テストで確かめる。 |
| R23 | 欠番。 | – |
| R24 | テーマが持たないコントロール側の処理。NumericUpDown はボタンの押下でフォーカスを得るとテキストを選択し、TextBox は Tab で全選択する。GPUI はしない。 | テストで選択を戻してから比べる。 |
| R25 | キャレットの点滅の位相。 | キャプチャではキャレットを隠す。 |
| R26 | 欠番。 | – |
| R27 | ポインター押下時のフォーカス。GPUI はフォーカスを移さない。Avalonia は移すが `:focus-visible` にはしない。 | どちらもリングは出ない。挙動テストで確かめる。 |
| R28 | Avalonia のコントロールが開いたときやフォーカスを得たときに先頭を選ぶ（メニューの先頭の項目、DataGrid の先頭行）。GPUI は何も選ばない。 | テーマは選択にも hover と同じ見た目を付ける。テストは開いた直後・フォーカス直後の選択を外してから比べる。 |
| R29 | スクロールバーの帯の上のポインター。GPUI は帯の下の行に hover を付けるが、Avalonia では帯の ScrollBar がポインターを受ける。 | 一覧のケースはポインターを帯にかけない。 |
| R30 | GPUI は 0 でない線を少なくとも 1 デバイス px に広げる（snap_stroke）。Avalonia のレイアウトの丸めは半デバイス px 未満の線を消す。消えかけの線とインクは描くかどうかが分かれる。 | 不透明度 5% 未満の線とインクは構造では比べず、画素だけで比べる。 |
| R31 | GPUI はアトラスの透明な隣接画素と補間して拡大した画像の縁を半ソース画素ぶん薄める。Avalonia は端を伸ばす。 | 画像の縁 2.5 デバイス px を ImageEdge 領域として平均 12 まで認める。 |
| R32 | GPUI のシェーダーはグラデーションをディザする。Skia はしない。1 つの三角分布のノイズで各色を ±2/255、アルファを ±3/255 動かすので、不透明なグラデーションも最大 3/255 透けて下の色が混ざる。差は最大で 2 + 3 × 下との色の差（≦ 5）、平均で (4 + 3 × 下との色の差) / 6（≦ 1.17）になる。 | グラデーションの内側を Gradient 領域として最大 5、平均 1.25 まで認める。 |
| R33 | 同梱の Inter にない文字（AvatarGroup の省略記号「⋯」、U+22EF）は、描画系がそれぞれのフォールバックのフォントで描く。 | その文字を含むアバターの輪の内側だけ画素を比べない（`VisualAssert.Matches` の `excluded`）。色と、塗り・輪の形は比べる。 |
| R34 | GPUI は折り返した行の末尾の空白も含めて行を中央に寄せ、折り返す位置を決める。Avalonia は末尾の空白を数えない。 | 中央寄せの文字（EmptyState）は 1 行に収まるケースで比べる。折り返す位置が空白 1 つ分の差で変わる幅（Alert の small）はケースの幅を変えて避ける。 |

## 利用側の約束

GPUI と同じ見た目・挙動にするため、アプリ側で次の設定をする。

| 項目 | 設定 | 理由 |
| --- | --- | --- |
| サブメニューの表示 | `DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero` | GPUI は hover するとすぐにサブメニューを開く。 |
| MenuFlyout の配置 | `Placement="BottomEdgeAlignedLeft"`（SplitButton は `BottomEdgeAlignedRight`） | テーマが GPUI のトリガーとの間隔 4px を付ける。ContextMenu はポインター位置に開く。 |
| フォント | `UIKit.FontFamily` リソースを差し替える | 既定はシステム UI フォント（GPUI の `.SystemUIFont` と同じ）。検証と同じ Inter にする場合は差し替える。 |
| NumericUpDown | `ButtonSpinnerLocation` は効かない | GPUI と同じく [−] 値 [+] の順に固定。 |
| スクロールバーの表示モード | `ScrollViewer.AllowAutoHide` と `uikit:Scrollbars.ShowOnHover` | False / – が Always、True / True（既定）が Hover、True / False が Scrolling。 |
| ButtonGroup | `StackPanel Classes="button-group"`、Spacing 0 | 中の Button の角と境界を GPUI のグループにする。 |
| ToggleGroup | `ListBox Classes="toggle-group"`、`SelectionMode="Multiple,Toggle"`（単一選択なら `Single`） | 選択の規則はアプリが決める。`segmented` で角をつなぐ。 |
| Label | `TextBlock Classes="label"`、補足は `Run Classes="secondary"` | TextBlock の既定テーマは置かない（全テンプレートの文字に効くため）。 |
| Input | パスワードは `PasswordChar="•"`、表示の切り替えは `revealPasswordButton` クラス | GPUI のマスク文字と mask_toggle。 |
| InputGroup | `TextBox Classes="group"` と `InnerLeftContent` / `InnerRightContent` | 前後の内容と入力欄を 1 つの枠にする。 |
| List | 行の内容は `ItemTemplate`。キー操作の後はポインターを動かすまで hover を描き直さない（GPUI） | ケースは先にポインターを外す。 |
| Combobox | 入力で絞り込むなら `AutoCompleteBox`、選ぶだけなら `ComboBox Classes="combobox"` | – |
| Popover | GPUI のアンカーと Placement の対応（TopLeft = BottomEdgeAlignedLeft、TopCenter = Bottom、TopRight = BottomEdgeAlignedRight、BottomLeft = TopEdgeAlignedLeft、BottomCenter = Top、BottomRight = TopEdgeAlignedRight、LeftCenter = Right、RightCenter = Left）、`PlacementConstraintAdjustment="SlideX, SlideY"`、offset は `HorizontalOffset` / `VerticalOffset` に n − 4 | テーマが 4px の間隔を付ける。GPUI は反転しない。`FlyoutPresenterClasses` に `arrow` / `plain`。 |
| Tabs | `TabStrip` / `TabControl` に `outline` `pill` `segmented` `underline`、アイコンだけのタブに `icon-only` | – |
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
| Carousel | `Focusable="True"`（GPUI はタブ停止）。前後のボタンは `Button Classes="outline icon-only rounded-full"` を 16px 外側、ページ番号は `PipsPager Classes="carousel"` を 16px 下に置き、`SelectedPageIndex` と `SelectedIndex` を双方向に | テーマはフォーカス可能性を変えない。 |
| Sidebar | Icon = `SplitView` の `CompactInline`、Offcanvas = `Inline`、折り畳めない = `Inline` + `IsPaneOpen="True"`。`DrawerPage` なら `Locked` / `CompactInline` / `Split` | 幅は OpenPaneLength（既定 255）。 |
| Sheet | `DrawerPage Classes="sheet" DrawerBehavior="Flyout"`、暗転なしは `BackdropBrush="{x:Null}"`、GPUI の 34px のタイトルバーの下に出すなら `UIKit.Sheet.Margin` | – |
| TitleBar | `UIKit.TitleBar.Padding`（既定 12） | Avalonia が装飾を描く OS（Windows の拡張、X11、Wayland）でだけ使われる。 |
| ColorPicker | `UIKitColorPickerTheme` を追加、ColorSelect は `Classes="field"` | パレットは標準の FluentColorPalette（`Palette` で変える）。 |
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

Tooltip の表示遅延（500ms）、間隔（300ms）、配置（上）は、テーマがすべてのコントロールに設定する。

## 許容値を変えるとき

1. `AVALONIA_UIKIT_CALIBRATE=1` で全テストを流し、`tests/artifacts/pixel-stats.csv`、`ink-mass.csv`、`border-mass.csv` の分布を確認する。
2. 実測の最大値に余裕を少し足した値にする。理由のない緩和はしない。
3. 新しい緩和には ID を付け、この表とコードのコメントに書く。
