---
number: 27
title: License the project under MIT and ship third-party notices with the packages
status: accepted
date: 2026-10-04
links:
- target: 13
  kind: amends
---

# License the project under MIT and ship third-party notices with the packages

## Context and Problem Statement

リポジトリにはライセンスがなかった。第三者のライセンス文は、Lucide（`src/AvaloniaUIKit/Themes/Icons/LUCIDE-LICENSE.txt`、ADR 13）、Inter（`assets/fonts/inter/OFL.txt`）、shadcnui-hono-jsx（`sites/LICENSE-shadcnui-hono-jsx.txt`）が個別に置かれていただけで、一覧はなかった。NuGet パッケージにはライセンスのメタデータがなく、DLL に埋め込む Lucide の形状のライセンス文も同梱していなかった。

テーマの大部分は GPUI Kit（Apache-2.0、Copyright 2024 - 2026 Longbridge）から派生している。

* コンポーネントの見た目、寸法、動きを Rust から XAML と C# に移植している（`src/`）。
* 色の表（`Palettes.g.cs`）は、GPUI Kit のテーマを GPUI Kit の関数で解決して生成している（ADR 6、ADR 26）。
* `reference/patches/` は GPUI Kit と gpui-pre（Zed の GPUI のスナップショット、Apache-2.0、Copyright 2022 - 2025 Zed Industries, Inc.）への差分で、`goldens/` は GPUI Kit の描画結果。

どちらの上流も、固定した版に NOTICE ファイルを持たない。プロジェクトをどのライセンスで出し、上流の条件をどう満たすかを決める。

## Decision Drivers

* Avalonia（MIT）の利用者が、ふだんどおりの条件で使えること。
* GPUI Kit の Apache-2.0 第 4 条の条件（ライセンス文の添付、帰属表示の保持、変更した旨の明記）と、Lucide の ISC の条件（著作権表示と許諾文の添付）を、ソースでもパッケージでも満たすこと。
* どの第三者の素材が、パッケージに入るのか、リポジトリとサイトだけにあるのかを区別できること。

## Considered Options

* MIT。第三者の素材は `THIRD-PARTY-NOTICES.md` にまとめ、パッケージにも入れる
* Apache-2.0（GPUI Kit と同じ）
* MIT OR Apache-2.0 のデュアルライセンス

## Decision Outcome

Chosen option: "MIT。第三者の素材は `THIRD-PARTY-NOTICES.md` にまとめ、パッケージにも入れる", because Avalonia、shadcn/ui、shadcnui-hono-jsx と同じで .NET の利用者にとって最も一般的なうえ、Apache-2.0 第 4 条は条件を満たせば派生物全体を別の条件で配布することを認めているから。

* **ライセンス:** ルートの `LICENSE` は MIT、著作権者は supermomonga。
* **第三者の通知:** ルートの `THIRD-PARTY-NOTICES.md` に、GPUI Kit、GPUI（gpui-pre）、Lucide、Inter、shadcn/ui と shadcnui-hono-jsx を並べる。それぞれ出典（GPUI Kit は固定した commit）、著作権表示、ライセンス、このリポジトリのどこで使い何を変えたかを書き、パッケージに入るものとリポジトリやサイトだけのものを分ける。Apache-2.0 と Lucide（ISC と、Feather 由来のアイコンの MIT）は全文を載せる。Inter と shadcnui-hono-jsx はパッケージに入らないので、リポジトリ内の既存のファイルを指す。
* **パッケージ:** `src/Directory.Build.props` が、`src/` の全パッケージに `PackageLicenseExpression`（`MIT`）、`Authors`、`Copyright`、`PackageProjectUrl`、`RepositoryUrl` を設定し、`LICENSE` と `THIRD-PARTY-NOTICES.md` をパッケージのルートに入れる。ライセンスの式は dotnet/runtime などと同じく自分のライセンスだけにし、第三者の分は通知ファイルで示す。
* **既存のファイル:** `LUCIDE-LICENSE.txt` はアイコンの隣に残す（ADR 13、生成物のヘッダーが参照する）。全文は `THIRD-PARTY-NOTICES.md` と重複する。
* **サイト:** フッターに MIT License へのリンクを置く。

### Consequences

* Good, because 利用者は MIT と通知ファイルを見れば、パッケージに含まれる第三者の素材とその条件がわかる。
* Good, because 移植した部分の出典と変更点が 1 か所に書かれ、Apache-2.0 の条件を満たしていることを確かめやすい。
* Bad, because GPUI Kit の固定点を更新したら、`THIRD-PARTY-NOTICES.md` の commit と著作権表示の年も直す必要がある。Lucide のライセンス文を更新するときは `LUCIDE-LICENSE.txt` と通知ファイルの両方を直す。
* Bad, because ライセンスの式は `MIT` だけなので、ISC と Apache-2.0 の素材が入っていることは nuget.org の表示からはわからず、通知ファイルを読む必要がある。

### Confirmation

* `dotnet pack` で作った 3 つの nupkg のルートに `LICENSE` と `THIRD-PARTY-NOTICES.md` があり、nuspec に `<license type="expression">MIT</license>` があることを確かめた。
* 上流のライセンスは固定した版で確認した。GPUI Kit は `2c5162f` の `LICENSE-APACHE` と `crates/assets/LICENSE-LUCIDE`（`LUCIDE-LICENSE.txt` と同一）、gpui-pre は crates.io の 0.3.7 の `Cargo.toml` と `LICENSE-APACHE`。どちらにも NOTICE はない。

## Pros and Cons of the Options

### Apache-2.0（GPUI Kit と同じ）

* Good, because 移植した部分と自作の部分の条件が同じになり、特許の許諾も付く。
* Bad, because Avalonia の生態系では少数派で、利用者は NOTICE の扱いなど MIT より多い条件を負う。
* Neutral, because Lucide の ISC の表示は、どちらにしても別に必要。

### MIT OR Apache-2.0 のデュアルライセンス

* Good, because 利用者が条件を選べる。Rust では一般的。
* Bad, because ライセンスのファイルが 2 つになり、.NET では見慣れない。GPUI Kit への帰属表示が要ることは変わらない。

## More Information

* ADR 13（Lucide のライセンス文の置き場所）に、パッケージへの同梱を加える。
* GPUI Kit の固定点は ADR 3。
