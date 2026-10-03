---
number: 16
title: Ship themes for optional packages as separate assemblies
status: accepted
date: 2026-10-04
---

# Ship themes for optional packages as separate assemblies

## Context and Problem Statement

GPUI Kit の ColorPicker と DataTable（DataGrid 版）の対応先は、Avalonia 本体ではなく公式の別パッケージ `Avalonia.Controls.ColorPicker`（12.1.3）と `Avalonia.Controls.DataGrid`（12.1.2、本体とは別のリポジトリと版）にある。テーマを書くにはその型を参照する必要がある。

* `GpuiTheme` がこれらのパッケージを参照すると、使わないアプリにも依存が増える。
* DataGrid は本体がトリム・NativeAOT に対応していない（`Type.GetProperty` などのリフレクション）。`GpuiTheme` が参照すると、ギャラリーの NativeAOT publish（警告をエラーにする）が通らなくなる（ADR 11）。

## Considered Options

* パッケージごとに別アセンブリのテーマを作る
* `GpuiTheme` に含める
* 対応しない

## Decision Outcome

Chosen option: "パッケージごとに別アセンブリのテーマを作る", because `GpuiTheme` の依存と NativeAOT の保証を保ったまま、別パッケージを使うアプリにも同じ見た目を提供できるから。

* `src/AvaloniaUIKit.ColorPicker`（`GpuiColorPickerTheme`）と `src/AvaloniaUIKit.DataGrid`（`GpuiDataGridTheme`）を作る。どちらも `Styles` で、アプリは `GpuiTheme` の後に追加する。
* 名前空間は `AvaloniaUIKit` のままにする。`AvaloniaUIKit.ColorPicker` にすると、`AvaloniaUIKit.*` の中のコードで型 `ColorPicker` が名前空間に隠れる。
* `Gpui.*` のトークンやアイコンは再定義せず、`DynamicResource` で `GpuiTheme` のものを使う。`Tables` の継承値（ADR 17）とキーボードのリングの Behavior も共用する。
* どちらのアセンブリも `IsAotCompatible` で、自身の解析警告はない。DataGrid のテーマはギャラリー（NativeAOT）に入れない。DataGrid 本体の警告はアプリ側の判断になる。

### Consequences

* Good, because `GpuiTheme` だけを使うアプリの依存も NativeAOT の保証も変わらない。
* Good, because 別パッケージを使うアプリは、テーマを 1 つ足すだけで同じ見た目になる。
* Bad, because パッケージの版（特に DataGrid）を本体とは別に追う必要がある。
* Neutral, because DataGrid は公式に非推奨とされている。表の基本の対応先は本体の `TableView` で、DataGrid のテーマはこのパッケージを使う場合のためにある。
