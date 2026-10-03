---
number: 2
title: Ship GpuiTheme as a standalone Styles theme
status: accepted
date: 2026-10-03
links:
- target: 21
  kind: amendedby
---

# Ship GpuiTheme as a standalone Styles theme

## Context and Problem Statement

GPUI Kit の見た目を Avalonia の既存コントロールに適用するテーマを、どの形で提供するか。アプリは GpuiTheme だけを使う場合も、FluentTheme で残りのコントロールを補う場合もある。GPUI は Window の背景、ポップアップの影、フォーカスリングを自分で描くので、Avalonia の基盤部分（Window、PopupRoot など）の見た目もテーマに含める必要がある。

## Considered Options

* `Styles` を継承した `GpuiTheme` を、FluentTheme と同じ形（XAML + `AvaloniaXamlLoader.Load`）で提供する
* FluentTheme を前提にし、その上に差分のスタイルだけを重ねる
* コントロールごとの `ResourceDictionary` をアプリに個別に読み込ませる

## Decision Outcome

Chosen option: "`Styles` を継承した `GpuiTheme`", because 単独でも FluentTheme と重ねても動き、アプリは 1 行で読み込めるから。

* `GpuiTheme : Styles` は `GpuiTheme.axaml` をコンパイル済み XAML として読み込む。トークン、アイコン、各コントロールの `ControlTheme` を `MergeResourceInclude` でまとめる。
* 基盤のテーマを自前で持つ: `Window`、`EmbeddableControlRoot`、`PopupRoot`、`OverlayPopupHost`、`AdornerLayer`、`ContentControl`、`TransitioningContentControl`、`PathIcon`。
* テンプレートの部品には名前付きのテーマを使う: `GpuiSplitButtonPart`、`GpuiSpinnerRepeatButton`、`GpuiNumericTextBox`、`GpuiTopLevelMenuItem`、`GpuiMenuScrollViewer`、`GpuiScrollBarThumb`、`GpuiScrollBarPageButton`。
* Spinner は Avalonia に対応する型がないので、`ProgressBar` の名前付きテーマ `GpuiSpinner` にする。
* FluentTheme と重ねる場合は、Fluent を先に追加する（後のテーマが優先される）。

### Consequences

* Good, because アプリは `<gpui:GpuiTheme />` を追加するだけで使え、FluentTheme がなくても全コントロールが描画される。
* Good, because FluentTheme の上に重ねても見た目が変わらないことを `FluentLayeringTests` で確かめている（Fluent を後に置くと 19 件中 18 件が失敗するので、検査として効いている）。
* Bad, because テーマが対象にしていないコントロール（TextBox 単体など）は、GpuiTheme だけでは既定のテンプレートを持たない。FluentTheme との併用が前提になる。
