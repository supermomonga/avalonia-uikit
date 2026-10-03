---
number: 12
title: Run TUnit tests on the Avalonia headless platform with Skia
status: accepted
date: 2026-10-03
---

# Run TUnit tests on the Avalonia headless platform with Skia

## Context and Problem Statement

AGENTS.md はテストに TUnit を使うよう定めている。Avalonia のコントロールは UI スレッドで動かす必要があり、Avalonia の公式のテスト統合（`Avalonia.Headless.XUnit` / `NUnit`）は TUnit に対応していない。比較には、ダミーではない本物の描画結果（Skia）が要る。

## Considered Options

* TUnit の `ITestExecutor` で、各テストを Avalonia のヘッドレスのセッションに載せる
* Avalonia が公式に統合している xUnit / NUnit を使う
* テストのたびに Avalonia のアプリを別プロセスで起動する

## Decision Outcome

Chosen option: "TUnit の `ITestExecutor` で載せる", because AGENTS.md の TUnit の方針を守ったまま、公式のヘッドレスのセッションをそのまま使えるから。

* TUnit の `ITestExecutor` を実装した `AvaloniaHeadlessExecutor` を `[assembly: TestExecutor<AvaloniaHeadlessExecutor>]` で登録する。中で `HeadlessUnitTestSession.StartNew(typeof(TestApp), AvaloniaTestIsolationLevel.PerTest)` を起動し、各テストを `Dispatch` で UI スレッドに載せる。テストごとにアプリを作り直す。
* `[assembly: NotInParallel]` で並列実行しない（UI スレッドとアプリが 1 つのため）。
* テストアプリは `UseSkia().UseHarfBuzz().UseHeadless(UseHeadlessDrawing = false)` で実際に描画する。描画倍率は GPUI と同じ 2。
* 時刻はテストごとに仮想時計に替え、テストが進めた分だけ進める（ADR 14）。Tooltip の表示遅延のようなタイマーも、仮想時計で期限が来たときに動く。
* ケースは `goldens/gpui-2c5162f/manifest.json` から `[MethodDataSource]` で読む。コンポーネントからコントロールの生成と状態の操作への対応は、静的な対応表（`Adapters`）で行う。
* テストは 1 つのプロジェクト（`tests/AvaloniaUIKit.Tests`）にまとめた。当初は高速なテストと描画のテストを分ける計画だったが、どちらも同じヘッドレスのアプリと参照データを使うため、分ける利点が小さかった。

### Consequences

* Good, because TUnit のまま、本物の描画で比べられる。
* Neutral, because 並列には実行できない。仮想時計にしてからは、全 1440 件で約 35 秒かかる。
