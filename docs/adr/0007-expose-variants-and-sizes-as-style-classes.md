---
number: 7
title: Expose variants and sizes as style classes
status: accepted
date: 2026-10-03
links:
- target: 21
  kind: amendedby
---

# Expose variants and sizes as style classes

## Context and Problem Statement

GPUI Kit のコンポーネントは、ビルダーのメソッドで色違い（`primary()`、`ghost()` など）、修飾（`outline()`、`compact()`）、サイズ（`xsmall()` など）を選ぶ。Avalonia の既存コントロールにはこれに当たるプロパティがない。新しいコントロール型や添付プロパティを作ることは、対応表の方針（新規コントロールを作らない）で避けたい。

## Considered Options

* スタイルクラス（`Classes="primary small"`）で選ぶ
* 添付プロパティ（`gpui:Button.Variant="Primary"`）を定義する
* 色違いごとに名前付きの `ControlTheme` を用意する（`Theme="{StaticResource GpuiPrimaryButton}"`）

## Decision Outcome

Chosen option: "スタイルクラス", because コードを足さずに XAML だけで書け、色違い・修飾・サイズを自由に組み合わせられるから。名前付きテーマでは組み合わせの数だけテーマが要る。

| コントロール | クラス |
| --- | --- |
| Button | 色: `primary` `secondary` `danger` `warning` `success` `info` `ghost` `link` `text`（なしが Default）。修飾: `outline` `compact` `selected` `icon-only`。サイズ: `xsmall` `small` `large`（なしが medium）。角丸: `rounded-none` `rounded-small` `rounded-large` |
| SplitButton | Button と同じ色、`outline`、`selected`、サイズ |
| ToggleButton | `outline`（なしが ghost）、サイズ |
| CheckBox / RadioButton / NumericUpDown / ProgressBar | サイズ |
| ToggleSwitch | サイズ、`label-left` |
| GroupBox | `fill` / `outline`（なしが normal） |
| Separator | `vertical` / `dashed` |
| Spinner | `ProgressBar` に `Theme="{StaticResource GpuiSpinner}"`、サイズ |

* 修飾なし・サイズなしの規則を先に書き、クラス付きの規則を後に書く（後の規則が優先されるため）。
* 組み合わせの多い Button、SplitButton、CheckBox、RadioButton のテーマは、`scripts/gen_*.py` で生成する。手で書くと組み合わせの漏れが出るため。

### Consequences

* Good, because XAML だけで GPUI Kit と同じ語彙で書ける。
* Good, because 新しい型やコードがないので NativeAOT の制約（ADR 11）に触れない。
* Bad, because クラス名の誤りはコンパイル時に検出されず、既定の見た目になる。
