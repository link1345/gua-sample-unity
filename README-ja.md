# Gua UI Lab for Unity

これは [Gua](https://github.com/link1345/gua) のUnity版サンプルプロジェクトです。
[`gua-sample-godot`](https://github.com/link1345/gua-sample-godot) と同じ画像素材、画面構成、操作フローをUnity 6のuGUIで再現しています。

`Start`で2画面目へ移動し、6秒間の`Loading....`表示中は`Back`が無効になります。Loading終了後は`Back`で1画面目へ戻れます。`End`では終了確認が表示され、`Cancel`で戻り、`OK`で終了します。

ウィンドウはリサイズ可能です。541×857のデザイン比率を維持して一様に拡大・縮小し、画面比率から余る領域は黒いレターボックスとして表示します。

UI自動化にはGua v0.15.0のUnity UPMパッケージを使用しています。実行中は既定で `ws://127.0.0.1:8765` にGua bridgeを開き、標準uGUIツリーを自動的に公開します。ポートは環境変数 `GUA_BRIDGE_PORT` で変更できます。

## 実行

1. Unity Hubでこのディレクトリを開きます。
2. Unity `6000.5.3f1`で `Assets/Scenes/Main.unity` を開きます。
3. Playを押します。

Guaの管理DLLとWindows x64ネイティブDLLを含む公式UPMパッケージ v0.15.0は `Packages/com.link1345.gua` に固定しています。そのため、隣接するGuaソースリポジトリへの参照や別途のパッケージダウンロードは不要です。

## UIテスト

Windows x64 Mono Playerをビルドしてから、NUnitで実際のPlayerとSemantic UI Treeを操作します。

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath $PWD `
  -buildWindows64Player "$PWD\Builds\GuaUiLab.exe" `
  -logFile "$PWD\unity-build.log"
dotnet test tests\GuaUiLab.Unity.Tests.csproj
```

Playerを別の場所へ出力した場合は、`GUA_UNITY_PLAYER`にexeの絶対パスを指定します。失敗時のGua UI Tree、ログ、Unity Playerログ、スクリーンショットなどは `artifacts/gua` に保存されます。

### Visual / Recording検証

`Gua.Testing.Visual`で初期画面をレビュー済みPNGと比較し、画像欠落、配置ずれ、意図しないオーバーレイを検出します。基準画像を意図的に更新する場合だけ、差分を確認した上で次を実行します。

```powershell
$env:GUA_UPDATE_BASELINES = "1"
dotnet test tests\GuaUiLab.Unity.Tests.csproj --filter TitleScreenMatchesReviewedVisualBaseline
Remove-Item Env:GUA_UPDATE_BASELINES
```

`Gua.Testing.Recording`では`End` → `Cancel`のセマンティック操作を実際に記録し、JSONへの保存・再読込・同一Unity Playerセッションへの再生まで検証します。成功時の記録JSONはテスト出力ディレクトリの`artifacts/gua/recordings`に保存されます。

Visual Report用の差分成果物をローカル生成する場合は、次を実行します。

```powershell
$env:GUA_VISUAL_REPORT_DEMO = "1"
dotnet test tests\GuaUiLab.Unity.Tests.csproj --filter VisualReportViewerDemoProducesPixelDifferenceArtifact
Remove-Item Env:GUA_VISUAL_REPORT_DEMO
```

## GitHub Actions

`.github/workflows/gua-tests.yml`で[`link1345/gua-tester`](https://github.com/link1345/gua-tester) v2を使用し、`main`へのpushとpull requestでUnity 6のWindows x64 Mono PlayerをビルドしてUIテストを実行します。Unity Playerのビルドにはrepository secretsの`UNITY_EMAIL`、`UNITY_PASSWORD`と、Personal用の`UNITY_LICENSE`またはProfessional用の`UNITY_SERIAL`が必要です。

forkからのpull requestではUnity credentialsを渡さず、Unity jobをスキップします。`pull_request_target`は使用しません。

### Visual差分Viewer

pull requestでVisual比較が失敗すると、`visual-report@v2`が`comparison.json`とPNGを静的Viewerへ変換し、`gua-visual-report`というActions artifactとして保存します。通常のUnity build log、Player、TRX、Gua診断・Visual成果物も`gua-tester` v2のworkflow artifactとして保存されます。

`main`へのpushと手動実行では、最新結果をGitHub Pages artifactとしてアップロードし、専用jobからPagesへdeployします。repositoryのPages sourceを事前に **GitHub Actions** へ設定してください。

手動実行で`visual-report-demo`を有効にすると、通常テストでビルド済みのUnity Playerを再利用し、`VisualReportViewerDemoProducesPixelDifferenceArtifact`でtitle画面とloading画面の意図的なpixel差分を生成します。テスト自体は期待した比較失敗を検証して成功し、ViewerにはExpected／Diff／Actualが表示されます。

> [!WARNING]
> screenshotにはゲーム画面へ描画された秘密情報や個人情報が含まれる可能性があります。特にpublic repositoryでPagesを有効にする前に、公開内容を確認してください。

## Guaで公開する主要ノード

| ID | 役割 | 画面 |
| --- | --- | --- |
| `start` | button | 1画面目 |
| `end` | button | 1画面目 |
| `loading` | text | 2画面目（Loading中のみ） |
| `back` | button | 2画面目 |
| `exit_question` | text | 終了確認 |
| `cancel_exit` | button | 終了確認 |
| `confirm_exit` | button | 終了確認 |

Gua v0.15.0が最初に対応するUnity構成に合わせ、Windows x64・Monoを対象にしています。IL2CPPとWindows以外のPlayerはこのサンプルの検証対象外です。
