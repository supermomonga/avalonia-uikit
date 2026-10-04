---
number: 26
title: Ship GPUI Kit's color themes as theme variants of UIKitTheme
status: accepted
date: 2026-10-04
links:
- target: 6
  kind: amends
- target: 21
  kind: amends
---

# Ship GPUI Kit's color themes as theme variants of UIKitTheme

## Context and Problem Statement

テーマは GPUI Kit の Default Light / Default Dark の色だけを持ち、名前は `NovaTheme` だった（ADR 21）。GPUI Kit は `themes/*.json` に 20 ファミリー、36 のテーマ（Aurora Light、Ayu Light / Dark、Catppuccin の 4 種、Tokyo Night / Storm / Moon など）を同梱し、gpui-kit.com はパレットでそれらを切り替え、ライブデモも同じテーマで描く。同じことを Avalonia のテーマとサイトでできるようにする。

* テーマのファイルは 30〜45 色だけを書き、残りの約 100 色は GPUI Kit がフォールバックで導出する。さらにコンポーネントが描画時に約 490 色を計算する（ADR 6 の派生色）。
* テーマは 1 つが「名前とモード」の組で、モードが片方しかないファミリーも、同じモードが 3 つあるファミリーもある。
* Aurora Light は、ボタン、primary、スイッチ、スライダーのつまみ、プログレスバー、スクロールバーのつまみなどの背景を `linear-gradient` で塗る。GPUI Kit のテーマの色（`theme.primary`）は単色の代表色（最初の停止点）のままで、コンポーネントは `theme.tokens.<field>` の `Background` を塗る。
* テーマの色以外の設定は `shadow: false` だけで、影響するのは Custom のボタンの影だけ（この移植には関係しない）。

## Decision Drivers

* 色は Default と同じく、GPUI Kit の関数で解決した値と完全に一致させる（ADR 6）。
* アプリの XAML は Default のときと同じリソースキーで書けること（ADR 21）。
* 36 のテーマを同梱しても、DLL と WebAssembly のバンドルを大きくしないこと。NativeAOT とトリミングに対応すること（ADR 11）。
* サイトで実行中に切り替えられること。

## Considered Options

* API: `UIKitTheme` 1 つと、テーマごとの `ThemeVariant`
* API: `DefaultTheme`、`AuroraTheme`、`AyuTheme`… とテーマごとの `Styles` クラス
* 色の持ち方: テーマごとの XAML の `ResourceDictionary`（`Colors.g.axaml` を増やす）
* 色の持ち方: 生成した C# の色の表と、引かれたときに値を作るリソースプロバイダー
* グラデーション: 代表色の単色で近似する
* グラデーション: GPUI Kit と同じグラデーションで塗る

## Decision Outcome

Chosen option: "`UIKitTheme` とテーマごとの `ThemeVariant`"、"生成した C# の色の表とリソースプロバイダー"、"GPUI Kit と同じグラデーション", because GPUI Kit の「名前とモードを持つ 1 テーマ」が Avalonia の継承付きの独自バリアントにそのまま対応し、36 テーマでも 1 テーマ約 2.5KB の表で済むから。

* **名前:** `NovaTheme`、`NovaColorPickerTheme`、`NovaDataGridTheme` を `UIKitTheme`、`UIKitColorPickerTheme`、`UIKitDataGridTheme` にする。色のテーマ（Default、Aurora…）と見た目のテーマを分け、見た目のテーマはライブラリ名で呼ぶ。Avalonia の `Light` / `Dark` が GPUI Kit の Default Light / Default Dark になる。旧名の別名は用意しない（NuGet に未公開、ADR 21 と同じ理由）。
* **バリアント:** 同梱のテーマは `UIKitThemeVariants` の静的プロパティ（`AuroraLight`、`AyuDark`、`MacOSClassicLight` など）。キーは GPUI Kit の名前（`Ayu Dark`）で、モードにより `ThemeVariant.Light` か `Dark` を継承する。アプリは `RequestedThemeVariant` で選び、`ThemeVariantScope` で一部だけ変えられる。FluentTheme などのリソースは継承先のモードのものが効く。`All`、`Find(name)`（`Default Light` / `Default Dark` も引ける）、`IsDark(variant)` を添える。
* **色の生成:** 生成器（`reference/`）が GPUI Kit のレジストリと同じ手順でテーマを適用し（モードの `light_theme` / `dark_theme` に設定して `Theme::change`）、Default と同じ dump を取る。出力は `tokens/gpui-theme.json`（Default、従来の形に `fills` を加える）、`tokens/gpui-themes.json`（同梱テーマ）、`src/AvaloniaUIKit/Themes/Tokens/Palettes.g.cs`。`reference tokens` は色だけを書き出す。
* **色の持ち方:** `Palettes.g.cs` は、全テーマに共通のキーの並びと、テーマごとの ARGB の配列とグラデーションを持つ。`UIKitTheme` は自分の `Resources` の先頭のマージ辞書に、Default（Default Light）、`Dark`（Default Dark）と各バリアントの `PaletteResources`（`ResourceProvider` + `IThemeVariantProvider`）を `ThemeDictionaries` として入れる。値は最初に引かれたときに作って保持する（`ImmutableSolidColorBrush`、`ImmutableLinearGradientBrush`、`Color`）。`Colors.g.axaml` はやめる。探索の順序は従来と同じ（マージ辞書の先頭なので最後に探す）。
* **グラデーション:** 派生色の値を `Hsla` から GPUI の `Background` にし、GPUI が `theme.tokens` の背景を塗る派生色（ボタンの各状態、プログレスバーのトラック、無効なスイッチのトラックなど）はテーマのグラデーションのまま書き出す。表記は `linear-gradient(180deg, #rrggbbaa 0%, #rrggbbaa 100%)`。GPUI Kit が背景として読む 78 のフィールド（`apply_background_color!`）は、トークンの背景を `UIKit.<Field>.Fill` として持つ（グラデーションでないテーマでは単色）。`UIKit.<Field>` と `.Color` は代表色の単色のまま。
  * XAML は、同梱のテーマのどれかがグラデーションにするフィールド（Aurora の 44 個）について、GPUI が `theme.tokens` で塗る箇所を `.Fill` にする: CheckBox / RadioButton のチェック、ToggleSwitch のトラックとつまみ、Slider のバーとつまみ、ProgressBar、ScrollViewer のつまみ、StatusBar、Calendar の選択、Stepper のチェックと通過した線、Tabs の pill と underline のインジケーター。どのテーマでも単色のフィールドの塗りは、単色のキーのままにする。
  * 角度は GPUI のシェーダー（`fill_color`）と同じく、quad の範囲を始点から終点へ、sRGB で補間する。軸に沿った角度（同梱テーマは 180° と 90° だけ）は相対座標の `LinearGradientBrush` と一致する。斜めの角度は GPUI が quad の縦横比で方向を変えるので、近似になる。
  * 色のトランジション（`Motion.ColorTransition`）は単色どうしだけを補間し、グラデーションはすぐに切り替える。
* **サイト:** 生成器が `sites/app/lib/themes.g.json`（テーマの一覧）と `sites/app/styles/themes.g.css`（`html[data-theme="<slug>"]` ごとのサイトの CSS 変数、gpui-kit.com と同じ対応）も書き出す。パレットは gpui-kit.com と同じく全テーマを並べ、ライブデモには `Demos.SetTheme(name)` で GPUI Kit の名前を渡す（docs/site.md）。

### Consequences

* Good, because 38 テーマすべての色が GPUI Kit と 8bit で一致し、グラデーションも角度と停止点まで一致する（`TokenTests`）。
* Good, because アプリは同じキーのまま、`RequestedThemeVariant` だけでテーマを替えられる。サブツリーごとにも替えられる。
* Good, because 色は配列の初期化データで、テーマ 1 つが約 2.5KB。使わないテーマは空の配列 1 つで、XAML のコンパイル結果も増えない。リフレクションも使わない。
* Bad, because `.Fill` は Aurora がグラデーションにするフィールドの塗りの箇所だけで使う。GPUI Kit を更新して別のフィールドがグラデーションになるテーマが入ったら、そのフィールドの塗りの箇所を `.Fill` に替える必要がある。
* Bad, because 色のリソースは XAML の `ResourceDictionary` ではなくなり、テーマの辞書を列挙しても見えない（キーで引けば従来どおり得られる）。
* Bad, because Aurora 以外の同梱テーマは画素では比べず、色の一致だけを確かめる。見た目の違いが色だけなので、レイアウトは Default のケースで確かめている。

### Confirmation

* `TokenTests` が、Default Light / Default Dark と同梱の 36 テーマのそれぞれで、全色（派生色、`.Color`、`.Fill`）が GPUI Kit の dump と一致すること、キーに過不足がないこと、各バリアントが自分のモードを継承することを確かめる。
* Aurora Light のケース（106。ボタン、チェックボックス、ラジオ、スイッチ、スライダー、プログレス、タブ、カレンダー、ステッパー、スクロールバー、ステータスバー）を GPUI Kit に描かせ、構造と画素で比べる（`themes = ["aurora-light"]`）。
* GPUI のシェーダーはグラデーションの色を ±2/255、アルファを ±3/255 でディザし、不透明なグラデーションも下の色が透ける。Gradient 領域の許容値を最大 5、平均 1.25 にした（R32、docs/testing.md）。グラデーションで塗った角丸の輪郭も Edge 領域にした（R2）。

## Pros and Cons of the Options

### API: テーマごとの `Styles` クラス

* Good, because `<uikit:AyuTheme />` と 1 行で書ける。
* Bad, because モードが片方しかないファミリー（Aurora は Light だけ、Tokyo は Dark が 3 つ）を Light / Dark の組に割り当てる規則が要る。
* Bad, because 実行中の切り替えが `Styles` の差し替えになり、スタイル全体を適用し直す。

### 色の持ち方: テーマごとの XAML

* Good, because Default と同じ仕組みで、生成器の変更が小さい。
* Bad, because 約 1,260 のリソース × 38 テーマがコンパイル済み XAML になり、DLL と WebAssembly のバンドルが数 MB 増える。

### グラデーション: 代表色の単色で近似する

* Good, because 派生色の型も XAML も変えずに済む。
* Bad, because Aurora の見た目（グラデーション）にならず、GPUI Kit の描画と一致しない。

## More Information

* ADR 6（色トークンの生成）を、出力先（`Palettes.g.cs`）と対象（同梱テーマ、`fills`、グラデーション）について改める。
* ADR 21（名前）を、テーマの名前について改める。リソースの接頭辞 `UIKit` はそのまま。
