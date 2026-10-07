---
number: 38
title: Ship every IconName and add the icons an app uses with a source generator
status: accepted
date: 2026-10-07
links:
- target: 13
  kind: amends
- target: 36
  kind: amends
---

# Ship every IconName and add the icons an app uses with a source generator

## Context and Problem Statement

ADR 13 は、テーマが使うアイコンだけを `StreamGeometry` にすると決め、テーマは GPUI Kit のコンポーネントの `IconName`（`crates/assets/default-icons.txt` の 106 個）とテーマが描くものを合わせた 109 個を持ってきた。一方、固定した GPUI Kit は `crates/assets/assets/icons` に 1830 個の SVG（Lucide 1.43.0 の 1818 個と GPUI Kit 独自の 12 個）を同梱し、共有の `IconName`（`gpui_kit::assets`）で全部を名前で呼べる。GPUI Kit 自身は、既定の `Assets` にはコンポーネントのアイコンだけを埋め込み、全部入りの `AllAssets` は明示したときだけ使う。アプリが Lucide の残りのアイコンも使えるようにしたいが、塗りの輪郭にしたパスは 1 個あたり約 1.3 KB あり、全部で約 2.2 MB になる。

今の作り方のまま全部を `Lucide.g.axaml` に入れると、コンパイル済み XAML はパスを `StreamGeometry.Parse` の文字列リテラル（UTF-16）として持つので、DLL は約 4.4 MB 増える。辞書を組み立てるメソッドが全アイコンの遅延生成を参照し、アイコンは実行時に文字列のキーで引かれるので、ILLink のトリミングでも使わないアイコンは消えない。トリミングが効くのも `PublishTrimmed` や NativeAOT で発行したときだけである。全アイコンを名前で使えるようにしつつ、アプリには使ったアイコンだけを入れるにはどうするかを決める。

## Decision Drivers

* `IconName` の全アイコンを、今と同じ `uikit:Icon Kind` と `UIKit.Icon.*` のキーで使えるようにしたい。
* アプリが持つアイコンのデータは、使ったものだけにしたい。
* 名前は IDE の補完に出し、綴りの誤りはビルドで分かるようにしたい。
* NativeAOT でビルドでき、リフレクションを使わないこと（AGENTS.md）。
* 形は今と同じ生成器（usvg と tiny-skia の輪郭化）から作り、テーマが持つ 109 個は変えたくない。

## Considered Options

* 全アイコンをテーマの XAML（`Lucide.g.axaml`）に入れる
* 全アイコンを UTF-8 のデータとしてライブラリの DLL に入れる
* テーマは 109 個のまま、残りはソースジェネレーターが使うアプリに足す
* アイコンを C# の静的メンバーとして生成し、ILLink のトリミングに任せる

## Decision Outcome

Chosen option: "テーマは 109 個のまま、残りはソースジェネレーターが使うアプリに足す", because 全アイコンを名前で使えるまま、アプリに入るのが使ったアイコンだけになり、トリミングの有無にもアプリの発行方法にも左右されないから。

* **名前:** `IconName` は `None` と 1830 個のアイコンになる（GPUI Kit の共有の `IconName` と同じ名前と順序）。`Icon.Kind` は `IconName?` から非 Nullable の `IconName` にし、既定値の `None` で `Data` をアプリに任せる。VS Code の Avalonia 拡張（12.2.1）の言語サーバーを逆コンパイルして読んだ限り、属性値の補完は型が enum のときだけ値を出し、`Nullable<IconName>` には出さないため。`IconNames.Bundled` はテーマが持つ 109 個を、内部の `IconNames.Component` は GPUI Kit のコンポーネントのアイコン 106 個（参照データの一覧のケース）を返す。
* **生成:** アイコンの変換を、GPUI に依存しないクレート `reference/icons`（`uikit-icons`）に分ける。`cargo run --release -p uikit-icons` が、`Lucide.g.axaml`（109 個。中身は今と同じ）、`IconName.g.cs`、ジェネレーターのデータ `src/AvaloniaUIKit.Generators/Icons.g.tsv`（全アイコンの名前と、テーマが持たないものの形）を書く。`reference generate` も同じ関数を呼ぶ。
* **ジェネレーター:** `AvaloniaUIKit.Generators`（netstandard2.0、Roslyn 5.0.0 の増分ジェネレーター）を AvaloniaUIKit パッケージの `analyzers/dotnet/cs` に入れる。C# の `IconName.X`（意味解析で型を確かめる）と `"UIKit.Icon.X"`、XAML（Avalonia のビルドが追加ファイルとして渡す `.axaml`）の `Kind="X"`、`Value="X"`、`UIKit.Icon.X`、`IconName.X`、要素の中身を拾い、テーマが持たないものだけを、UTF-8 の文字列リテラル（`u8` 接尾辞）を返す switch と、それを登録するモジュール初期化子としてアプリのアセンブリに書く。XAML は XML ではなく文字列として読み、編集中の壊れたファイルでも拾う。誤って拾っても形が 1 つ増えるだけなので、多めに拾う側に倒す。
* **実行時に選ぶアイコン:** パッケージの `buildTransitive` の設定で、`<UIKitIcon Include="Bike" />`（`IconName`、ファイル名、キーのどれでもよい）と `<UIKitIcons>All</UIKitIcons>`（全アイコン）を読む。アイコンでない名前は警告 `UIKIT001`、`All` と `Used` 以外のモードは `UIKIT002` にする。
* **テーマ側:** `IconGeometries` が各アセンブリの登録を集め、キーが初めて引かれたときにパースしてキャッシュする。`UIKitTheme.axaml` の結合辞書の先頭（後ろから引かれるので最後）に `GeneratedIcons`（`ResourceProvider`）を置き、テーマが持たない `UIKit.Icon.*` だけをそこから返す。どのアセンブリも足していないアイコンは何も描かず、キーごとに 1 回だけ Avalonia のログに警告を出す。
* **リポジトリ内:** プロジェクト参照の利用側は `src/AvaloniaUIKit.Generators/AvaloniaUIKit.Generators.targets` を取り込む（パッケージが持ち込むものと同じ）。コントロールカタログはプロパティグリッドで任意の `IconName` を選べるので `All`、Icon のギャラリーのデモは `IconNames.Bundled` を並べる。
* **サイト:** Icons ページ（ADR 36）は `Icons.g.tsv` も読み、全アイコンを並べる。全部の輪郭を HTML に入れると約 3.2 MB（gzip で約 700 KB）になるので、Lucide の 1818 個はサイトの `lucide` パッケージの SVG の要素を Lucide と同じ線で描き、輪郭は GPUI Kit 独自の 12 個だけにする。輪郭は同じ線を tiny-skia で塗りに変えたものなので、形の元データは同じである。`lucide` は GPUI Kit が同梱する Lucide と同じ版（1.43.0）に固定し、`sites/app/lib/icons.ts` が `Icons.g.tsv` に書かれた版と照合して、ずれたらビルドを止める。GPUI Kit は Lucide をリリースのアーカイブとそのチェックサムで固定して取り込む（`crates/assets/lucide.json`）ので、版が同じなら形も同じとみなす。実装の時点で、1818 個すべての要素が GPUI Kit の SVG と一致することを確かめた。形そのものを照合する案（`uikit-icons` が SVG の指紋を書き、ビルドが `lucide` の要素と比べる）も試したが、GPUI Kit が SVG に手を入れる場合と名前の対応がずれる場合にしか効かず、正規化を Rust と TypeScript の両方に持つ手間に見合わないので採らない。ADR 36 が `lucide` パッケージを退けた理由（版のずれ、Lucide にないアイコン、2 色のアイコン）は、この固定と照合、12 個の輪郭で解消する。ページは約 1.4 MB（gzip で約 160 KB）。ジェネレーターが足すアイコンの詳細には、`UIKitIcon` での指定を案内する。

### Consequences

* Good, because 1830 個すべてを `Kind` とキーで使え、アプリに入るのは使ったアイコンの UTF-8 データ（1 個あたり約 1.3 KB）だけになる。`All` でも約 2.2 MB で、全部を XAML に入れる場合の約半分で済む。
* Good, because 名前は enum なので、C# と XAML で補完に出せ、綴りを誤った `Kind` はビルドエラー（AVLN3000）になる。
* Good, because テーマが持つ 109 個とそのキー、`Kind` の書き方は変わらない。ジェネレーターのない環境でも、`IconNames.Bundled` は今までどおり描ける。
* Bad, because 実行時にしか決まらないアイコン（ファイルや設定から読んだ名前、`Enum.Parse<IconName>`、組み立てたキー）はジェネレーターに見えない。`UIKitIcon` か `All` で足す必要があり、足し忘れはビルドではなく、実行時のログの警告で分かる。
* Bad, because ソースジェネレーターなので C# のプロジェクトでしか動かない。F# などのプロジェクトはテーマが持つ 109 個だけになる。
* Bad, because XAML プレビューアーはビルド済みのアセンブリを使うので、XAML に新しく書いたアイコンは、再ビルドするまでプレビューに出ない見込み（確かめていない）。
* Bad, because `Kind` の型（`IconName?` から `IconName`）と `IconName` の値の番号が変わる、互換性のない変更である。`Kind = null` は `IconName.None` に書き換える。
* Bad, because サイトの `lucide` の版は、GPUI Kit の固定を更新するたびに手で合わせる必要がある。ずれれば、`src/` か `sites/` に触れる PR で CI のサイトのビルドが止まる。
* Neutral, because パッケージには約 2.2 MB のジェネレーターの DLL が加わるが、コンパイラーが読むだけで、アプリの出力には入らない。

### Confirmation

* `IconGeneratorTests` が、ジェネレーターをメモリ上のプロジェクトで動かして C#、XAML、`UIKitIcon`、`All`、警告、テーマのないプロジェクト、生成したコードのコンパイルを確かめ、`Icons.g.tsv` と `IconName` の名前の一致を確かめる。テストのアセンブリ自身もジェネレーターを通しており、名前を書いたアイコン（Bike）の描画、`UIKitIcon` で足したアイコン（Tent）、誰も足していないアイコンの警告を確かめる。
* `scripts/aot-smoke.sh` の画面は、テーマが持たない `Kind="Rocket"` と `{StaticResource UIKit.Icon.Sparkles}`（無ければ読み込みで例外になる）を NativeAOT で描く。
* サイトのビルド（CI の Site の PR でも動く）が、`lucide` の版を GPUI Kit の Lucide の版と照合する。実装の時点で、`lucide` を 1.48.0 にすると止まることを確かめた。
* `sites/scripts/smoke.ts` が、Icons ページのどのタイルも形を描くことを確かめる。
* 実装の時点で、`dotnet pack` したパッケージを新しいアプリから参照し、C#、XAML の `Kind` とキー、`UIKitIcon` のアイコンだけが生成され、テーマが持つものは生成されないことを確かめた。

## Pros and Cons of the Options

### 全アイコンをテーマの XAML（`Lucide.g.axaml`）に入れる

* Good, because 生成器の出力を増やすだけで、仕組みは今のままでよい。
* Bad, because パスが UTF-16 の文字列になり、テーマを使うすべてのアプリの DLL が約 4.4 MB 増える。XAML のリソースはトリミングでも消えない。

### 全アイコンを UTF-8 のデータとしてライブラリの DLL に入れる

* Good, because 実行時に選ぶアイコンも含めて、どのアイコンも追加の設定なしで描ける。言語も問わない。
* Bad, because 使うアイコンが数個でも、すべてのアプリが約 2.2 MB を持つ。

### テーマは 109 個のまま、残りはソースジェネレーターが使うアプリに足す

* Good, because アプリには使ったアイコンだけが入り、発行の方法（トリミングの有無）に左右されない。
* Good, because データはジェネレーターの DLL にあり、コンパイル時にしか読まれない。
* Bad, because 実行時にしか決まらないアイコンは、プロジェクトに書いて足す必要がある。C# のプロジェクトに限られる。

### アイコンを C# の静的メンバーとして生成し、ILLink のトリミングに任せる

* Good, because 使わないメンバーはトリミングで消える。
* Bad, because 効くのは `PublishTrimmed` か NativeAOT で発行したアプリだけで、通常の発行では全部が残る。
* Bad, because `uikit:Icon` が `IconName` から形を引く switch が全メンバーを参照するので、`Kind` で使う限り消えない。

## More Information

* 実装: `reference/icons`、`src/AvaloniaUIKit.Generators`、`src/AvaloniaUIKit/Controls/IconGeometries.cs`、`src/AvaloniaUIKit/buildTransitive`、`src/AvaloniaUIKit/Controls/Icon.cs`
* GPUI Kit の既定の `Assets` と `AllAssets`: `crates/assets/README.md`（2c5162f）
* ADR 13（アイコンの同梱）を補い、ADR 36（Icons ページ）のデータを広げる。使い方は `sites/content/docs/icons.mdx`。
