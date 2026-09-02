# Gua UI Lab for Unity

English | [日本語](README-ja.md)

This is a Unity sample project for [Gua](https://github.com/link1345/gua). It recreates the same visual assets, screen layout, and interaction flow as [`gua-sample-godot`](https://github.com/link1345/gua-sample-godot) using Unity 6 and uGUI.

`Start` opens the second screen, where `Back` remains disabled during a six-second `Loading....` state. After loading finishes, `Back` returns to the first screen. `End` opens an exit confirmation dialog; `Cancel` closes it, and `OK` exits the application.

The window is resizable. The UI scales uniformly while preserving its 541×857 design aspect ratio, and any space outside that ratio is rendered as black letterboxing.

UI automation uses the Gua v1.0.7 Unity UPM package. At runtime, it opens a Gua bridge at `ws://127.0.0.1:8765` by default and automatically exposes the standard uGUI tree. Set the `GUA_BRIDGE_PORT` environment variable to use a different port.

## Run the sample

1. Open this directory in Unity Hub.
2. Open `Assets/Scenes/Main.unity` with Unity `6000.5.3f1`.
3. Enter Play Mode.

The official UPM package v1.0.7, including the managed assemblies and Windows and Linux x64 native libraries, is pinned in `Packages/com.link1345.gua`. No adjacent Gua source repository or separate package download is required.

## UI tests

Build a Windows x64 Mono Player, then use NUnit to operate the real Player through its Semantic UI Tree.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath $PWD `
  -buildWindows64Player "$PWD\Builds\GuaUiLab.exe" `
  -logFile "$PWD\unity-build.log"
dotnet test tests\GuaUiLab.Unity.Tests.csproj
```

If the Player was built elsewhere, set `GUA_UNITY_PLAYER` to the absolute path of its executable. On failure, the Gua UI Tree, logs, Unity Player log, screenshots, and other diagnostics are saved under `artifacts/gua`.

### Visual and recording validation

`Gua.Testing.Visual` compares the initial screen with a reviewed PNG baseline to detect missing images, layout shifts, and unintended overlays. Only update the baseline intentionally after reviewing the difference:

```powershell
$env:GUA_UPDATE_BASELINES = "1"
dotnet test tests\GuaUiLab.Unity.Tests.csproj --filter TitleScreenMatchesReviewedVisualBaseline
Remove-Item Env:GUA_UPDATE_BASELINES
```

`Gua.Testing.Recording` records real semantic interactions for `End` → `Cancel`, saves them to JSON, reloads them, and replays them in the same Unity Player session. Successful recording files are saved under `artifacts/gua/recordings` in the test output directory.

To generate a local pixel-difference artifact for the Visual Report, run:

```powershell
$env:GUA_VISUAL_REPORT_DEMO = "1"
dotnet test tests\GuaUiLab.Unity.Tests.csproj --filter VisualReportViewerDemoProducesPixelDifferenceArtifact
Remove-Item Env:GUA_VISUAL_REPORT_DEMO
```

## GitHub Actions

`.github/workflows/gua-tests.yml` uses [`link1345/gua-tester`](https://github.com/link1345/gua-tester) v3.1 to build a Unity 6 Linux x64 Mono Player and run UI tests under Xvfb on pushes to `main` and on pull requests. Building the Unity Player requires the repository secrets `UNITY_EMAIL`, `UNITY_PASSWORD`, and either `UNITY_LICENSE` for a Personal license or `UNITY_SERIAL` for a Professional license.

Pull requests from forks do not receive Unity credentials, so the Unity job is skipped. The workflow does not use `pull_request_target`.

### Visual difference viewer

When a visual comparison fails in a pull request, `visual-report@v3.1` turns `comparison.json` and its PNGs into a static viewer and stores it as the `gua-visual-report` Actions artifact. The normal Unity build log, Player, TRX file, and Gua diagnostic and visual artifacts are also stored in the `gua-tester` v3.1 workflow artifact.

On pushes to `main` and manual runs, the latest report is uploaded as a GitHub Pages artifact and deployed by a dedicated job. Configure the repository's Pages source as **GitHub Actions** first.

Enable `visual-report-demo` in a manual run to reuse the Unity Player from the normal test build and have `VisualReportViewerDemoProducesPixelDifferenceArtifact` generate an intentional pixel difference between the title and loading screens. The test succeeds after verifying the expected comparison failure, while the viewer displays Expected, Diff, and Actual images.

> [!WARNING]
> Screenshots may contain secrets or personal information rendered inside the game. Review what will be published before enabling Pages, especially in a public repository.

## Main nodes exposed through Gua

| ID | Role | Screen |
| --- | --- | --- |
| `start` | button | First screen |
| `end` | button | First screen |
| `loading` | text | Second screen, while loading |
| `back` | button | Second screen |
| `exit_question` | text | Exit confirmation |
| `cancel_exit` | button | Exit confirmation |
| `confirm_exit` | button | Exit confirmation |

This sample's CI targets Linux x64 and Mono, matching the cross-platform Unity configuration in Gua v1.0.7. IL2CPP remains outside the sample's validated scope.
