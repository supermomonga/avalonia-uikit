---
number: 37
title: Release the packages under one version from a merged release pull request through trusted publishing
status: accepted
date: 2026-10-07
links:
- target: 39
  kind: amendedby
---

# Release the packages under one version from a merged release pull request through trusted publishing

## Context and Problem Statement

このリポジトリは 4 つの NuGet パッケージ（`AvaloniaUIKit`、`AvaloniaUIKit.ColorPicker`、`AvaloniaUIKit.DataGrid`、`AvaloniaUIKit.Dock`。ADR 16、ADR 28）を持つが、まだ 1 度も公開していない。バージョンはどこにも書かれておらず、pack すると SDK の既定の 1.0.0 になる。公開の手順も、それを行うワークフローもなかった。

3 つのパッケージは `ProjectReference` で `AvaloniaUIKit` を参照し、pack はそれを「同じバージョン以上の `AvaloniaUIKit`」への依存にする。ColorPicker、DataGrid、Dock のテーマは `AvaloniaUIKit` のリソースのキーとトークンを `DynamicResource` で使う（ADR 16、ADR 21）ので、組み合わせを確かめていないバージョンどうしが混ざると見た目が崩れうる。

作者の別のリポジトリ（supermomonga/mmql）は、`workflow_dispatch` でバージョンを上げる Pull Request を開き、`release` ラベルの付いたその Pull Request のマージで NuGet に公開している。認証は nuget.org の API キーをシークレットに置く。

## Decision Drivers

* 4 つのパッケージを、確かめた組み合わせのまま出す。
* 出すときの操作を少なくし、バージョンの変更を Pull Request として残す。
* 期限の切れる長期の秘密を持たない。
* 一度出したバージョンは nuget.org から消せないので、同じバージョンを別の中身で出さない。

## Considered Options

* バージョン: 4 つで 1 つのバージョン
* バージョン: パッケージごとのバージョン
* 起点: `release` ラベルの Pull Request のマージ（と main での手動実行）
* 起点: タグの push
* 起点: main への push で `<Version>` が変わったこと
* 認証: nuget.org の trusted publishing
* 認証: nuget.org の API キー

## Decision Outcome

Chosen option: 「4 つで 1 つのバージョン」「`release` ラベルの Pull Request のマージ」「trusted publishing」, because 組み合わせを確かめたバージョンだけが並び、作者の別のリポジトリと同じ操作で出せて、長期の秘密を持たずに済むから。

* **バージョン:** `src/Directory.Build.props` の `<Version>` に 1 つだけ書く。パッケージの情報（作者、ライセンス、URL）と同じ場所に置き、samples と tests には関係させない。`FileVersion` と `AssemblyVersion` は SDK が `<Version>` から作るので書かない。最初のリリースまでは `0.0.0` にしておき、最初の Version Bump で選ぶ（minor なら 0.1.0）。
* **Version Bump（`.github/workflows/version-bump.yml`）:** 手動で実行し、`supermomonga/action-bump-cli`（mattn/bump）で `<Version>` を上げて、`release` ラベルを付けた Pull Request を `peter-evans/create-pull-request` で開く。種類（patch / minor / major）か、直接のバージョンを選ぶ。
* **Release（`.github/workflows/release.yml`）:** `release` ラベルの Pull Request が main にマージされたとき、または main で手動で実行したときに走る。マージで main にできたコミット（`merge_commit_sha`）を checkout し、`src/*/*.csproj` を 1 つずつ pack して、nuget.org に push し、そのコミットにタグ `v<version>` を付けた GitHub の Release（.nupkg を添付、リリースノートは自動生成）を作る。ソリューション全体を pack しないのは、ブラウザーのアプリ（`net10.0-browser`）など、WebAssembly のワークロードが要るプロジェクトまでビルドすることになるからである。
* **1 つのバージョンは 1 つのコミットから:** タグ `v<version>` が別のコミットにあれば、push の前に失敗させる。同じ実行の再実行は続けられるようにする（push は `--skip-duplicate`、Release はなければ作る）。mmql は同じバージョンのタグと Release を消して作り直すが、nuget.org のパッケージは作り直せないので、タグだけが別のコミットに移ってしまう。
* **trusted publishing:** Release のジョブが `id-token: write` で GitHub の OIDC トークンを取り、`NuGet/login` で 1 時間だけ有効な API キーに替えて push する。nuget.org のポリシーは、リポジトリ（`supermomonga/avalonia-uikit`）とワークフローのファイル名（`release.yml`）で信頼する。nuget.org のユーザー名はシークレット `NUGET_USER` に置く。
* **テスト:** Release はテストを流さない。テストを流す CI はなく、手元で `scripts/verify.sh` を通したコミットからリリースする（`docs/release.md`）。

### Consequences

* Good, because 4 つのパッケージがいつも同じバージョンでそろい、利用者は 1 つの番号で組み合わせを選べる。
* Good, because バージョンの変更が Pull Request として残り、マージ 1 回で公開まで進む。
* Good, because 期限の切れる API キーを作ってシークレットに置き、更新する必要がない。
* Bad, because 変更のないパッケージも毎回新しいバージョンで出る。
* Bad, because Version Bump が GITHUB_TOKEN で開いた Pull Request ではワークフローが走らず、Pull Request にチェックが付かない。差分は `<Version>` の 1 行だけで、マージ後の main への push では `site.yml` がふつうに走る。
* Bad, because ワークフローのファイル名を変えると、nuget.org のポリシーを変えるまで公開できなくなる。
* Bad, because テストを確かめるのが手元の手順に任される。

### Confirmation

* 4 つのプロジェクトを `-p:Version=0.0.1 -p:ContinuousIntegrationBuild=true` で pack した。`TreatWarningsAsErrors` のまま警告はなく、どのパッケージも 0.0.1 になった。ColorPicker、DataGrid、Dock の nuspec は `AvaloniaUIKit` 0.0.1 への依存を持ち、4 つとも `LICENSE`、`THIRD-PARTY-NOTICES.md`、XML ドキュメント、リポジトリの URL とコミットを含んだ。
* `dotnet msbuild <project> -getProperty:Version` は restore なしで 4 つとも `0.0.0` を返した。Release はこれでバージョンを読む。
* mattn/bump に Version Bump と同じパターンを渡すと、patch / minor / major / set のどれでも `src/Directory.Build.props` の `<Version>` の行だけが変わった。
* `actionlint`（shellcheck を含む）が 2 つのワークフローに何も報告しない。
* mmql で Pull Request のマージから作られた直近 3 つのタグ（v0.2.2、v0.2.3、v0.3.0）は、どれも main のマージコミットを指していた。同じイベントで走る Release も main のコミットにタグを付ける。
* GitHub の上での実行（Pull Request のマージで走ること、`pull_request` のイベントからの OIDC トークンを nuget.org が受け付けること、まだ存在しないパッケージ ID への最初の push。ポリシーのスコープ「Push new packages and package versions」で許す）は、最初のリリースで確かめる。

## Pros and Cons of the Options

### バージョン: 4 つで 1 つ

* Good, because 確かめた組み合わせが 1 つの番号で決まり、バージョンを上げる場所も 1 つで済む。
* Bad, because 変更のないパッケージも上がる。

### バージョン: パッケージごと

* Good, because 変更のあったパッケージだけが上がる。
* Bad, because どのバージョンどうしを組み合わせてよいかを別に管理する必要があり、バージョンを上げる手順もパッケージの数だけ要る。

### 起点: `release` ラベルの Pull Request のマージ

* Good, because バージョンの変更をレビューしてから出せて、作者の別のリポジトリと同じ操作で済む。
* Bad, because ラベルを付けずにマージした Pull Request では出ない（main で手動で実行して出す）。

### 起点: タグの push

* Good, because GitHub Actions でよく使われる形で、タグのコミットがそのまま出すコミットになる。
* Bad, because `<Version>` とタグを別々に合わせる必要があり、どちらかを忘れると食い違う。

### 起点: main への push で `<Version>` が変わったこと

* Good, because ラベルに頼らず、`<Version>` を変えたコミットが main に入れば出る。
* Bad, because 前のコミットとの比較が要り、`<Version>` を変える Pull Request がそのまま公開になることがわかりにくい。

### 認証: trusted publishing

* Good, because 長期の秘密がなく、漏れても 1 時間で失効する。
* Bad, because nuget.org のポリシーがワークフローのファイル名に結びつく。

### 認証: API キー

* Good, because 作者の別のリポジトリと同じで、ワークフローが単純。
* Bad, because キーに期限があり、切れるたびに作り直してシークレットを替える必要がある。漏れれば期限まで使われる。

## More Information

手順、約束事、nuget.org のポリシーとシークレットの初回の設定は `docs/release.md` にまとめる。
