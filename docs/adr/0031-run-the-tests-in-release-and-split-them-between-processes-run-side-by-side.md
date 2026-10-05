---
number: 31
title: Run the tests in Release and split them between processes run side by side
status: accepted
date: 2026-10-05
links:
- target: 12
  kind: amends
---

# Run the tests in Release and split them between processes run side by side

## Context and Problem Statement

全 6985 件のテストに、macOS arm64 で約 13 分かかっていた。テストは 1 つのヘッドレスのセッションの UI スレッドで 1 件ずつ動くので、プロセスの中では並列にできない（ADR 12）。

テストごとの時間を測ると、大半は描画ではなく、テスト側の画素の比較（領域の分類、インク量、枠線の量）に使われていた。`scripts/verify.sh` は Debug でビルドしていたので、この画素ごとのループに JIT の最適化がかかっていなかった。

## Considered Options

* Release でビルドして実行する
* テストのプロジェクトだけ `<Optimize>true</Optimize>` にする
* テストを複数のプロセスに分けて同時に実行する（テストクラス単位で分ける / テストごとのハッシュで分ける）
* `AvaloniaTestIsolationLevel.PerAssembly` にして、テストごとのアプリの作り直しをやめる

## Decision Outcome

Release で実行し、テストを複数のプロセスに分けて同時に実行する。

* `scripts/verify.sh` は Release でビルドして実行する。`src` にも `tests` にも `#if DEBUG` や `Debug.Assert` はなく、検証の内容は変わらない。Release にするだけで、全件が 13 分 22 秒から 3 分 12 秒になった。テストのプロジェクトだけを最適化する案は、スクリプトのビルド構成を変えるだけで済むほうが単純なので採らなかった。
* 引数なしの `scripts/verify.sh` は、テストを N 個のプロセスに分けて同時に実行する。N は macOS では P コアの数（`hw.perflevel0.physicalcpu`）、ほかの OS では論理コアの数で、`AVALONIA_UIKIT_SHARDS` で変えられる。
  * TUnit の `ITestDiscoveryEventReceiver` を実装したアセンブリ属性（`Infrastructure/Shard.cs`）が、`AVALONIA_UIKIT_SHARDS` が設定されているとき、各テストに `Shard` プロパティを付ける。値は、クラス名と表示名の FNV-1a ハッシュを N で割った余り。各プロセスは `--treenode-filter "/*/*/*/*[Shard=k]"` で自分の分を選ぶ。ハッシュはどのプロセスでも同じなので、どのテストもちょうど 1 回実行される（4 分割で 1 プロセスあたり 1713〜1768 件）。
  * クラス単位で分ける案は採らなかった。1 件の所要時間がクラスによって 5ms から 1 秒まで違い、均等に分けるには過去の実行時間が要る。テストごとのハッシュなら、所要時間を知らなくてもほぼ均等になる。
  * 引数を渡したとき（一部だけの実行など）と校正（`AVALONIA_UIKIT_CALIBRATE=1`。CSV に追記する）は、これまでどおり 1 プロセスで実行する。
* プロセスの中では、これまでどおり 1 件ずつ実行する（ADR 12）。`PerAssembly` にする案は採らなかった。アプリの作り直しは 1 件あたり約 1.2ms（全体で約 8 秒）しかかからず、テスト間で状態が漏れる危険に見合わない。
* あわせて、画素の比較の手順を見直した。結果は変えていない（全ケースの領域の分類とインク量が変更前と一致することを確かめた）。
  * 下の塗りを合成する `Beneath` が、画素ごとにシーンの quad を並べ替えていた。並べ替えはフレームごとに 1 回にした。
  * 輪郭の近くを探す検査（Edge 領域、影、画像の縁、枠線の量）が、箱の内側をすべて調べていた。輪郭から十分に深い内側は飛ばす。大きな箱でも、調べる画素は面積ではなく周長に比例する。
  * Release のまま 1 プロセスで実行すると、3 分 12 秒が 1 分 49 秒になった。

### Consequences

* Good, because 全件が約 50〜70 秒で終わる（macOS arm64、P コア 4 つに 4 プロセス。マシンの状態で揺れる）。同じ時期に測った 1 プロセスは 103 秒、2 プロセスは 71 秒、3 プロセスは 64 秒、4 プロセスは 48〜59 秒で、Debug で 1 プロセスだった頃の約 13 分の 1/10 以下になる。
* Bad, because プロセスごとに JIT とヘッドレスのセッションの起動が重なるので、CPU 時間の合計は 1 プロセスのときの約 2 倍になる。P コアより多く分けると遅くなった（5 プロセスと 6 プロセスは 4 プロセスより遅い）。
* Bad, because 実行中は進捗が出ず、各プロセスの結果は終わってからまとめて表示される。失敗したプロセスはログを丸ごと表示する。
* Neutral, because Debug でしか起きない不具合は、このスクリプトでは見えない。今のところ、テストにも `src` にも Debug だけの経路はない。
