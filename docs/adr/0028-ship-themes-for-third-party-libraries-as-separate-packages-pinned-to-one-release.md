---
number: 28
title: Ship themes for third-party libraries as separate packages pinned to one release
status: accepted
date: 2026-10-04
links:
- target: 16
  kind: amends
- target: 19
  kind: amends
---

# Ship themes for third-party libraries as separate packages pinned to one release

## Context and Problem Statement

GPUI Kit の Dock（DockArea と TabPanel）と、Tabs のタブを閉じる操作・並べ替えには、Avalonia 本体にも公式の別パッケージにも対応するコントロールがない。対応表は Dock を非対応とし、第三者製コントロールに範囲を広げないとしていた（ADR 19 も Dock を範囲外にした）。

同じ機能は第三者のライブラリが持っている。

* **Dock.Avalonia**（wieslawsoltes/Dock、MIT）: ドッキングできるレイアウト。12.1.0.6 が Avalonia 12.1.1 以上に対応する。
* **Tabalonia**（egorozh/Tabalonia、MIT）: ドラッグで並べ替えられ、閉じる・追加するボタンを持つタブ。12.0.0 が Avalonia 12.0.0 以上に対応する。

ADR 16 の別アセンブリのテーマは、公式の別パッケージだけを対象にしている。第三者のライブラリには、それと違う事情がある。

* **テンプレートの約束が版ごとに変わる。** テンプレートの部品（`PART_*`）、添付プロパティ、疑似クラスの約束が、パッチ版でも変わる（Dock の ToolChromeControl は 12.0.0.2 と 12.1.0.6 で構造が違う）。
* **上流のテーマが別にある。** Dock.Avalonia.Themes.Fluent、Tabalonia の FluentTheme / CustomTheme。Tabalonia の CustomTheme は `Style` で書かれているので、後から足したテーマの ControlTheme より強い。
* **NativeAOT への対応が分かれる。** Tabalonia は `IsAotCompatible` を宣言していて、警告なしで publish できる。Dock はどのパッケージも宣言していない。Dock を使うアプリを NativeAOT で publish すると、Dock.Avalonia（既定のデータテンプレートが名前で作る Binding）、Dock.Model.Avalonia（ItemsSource から作るドックの `GetProperty`）、Dock.Avalonia.Themes.Fluent（コンパイルしないバインディング）が警告を出す。分割線の `CanResize` と `ResizePreview` はトリミングで読めなくなり、黙って効かなくなる。

## Decision Drivers

* GPUI Kit の Dock と Tabs のページにある機能を、ほかのコンポーネントと同じ見た目で使えるようにする。
* `UIKitTheme` だけを使うアプリの依存と NativeAOT の保証を変えない（ADR 11、ADR 16）。
* 第三者のライブラリのテンプレートが変わったことに、気づける形で追随する。

## Considered Options

* ライブラリごとに別パッケージのテーマを作り、ライブラリの版を 1 つに固定する
* ライブラリごとに別パッケージのテーマを作り、版の下限だけを指定する
* `UIKitTheme` に含める
* 対応しない

## Decision Outcome

Chosen option: "ライブラリごとに別パッケージのテーマを作り、ライブラリの版を 1 つに固定する", because `UIKitTheme` の依存と保証を保ったまま、検証した版のテンプレートの約束にだけ合わせたテーマを出せるから。

* **パッケージ:** `AvaloniaUIKit.Tabalonia`（`UIKitTabaloniaTheme`）と `AvaloniaUIKit.Dock`（`UIKitDockTheme`）。どちらも `Styles` で、アプリは `UIKitTheme` の後に、上流のテーマの代わりに追加する（上流のテーマとは併用しない）。名前空間は `AvaloniaUIKit`（ADR 16）。
* **採用の条件:**
  * 固定した Avalonia（ADR 3）で動く版がある。
  * MIT と両立するライセンス。
  * 保守されている（最近のリリースが Avalonia の最新のマイナー版に対応している）。
  * GPUI Kit に対応するコンポーネントがある。
* **版:** `Directory.Packages.props` で `[x.y.z]` の完全一致に固定する（Tabalonia 12.0.0、Dock.Avalonia 12.1.0.6）。更新するときは、上流のテンプレートの差分（部品、添付プロパティ、疑似クラス）を確かめてテストを流し、参照用のクローン（AGENTS.md）も同じ版にそろえる。
* **テンプレート:** 上流のテーマには依存せず、ControlTheme を一から書く。部品の名前と添付プロパティは上流に合わせる。上流のテンプレートの構造や文字列を写した部分は、出典とライセンスを `THIRD-PARTY-NOTICES.md` に書く（ADR 27）。
* **トークンとアイコン:** `UIKitTheme` のものを `DynamicResource` で使う（ADR 16、ADR 21）。足りないアイコンは `UIKitTheme` の Lucide に加える。
* **NativeAOT:** どちらのアセンブリも `IsAotCompatible` で、自身の解析警告はない。
  * Tabalonia のテーマは NativeAOT を保証し、ギャラリー（`samples/AvaloniaUIKit.AotSmoke`）に入れる。
  * Dock のテーマは DataGrid と同じく NativeAOT を保証せず、ギャラリーに入れない。Dock 本体の警告はアプリ側の判断になる。分割線のモデル（`ProportionalDockSplitter`）の型はアプリが選ぶモデルのパッケージ（Dock.Model.Avalonia、Dock.Model.Mvvm など）にあるので、その `CanResize` と `ResizePreview` をトリミングから守る `DynamicDependency` もアプリが付ける。サイトのページに書く。
* **ブラウザ:** サイトのデモは、Tabalonia のタブの切り離し（新しい `Window` を作る）を切り、Dock の浮動ウィンドウを管理モード（DockControl の中に描く）にする。Dock 12.1.0.6 の管理モードのドラッグのプレビューは、Avalonia 12 では位置を求められず（視覚ツリーの根を `TopLevel` と仮定している）、DockControl の左上に残る。上流の不具合で、テーマでは直さない。テストはプレビューを Dock がデスクトップで置く位置（別ウィンドウ、すべての上）に置いて比べる（緩和 R35）。
* **検証:** GPUI Kit の描画との比較（ADR 10）で確かめる。ライブラリにしかない部分（ドラッグの最中、浮動ウィンドウ、追加ボタン）は挙動テストで確かめる。
* **ADR 19 との関係:** 範囲外にした Dock を、Dock.Avalonia を使う場合に限って対応に改める。ほかの範囲外のコンポーネントはそのまま。

### Consequences

* Good, because `UIKitTheme` だけを使うアプリの依存も NativeAOT の保証も変わらない。
* Good, because Dock.Avalonia と Tabalonia を使うアプリは、テーマを 1 つ足すだけで GPUI Kit の見た目になる。
* Bad, because 第三者のライブラリの版ごとにテンプレートを追う必要がある。固定した版と違う版を使うアプリでは、部品が合わずに例外が出たり見た目が崩れたりしうる。
* Bad, because Tabalonia は全タブを `TabItemWidth` の同じ幅に並べ、その配置（`TabsControl` が持つ `TabsPanel`）は差し替えられない。GPUI Kit のタブは中身に合わせた幅なので、ここだけは一致しない。比べる参照データは、GPUI Kit のタブを同じ幅（`max_width` と `w`）にして作る。
* Neutral, because Dock の NativeAOT は保証しない。DataGrid と同じ扱いになる。

### Confirmation

* Tabalonia 12.0.0 と Dock.Avalonia 12.1.0.6 を使う最小のアプリを、Avalonia 12.1.3 で NativeAOT（osx-arm64）と WebAssembly に publish した。
  * Tabalonia は警告なしで、起動して描画した。
  * Dock は 3 つのアセンブリで警告が出た。起動して描画したが、分割線の `CanResize` / `ResizePreview` の accessor がトリミングで消えていた。`DynamicDependency` で残せることを確かめた。
* `cases/tabalonia.toml` の参照データとの比較、動き、挙動テスト（`TabaloniaBehaviorTests`）で Tabalonia のテーマを確かめる。

## Pros and Cons of the Options

### ライブラリごとに別パッケージのテーマを作り、版の下限だけを指定する

* Good, because アプリが新しい版を選べる。
* Bad, because テンプレートの約束が変わった版で、気づかないまま壊れる。

### `UIKitTheme` に含める

* Bad, because 使わないアプリにも依存が増え、Dock のトリミングの警告がギャラリーの NativeAOT の publish に入る。

### 対応しない

* Bad, because GPUI Kit の Dock と、Tabs の閉じる操作・並べ替えが使えないまま残る。

## More Information

* 公式の別パッケージのテーマは ADR 16、範囲は ADR 19、Avalonia の固定点は ADR 3。
* 上流のテンプレートに置けない GPUI Kit の部品の扱いは ADR 29。
