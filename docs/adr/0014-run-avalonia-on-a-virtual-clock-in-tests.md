---
number: 14
title: Run Avalonia on a virtual clock in tests
status: accepted
date: 2026-10-03
---

# Run Avalonia on a virtual clock in tests

## Context and Problem Statement

ADR 10 では、Avalonia に時刻を指定する公開 API がないため、動きを 3 つに分けて検証していた。(a) テーマが宣言した曲線をテスト側で計算して GPUI の記録と比べる、(b) その値をコントロールに直接設定したフレームを比べる、(c) 実時間で動かして終了状態を比べる、の 3 つである。この方式には次の問題があった。

* Avalonia のアニメーターの計算（Transition、繰り返し、KeySpline）をテスト側で再実装していて、本物とずれる可能性がある。
* テーマの宣言を、セレクターの文字列照合で探している。
* 動きごとに専用のクラスが要り、テンプレートの内部を知っている必要がある。
* 実際に描かれる動きは確かめていない。実際、スクロールバーのフェードは状態を切り替えた瞬間に値が飛んでいて、まったく動いていなかった。ProgressBar の表示直後の幅にも、GPUI にない動きが付いていた。どちらもこの方式では見つからなかった。

## Considered Options

* テストに限り、Avalonia の内部に手を入れて時計を差し替える
* 動きを分解して検証する方式を続ける（ADR 10）

## Decision Outcome

Chosen option: "テストに限り、内部に手を入れて時計を差し替える", because Avalonia 自身のアニメーターが描いたフレームを、GPUI と同じ時刻で比べられるから。テストだけの変更で、ライブラリはリフレクションを使わないまま（ADR 11）。

* `Infrastructure/VirtualTime.cs` が、テストごとに次の 2 つを仮想時計に替える。
  * **動きの時計:** Transition と Animation が使う、継承されるプロパティ `Animatable.Clock` に、テストが進めたときだけ時刻が進む時計を設定する。プロパティは公開の `AvaloniaPropertyRegistry.FindRegistered` で取得する。時計は `UnsafeAccessor` と `UnsafeAccessorType` で内部の `ClockBase` を作り、`Pulse` を呼んで進める。
  * **Dispatcher の時刻:** `DispatcherTimer` の時刻の取得元（`Dispatcher._timeProvider`）を差し替え、ディスパッチャーのループのストップウォッチ（`ManagedDispatcherImpl._clock`）を止める。期限の来たタイマーは `Dispatcher.PromoteTimers` で実行する。`_impl` は型が参照アセンブリにないので、リフレクションで取得する。
* 時刻は 1ms ずつ進め、そのたびにタイマー、ジョブ、時計の順に処理する。
* 動きのテストは、時刻を GPUI が記録した各時刻まで進めて描画し、構造と画素で比べる 1 種類にした。静止状態のテストも Transition を外さず、GPUI と同じく、操作の直後（`wait-` があればその分だけ進めた後）を撮る。

### Consequences

* Good, because 動き専用のテストコードが約 750 行から約 200 行になった。動きを足すときは TOML にケースを足すだけでよい。
* Good, because テーマの不具合を 2 つ見つけて直せた（上記）。
* Good, because 実時間の待ちがなくなり、全テストが約 10 分から約 35 秒になった。結果も実行のたびに変わらない。
* Bad, because Avalonia の内部の名前 6 か所に依存する。Avalonia の更新で変わると、テストが起動時に失敗する（緩和 R7）。
* Neutral, because 実時間での動作そのものは確かめない。時刻の取得元が違うだけで、同じアニメーターが動く。
