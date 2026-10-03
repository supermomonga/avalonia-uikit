---
number: 3
title: Pin GPUI Kit 2c5162f and Avalonia 12.1.3 as the porting baseline
status: accepted
date: 2026-10-03
---

# Pin GPUI Kit 2c5162f and Avalonia 12.1.3 as the porting baseline

## Context and Problem Statement

GPUI Kit も Avalonia も更新が速い。「GPUI Kit と一致している」と言うには、どの版の GPUI Kit と、どの版の Avalonia で比べたかを固定しなければならない。AGENTS.md は C#、.NET SDK、Avalonia の最新版を対象にするよう求めている。

## Considered Options

* 移植開始時点の最新に固定し、更新は手順を決めて行う
* 対応表の調査時点（gpui-kit 32030ed、Avalonia 12.1.2）に固定する
* 固定せず、常に最新と比べる

## Decision Outcome

Chosen option: "移植開始時点の最新に固定する", because 参照データ（ゴールデン）は特定の版の GPUI Kit から作るしかなく、最新を基準にすることは AGENTS.md の方針とも合うから。

| 対象 | 固定点 |
| --- | --- |
| GPUI Kit | `2c5162f8c5b0c7fcec066ed53125d304c632bfe2`（gpui-pre 0.3.7） |
| Avalonia | 12.1.3（`Directory.Packages.props` の `AvaloniaVersion`） |
| .NET SDK | 10.0.301（`global.json`、`latestFeature` で繰り上げ） |
| C# | `LangVersion=latest` |
| TUnit | 1.72.16 |

固定点は `reference/scripts/vendor.sh` と生成器の `GPUI_KIT_REV`、ゴールデンのディレクトリ名 `goldens/gpui-2c5162f/`、生成物のヘッダー、`docs/references/compatibility-list.md` に記録する。

### 更新の手順

1. `vendor.sh` と生成器の `GPUI_KIT_REV` を変え、時計パッチ（ADR 4）が当たることを確かめる。
2. `scripts/generate-goldens.sh` で新しいディレクトリ（`goldens/gpui-<短縮 SHA>/`）に生成し、テストの `Repo.Goldens` を切り替える。古いディレクトリは削除する。
3. `scripts/verify.sh` で差分を調べ、テーマを直す。許容値は変えない。変える場合は ADR 10 の手順に従う。
4. Avalonia の更新は `AvaloniaVersion` を変えて全テストと `scripts/aot-smoke.sh` を流す。
5. 対応表の固定点を更新する。

### Consequences

* Good, because どの版どうしで一致を確かめたかが明確になる。
* Bad, because GPUI Kit の更新に追従するたびに、ゴールデンの再生成と差分の確認が必要になる。
