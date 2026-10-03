---
number: 11
title: Enforce NativeAOT compatibility and ban reflection
status: accepted
date: 2026-10-03
---

# Enforce NativeAOT compatibility and ban reflection

## Context and Problem Statement

AGENTS.md は「このライブラリは NativeAOT でビルドできなければならない。そのためリフレクションの使用を禁止する」と定めている。XAML のテーマは、書き方によっては実行時にリフレクションを使う（反射バインディング、実行時の XAML 読み込み、型名からの生成）。守れているかを、人の注意ではなく機械で確かめたい。

## Decision Outcome

ビルドと実行の両方で強制する。

* **ビルド時:**
  * `IsAotCompatible`、`IsTrimmable`、`EnableTrimAnalyzer` を有効にし、`TreatWarningsAsErrors` で警告をエラーにする。
  * `AvaloniaUseCompiledBindingsByDefault` でコンパイル済みバインディングを既定にし、`AvaloniaXamlIlVerifyIl` で生成した IL を検査する。
  * `Microsoft.CodeAnalysis.BannedApiAnalyzers` で、`System.Reflection` の主要な型、`Activator`、`Type.GetType(string)`、`ReflectionBindingExtension`、`Avalonia.Data.Binding` を禁止する（`BannedSymbols.txt`）。名前空間ごと禁止すると、コンパイラが生成する AssemblyInfo まで引っかかるため、型単位で禁止する。
* **実行時:** `samples/AvaloniaUIKit.AotSmoke` を NativeAOT で publish し（`IlcTreatWarningsAsErrors`）、全コントロールを Light / Dark で描画して終了する（`scripts/aot-smoke.sh`）。

テストプロジェクトは NativeAOT の対象外。テストはテーマの宣言（Transition や Animation）を読むために、テーマのオブジェクトを調べる。それでもリフレクションは使っていない。

### Consequences

* Good, because リフレクションを使う書き方はビルドエラーになり、AOT での動作は publish と実行で確かめられる。
* Bad, because XAML でのバインディングは `TemplateBinding` とコンパイル済みバインディングに限られる。ブラシの中の要素など、コンパイル済みバインディングで親を辿れない場所では書き方を工夫する必要がある（破線の Separator は Path の破線に変えた）。
