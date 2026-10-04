---
number: 25
title: Keep children inside rounded borders and check each border's ink
status: accepted
date: 2026-10-04
links:
- target: 8
  kind: amends
- target: 10
  kind: amends
- target: 18
  kind: amends
---

# Keep children inside rounded borders and check each border's ink

## Context and Problem Statement

ドキュメントサイトの Accordion で、カードの枠線の四隅が欠けていた。Wasm 版だけでなく、デスクトップ（Skia）でも同じだった。

* GPUI は要素の枠線を子より後（上）に描く。Avalonia の `Border` は枠線を描いてから子を描く。`ClipToBounds` のクリップは外形の角丸（`ClipToBoundsRadius` は `CornerRadius`）なので、不透明な背景を持つ子の四角い角が、枠線の弧を塗りつぶす。
* 比較の 4 層（ADR 10）はこれを見逃していた。構造は図形の集合を照合するので、重なる順番を見ない。画素の Edge 領域は 1 デバイス px 以内で最も近い GPUI の画素と比べるので、細い線が消えても隣の背景色と一致する。インク量の検査は文字・アイコン・パスだけが対象だった。
* 新しい検査を入れると、メニューの区切り線の差も見つかった。GPUI は高さ 2px の箱に 2px の下枠を描くが、シェーダー（`quad_fragment`）は点のある象限の辺の太さを使うので、枠は箱の中心線までしか塗られず、1px の線になる。Avalonia は 2px の線を描いていた。

## Considered Options

* テーマ: 角丸の枠の内側の子を、枠の内縁の角丸でクリップする
* テーマ: 背景を子からカードに移し、枠線が自分の背景の上に描かれるようにする
* テスト: 枠線ごと・角と辺ごとのインク量の比を、画素の層で比べる
* テスト: Edge 領域の許容値を厳しくする

## Decision Outcome

テーマは、内縁でクリップする。テストは、枠線のインク量を角と辺ごとに比べる。

* **内縁でのクリップ:** `Border.accordion > StackPanel.accordion` の `Clip` を、パネルの大きさの角丸の矩形（半径は radius_lg 8 − 枠 1 = 7、`RoundedClipConverter`）にする。背景は GPUI と同じく各項目が持つ。背景をカードに移す案は画素では一致したが、構造で GPUI の Scene（角丸なしの項目の塗り 3 枚）と対応が取れなくなる。合わせるには同色の塗りを比べない正規化が要り、構造の検出力が下がる。
* **枠線の量:** GPUI の枠付きの quad ごとに、帯（外形と内縁の間、AA の 1.5 デバイス px を含む）の画素を 4 つの角と 4 つの辺に分け、インク量（下の塗りとの最大チャンネル差の総和）を GPUI と Avalonia で比べる。GPUI の量が 200 以上の部分で、比は 0.6〜1.6 倍（実測 0.69〜1.32）。欠けた Accordion の角は 0.41〜0.45、2px の区切り線は 2.0 だった。
  * GPUI がクリップごとに分けて描いた同じ枠は、1 つにまとめる。
  * 位置が 1 デバイス px ずれても帯から外れないよう、クリップを 1.5 デバイス px 広げて数える（R9）。
  * クリップが辺を切る位置は丸めが分かれ、細い線が 2 倍にも 0 にもなるので、その辺と両端の角は数えない（R9）。
  * フェード中のフレームは、半透明の背景の下に透ける影のほうが枠より濃いので比べない（R5）。
* **枠の太さの正規化:** GPUI の Scene を読むとき、各辺の枠の太さを箱の幅・高さの半分までにする（ADR 18 の正規化に加える）。メニューの区切り線は、2px の箱の下側 1px の枠として描く。`CornerRadius` は、枠のある下側が中心線で 0.5（外形で GPUI の 1）、枠のない上側が 1。

### Consequences

* Good, because 子が枠を塗りつぶす不具合と、枠の太さの違いを、画素の層で検出できる。Edge の許容値は変えずに済む。
* Good, because 構造は GPUI の Scene と 1 対 1 のまま照合できる。
* Bad, because 内縁の角では項目の背景の AA が枠の内側の AA に重なり、枠の量は GPUI の 0.86〜0.88 倍になる。見た目の差はほとんどない。
* Bad, because クリップの半径はカードの角丸と枠の太さから手で求めた値で、カードの `CornerRadius` を変えるアプリは合わせて変える必要がある。
* Bad, because クリップに切られた辺とフェード中のフレームの枠は、この検査では比べない。
