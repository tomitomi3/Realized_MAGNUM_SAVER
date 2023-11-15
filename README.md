# Realized MAGNUM SAVER / あの頃夢見た MAGNUM SAVER

## 概要
ミニ四駆の第2次ブーム「爆走兄弟レッツ＆ゴー!!」という漫画があり、その主人公が使用するミニ四駆が「MAGNUM SABER（マグナムセイバー）」です。
漫画ではミニ四駆と主人公が並走しながらゴールを目指します。主人公が**声をかけるとミニ四駆も呼応・反応**します。

そんなミニ四駆を夢見て早何十年。。。なんか**出来そうな気がしてきました**。

<img src="https://github.com/tomitomi3/Realized_MAGNUM_SAVER/blob/main/img/system_risou.JPG" width="600">

### 出展
| [M5 Japan Tour 2023 Spring Kanazawa](https://makezine.jp/event/makers-mft2023/m0215/) | [ミニN駆実行委員会@MFT2023](https://makezine.jp/event/makers-mft2023/m0215/) |
|-|-|
| ![MAGNUM M5 2023 Tour](https://github.com/tomitomi3/Realized_MAGNUM_SAVER/blob/main/img/magnum_1.jpg) | ![MAGNUM MFT2023](https://github.com/tomitomi3/Realized_MAGNUM_SAVER/blob/main/img/magnum_2.jpg) |

## 仕様
* PWMによるDCモーターの回転制御
* デッドマンスイッチとして一定時間毎にモーター回転を原則
* 声に応じてマシンが呼応
  * 「爆走兄弟レッツ&ゴー!! 1」で主人公が発した単語でモーターの回転制御を行う

## システム構成

IoTなミニ四駆を実現する構成とする。音声認識を行う部分と無線で通信を行いモータ制御を行う構成とする。

<img src="https://github.com/tomitomi3/Realized_MAGNUM_SAVER/blob/main/img/system_block.JPG" width="700">

### ハードウェア構成

<img src="https://github.com/tomitomi3/Realized_MAGNUM_SAVER/blob/main/img/connection.PNG" width="500">

* 部品

| 部品 | 備考 |
|-|-|
| M5StickC | - |
| MOSFET | [ＮｃｈパワーＭＯＳＦＥＴ　６０Ｖ５Ａ　２ＳＫ４０１７](https://akizukidenshi.com/catalog/g/gI-07597/) |
| R | 抵抗 10kΩ～100kΩ |
| C | 電解コンデンサ |
| ダイオード | 抵抗 10kΩ～100kΩ |
| LiPO | リチウムイオン電池 |
| LiPO電源管理 | 電源管理IC モジュール |

### ソフトウェア構成

音声認識とデータ送信のアプリケーションと送信されたデータを受信してPWM制御を行うM5StickC側のファームからなる。

* [VoiceCtrlApp](https://github.com/tomitomi3/Realized_MAGNUM_SAVER/tree/main/VoiceCtrlApp)
  * C#で作成。
  * 音声認識エンジンにより2種類。MS標準の[SpeechRecognition](https://learn.microsoft.com/ja-jp/windows/apps/design/input/speech-recognition)と[Vosk](https://alphacephei.com/vosk/)を使用。
  * UDPプロトコロルによりM5StickCにデータを1バイト送信する

* [MiniNWDCtrl](https://github.com/tomitomi3/Realized_MAGNUM_SAVER/tree/main/MiniNWDCtrl)
  * 受信したデータに基づきG26をPWM制御

#### 単語

下記の単語を抜粋してソフトに登録しています。下記は爆走兄弟レッツ&ゴー!! 1巻から収集しています。

* セイバーゴー
* ひゃっほー
* いっけー
* 直線なら俺のもんだぜ
* かっとべマグナム
* しっかりしろマグナム
* マグナムゴー
* がんばれマグナム
* いけぇマグナム

#### 指定IPアドレスに指定文字列をUDPで送信するスクリプト

* [sendUDPpacket.ps1](https://github.com/tomitomi3/Realized_MAGNUM_SAVER/tree/main/testscript)

コマンドプロンプトで下記powershellスクリプトを実行する。

```
> powershell .\sendUDPpacket.ps1 IPアドレス 文字
```

```
> powershell .\sendUDPpacket.ps1 192.168.1.1 1
```

## 今後
* 加速度・角速度センサーを用いたコーナリング時の加減速
* MCU、モーター制御部をコンパクト

## 参考文献
1. こした てつひろ. "爆走兄弟レッツ&ゴー!! 1". 第13刷, 小学館, 1996.
