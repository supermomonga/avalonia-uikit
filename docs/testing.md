# テストと一致検証

GpuiTheme が GPUI Kit と同じ見た目・動きになっていることを、GPUI Kit 自身が描いた参照データとの比較で検証する。この文書は、検証の仕組み、許容値、許容値を緩めた理由（緩和 ID）、テーマの利用側に求める約束をまとめる。

- 移植元: gpui-kit [`2c5162f8c5b0c7fcec066ed53125d304c632bfe2`](https://github.com/longbridge/gpui-kit/tree/2c5162f8c5b0c7fcec066ed53125d304c632bfe2)（gpui-pre 0.3.7）
- 移植先: Avalonia 12.1.3、.NET 10、TUnit 1.72.16
- 参照データ: `goldens/gpui-2c5162f/`（1408 ケース。PNG、Scene JSON、トークン）

## コマンド

| 目的 | コマンド | 備考 |
| --- | --- | --- |
| 全テスト | `scripts/verify.sh` | `dotnet build tests/AvaloniaUIKit.Tests && dotnet run --no-build --project tests/AvaloniaUIKit.Tests` と同じ。macOS 以外でも動く。 |
| 一部だけ | `scripts/verify.sh --treenode-filter "/*/*/ButtonTests/*"` | クラス名で絞る。 |
| 許容値の校正 | `AVALONIA_UIKIT_CALIBRATE=1 scripts/verify.sh` | `tests/artifacts/pixel-stats.csv`（領域ごとの n / max / mean / bias）と `ink-mass.csv` を書き出す。 |
| 参照データの再生成 | `scripts/generate-goldens.sh [--only <id 接頭辞>]` | macOS（Metal）専用。`reference/vendor/` を作り直し、生成後に 2 回描画して一致を確かめる。`Colors.g.axaml` と `Lucide.g.axaml` も再生成する。 |
| NativeAOT | `scripts/aot-smoke.sh` | ギャラリーを NativeAOT で publish し（trim / AOT 警告はエラー）、`--smoke` で Light / Dark を描画して終了する。 |

失敗したケースは `tests/artifacts/<ケース ID>/` に `gpui.png`、`avalonia.png`、`diff.png`、`mask.png`（領域の分類）、`report.txt` を出力する。

## 参照データの作り方

`reference/` は GPUI Kit を固定コミットのまま使う Rust 製の生成器で、GPUI Kit を変更しない。

1. `reference/scripts/vendor.sh` が gpui-kit を `git archive` で、gpui-pre 0.3.7 を crates.io から（Cargo.lock のチェックサムを照合して）`reference/vendor/` に展開する。
2. 時刻の読み取りだけを差し替えるパッチを当てる（R15）。gpui-pre の `elements/animation.rs` と gpui-base の `scrollbar.rs` の `Instant::now()` を、テスト用 executor の時計に置き換える。置き換え漏れがあれば止まる。描画結果は変わらない。
3. `HeadlessAppContext` と Metal の headless レンダラで、`cases/*.toml` に定義した組み合わせを描く。フォントは同梱の Inter（`assets/fonts/inter/`）、スケールは 2。状態は GPUI の入力（hover、マウス押下、Tab、クリック、右クリック）と `advance_clock` で作る。
4. 各ケースについて、PNG と Scene（quad、影、下線、スプライト、パス）の JSON、要素の bounds を書き出す。トークン（解決済みの色）は `tokens/gpui-theme.json` と `Colors.g.axaml` になる。

ケース ID は `<コンポーネント>/<グループ>.<組み合わせ>/<状態>/<テーマ>` の形（例: `button/outline.primary.small/hover/dark`）。動きのケースは `<ID>/<経過 ms>` のフレーム列を持つ。

## テストの構成

1440 件。macOS arm64 での最新の実行結果は全件成功し、全体で約 35 秒かかる。テストの時刻はすべて仮想時計で進める（[時刻](#時刻)）。

| テスト | 件数 | 内容 |
| --- | --- | --- |
| `*_matches_gpui`（コンポーネント別 18 クラス） | 1398 | 静止状態の全ケース。構造と画素を比較する。 |
| `MotionTests` | 10 | 動きを GPUI が記録した時刻ごとに描画し、フレームを比較する。 |
| `TokenTests` | 3 | トークンの完全一致と過不足。 |
| `BehaviorTests` | 10 | 時間・入力・無効状態の挙動。 |
| `FluentLayeringTests` | 19 | FluentTheme の上に重ねても見た目が変わらないこと。 |

コンポーネント別の静止ケース数:

| コンポーネント（GPUI → Avalonia） | ケース | 組み合わせ |
| --- | --- | --- |
| Button → Button | 720 | 10 色 × 4 サイズ × 5 状態、outline、selected、compact、rounded、アイコンのみ |
| Toggle → ToggleButton | 160 | ghost / outline × 4 サイズ × checked、ラベル / アイコン |
| DropdownButton → SplitButton | 150 | 色 × サイズ、outline、各部の hover / 押下 / フォーカス、selected、メニューを開いた状態 |
| Switch → ToggleSwitch | 88 | 4 サイズ × checked × hover / focus / disabled、ラベルなし |
| Radio → RadioButton | 80 | 4 サイズ × checked × hover / focus / disabled、ラベルなし |
| Checkbox → CheckBox | 80 | 同上 |
| NumberInput → NumericUpDown | 28 | 4 サイズ × disabled、増減ボタンの hover / 押下 |
| Progress → ProgressBar | 24 | 4 サイズ × 値 0 / 40 / 100 |
| Scrollbar → ScrollViewer | 12 | Always / Hover モード、つまみの hover |
| GroupBox | 10 | normal / fill / outline、タイトルの有無 |
| DropdownMenu → MenuFlyout | 10 | 開いた状態、各項目の hover、サブメニュー |
| Spinner → ProgressBar（GpuiSpinner） | 8 | 4 サイズ |
| Separator | 8 | 横 / 縦 × 実線 / 破線 |
| Link → HyperlinkButton | 6 | normal / hover / pressed |
| Tooltip → ToolTip | 4 | 表示後、サイズ |
| AppMenuBar → Menu | 4 | 通常、項目の hover |
| ContextMenu | 4 | 開いた状態、項目の hover |
| 背景（Window） | 2 | Light / Dark |

静止ケースはすべて Light と Dark の両方を持つ（Tooltip のサイズ違いを除く）。動きは時間の比較が目的なので Light だけで行う。

## 比較の 4 層

### 1. トークン

`Gpui.*` の色リソースが、GPUI Kit の Default Light / Default Dark で解決した値と 8bit で完全一致すること。GPUI にない色がテーマに定義されていないこと。

### 2. 構造

GPUI の Scene と Avalonia の可視ツリーを、どちらも Fill（塗り）、Band（枠線）、Shadow（影）の図形に正規化して 1 対 1 で照合する。対応の付かない図形が残れば失敗する。文字・アイコン・下線は色の集合で照合する。

| 項目 | 許容値 | 緩和 |
| --- | --- | --- |
| 色 | 各チャンネル ±1/255 | R11 |
| 位置・寸法・角丸 | ±0.26 論理 px（動きの途中フレームは ±0.51） | R9 |
| 枠線の太さ | ±0.001 px | – |
| 文字を含む箱の幅 | GPUI より 1 論理 px 未満だけ狭くてよい（文字列ごと） | R9 |
| spread 付きの影の角丸 | ±2.01 px | R3 |
| クリップされた図形の角丸 | クリップ側の角丸でよい | R19 |

### 3. 画素

GPUI の Scene から各デバイス画素を 4 つの領域に分類し、領域ごとの許容値で比べる。差は最大チャンネル差（1/255 単位）。

| 領域 | 対象 | 許容値 | 実測の最大（静止ケース） | 緩和 |
| --- | --- | --- | --- | --- |
| Flat | 縁から離れた平坦部 | 最大 1 | 1 | R11 |
| Edge | 輪郭から 1.5 デバイス px 以内 | 最大 64、平均 3 | 62、1.18 | R2, R9 |
| Ink | 文字・アイコン・下線 | 平均 18 | 15.4 | R1, R4, R18 |
| Ink（量） | インクの総量の比 | 0.6〜1.6 倍 | 0.74〜1.07 | R1, R4 |
| Shadow | 影の広がり | 最大 12 | 9 | R3 |

- Edge と Ink は、1 デバイス px ずれた位置との差のうち最小のものを使う（R9）。
- インク量は、各インク画素がその下の塗りからどれだけ離れているかの総和。文字が少し太い・細いのは許し、文字・アイコン・線が欠けたり余計に描かれたりしたら検出する。GPUI 側の量が 200 未満のケースでは調べない。破線の Separator が描かれていなかった不具合は、この検査で見つかった（比 0）。

ケース単位で許容値を変えているのは、動きの途中フレームの 2 か所だけ:

| ケース | 変更 | 理由 |
| --- | --- | --- |
| 不定値 Progress の途中で、GPUI のバーが角丸より細いフレーム | Flat 64、Edge 128 / 平均 6 | R19 |
| Tooltip の表示途中（GPUI の吹き出しの不透明度 < 1） | Flat 13、Ink 平均 24 | R5 |

### 4. 動き

GPUI 側は仮想時計で 1 フレームずつ記録する（R15）。Avalonia 側も仮想時計で動かす。動きの始まりの状態を作ってトリガーを操作した後、時計を GPUI が記録した各時刻まで進めて描画し、そのフレームを構造と画素で比べる。描くのは Avalonia 自身のアニメーターなので、テーマが宣言した Transition や Animation が実際にどう動くかをそのまま確かめられる。位置は GPUI が動く端をデバイス px に丸めるので ±0.51 論理 px まで認める（R8、R9）。

| 動き | GPUI | Avalonia |
| --- | --- | --- |
| Checkbox / Radio のチェック | spring_control で不透明度 | SpringEasing（D=265ms） |
| Switch のつまみ | spring_move で位置 | KnobTransitions の SpringEasing（D=234/271/302ms） |
| Progress の値 | 180ms、easing_move | Width の Transition、SplineEasing(0.2,0,0,1)。最初に収まる幅は動かさない |
| 不定値 Progress | 1 秒周期の左右端 | 幅のキーフレーム + KeySpline |
| Spinner | 0.8 秒で 1 回転 | RotateTransform のキーフレーム |
| Tooltip の表示 | 500ms 待ってから 150ms、ease-out-cubic でフェードと 4px | ShowDelay と、Opacity / TranslateTransform のアニメーション |
| スクロールバーの表示 | 300ms、linear | Opacity の Transition |
| スクロールバーの消去 | 2 秒待ってから 500ms、ease-in-cubic でフェードと 16px のスライド | ScrollBar の `HideDelay` と、Opacity / Track の RenderTransform の Transition |
| つまみの拡大 | 300ms、ease-out-cubic で 6→8px | Width の Transition |

### 時刻

Avalonia には時刻を指定する公開 API がないので、テストに限り内部に手を入れて仮想時計にする（`Infrastructure/VirtualTime.cs`）。テーマ本体はリフレクションを使わない。

- Transition と Animation は、継承されるプロパティ `Animatable.Clock` の時計で動く。テストの各ウィンドウに、テストが進めたときだけ時刻が進む時計を設定する（`UnsafeAccessor` で内部の `ClockBase` を作り、`Pulse` を呼ぶ）。
- `DispatcherTimer`（Tooltip の表示遅延、スクロールバーの消去遅延、キャレットの点滅）は Dispatcher の時刻で動く。その時刻の取得元を仮想時計に差し替え、ディスパッチャーのループ自身のストップウォッチを止め、時刻を進めるたびに期限の来たタイマーを実行する。
- 時刻は 1ms ずつ進める。タイマーで始まった動きも、GPUI と同じ時刻から始まる。
- Avalonia の内部の名前（`Dispatcher._timeProvider`、`Dispatcher._impl`、`Dispatcher.PromoteTimers`、`ManagedDispatcherImpl._clock`、`ClockBase`、`Animatable.Clock`）に依存する（R7）。Avalonia の更新で変わった場合は、起動時の例外で分かる。

### 挙動

| テスト | 内容 |
| --- | --- |
| Tooltip の遅延 | 499ms では開かず、500ms で開く（GPUI と同じ）。 |
| ポインター押下とフォーカスリング | Button / CheckBox / Radio はクリックでリングを出さず、Tab で出す（R27）。 |
| Switch のリング | クリック後にリングを出す（GPUI と同じ）。 |
| 無効な Button | クリックを無視する。 |
| メニューの矢印キー | 矢印キーで選んだ項目が hover と同じ見た目になる（R28）。 |

## 緩和の一覧

100% の一致が原理的にできない箇所だけ、理由を付けて許容値を緩めている。コード中のコメントにも同じ ID を書いている。

| ID | 内容 | 扱い |
| --- | --- | --- |
| R1 | グリフのラスタライズが異なる（GPUI は CoreText、Avalonia は Skia + HarfBuzz）。同じ Inter でも濃さと AA が違う。 | Ink 領域の平均とインク量の比で比べる。 |
| R2 | 図形の縁の AA が異なる（GPUI は SDF、Skia は解析的 AA）。 | Edge 領域を別の許容値で比べる。 |
| R3 | 影のぼかしの近似が異なる。spread 付きの影の角丸は、GPUI では要素のまま、Skia では spread 分だけ大きくなる。 | σ を Blur に変換（σ = 0.288675 × Blur + 0.5）し、Shadow 領域の最大値と影の角丸を緩める。 |
| R4 | SVG アイコンのラスタライズが異なる（resvg と Skia の Path）。 | R1 と同じく Ink 領域で比べる。 |
| R5 | 不透明度のかけ方が異なる。GPUI は図形ごと、Avalonia はグループ全体にかける。重なった影がフェード中だけ違って見える。 | Tooltip の表示途中だけ Flat を 13 に緩める（理論上の最大 12.75）。 |
| R6 | spring の途中で目標が変わったときの速度の引き継ぎ。Avalonia の Transition は速度 0 から始まる。 | 検証の対象外。動きは開始から終了までを比べる。 |
| R7 | Avalonia に時刻を指定する公開 API がない。 | テストに限り、内部の時計と Dispatcher の時刻を差し替える（[時刻](#時刻)）。Avalonia の内部に依存する。 |
| R8 | spring の終端で GPUI は ε（0.1px）以内になると止める。Avalonia の SpringEasing は終了時刻に目標へ合わせる。 | 動きのフレームの位置の許容値 ±0.51px に含める。 |
| R9 | レイアウトの丸めが異なる。GPUI は文字幅を論理 px に切り上げ、端をデバイス px に丸める。Avalonia はデバイス px に丸める。 | 位置 ±0.26px、文字を含む箱の幅、Edge / Ink の近傍比較。 |
| R10 | 行の高さがフォント本来の高さより小さいときの文字の寄せ方。 | 今回のケースでは差が出なかった。予備の ID。 |
| R11 | 色の量子化（GPUI は float の HSLA、Avalonia は 8bit）。 | 色と Flat 領域で ±1/255。 |
| R12 | ポップアップを画面内に収める処理が異なる。 | 画面端にかからない位置のケースだけを比べる。 |
| R13 | テーマの既定フォントはシステム UI フォント。 | 検証はすべて同梱の Inter で行う。システムフォントでの一致は保証しない。 |
| R14 | 参照データは macOS（Metal）でしか作れない。 | 許容値は macOS arm64 で校正した。ほかの OS でもテストは動くが、校正はしていない。 |
| R15 | 参照の生成器は時刻の読み取りだけにパッチを当てている。 | 描画には影響しない。`vendor.sh` が置き換え漏れを検査する。 |
| R16 | Avalonia にだけある状態（CheckBox の不確定状態など）。 | 比較しない。読める見た目であることだけを保つ。 |
| R17 | rem は 16px に固定。 | GPUI の rem を変える設定は対象外。 |
| R18 | 下線の位置と太さ。GPUI は descent の 0.618 倍、Avalonia はフォントの値を使う。 | Ink 領域として比べる。 |
| R19 | クリップされた図形の輪郭はクリップ側になる。不定値 Progress で角丸より細いバーは、GPUI では幅 2r の pill、Avalonia では 2 つの丸い端が重なった形になる。 | 構造比較でクリップ側の角丸を認め、該当フレームの画素の許容値を緩める。 |
| R20 | スクロールバーの Scrolling モード（スクロール中だけ表示）と、Hover モードで隠れたバーのつまみを直接指したときのスライド入場（SlideAndFade）。 | 対象外。Always と Hover を移植し、Hover は帯に入ったときのフェード入場と、離れたときのフェード・スライド退場を再現した。 |
| R21 | Tooltip の閉じる前の猶予と、隣への切り替えスライド。 | 対象外。 |
| R22 | ショートカットの表記は OS ごとに異なる。 | 修飾キーのないショートカット（F5）だけを比べる。 |
| R23 | 欠番。 | – |
| R24 | テーマが持たないコントロール側の処理。NumericUpDown は、ボタンの押下でフォーカスを得るとテキストを選択する。GPUI はしない。 | テストで選択を戻してから比べる。 |
| R25 | キャレットの点滅の位相。 | キャプチャではキャレットを隠す。 |
| R26 | 欠番。 | – |
| R27 | ポインター押下時のフォーカス。GPUI はフォーカスを移さない。Avalonia は移すが `:focus-visible` にはしない。 | どちらもリングは出ない。挙動テストで確かめる。 |
| R28 | Avalonia はメニューを開くと先頭の項目を選択する。GPUI は何も選択しない。 | テーマは選択・hover・サブメニューを開いた項目に同じ見た目を付ける。テストは開いた直後の選択を外してから比べる。 |

## 利用側の約束

GPUI と同じ見た目・挙動にするため、アプリ側で次の設定をする。

| 項目 | 設定 | 理由 |
| --- | --- | --- |
| サブメニューの表示 | `DefaultMenuInteractionHandler.MenuShowDelay = TimeSpan.Zero` | GPUI は hover するとすぐにサブメニューを開く。 |
| MenuFlyout の配置 | `Placement="BottomEdgeAlignedLeft"`（SplitButton は `BottomEdgeAlignedRight`） | テーマが GPUI のトリガーとの間隔 4px を付ける。ContextMenu はポインター位置に開く。 |
| フォント | `Gpui.FontFamily` リソースを差し替える | 既定はシステム UI フォント（GPUI の `.SystemUIFont` と同じ）。検証と同じ Inter にする場合は差し替える。 |
| NumericUpDown | `ButtonSpinnerLocation` は効かない | GPUI と同じく [−] 値 [+] の順に固定。 |

Tooltip の表示遅延（500ms）、間隔（300ms）、配置（上）は、テーマがすべてのコントロールに設定する。

## 許容値を変えるとき

1. `AVALONIA_UIKIT_CALIBRATE=1` で全テストを流し、`tests/artifacts/pixel-stats.csv` と `ink-mass.csv` の分布を確認する。
2. 実測の最大値に余裕を少し足した値にする。理由のない緩和はしない。
3. 新しい緩和には ID を付け、この表とコードのコメントに書く。
