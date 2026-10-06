---
number: 34
title: Let the dragged tab draw the selected tab's indicator over the other tabs
status: accepted
date: 2026-10-06
links:
- target: 33
  kind: amends
---

# Let the dragged tab draw the selected tab's indicator over the other tabs

## Context and Problem Statement

ADR 33 で、`uikit:Tabs.Reorderable` のタブをドラッグする間、インジケーター（pill、segmented、underline で選択中のタブを示す `PART_Indicator`）は、ばねを使わずにドラッグ中のタブに付いていくことにした。

pill のバーでタブをドラッグすると、ドラッグ中のタブの背景（インジケーター）の上に、通り過ぎるほかのタブの文字が描かれた。インジケーターはタブの行（`PART_TabsRow`）の下の別の層（`PART_IndicatorLayer`）にあるので、ほかのタブの文字より下に描かれる。ドラッグ中のタブの `ZIndex` を上げても、効くのは同じパネルのタブの間だけで、別の層のインジケーターには効かない。segmented でも同じことが起きる。

outline と underline のタブは背景を描かないので、ドラッグ中のタブの文字と、通り過ぎるタブの文字が重なって見えた。

また、タブを途中で離して元の位置に戻ると、インジケーターだけが離した位置に残った。タブはレンダートランスフォームで動かしていて、元の位置に戻してもレイアウトが走らない。インジケーターはレイアウトの後（`LayoutUpdated`）にしか置き直さないので、離した位置のままになる。pill、segmented、underline のどれでも起きる。

## Decision Drivers

* ドラッグ中のタブは、選択中のタブの見た目のまま、ほかのタブより上に見え、通り過ぎるタブの文字と重ならないこと。
* 離したときは、並び順が変わったかどうかに関係なく、インジケーターがすぐにタブの位置へ戻ること。
* ドラッグしていないときの見た目（GPUI Kit と比べるケース）を変えないこと。

## Considered Options

* ドラッグ中のタブに `:dragging` 疑似クラスを付けてインジケーターを描かせ、バーのインジケーターはドラッグの間隠す
* ドラッグ中のタブにインジケーターを描かせ、バーのインジケーターもドラッグ中のタブに付いていくままにする
* ドラッグの間、インジケーターの層をタブの行より上に置く

## Decision Outcome

Chosen option: "ドラッグ中のタブに `:dragging` 疑似クラスを付けてインジケーターを描かせ、バーのインジケーターはドラッグの間隠す", because タブ自身が描くものだけがほかのタブより上に出せ、同じものを 2 回描かないから。

* **疑似クラス:** ドラッグ中（ポインターに付いていく間）のタブに `:dragging` を付け、離したとき、別のバーへ移ったとき、ウィンドウへ切り離したときに外す。付け外しは `TabDragSession` が `ZIndex` と一緒に行う。
* **テーマ:** `scripts/gen_tabs_theme.py` が、インジケーターの塗り（`PART_IndicatorFill`）の定義を 1 か所に持ち、そこから `:dragging` のタブのスタイルも作る。塗りのブラシはタブの `Background`、それ以外（高さ、角の丸み、影、縦の位置）はタブの `PART_Background` に付ける。`PART_Background` の `Background` はテンプレートの TemplateBinding なので、スタイルでは上書きできないため。インジケーターのない TabControl の縦置き（`TabStripPlacement` が Left / Right）には付けない。
* **背景のないタブ:** outline と underline のドラッグ中のタブは、`UIKit.TabActive` の背景を持つ。GPUI Kit の Dock がドラッグ中のタブを描くプレビュー（`DragPanelPreview`）と同じ色で、既定の Default Light / Dark では `UIKit.Background` と同じ色になる。underline のタブは文字だけで横の余白がないので、背景をタブの間隔の半分だけ左右に広げ、見えている隣のタブの文字と離す。下は下枠（2px）の手前で止め、バーのベースラインを残す。underline のインジケーターの線は `PART_Background` を背景に使うため、タブ自身の下枠（GPUI の underline のタブが持つ 2px の `border_b`、ふだんは透明）で描く。親をたどって実際の背景色を拾う方法は、グラデーションや画像、半透明の層に対応できないので採らない。
* **インジケーター:** `Tabs.Indicator` は、バーのタブがドラッグされている間は隠れる。ドロップでタブを元の位置に戻した直後（並び順を変える前）に、ばねを使わずにそのタブの位置へ置き直す。並び順が変わったときは、そのあとのレイアウトでもう一度置き直す。
* **ADR 33 との関係:** ADR 33 の「インジケーターはドラッグ中のタブにばねなしで付いていく」を、この決定で置き換える。ほかの部分は ADR 33 のまま。

### Consequences

* Good, because どの variant でも、ドラッグ中のタブが通り過ぎるタブの文字を隠す。pill と segmented はインジケーターと同じ定義から作るので、ドラッグ中の見た目は止まっているときと同じになる。
* Good, because 途中で離しても、インジケーターがタブの位置へ戻る。
* Bad, because `Tabs.Indicator` を使う独自のテンプレートは、`:dragging` のスタイルを足さないと、ドラッグ中のタブに選択の見た目がなくなる。`Tabs.Indicator` と `Tabs.Reorderable` の説明に書く。
* Bad, because `UIKit.TabActive` と違う色の面（グレーのカードなど）に置いた outline と underline のバーでは、ドラッグの間だけ、ドラッグ中のタブの背景が四角く見える。

### Confirmation

* `TabsEditingBehaviorTests` で、pill と segmented（TabStrip と TabControl）のドラッグ中のタブが `:dragging` を持ち、ほかのタブより上にあり、`PART_Background` がインジケーターの塗りと同じ位置、大きさ、色、角の丸み、影を持つこと、インジケーターが隠れることを確かめる。
* 同じテストで、outline と underline のドラッグ中のタブが `UIKit.TabActive` の背景を持つこと（underline は間隔の半分まで広げ、下枠の手前で止めること）、underline のドラッグ中のタブがインジケーターと同じ位置、太さ、色の線を下枠で描くことを確かめる。
* 同じテストで、元の位置に離したときと別の位置に離したときの両方で、インジケーターがすぐにタブの位置にあることを確かめる。
* 止まっているときの見た目は、今までどおり GPUI Kit との比較（`*_matches_gpui`）で確かめる。

## Pros and Cons of the Options

### ドラッグ中のタブに `:dragging` 疑似クラスを付けてインジケーターを描かせ、バーのインジケーターはドラッグの間隠す

* Good, because 選択の見た目を描くものがいつも 1 つで、segmented の影も重ならない。
* Good, because 見た目はテーマのスタイルに置くので、アプリのテーマも `:dragging` で変えられる。
* Bad, because インジケーターの塗りの定義が、バーの部品とタブのスタイルの 2 か所に出る（生成スクリプトでは 1 か所にまとめる）。

### ドラッグ中のタブにインジケーターを描かせ、バーのインジケーターもドラッグ中のタブに付いていくままにする

* Good, because `:dragging` のスタイルのない独自のテンプレートでも、ADR 33 の見た目（下に描かれるインジケーター）が残る。
* Bad, because 同じものを 2 回描く。segmented の影は 2 回重なって濃くなる。

### ドラッグの間、インジケーターの層をタブの行より上に置く

* Good, because テーマのスタイルを足さずに済む。
* Bad, because インジケーターがドラッグ中のタブ自身の文字も覆う。層とタブは親が違うので、ドラッグ中のタブだけをインジケーターより上に置けない。

## More Information

* タブのドラッグは ADR 33。インジケーターの Behavior（`Tabs.Indicator`）は `docs/references/compatibility-list.md` の Tabs の行にある。
