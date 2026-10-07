# リリース

NuGet パッケージ（`AvaloniaUIKit`、`AvaloniaUIKit.ColorPicker`、`AvaloniaUIKit.DataGrid`、`AvaloniaUIKit.Dock`）のバージョンの上げ方と公開の手順。決定の理由は ADR 37。

## 構成

| 場所 | 役割 |
| --- | --- |
| `src/Directory.Build.props` の `<Version>` | 4 つのパッケージに共通のバージョン。ColorPicker、DataGrid、Dock のパッケージは、同じバージョン以上の `AvaloniaUIKit` に依存する（`ProjectReference` から pack が作る依存）。最初のリリースまでは `0.0.0`。 |
| `.github/workflows/version-bump.yml` | 手動で実行する。`<Version>` を上げ、`release` ラベルを付けた Pull Request（ブランチ `release/v<version>`）を開く。 |
| `.github/workflows/release.yml` | `release` ラベルの付いた Pull Request が main にマージされたとき、または main で手動で実行したときに、`src/*` の 4 つを pack して nuget.org に push し、GitHub の Release（タグ `v<version>`、.nupkg を添付、リリースノートは自動生成）を作る。 |

## 手順

1. 手元で `scripts/verify.sh` を通す。テストを流す CI はないので、リリースするコミットのテストは手元で確かめる。
2. Actions の「Version Bump」を main で実行する。`release_type`（patch / minor / major）を選ぶか、`version` に `1.2.3` の形で直接書く（`version` を書けば `release_type` は使わない）。最初のリリースは `0.0.0` から minor で `0.1.0` になる。
3. 開いた Pull Request（`🔧 chore: Release v<version>`）の差分（`<Version>` の 1 行）を確かめてマージする。GITHUB_TOKEN で開いた Pull Request が起動するワークフローの実行は承認待ちになるが、`site.yml` は `src/Directory.Build.props` だけの変更では動かないので（ADR 39）、この Pull Request では何も起動しない。ほかのワークフローが承認待ちで出ても、承認せずにマージしてよい（Release はマージで走る）。
4. マージで「Release」が走る。nuget.org では検証とインデックスに数分から数十分かかり、その後にパッケージが検索に出る。

## 約束事

- **バージョンは 1 つ:** 4 つのパッケージを毎回そろって同じバージョンで出す。変更のないパッケージも上がる。
- **1 つのバージョンは 1 つのコミットから:** タグ `v<version>` がすでに別のコミットにあれば、Release は push の前に失敗する。続けるには Version Bump でバージョンを上げる。同じ実行の再実行（push の途中で失敗したときなど）は続けられる。push は `--skip-duplicate` で残りだけを送り、Release がなければ作る。
- **消せない:** nuget.org に出したバージョンは消せない（unlist だけできる）。間違えたら次のバージョンを出す。
- **リリースするコミット:** マージで作られた main のコミット（`merge_commit_sha`）から pack し、タグもそこに付ける。
- **手動の実行:** Release を手で実行すると、main の先頭のバージョンを出す。main 以外のブランチでは何もしない。
- **ワークフローのファイル名:** nuget.org は `release.yml` というファイル名でワークフローを信頼する（下の「初回の設定」）。名前を変えたら nuget.org のポリシーも変える。

## 初回の設定

nuget.org への push には、API キーの代わりに trusted publishing を使う。Release のジョブが GitHub の OIDC トークンを nuget.org に渡し、1 時間だけ有効な API キーを受け取って push する（`NuGet/login`）。

1. nuget.org にログインし、ユーザー名のメニューの「Trusted Publishing」（https://www.nuget.org/account/trustedpublishing ）でポリシーを作る（2026-10-07 に作成済み）。
   - Policy Name: 任意（`avalonia-uikit release` など）
   - Package Owner: `supermomonga`
   - CI/CD Provider: GitHub Actions
   - Repository Owner: `supermomonga`
   - Repository: `avalonia-uikit`
   - Workflow File: `release.yml`（パスを含めない）
   - Environment: 空
   - Scopes: Push の「Push new packages and package versions」。最初のリリースではどのパッケージ ID も nuget.org にないので、「Push only new package versions」では push できない。Unlist / relist は使わないので付けない。
   - Glob Patterns and Packages: `AvaloniaUIKit` と `AvaloniaUIKit.*` の 2 行（このリポジトリの 4 つのパッケージだけに限る）
2. リポジトリのシークレット `NUGET_USER` に nuget.org のユーザー名（プロフィール名。メールアドレスではない）を入れる（`gh secret set NUGET_USER`。2026-10-07 に `supermomonga` を設定済み）。
3. リポジトリの Settings → Actions → General の「Allow GitHub Actions to create and approve pull requests」を有効にする（Version Bump が Pull Request を開くため。2026-10-07 の時点で有効）。
