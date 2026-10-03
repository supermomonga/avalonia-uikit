---
number: 6
title: Generate color tokens from the resolved GPUI Kit theme
status: accepted
date: 2026-10-03
---

# Generate color tokens from the resolved GPUI Kit theme

## Context and Problem Statement

GPUI Kit の色は、テーマ JSON の参照を解決した値に加えて、各コンポーネントが描画時に計算する色（例: hover で `opacity(0.9)`、disabled で `opacity(0.5)`）がある。XAML では色の演算ができないので、状態ごとの色をあらかじめ計算しておく必要がある。手で計算すると GPUI の色演算（HSLA と丸め）とずれる。

## Considered Options

* 参照データの生成器（Rust）が GPUI Kit の色関数で計算し、`Colors.g.axaml` を書き出す
* C# のツールでテーマ JSON を読み、GPUI の色演算を再実装して書き出す（当初の計画）
* 手書きする

## Decision Outcome

Chosen option: "生成器が GPUI Kit の色関数で計算する", because GPUI 自身の関数で計算すれば、演算と丸めを再実装する必要がなく、値が必ず一致するから。

* `reference generate` が `tokens/gpui-theme.json` と `src/AvaloniaUIKit/Themes/Tokens/Colors.g.axaml` を書き出す。
* Light は `ThemeDictionaries` の `Default`、Dark は `Dark` に入れる。キーは `Gpui.<PascalCase>`（Brush）と `Gpui.<PascalCase>.Color`（Color）。
* コンポーネントが描画時に計算する色は、生成器の `derived_colors` で GPUI Kit のコードと同じ式で求め、`Gpui.Button.Primary.Hover.Background` のような名前で出力する。
* 生成物には DO NOT EDIT のヘッダーを付けてコミットする。`TokenTests` が、全リソースが参照データと 8bit で完全一致すること、過不足がないことを確かめる。
* 寸法（高さ、余白、角丸、文字サイズ）は値が少ないので `Metrics.axaml` と各テーマに出典付きで手書きする。

### Consequences

* Good, because 色はすべて GPUI Kit と完全に一致する。
* Bad, because 色を足すには Rust 側の `derived_colors` を変えて再生成する必要がある（macOS が必要）。
