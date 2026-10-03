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
* 実時間を待つテストは、`DispatcherFrame` でディスパッチャーを回しながら待つ（`CaseHost.Pump`）。タイマーで動く Tooltip の表示やスクロールバーの自動非表示も実際に動く。
* ケースは `goldens/gpui-2c5162f/manifest.json` から `[MethodDataSource]` で読む。コンポーネントからコントロールの生成と状態の操作への対応は、静的な対応表（`Adapters`）で行う。
* テストは 1 つのプロジェクト（`tests/AvaloniaUIKit.Tests`）にまとめた。当初は高速なテストと描画のテストを分ける計画だったが、どちらも同じヘッドレスのアプリと参照データを使うため、分ける利点が小さかった。

### Consequences

* Good, because TUnit のまま、本物の描画で比べられる。
* Bad, because 並列に実行できないので、全 1461 件の実行に約 10 分かかる。
