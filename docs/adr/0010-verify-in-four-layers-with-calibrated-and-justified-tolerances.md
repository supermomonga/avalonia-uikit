---
number: 10
title: Verify in four layers with calibrated and justified tolerances
status: accepted
date: 2026-10-03
---

# Verify in four layers with calibrated and justified tolerances

## Context and Problem Statement

GPUI（Metal、CoreText、SDF の AA）と Avalonia（Skia、HarfBuzz、解析的 AA）は描画エンジンが違う。同じ形を描いても画素は完全には一致しない。画素の差だけで判定すると、許容値を広げざるを得ず、色や寸法の誤りを見逃す。動きは、Avalonia に仮想時計がないため、GPUI と同じ時刻のフレームを撮れない。

## Considered Options

* 層を分け、エンジンに依存しない層（色、寸法）は厳密に、依存する層（画素）は領域ごとに校正した許容値で比べる
* 画素の差（全体の平均や最大）だけで比べる
* 人の目で比べる

## Decision Outcome

Chosen option: "層を分ける", because エンジンの違いが出る箇所だけを緩め、それ以外は厳密に比べられるから。

1. **トークン:** 色リソースが GPUI の値と 8bit で完全一致。
2. **構造:** GPUI の Scene と Avalonia の可視ツリーを図形（塗り、枠線、影）に正規化して照合する。色 ±1/255、寸法 ±0.26px。
3. **画素:** GPUI の Scene から画素を Flat / Edge / Ink / Shadow に分類し、領域ごとの許容値で比べる。インク量の比で、文字・アイコン・線の欠けを検出する。
4. **動き:** (a) テーマが宣言する曲線を GPUI の記録と数値で比べる、(b) その値で止めたフレームを比べる、(c) 実時間で動かして終了状態を比べる。リフレクションで時計を差し替えない（R7）。

このほか、時間・入力・無効状態の挙動テストと、FluentTheme と重ねたときのテストを持つ。

### 許容値の決め方

* 許容値は、全ケースの実測値（`AVALONIA_UIKIT_CALIBRATE=1` で書き出す分布）に少しの余裕を足して決める。
* 緩めるときは、必ず原理的な理由を緩和 ID（R1〜R28）として `docs/testing.md` に書き、コードのコメントにも ID を書く。ケース単位の上書きは、緩和 ID を付けた 2 か所（R5、R19）だけ。
* 許容値と緩和の一覧は `docs/testing.md` が正本。

当初は許容値を `tolerances.json` に置く計画だったが、領域の判定や上書きの条件がコードと一体なので、`PixelTolerance` などの C# の型に置いた。

### Consequences

* Good, because 色と寸法の誤りは 1/255、0.26px の単位で検出できる。破線の Separator が描かれていなかった不具合も、インク量の検査で検出できた。
* Good, because どこを、なぜ緩めたかが一覧になっている。
* Bad, because 比較の仕組みが大きい（図形の正規化、領域の分類）。Avalonia のテンプレートの構造が変わると、正規化の側を直す必要がある。
* Bad, because 許容値は macOS arm64 で校正した（R14）。
