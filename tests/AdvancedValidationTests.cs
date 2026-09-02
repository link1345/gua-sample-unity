using Gua.Testing;
using Gua.Testing.Recording;
using Gua.Testing.Unity;
using Gua.Testing.Visual;
using NUnit.Framework;

namespace GuaUiLab.Unity.Tests;

[TestFixture]
[NonParallelizable]
public sealed class AdvancedValidationTests
{
    private const int RenderedWidth = 541;
    private const int RenderedHeight = 700;
    private static readonly string VisualVariant =
        Environment.GetEnvironmentVariable("GUA_VISUAL_VARIANT")
        ?? (OperatingSystem.IsLinux()
            ? "unity-LinuxX64"
            : "windows-unity-6000.5.3f1-d3d11-541x700");
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RecordingActionTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);
    private static readonly string ProjectRoot = FindProjectRoot();

    [Test]
    public async Task TitleScreenMatchesReviewedVisualBaseline()
    {
        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);

        await GuaAssertions.WaitForVisibleAsync(
            host.Context,
            "start",
            timeout: ShortTimeout,
            pollInterval: PollInterval);
        var screenshot = host.CaptureScreenshot(ShortTimeout);

        var result = await GuaVisualAssertions.ExpectScreenshotAsync(
            host.Context,
            "title-screen",
            new ScreenshotOptions
            {
                BaselineDirectory = Path.Combine(ProjectRoot, "tests", "baselines"),
                ArtifactDirectory = Path.Combine(ProjectRoot, "artifacts", "gua"),
                BaselineVariant = VisualVariant,
                PixelThreshold = 0.02f,
                MaxDifferentPixelRatio = 0.001,
                WaitForStableSnapshot = true,
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Matched, Is.True);
            Assert.That(screenshot.Width, Is.EqualTo(RenderedWidth));
            Assert.That(screenshot.Height, Is.EqualTo(RenderedHeight));
        });
    }

    [Test]
    public async Task VisualReportViewerDemoProducesPixelDifferenceArtifact()
    {
        if (Environment.GetEnvironmentVariable("GUA_VISUAL_REPORT_DEMO") != "1")
        {
            Assert.Ignore("Set GUA_VISUAL_REPORT_DEMO=1 to build the viewer demo.");
        }

        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);
        await GuaAssertions.WaitForVisibleAsync(
            host.Context,
            "start",
            timeout: ShortTimeout,
            pollInterval: PollInterval);
        var titleScreenshot = host.CaptureScreenshot(ShortTimeout).DecodePng();

        var baselineDirectory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "visual-report-demo-baselines");
        var artifactDirectory = Path.Combine(ProjectRoot, "artifacts", "gua");
        const string comparisonName = "visual-report-viewer-demo";
        var variant = VisualVariant;
        var comparisonArtifactDirectory = Path.Combine(artifactDirectory, comparisonName);

        await GuaVisualAssertions.ExpectScreenshotAsync(
            host.Context,
            comparisonName,
            new ScreenshotOptions
            {
                BaselineDirectory = baselineDirectory,
                ArtifactDirectory = artifactDirectory,
                BaselineVariant = variant,
                UpdateBaselines = true,
            });

        await GuaAssertions.GetById(host.Context, "start").ClickAsync(ShortTimeout, PollInterval);
        await GuaAssertions.WaitForVisibleAsync(
            host.Context,
            "loading",
            timeout: ShortTimeout,
            pollInterval: PollInterval);

        var screenshotDeadline = DateTimeOffset.UtcNow + ShortTimeout;
        while (host.CaptureScreenshot(ShortTimeout).DecodePng().SequenceEqual(titleScreenshot))
        {
            if (DateTimeOffset.UtcNow >= screenshotDeadline)
            {
                Assert.Fail("The loading screen did not publish a different viewport screenshot.");
            }
            await Task.Delay(PollInterval);
        }

        var failure = Assert.ThrowsAsync<InvalidOperationException>(() =>
            GuaVisualAssertions.ExpectScreenshotAsync(
                host.Context,
                comparisonName,
                new ScreenshotOptions
                {
                    BaselineDirectory = baselineDirectory,
                    ArtifactDirectory = artifactDirectory,
                    BaselineVariant = variant,
                    PixelThreshold = 0.02f,
                    MaxDifferentPixelRatio = 0.001,
                }));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("Screenshot comparison failed"));
            Assert.That(
                Directory.GetFiles(comparisonArtifactDirectory, "comparison.json", SearchOption.AllDirectories),
                Is.Not.Empty);
            Assert.That(
                Directory.GetFiles(comparisonArtifactDirectory, "diff.png", SearchOption.AllDirectories),
                Is.Not.Empty);
        });
    }

    [Test]
    public async Task RecordedEndCancelJourneyRoundTripsAndReplays()
    {
        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);

        var recorder = new GuaRecorder(host.Context);
        await recorder.ClickAsync(
            new(Id: "end"),
            waitCondition: GuaWaitConditions.Visible("end"),
            timeout: RecordingActionTimeout,
            pollInterval: PollInterval);
        await recorder.ClickAsync(
            new(Id: "cancel_exit"),
            waitCondition: GuaWaitConditions.Visible("cancel_exit"),
            timeout: RecordingActionTimeout,
            pollInterval: PollInterval);
        await GuaAssertions.WaitForVisibleAsync(
            host.Context,
            "end",
            timeout: ShortTimeout,
            pollInterval: PollInterval);

        var recordingDirectory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "artifacts",
            "gua",
            "recordings");
        var recordingPath = Path.Combine(recordingDirectory, "end-cancel.json");
        GuaRecordingFile.Save(recordingPath, recorder.Recording);
        TestContext.AddTestAttachment(recordingPath, "Recorded Gua end/cancel journey");

        var recording = GuaRecordingFile.Load(recordingPath);
        var replay = await GuaReplayer.ReplayAsync(
            host.Context,
            recording,
            new GuaReplayOptions
            {
                TimingMode = GuaReplayTimingMode.PreferConditions,
                ActionTimeout = RecordingActionTimeout,
                PollInterval = PollInterval,
            });

        await GuaAssertions.WaitForVisibleAsync(
            host.Context,
            "end",
            timeout: ShortTimeout,
            pollInterval: PollInterval);
        await GuaAssertions.WaitForHiddenAsync(
            host.Context,
            "exit_question",
            timeout: ShortTimeout,
            pollInterval: PollInterval);

        Assert.Multiple(() =>
        {
            Assert.That(recording.SchemaVersion, Is.EqualTo(1));
            Assert.That(recording.Steps, Has.Count.EqualTo(2));
            Assert.That(replay.Steps, Has.Count.EqualTo(2));
            Assert.That(
                replay.Steps.All(step => step.Completion is { Succeeded: true }),
                Is.True);
        });
    }

    private static UnitySceneTestHost StartHost()
    {
        var player = Environment.GetEnvironmentVariable("GUA_UNITY_PLAYER")
            ?? Path.Combine(ProjectRoot, "Builds", "GuaUiLab.exe");
        return UnitySceneTestHost.LoadRenderedPlayer(player, new UnitySceneTestHostOptions
        {
            UseAvailableBridgePort = true,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            SceneTimeout = TimeSpan.FromSeconds(15),
            StartupResetPolicy = GuaResetPolicy.Strict,
            TeardownResetPolicy = GuaResetPolicy.Strict,
            CaptureDiagnosticsBeforeTeardown = true,
            DiagnosticsOutputDirectory = Path.Combine(ProjectRoot, "artifacts", "gua"),
            EnvironmentVariables = CreatePlayerEnvironment(),
            AdditionalArguments =
            [
                "-screen-width",
                RenderedWidth.ToString(),
                "-screen-height",
                RenderedHeight.ToString(),
                "-screen-fullscreen",
                "0",
            ],
        });
    }

    private static IReadOnlyDictionary<string, string> CreatePlayerEnvironment()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new Dictionary<string, string>();
        }

        var alsaConfigPath = Path.Combine(Path.GetTempPath(), "gua-unity-alsa-null.conf");
        File.WriteAllText(
            alsaConfigPath,
            """
            pcm.!default {
                type null
            }
            ctl.!default {
                type null
            }
            """);
        return new Dictionary<string, string>
        {
            ["ALSA_CONFIG_PATH"] = alsaConfigPath,
        };
    }

    private static IDisposable CreateAssertionScope(UnitySceneTestHost host)
    {
        var diagnostics = host.CreateDiagnosticsSession(
            TestContext.CurrentContext.Test.FullName,
            outputDirectory: Path.Combine(ProjectRoot, "artifacts", "gua"),
            captureScreenshot: true);
        return GuaAssertionScope.Use(new GuaAssertionOptions
        {
            DiagnosticsSession = diagnostics,
        });
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the Unity project above the NUnit test output directory.");
    }
}
