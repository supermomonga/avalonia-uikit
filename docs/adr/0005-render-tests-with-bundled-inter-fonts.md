---
number: 5
title: Render tests with bundled Inter fonts
status: accepted
date: 2026-10-03
---

# Render tests with bundled Inter fonts

## Context and Problem Statement

GPUI Kit の既定フォントはシステム UI フォント（`.SystemUIFont`）。OS やその版によって字形と寸法が変わるので、そのままでは GPUI 側と Avalonia 側で同じ文字列を同じ幅に描けない。文字の位置と幅がずれると、ボタンの幅や中央寄せまで比べられなくなる。

## Considered Options

* 同じフォントファイルをリポジトリに同梱し、両側で使う
* 両側ともシステム UI フォントを使い、文字の比較を緩める
* 文字を比較の対象から外す

## Decision Outcome

Chosen option: "同じフォントファイルを同梱する", because 両側の字形と送り幅が同じになり、文字を含むレイアウトを厳密に比べられるから。

* `assets/fonts/inter/` に Inter の静的 TTF（Regular / Medium / SemiBold / Bold）と `OFL.txt`、`SHA256SUMS` を置く。
* GPUI 側は `add_fonts` で読み込み、テーマのフォントを Inter にする。Avalonia 側はテストアプリで `EmbeddedFontCollection` として登録し、`Gpui.FontFamily` リソースを Inter に差し替える。
* テーマ自体の既定は `Gpui.FontFamily = $Default`（システム UI フォント）のままにする。GPUI Kit の既定と同じ。

### Consequences

* Good, because 文字の幅の違いは丸め方の違い（緩和 R9）とラスタライズの違い（R1）だけになる。
* Bad, because システム UI フォントでの一致は検証しない（緩和 R13）。アプリが同じ見た目を求めるなら `Gpui.FontFamily` を差し替える。
