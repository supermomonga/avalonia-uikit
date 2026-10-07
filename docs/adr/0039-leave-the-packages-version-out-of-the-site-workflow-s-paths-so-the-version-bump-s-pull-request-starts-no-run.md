---
number: 39
title: Leave the packages' version out of the site workflow's paths so the version bump's pull request starts no run
status: accepted
date: 2026-10-07
links:
- target: 37
  kind: amends
---

# Leave the packages' version out of the site workflow's paths so the version bump's pull request starts no run

## Context and Problem Statement

ADR 37 は、Version Bump が GITHUB_TOKEN で開く Pull Request では「ワークフローが走らず、Pull Request にチェックが付かない」と書いた。実際には違った。今の GitHub では、GITHUB_TOKEN で作った Pull Request の `pull_request`（`opened`、`synchronize`、`reopened`）も実行を作り、その実行は承認待ち（`action_required`）になる。書き込み権限のある人がマージボックスの「Approve workflows to run」を押すまで走らない。ほかのイベントは今も実行を作らない。承認待ちの実行は 30 日で期限が切れ、失敗として残る。

最初のリリースの Pull Request（#20、v0.1.0）では、`site.yml` の PR ビルドが承認待ちになった。差分は `src/Directory.Build.props` の `<Version>` の 1 行で、`site.yml` の `paths` の `src/**` に当たる。承認して走らせても、サイトはバージョンを表示しないので出力は変わらない。main にブランチ保護はないので、承認しなくてもマージはでき、Release（マージした人の `closed` で起動する）も承認なしで走った。

## Decision Drivers

* リリースのたびに意味のない承認待ちを出さない。
* 長期の秘密を増やさない（ADR 37）。
* サイトの出力に効く変更では、これまでどおり `site.yml` を動かす。

## Considered Options

* `site.yml` の `paths` から `src/Directory.Build.props` を外す
* GitHub App のトークンで Pull Request を開く
* そのままにして、承認せずにマージする

## Decision Outcome

Chosen option: "`site.yml` の `paths` から `src/Directory.Build.props` を外す", because Version Bump の Pull Request が何も起動しなくなり、秘密も手順も増えないから。

* push と pull_request の両方の `paths` で、`src/**` のすぐ後に `!src/Directory.Build.props` を置く。後ろにある否定のパターンが前の一致を打ち消す。ルートの `Directory.Build.props` は別のファイルなので、これまでどおり動く。
* `src/Directory.Build.props` はパッケージのバージョンと情報（作者、ライセンス、URL、pack に入れる `LICENSE` と `THIRD-PARTY-NOTICES.md`）だけを持ち、サイトは使わない。ビルドに効く設定を足すときは、この除外を見直す（`docs/site.md`）。
* キャッシュのキーのハッシュ（ADR 32）は変えない。`src/**` は `src/Directory.Build.props` を含んだままなので、バージョンを上げた後の最初のサイトのビルドは .NET の出力を作り直す。
* ADR 37 の「ワークフローが走らず、チェックが付かない」を、「`site.yml` は起動しない。ほかのワークフローが起動すれば承認待ちになり、承認せずにマージしてよい」に改める（`docs/release.md`）。

### Consequences

* Good, because リリースの Pull Request に承認待ちも失敗の跡も残らない。
* Good, because GitHub App も Personal Access Token も要らない。
* Bad, because `src/Directory.Build.props` だけを変える Pull Request では、サイトのビルドを確かめない。今はサイトの出力に効かないファイルだが、効く設定を足すと気づかずに壊しうる。
* Bad, because これから Pull Request ごとに走るワークフローを足すと、リリースの Pull Request ではそれが承認待ちになる。そのときは対象から外すか、GitHub App のトークンに切り替えるかを決め直す。
* Neutral, because リリースのマージの後の main への push でも `site.yml` は動かない。サイトに変わるところはない。

### Confirmation

* `actionlint` が `site.yml` に何も報告しない。
* v0.1.0 の Release（#20 のマージ、実行 37563362201）は、承認なしで全ステップが通った。`pull_request` の `closed` からの OIDC トークンで nuget.org の trusted publishing が 4 つの新しいパッケージ ID を受け付け、タグ `v0.1.0` は main のマージコミットに付いた。ADR 37 で最初のリリースに残した確認は、これで済んだ。
* 次の Version Bump の Pull Request で、承認待ちの表示が出ないことを確かめる。

## Pros and Cons of the Options

### `site.yml` の `paths` から外す

* Good, because 1 行ずつの変更で済み、秘密も手順も増えない。
* Bad, because そのファイルだけの変更でサイトを確かめなくなる。

### GitHub App のトークンで Pull Request を開く

* Good, because Pull Request で CI がふつうに走り、チェックが付く。今後 CI を足しても承認待ちにならない。
* Bad, because App の作成とインストール、App ID と秘密鍵のシークレットが要る。秘密鍵は長期の秘密になる。

### そのままにする

* Good, because 何も変えない。
* Bad, because リリースのたびに承認待ちの表示が出て、承認しなかった実行は 30 日後に失敗として残る。

## More Information

GitHub の挙動は「GITHUB_TOKEN」のドキュメント（https://docs.github.com/en/actions/concepts/security/github_token 、「When GITHUB_TOKEN triggers workflow runs」）による。
