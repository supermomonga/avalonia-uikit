---
number: 22
title: Model the docs site on gpui-kit.com and render its social image from a site page
status: accepted
date: 2026-10-04
links:
- target: 20
  kind: amends
- target: 36
  kind: amendedby
---

# Model the docs site on gpui-kit.com and render its social image from a site page

## Context and Problem Statement

ADR 20 で作ったドキュメントサイトは、道具立てを shadcn-hono.omofla.sh から借りたときに、見た目と構成まで ui.shadcn.com に寄っていた（中央寄せのヒーロー、カードの一覧、Geist、`/docs/components/*` の URL）。意図していたのは、部品に shadcnui-hono-jsx を使いながら、構成と見た目を GPUI Kit の公式サイト（gpui-kit.com、`longbridge/gpui-kit` の `website/`）にそろえることだった。あわせて、OG 画像（`og.png`）は `samples/AvaloniaUIKit.Previews --og` が `OgImage.axaml` から CI で描いていたため、コミット済みのものは古い名前のまま残り、サイトのデザインとも別物だった。サイトの構成と見た目をどこに合わせるか、OG 画像とアイコンを何から描くかを決める。

## Decision Drivers

* ライブラリの見た目は GPUI Kit に合わせて検証している。ドキュメントサイトも同じ造形言語で見せたい。
* 部品は shadcnui-hono-jsx のまま使い、サイト専用の部品を増やしすぎない。
* OG 画像はサイトのデザイン（ロゴ、書体、グリッド、ウィンドウの枠）と一致させたい。
* リンクのプレビューに出る画像を、デプロイのたびに描き直さずに確認できる状態で管理したい（shadcn-hono.omofla.sh と同じ運用）。

## Considered Options

* 構成と見た目を gpui-kit.com にそろえ、OG 画像はサイトの開発用ページを Playwright で撮ってコミットする
* 構成と見た目を gpui-kit.com にそろえ、OG 画像は `OgImage.axaml` を描き直して CI で描く
* 構成と見た目を ui.shadcn.com 寄りのまま直す

## Decision Outcome

Chosen option: "構成と見た目を gpui-kit.com にそろえ、OG 画像はサイトの開発用ページを Playwright で撮ってコミットする", because サイトの見た目と OG 画像が同じ CSS とロゴから作られ、食い違わなくなるから。

* **構成:** トップ、ガイド（`/docs`）、コンポーネント（`/components`）の 3 つ。ガイドとコンポーネントはそれぞれ自分のサイドバーを持つ。旧 URL の `/docs/components/*` は `sites/public/_redirects` で 301 転送する。
* **見た目:** shadcnui-hono-jsx の CSS 変数を GPUI Kit の neutral の hex で上書きし、GPUI Kit のトークン（`--brand`、`--data-*`、`--code-*`、グリッド線、影、角丸）を足す（`sites/app/style.css`）。サイトの書体はシステムフォント、コードの色は CSS 変数を参照する Shiki のテーマ。トップの帯、ドキュメントの 3 カラム、目次、検索とテーマのパレットも gpui-kit.com の配置に合わせる。
* **OG 画像とアイコン:** `sites/app/routes/og-image.tsx`（`disableSSG()` でビルドしない）を `sites/scripts/images.ts` が Playwright の Chromium で 1200×630、2 倍の解像度で撮り、`sites/public/og.png` を書く。アイコンは同じスクリプトが `sites/public/logo.svg` から描く。生成物はコミットし、CI は描かない。`OgImage.axaml` と Previews の `--og` は削除する。ADR 20 のうち「Previews が OG 画像を描く」の部分だけをこの ADR で置き換える。
* **ロゴ:** GPUI Kit のロゴ（直角だけで組んだ G に青いバー）と同じ造形で、直角だけで組んだ A のクロスバーをテーマの青にしたもの。

### Consequences

* Good, because サイトの見た目と OG 画像が同じスタイルシートとロゴから作られ、片方だけ古くなることがない。
* Good, because OG 画像の変更がコミットの差分として見え、デプロイ前に確認できる。CI に Chromium が要らない。
* Good, because ライブラリが合わせている GPUI Kit と同じ造形でドキュメントを見せられる。
* Bad, because OG 画像はデモのプレビューを使うので、描き直すにはプレビューを先に描き、Playwright の Chromium を用意する必要がある。コントロールの見た目が変わっても、描き直すまで OG 画像は古いまま残る。
* Bad, because URL が変わったので、外部からの旧 URL へのリンクはリダイレクトに頼る。
* Neutral, because OG 画像の書体は撮った環境のシステムフォントになる（macOS で撮れば SF Pro）。

### Confirmation

* `sites/scripts/smoke.ts` が、`og.png`、ロゴ、アイコン、`_redirects` があること、リダイレクト先のページがあること、`og-image.html` がビルドされていないことを確かめる。CI はサイトのビルドのたびにこれを実行する。
* 描き直しの手順は `docs/site.md` の「OG 画像とアイコン」にある。

## Pros and Cons of the Options

### 構成と見た目を gpui-kit.com にそろえ、OG 画像はサイトの開発用ページを Playwright で撮ってコミットする

* Good, because OG 画像にサイトと同じグリッド、ウィンドウの枠、ロゴ、書体を使える。
* Good, because shadcn-hono.omofla.sh と同じ運用で、手順を共有できる。
* Bad, because 描き直しは手作業で、忘れると古くなる。

### 構成と見た目を gpui-kit.com にそろえ、OG 画像は `OgImage.axaml` を描き直して CI で描く

* Good, because デプロイのたびに描くので、コントロールの見た目の変更に自動で追従する。
* Bad, because サイトのデザインを XAML で二重に書くことになり、サイトの CSS と食い違いやすい。
* Bad, because コミット済みの画像と配信される画像が別物になり、手元で確認しにくい。

### 構成と見た目を ui.shadcn.com 寄りのまま直す

* Good, because 変更が最も小さい。
* Bad, because ライブラリが合わせている GPUI Kit とサイトの見た目が一致せず、意図した構成にならない。

## More Information

* 構成と約束事: `docs/site.md`
* 参照したサイト: https://gpui-kit.com （`longbridge/gpui-kit` の `website/`）
* OG 画像の方式の先例: shadcn-hono.omofla.sh（`supermomonga/shadcnui-hono-jsx` の `site/scripts/images.ts`）
