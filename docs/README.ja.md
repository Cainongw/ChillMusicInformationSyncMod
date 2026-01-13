# Chill Music Information Sync（音楽情報同期）

[简体中文](../README.md) | [English](README.en.md) | [日本語](README.ja.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework 4.7.2](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net472)
[![BepInEx](https://img.shields.io/badge/BepInEx-Plugin-green.svg)](https://github.com/BepInEx/BepInEx)

ゲーム「*Chill with You*」用のBepInExプラグインで、Windows メディア コントロール API (SMTC) との双方向同期を実現します

---

[![Chill with You](../imgs/header_schinese.jpg)](https://store.steampowered.com/app/3548580/)

> 「Chill with You」は、ストーリーを書くのが好きな女の子・聡音と一緒に仕事をするビジュアルノベルゲームです。アーティストのオリジナル音楽、環境音、風景をカスタマイズして、集中できる作業環境を作ることができます。聡音との関係が深まるにつれて、彼女との特別なつながりを発見するかもしれません。

---

## 実際の使用効果：

![alt text](../imgs/image.png)

## ⚠️ 注意 ⚠️

SMTC を使用するには、音楽プレイヤーがそれをサポートしている必要があり、すべてのプレイヤーがすべての SMTC 機能をサポートしているわけではありません。

たとえば、Apple Music デスクトップ アプリはプログレスバーの双方向同期をサポートしていません。

SMTC をサポートする一般的な音楽プレイヤーは次のとおりです：

- Spotify
- Groove Music
- YouTube (Chrome)
- Bilibili (Chrome)
- Apple Music（ゲーム内でのプログレスバードラッグをサポートしません）
- KuGou Music（プログレスバーの問題）
- NetEase Cloud Music with [BetterNCM](https://github.com/std-microblock/chromatic/tree/v2)

## 主な機能

### プレイヤー側

- 曲名同期
- アーティスト同期
- アルバム カバー同期
- 再生ステータス同期
- プログレスバーの双方向同期

### ゲーム側

- 前の曲
- 次の曲
- 一時停止/再生
- プログレスバー ドラッグ

## 使用方法

Unity Mono ランタイムの制限により、Windows API を直接呼び出すことはできません。このプロジェクトは [SMTC-Bridge-Cpp](https://github.com/Cainongw/SMTC-Bridge-Cpp) を使用してネイティブ レイヤーで操作します

Release には既に含まれているので、自分でコンパイルする必要がない場合は、上記を無視してください。

---

### **要件：**

- Windows 10 1607 以降
- ゲーム本体
- [BepInEx 5.4.23.4](https://github.com/BepInEx/BepInEx/releases) ([RealTimeWeatherMod](https://github.com/Small-tailqwq/RealTimeWeatherMod) と同じ)

### インストール手順

1. BepInEx フレームワークが正しくインストールされていることを確認します
2. ゲームを 1 回起動します
3. Release のすべての内容を BepInEx\plugins に抽出します
4. ゲームを起動します

## シャッフル/リピートについて

音楽プレイヤーはシャッフルとリピート コントロールにあまりよくサポートしていません。

SMTC ライブラリにはシャッフルとリピートのコントロールが含まれていますが、テスト後、プレイヤーがこれらを実際にサポートしていません。

さらに、各プレイヤーはロジックを異なる方法で実装します。シャッフル/リスト/単一リピートを組み合わせるプレイヤーもあれば、シャッフルとリピートを分離するプレイヤーもあります。

これを実装する最良の方法は、キーボード ショートカットをシミュレートすることです。

したがって、現在のところサポートする予定はありません。現在の計画は、再生中にゲームのこれら 2 つの UI ボタンを無効にして非表示にすることです。

## 注釈

- Mod がゲーム UI を引き継いだ後、ゲームに組み込まれた曲を再生したい場合は、プレイリスト ボタン内の曲をクリックするだけです。

## 開発とビルド

1. リポジトリをローカルにクローンします
2. ゲームから `Assembly-CSharp.dll` を ./libs にコピーします
3. Visual Studio で開くか、dotnet build を実行します

## Todo リスト

- [x] 曲のカバー同期を追加する
- [x] 再生中にゲームのループ/シャッフル ボタンを非表示にする
- [x] カバー/ボタンの表示と非表示にアニメーションを追加する

## 他の Mod について

このゲームの他の Mod に興味がある場合は、[awesome-chillwithyou](https://github.com/clsty/awesome-chillwithyou) を参照してください
