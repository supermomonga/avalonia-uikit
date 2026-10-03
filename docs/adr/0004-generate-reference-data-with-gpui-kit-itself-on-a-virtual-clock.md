---
number: 4
title: Generate reference data with GPUI Kit itself on a virtual clock
status: accepted
date: 2026-10-03
---

# Generate reference data with GPUI Kit itself on a virtual clock

## Context and Problem Statement

見た目と動きの一致を自動テストで保証するには、比べる相手（正解）が要る。正解は GPUI Kit が実際に描いたものでなければならない。動きの途中の状態も、決まった時刻で何度でも同じように撮れる必要がある。GPUI のアニメーションは壁時計（`Instant::now()`）を読むため、そのままでは時刻を制御できない。

## Considered Options

* GPUI Kit を固定コミットのまま動かす Rust 製の生成器で、PNG と Scene（描画命令）を書き出す
* GPUI Kit のサンプルアプリを実画面で動かしてスクリーンショットを撮る
* GPUI Kit のソースを読んで値を手で書き写す

## Decision Outcome

Chosen option: "Rust 製の生成器", because 同じ入力から同じ画像と描画命令が得られ、画素だけでなく図形の単位でも比べられるから。

* `reference/` は `harness=false` の Rust バイナリ。`HeadlessAppContext` と Metal の headless レンダラで描画し、レンダラをラップして Scene を JSON に書き出す（quad、影、下線、スプライト、パス。座標は論理 px、色は GPUI 自身の変換）。
* ケースは `cases/*.toml` で定義し、Avalonia 側のテストと共有する。ID、要素の bounds、PNG、Scene を `goldens/gpui-2c5162f/manifest.json` にまとめる。
* GPUI Kit と gpui-pre は `vendor.sh` で `reference/vendor/` に展開する（gitignore）。参照元のリポジトリは読むだけで、変更しない。
* 時刻: gpui-pre の `elements/animation.rs` と gpui-base の `scrollbar.rs` にある `Instant::now()` を、テスト用 executor の時計（`background_executor().now()`）に置き換えるパッチを当てる。`advance_clock` で時刻を進めてフレームを撮る。パッチは時刻の読み取りだけを変え、描画には影響しない（緩和 R15）。`vendor.sh` は置き換え漏れがあれば止まる。
* `verify-determinism` は全ケースを 2 回描画し、バイト単位で一致することを確かめる（1408 ケースで一致）。全ケースを生成し直しても、コミット済みの参照データと差分が出ないことも確かめている。

### Consequences

* Good, because 正解が GPUI Kit 自身の描画になり、手で写した値の誤りが入らない。
* Good, because 動きを任意の時刻で、何度でも同じように撮れる。
* Bad, because 生成は macOS（Metal）でしかできない（緩和 R14）。テスト自体は Avalonia + Skia なのでほかの OS でも動く。
* Bad, because GPUI 側のパッチは、GPUI Kit を更新するたびに当たるか確かめる必要がある。
