# コントロールカタログ

`samples/AvaloniaUIKit.Demo.ControlCatalog` は、ドキュメントサイトのデモをデスクトップのアプリで動かし、その XAML を見ながらプロパティを変えて試すためのアプリ。デモはサイトと同じ `samples/AvaloniaUIKit.Demos` を使う（ADR 35）。

```sh
dotnet run --project samples/AvaloniaUIKit.Demo.ControlCatalog
dotnet run --project samples/AvaloniaUIKit.Demo.ControlCatalog -- --component tabs --theme "Tokyo Night"
```

`--component` はコンポーネントの slug（`sites/app/lib/catalog.ts`）、`--theme` は GPUI Kit のテーマ名（`Default Light`、`Default Dark`、同梱テーマの名前）。前回のコンポーネント、テーマ、サイドバーとプロパティグリッドの開閉と幅は、ユーザーのアプリケーションデータの `AvaloniaUIKit/ControlCatalog/settings.txt` に保存する。

## 画面

| 場所 | 内容 |
| --- | --- |
| タイトルバー | `uikit:TitleBar`。サイドバーの開閉（`uikit:SidebarToggleButton`）、テーマの選択（`uikit:Select`、System と Default、Light、Dark のグループ）、プロパティグリッドの開閉。 |
| サイドバー | `uikit:Sidebar`（`Offcanvas` で閉じる）。サイトと同じく「Avalonia Controls」「UIKit Controls」「Third-party Controls」に分け、名前順に並べる。上の検索欄（⌘K / Ctrl+K）は名前、別名、コントロール名で絞り込み、Enter で最初の項目を開く。 |
| ページ | サイトのページと同じ順に、区分、名前、説明、コントロール名、パッケージ、デモのカード。「Examples」以外の見出し（Tabs の「Closing, adding and dragging」など）もそのまま出す。「Documentation」はサイトのページを開く。 |
| デモのカード | 見出し、説明（mdx のデモの直前の段落）、ライブのデモ、コード。「Code」で XAML を開き、コードビハインドのあるデモは C# のタブも出す。コピーのボタン（`uikit:Clipboard`）は表示中のコードを写す。 |
| プロパティグリッド | アクティブなカード（押したかフォーカスのあるカード）のデモの要素を 1 つ編集する。下の「プロパティグリッド」。 |

コードの表示はサイトに合わせる。XAML はルート要素（`UserControl`）の中身を共通のインデントを除いて出し、色はサイトのコードブロックと同じテーマごとの色（`--code-*`）を使う。

## プロパティグリッド

- **要素:** カードを有効にすると、そのコンポーネントのコントロール（カタログの `avalonia` の型。`StackPanel.button-group` ならそのクラスのある StackPanel）を書いた最初の要素を選ぶ。ほかの要素は「Elements」（XAML の入れ子のアウトライン）、パンくず、「Pick」で選ぶ。Pick（または Alt+クリック）中はデモを押しても操作にならず、ポインターの下の要素を選ぶ（Esc でやめる）。選んだ要素は点線の枠、Pick 中にポインターの下にある要素は塗りで示す。アウトラインで薄く出る要素は、ロジカルツリーにコントロールのない要素（テンプレート、フライアウト、ツールチップ、列や Dock のモデルのようなコントロールでないオブジェクト）で、選べない。
- **クラス:** テーマがその型に付けるスタイルクラスを並べ、押すと付け外しする。サイズ（`xsmall`〜`large`）、色（`primary`、`danger` など）、角（`rounded-*`）はそれぞれ 1 つだけになる。下の欄にはクラスを空白区切りで直接書ける。
- **プロパティ:** 型ごとのグループに分け、その型自身のグループと「Attached」を開き、共通の基底型のもの（Appearance、Layout、Input、Visual など）は閉じて出す。名前が明るいプロパティは XAML に書かれているもの、太字と点は変えたもの。↺ で元に戻す。
- **エディター:** bool はチェックボックス（null を取るものは 3 状態）、列挙型は一覧（16 以上なら検索付きの `uikit:Select`）、色とブラシは `uikit:ColorSelect`（`field`）、ジオメトリ（`PathIcon.Data` など）は同梱アイコンの選択、ほかは XAML の書き方の文字列（Enter かフォーカスを外したときに確定、Esc で戻す、数値は ↑↓ で 1、Shift で 10 ずつ）。読めない値は入力欄を赤くして受け付けない。
- **変更の行き先:** 変更はライブのコントロールと、カードに出す XAML の両方に入る。既存の属性は値だけを書き換え、新しい属性は最後の属性の後ろ（属性が 1 行ずつなら同じ桁の新しい行）に足す。変えた属性はコードで背景色を付け、最初の変更でコードを開く。カードの「Reset」はデモを作り直して変更をすべて捨てる。

デモ自身の操作（チェックボックスを押す、文字を打つ）でプロパティが変わると、グリッドの値も追従する。ただし XAML に書くのはグリッドでの変更だけ。

## 仕組み

| 部分 | 内容 |
| --- | --- |
| `Catalog/Catalog.g.cs` | コンポーネントとデモの一覧、テーマごとのコードの色。`bun sites/scripts/control-catalog.ts` が `sites/app/lib/catalog.ts`、`sites/content/components/*.mdx`、`sites/app/style.css` と `sites/app/styles/themes.g.css` から生成する。コミットし、CI が一致を確かめる。 |
| デモの XAML とコードビハインド | プロジェクトが `samples/AvaloniaUIKit.Demos/Demos/**` を `AvaloniaResource` として `Sources/<Component>/<Name>.axaml.txt` に取り込み、`AssetLoader` で読む（`.txt` を付けて XAML コンパイラーの対象から外す）。ファイルを変えれば再生成なしで反映される。 |
| `Xaml/XamlDocument` | XAML の要素と属性を、テキスト上の位置と一緒に読む。ハイライト用のトークンも作る。 |
| `Xaml/XamlEdits` | 要素の番号と属性名ごとの変更を、元のテキストの書式を保ったまま書き込む。 |
| `Xaml/DemoMap` | XAML の要素と、デモのライブのコントロールの対応。XAML とロジカルツリーを並べてたどり、同じ型の子を順に、要素の書く内容（`Content`、`Header`、`Name`、`Classes` など）と矛盾しないものに対応させる。テンプレートが表示する中身（`EmptyState.Media`、`TextBox.InnerLeftContent`、`Sidebar.Header` など）はビジュアルツリーから探す。 |
| `Inspector/PropertyCatalog` | 編集するプロパティ。Avalonia のプロパティレジストリ（`AvaloniaPropertyRegistry`）から型ごとに取り、ドキュメントにある AvaloniaUIKit の添付プロパティ（`UIKitAttached`）と、親のパネルの添付プロパティ（`DockPanel.Dock`、`Grid.Row` など）を足す。 |
| `Inspector/StyleClasses` | テーマの `ControlTheme` のセレクター（`^.primary`）と、アプリのスタイルの型のセレクター（`StackPanel.button-group`）から読むクラス。カタログ自身のスタイル（`CatalogStyles`）は除く。 |

リフレクションは使わない（ADR 11）。プロパティの列挙はレジストリ、値の読み書きは `GetValue` / `SetValue`、文字列との変換は型ごとに書いた `XamlValues`。プロジェクトは `IsAotCompatible` でトリミングと AOT のアナライザーを有効にし、警告をエラーにする。

## 約束事

- デモの XAML を増やしたら、`bun sites/scripts/demo-registry.ts` に加えて `bun sites/scripts/control-catalog.ts` も実行してコミットする。mdx の説明やデモの見出しを変えたときも同じ（docs/site.md の「デモ」）。
- ドキュメントに載せる添付プロパティを足したら、`Inspector/PropertyCatalog.cs` の `UIKitAttached` に、付けるコントロールの型と一緒に加える。
- デモにコントロールでないオブジェクトの要素（新しい列やモデルの型）を足したら、テストの `NotControls`（`tests/AvaloniaUIKit.Demo.ControlCatalog.Tests/EveryDemoTests.cs`）に加える。

## テスト

`tests/AvaloniaUIKit.Demo.ControlCatalog.Tests`（TUnit、Avalonia.Headless と Skia、同梱の Inter）。`scripts/verify.sh` が本体のテストの前に実行する。

- XAML の読み取り、属性の書き換え、ルート要素の中身の取り出し、ハイライト。
- 値と文字列の変換。
- グリッドでの変更（属性の書き換えと追加、添付プロパティ、クラス、元に戻す、Reset）。
- 全デモ: 登録表とカタログが一致すること、各デモで対応したコントロールが要素と同じ型で、XAML の親の要素のコントロールの下にあること、最初に選ぶ要素があり、そのプロパティのエディターが作れること。コントロールのある要素の 99% 以上が対応すること（残りは閉じたポップアップの中身などで、テストの出力に一覧を出す）。

```sh
dotnet run --project tests/AvaloniaUIKit.Demo.ControlCatalog.Tests
```
