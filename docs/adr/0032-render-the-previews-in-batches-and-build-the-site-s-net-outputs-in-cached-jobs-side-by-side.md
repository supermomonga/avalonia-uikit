---
number: 32
title: Render the previews in batches and build the site's .NET outputs in cached jobs side by side
status: accepted
date: 2026-10-05
links:
- target: 20
  kind: amends
- target: 24
  kind: amends
---

# Render the previews in batches and build the site's .NET outputs in cached jobs side by side

## Context and Problem Statement

サイトのワークフロー（`.github/workflows/site.yml`）は、1 つのジョブでプレビューの描画、WASM の publish、サイトのビルドを順に行っていた（ADR 20）。2026-10-05 の Pull Request の実行（288 デモ）は 7 分 19 秒かかり、そのうち 334 秒がプレビューの描画だった。

プレビューの描画（`samples/AvaloniaUIKit.Previews`）は、デモを 1 つ表示するたびに、読み込み時の遷移が終わるまで実時間で約 0.5 秒（80ms の待ちを 6 回）待ってから撮っていた。ヘッドレスのレンダータイマーは実時間（`Stopwatch`）で進むので、待たずに時計だけを進めることはできない。Light と Dark で 2 回撮るので、待ちだけで 288 × 2 × 0.48 ≒ 276 秒になる。macOS arm64 で測ると、全体 298 秒のうち CPU 時間は 23 秒だった。

一方、サイトのビルドが使うのは `manifest.json` の大きさだけで、PNG は OG 画像（ADR 22）しか使わない。OG 画像はコミットしたものを使い、CI では描かない。CI は誰も見ない 576 枚の PNG を描いて配信していた（ADR 24 の「Previews は引き続き CI で描く」）。また、サイトの文章だけを変えたときも、.NET の 2 つの出力を毎回作り直していた。

## Decision Drivers

* PR の確認とデプロイを速くしたい。時間の 3/4 はプレビューの待ちだった。
* 出力（`manifest.json`、PNG、WASM のバンドル）を変えないこと。
* 古い出力のまま配信する危険を、仕組みと文書で抑えること。

## Considered Options

* 待ち: デモをまとめて表示し、1 回の待ちを共有する
* 待ち: デモをいくつかのプロセスに分けて同時に描く
* PNG: CI では `manifest.json` だけを書く
* PNG: CI でも PNG を描く
* 再利用: .NET の出力を、元になるファイルのハッシュをキーにキャッシュする
* 再利用: 毎回作る
* ジョブ: プレビュー、WASM、サイトの 3 つのジョブに分け、出力をキャッシュで受け渡す
* ジョブ: 3 つのジョブに分け、出力を artifact で受け渡す
* ジョブ: 1 つのジョブのまま

## Decision Outcome

Chosen option: 「まとめて表示」「CI では `manifest.json` だけ」「ハッシュをキーにキャッシュ」「3 つのジョブとキャッシュでの受け渡し」。

* **まとめて表示:** Previews は 32 個ずつ、デモの Light と Dark のウィンドウを表示し、1 回だけ待ってから順に大きさを測って撮る。ヘッドレスのウィンドウは別々のトップレベルなので、通知や Sheet のオーバーレイは混ざらない。待つ間に例外が出ると、どのデモが原因か分からない。そのときはそのバッチを 1 つずつ描き直し、失敗したデモを名指しで報告する。
* **CI では `manifest.json` だけ:** Previews に `--manifest-only` を加え、CI はこれで実行する。サイトには PNG を置かず、`_headers` の `/previews/*` の規則も消す。OG 画像を描き直すときは、手元で PNG まで描く（`docs/site.md`）。ADR 24 の「Previews は引き続き CI で描く」を、この ADR で「CI は大きさだけを測る」に変える。
* **キャッシュ:** プレビューと WASM の出力（`sites/public/previews/`、`sites/public/wasm/`）を、Actions のキャッシュに保存する。キーには、解決された SDK のバージョン（`dotnet --version`）と、出力の元になるファイルのハッシュを入れる。元になるファイルは `src/**`、Demos と Previews または Browser、`assets/**`、ルートの props、`global.json`、`site.yml`、WASM ではさらに `publish-wasm.sh`。同じキーがあれば（`lookup-only` で確かめる）、NuGet、ワークロード、ビルドを丸ごと省く。
* **3 つのジョブ:** `previews` と `wasm` が並んで走り、キーをジョブの出力として `site` に渡す。`site` は同じキーでキャッシュから復元する（`fail-on-cache-miss`）。artifact を使わないのは、キャッシュに当たったときに出力を落として上げ直す手間がなく、外れたときも 1 回の保存で済むからである。NuGet のキャッシュはジョブごとにキーを分ける。同じキーにすると、先に保存したジョブの中身（もう一方のパッケージを含まない）が使われ続ける。

### Consequences

* Good, because プレビューの描画が macOS arm64 で 298 秒から 15 秒（`--manifest-only` では 12 秒）になる。手元で PNG を描くときも同じだけ速くなる。
* Good, because サイトだけを変えた PR や push では .NET をビルドしない。.NET を変えたときも、プレビューと WASM が並んで走る。
* Good, because 使われない 576 枚の PNG をデプロイしなくなる。
* Bad, because キャッシュのキーに入れ忘れたファイルがあると、古い出力のまま配信される。出力の元を増やしたら（新しいプロジェクトの参照、`assets/` の外のファイル）、`site.yml` のハッシュの対象にも加える必要がある（`docs/site.md`）。ワークロードのマニフェストは SDK のバージョンとは別に更新されることがあるが、キーには入れていない。
* Bad, because PR で保存したキャッシュは main から読めない（GitHub のキャッシュの範囲）ので、PR のマージのあとの push では作り直す。
* Bad, because ジョブが 3 つになり、それぞれに起動と checkout の時間がかかる。
* Neutral, because スピナーや indeterminate の進捗、シマーのように繰り返すアニメーションの PNG は、撮る瞬間の位相で変わる。これはまとめる前から実行ごとに変わっていた。

### Confirmation

* macOS arm64 で、変更前と変更後の Previews の出力を比べた。`manifest.json` は一致した。PNG は 576 枚のうち 548 枚がバイト単位で一致した。残る 28 枚（14 デモ: Button、DropdownButton、Marker、ListView の loading、Progress と ProgressCircle の indeterminate、Shimmer、Skeleton、Spinner）は繰り返すアニメーションで、変更前のコードを 2 回実行しても、そのうち 27 枚が異なった。
* 待つ間に例外を投げる偽のデモを一時的に加えると、そのデモだけが `InvalidOperationException` として報告され、同じバッチのほかのデモは描かれ、終了コードは 1 になった。
* `--manifest-only` の `manifest.json` だけで `bun run build` と `bun scripts/smoke.ts` が通る。`actionlint` が `site.yml` に何も報告しない。

## Pros and Cons of the Options

### 待ち: まとめて表示

* Good, because 待ちがバッチの数（288 デモで 9 回）で済み、1 つのプロセスのままで済む。
* Bad, because 待つ間の例外がどのデモのものか分からない。1 つずつの描き直しで補う。

### 待ち: プロセスに分ける

* Good, because デモどうしが同じプロセスに同時にいない。
* Bad, because プロセスごとにヘッドレスのセッションを起動し、`manifest.json` を合わせる仕組みが要る。待ちは分けた数でしか割れない。

### PNG: CI では `manifest.json` だけ

* Good, because PNG のエンコードと配信を省ける。
* Bad, because CI で PNG の描画が壊れても気づかない。PNG を使う OG 画像は手元で描くので、そのとき気づく。

### PNG: CI でも描く

* Good, because 描画の失敗に CI で気づける。
* Bad, because 誰も使わないファイルを作って配信し続ける。

### 再利用: ハッシュをキーにキャッシュ

* Good, because 元が変わらなければ .NET のビルドを丸ごと省ける。
* Bad, because キーの対象を保守する必要があり、漏れると古い出力が配信される。

### 再利用: 毎回作る

* Good, because 古い出力を配信する危険がない。
* Bad, because サイトだけの変更でも .NET のビルドを待つ。

### ジョブ: キャッシュで受け渡す

* Good, because キャッシュに当たったジョブはファイルを落とさずに終わり、外れたときも保存は 1 回で済む。
* Bad, because 保存と復元の間にキャッシュが消えると、`site` が失敗する（再実行で直る）。

### ジョブ: artifact で受け渡す

* Good, because 受け渡しが実行の中で閉じている。
* Bad, because キャッシュに当たっても、出力を落として artifact として上げ直す必要がある。

### ジョブ: 1 つのジョブのまま

* Good, because ワークフローが単純。
* Bad, because プレビューと WASM が順に走る。キャッシュに当たったかどうかで手順を分けるのも、1 つのジョブの中では条件が増える。

## More Information

ADR 20 の CI の手順（1 つのジョブで順に行う）と、ADR 24 の「Previews は引き続き CI で描く」を変える。手順と約束事は `docs/site.md` の「プレビュー画像」と「ビルドとデプロイ」にまとめる。
