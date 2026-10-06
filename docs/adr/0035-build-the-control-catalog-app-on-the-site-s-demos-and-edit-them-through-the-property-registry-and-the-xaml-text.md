---
number: 35
title: Build the control catalog app on the site's demos and edit them through the property registry and the XAML text
status: accepted
date: 2026-10-06
---

# Build the control catalog app on the site's demos and edit them through the property registry and the XAML text

## Context and Problem Statement

ドキュメントサイト（ADR 20）のように各コントロールのデモを一覧して触れ、そのコードも読め、さらにデモのコントロールの設定値をその場で変えて試せるアプリ `AvaloniaUIKit.Demo.ControlCatalog` を作りたい。左に開閉できるコントロール名の一覧、中央にデモ、プロパティグリッドのように値を変える場所、という形である。

問題は 3 つある。何をデモとして載せるか（サイトとは別に作るか）、プロパティグリッドが何をどう編集するか（リフレクションは禁止。ADR 11）、変えた値をコードの表示にどう反映するか。

## Decision Drivers

* サイトのデモは 92 コンポーネント、286 個あり、mdx に見出しと説明がある。別にデモを書くと、2 つの一覧を保守することになる。
* ライブラリの外見の API の大半はスタイルクラス（`primary`、`small`、`outline`）と AvaloniaUIKit の添付プロパティ（`uikit:Buttons.IsLoading` など）にある。普通の CLR プロパティだけのグリッドでは、肝心の部分が触れない。
* リフレクションを使わない（ADR 11）。`PropertyInfo` を列挙する一般的なプロパティグリッドは使えない。
* コードは、コピーしてそのまま使える XAML であってほしい。値を変えたら、その変更がコードに出てほしい。
* 新しいデモやコンポーネントを足したとき、カタログの手入れを最小にしたい。

## Considered Options

* サイトのデモをそのまま載せ、デモ中の任意の要素を Avalonia のプロパティレジストリで編集し、変更をデモの XAML のテキストに書き戻す
* カタログ専用のプレイグラウンドをコンポーネントごとに作り、決めた「つまみ」（variant、size など）だけを変える（Storybook の Controls 風）
* サイトのデモをそのまま載せ、編集した要素をその場で XAML に直列化して見せる（元の XAML は別に見せる）

## Decision Outcome

Chosen option: "サイトのデモをそのまま載せ、レジストリで編集し、XAML のテキストに書き戻す", because デモと説明をサイトと共有でき、コンポーネントごとの手書きなしにすべてのデモのすべての要素を編集でき、コードの表示がデモの元の XAML に変更を加えたものになるから。約束事は `docs/control-catalog.md` にまとめる。

* **プロジェクト:** `samples/AvaloniaUIKit.Demo.ControlCatalog`（デスクトップ、`Avalonia.Desktop`）。`samples/AvaloniaUIKit.Demos` を参照し、デモのクラスは `DemoRegistry.Factories` から作る。ブラウザーには載せない（サイトがその役目を持つ）。
* **一覧と説明:** `sites/scripts/control-catalog.ts` が `catalog.ts`、各 mdx（説明、デモの順、見出し、デモの直前の段落）、サイトのコードの色（`--code-*`）から `Catalog/Catalog.g.cs` を生成する。`demo-registry.ts` と同じくコミットし、CI（`site.yml`）が一致を確かめる。
* **コード:** デモの `.axaml` と `.axaml.cs` を `AvaloniaResource` として取り込み、実行時に読む。サイトと同じくルート要素の中身を出し、同じ色で塗る。
* **要素とコントロール:** XAML を位置付きで読み（`XamlDocument`）、ロジカルツリーと並べてたどって要素ごとのコントロールを見つける（`DemoMap`）。同じ型の子を順に、要素の書く内容（`Content`、`Header`、`Name`、`Classes`）と矛盾しないものに対応させ、テンプレートが表示する中身はビジュアルツリーから探す。全デモで、コントロールのある要素の 99% 以上が対応する（残りは閉じたポップアップの中身など）。
* **編集するもの:** プロパティは `AvaloniaPropertyRegistry` から型ごとに取る。AvaloniaUIKit の添付プロパティは登録表から型を名前で探せないので、ドキュメントにあるものを付けるコントロールの型と一緒に列挙する（`PropertyCatalog.UIKitAttached`）。スタイルクラスは、テーマの `ControlTheme` とスタイルのセレクターを実行時に読んで出す。値と文字列の変換は型ごとに書く（`XamlValues`）。
* **書き戻し:** 変更は `GetValue` / `SetValue` でコントロールに入れ、同時に要素の番号と属性名で記録して、元のテキストの属性の値だけを書き換えるか、最後の属性の後ろに足す（`XamlEdits`）。変えた属性はコードで印を付ける。
* **リフレクションの禁止の確かめ方:** プロジェクトで `IsAotCompatible` を有効にし、トリミングと AOT のアナライザーの警告をエラーにする。カタログ自体を NativeAOT で publish はしない（DataGrid と Dock.Avalonia がトリミング非対応）。

### Consequences

* Good, because デモを足せば（生成を 1 回実行するだけで）カタログにも載り、見出しと説明もサイトと同じになる。
* Good, because どのデモのどの要素でも、クラス、プロパティ、添付プロパティを変えられ、変更は元の書式を保った XAML に入る。コピーしたコードがそのままアプリで使える。
* Good, because テーマの追加やクラスの追加は、テーマを読むので手入れなしで反映される。
* Bad, because 要素とコントロールの対応は推定であり、デモの書き方によっては外れうる。テスト（全デモの対応の型と親子関係、99% の対応率）で守る。
* Bad, because ドキュメントに載せる添付プロパティを増やしたら、`UIKitAttached` にも足す必要がある（レジストリでは見つからないため）。
* Bad, because XAML に書くのはグリッドでの変更だけで、デモ自身の操作（チェックを入れるなど）による値の変化はコードに出ない。バインディングやリソースの参照を書いた属性を変えると、その参照は値に置き換わる。
* Neutral, because 生成ファイルが 1 つ増え、mdx の説明を変えたときにも再生成が要る。

### Confirmation

* `tests/AvaloniaUIKit.Demo.ControlCatalog.Tests`（`scripts/verify.sh` が実行する）が、XAML の読み書き、値の変換、グリッドでの変更と元に戻す操作、全デモの対応とエディターの作成を確かめる。
* `site.yml` が `bun sites/scripts/control-catalog.ts --check` で生成ファイルの一致を確かめる。

## Pros and Cons of the Options

### サイトのデモをそのまま載せ、レジストリで編集し、XAML のテキストに書き戻す

* Good, because デモ、見出し、説明の元が 1 つ。
* Good, because 汎用で、コンポーネントごとのコードが要らない。
* Bad, because 要素とコントロールの対応を推定する仕組みが要る。
* Bad, because プロパティが多く（Button で 80 前後）、グリッドが長い。型ごとのグループに分け、基底型のものを閉じ、名前で絞り込めるようにして扱う。

### カタログ専用のプレイグラウンドとつまみ

* Good, because 見せたい設定だけを分かりやすく並べられ、コードもつまみから組み立てるだけで済む。
* Bad, because 92 コンポーネント分のプレイグラウンドとつまみを書き、ライブラリの変更のたびに保守する必要がある。サイトのデモとも二重になる。
* Bad, because つまみにない設定は試せない。

### 編集した要素をその場で XAML に直列化する

* Good, because 要素とテキストの位置の対応が要らない。
* Bad, because ライブのコントロールには、XAML に書いた値とテンプレートや既定値の区別が残らず、`{DynamicResource}` や `{Binding}` も解決済みの値になる。出てくる XAML が元のデモと違い、そのままでは使いにくい。
* Bad, because 子要素や添付プロパティ、プロパティ要素の書き方を再現するには、XAML の書き出しをほぼ一から作ることになる。
