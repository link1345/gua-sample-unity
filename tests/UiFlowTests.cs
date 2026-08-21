using System.Text.Json;
using Gua.Testing;
using Gua.Testing.Unity;
using NUnit.Framework;

namespace GuaUiLab.Unity.Tests;

[TestFixture]
[NonParallelizable]
public sealed class UiFlowTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LoadingTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);
    private static readonly string ProjectRoot = FindProjectRoot();

    [Test]
    public async Task StartLocksBackUntilLoadingFinishesThenReturnsToPageOne()
    {
        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);

        await GuaAssertions.WaitForVisibleAsync(host.Context, "start", ShortTimeout, PollInterval);
        GuaAssertions.GetById(host.Context, "end").ToBeVisible();

        await GuaAssertions.GetById(host.Context, "start").ClickAsync(ShortTimeout, PollInterval);
        await GuaAssertions.WaitForVisibleAsync(host.Context, "loading", ShortTimeout, PollInterval);
        await GuaAssertions.WaitForDisabledAsync(host.Context, "back", ShortTimeout, PollInterval);

        var rejection = Assert.ThrowsAsync<GuaActionException>(async () =>
            await GuaAssertions.GetById(host.Context, "back").ClickAsync());
        Assert.That(rejection!.Message, Does.Contain("disabled").IgnoreCase);

        await GuaAssertions.WaitForHiddenAsync(host.Context, "loading", LoadingTimeout, PollInterval);
        await GuaAssertions.WaitForEnabledAsync(host.Context, "back", ShortTimeout, PollInterval);
        await GuaAssertions.GetById(host.Context, "back").ClickAsync(ShortTimeout, PollInterval);
        await GuaAssertions.WaitForVisibleAsync(host.Context, "start", ShortTimeout, PollInterval);
    }

    [Test]
    public async Task EndCanBeCanceledAndOffersConfirmation()
    {
        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);

        await GuaAssertions.WaitForVisibleAsync(host.Context, "end", ShortTimeout, PollInterval);
        await GuaAssertions.GetById(host.Context, "end").ClickAsync(ShortTimeout, PollInterval);
        await GuaAssertions.WaitForVisibleAsync(host.Context, "exit_question", ShortTimeout, PollInterval);
        GuaAssertions.GetById(host.Context, "cancel_exit").ToBeVisible();
        GuaAssertions.GetById(host.Context, "confirm_exit").ToBeVisible();

        await GuaAssertions.GetById(host.Context, "cancel_exit").ClickAsync(ShortTimeout, PollInterval);
        await GuaAssertions.WaitForHiddenAsync(host.Context, "exit_question", ShortTimeout, PollInterval);
        await GuaAssertions.WaitForVisibleAsync(host.Context, "end", ShortTimeout, PollInterval);
    }

    [Test]
    public async Task RenderedPlayerPublishesThePortraitViewport()
    {
        using var host = StartHost();
        using var assertions = CreateAssertionScope(host);

        await GuaAssertions.WaitForVisibleAsync(host.Context, "start", ShortTimeout, PollInterval);
        var screenshot = host.CaptureScreenshot(ShortTimeout);
        Assert.Multiple(() =>
        {
            Assert.That(screenshot.Width, Is.EqualTo(541));
            Assert.That(screenshot.Height, Is.EqualTo(857));
            Assert.That(screenshot.DecodePng().Take(8), Is.EqualTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        });
    }

    [Test]
    public async Task WideWindowKeepsTheDesignCenteredAtItsOriginalAspectRatio()
    {
        const int width = 1000;
        const int height = 700;
        using var host = StartHost(width, height);
        using var assertions = CreateAssertionScope(host);

        await GuaAssertions.WaitForVisibleAsync(host.Context, "start", ShortTimeout, PollInterval);
        var screenshot = host.CaptureScreenshot(ShortTimeout);
        Assert.Multiple(() =>
        {
            Assert.That(screenshot.Width, Is.EqualTo(width));
            Assert.That(screenshot.Height, Is.EqualTo(height));
        });

        using var tree = JsonDocument.Parse(host.Context.GetUiTreeJson());
        var root = tree.RootElement.GetProperty("nodes")
            .EnumerateArray()
            .Single(node => node.GetProperty("id").GetString() == "root");
        var bounds = root.GetProperty("bounds");
        var expectedScale = Math.Min(width / 541.0, height / 857.0);
        var expectedWidth = 541.0 * expectedScale;
        var expectedHeight = 857.0 * expectedScale;

        Assert.Multiple(() =>
        {
            Assert.That(bounds.GetProperty("x").GetDouble(), Is.EqualTo((width - expectedWidth) * 0.5).Within(1.0));
            Assert.That(bounds.GetProperty("y").GetDouble(), Is.EqualTo((height - expectedHeight) * 0.5).Within(1.0));
            Assert.That(bounds.GetProperty("w").GetDouble(), Is.EqualTo(expectedWidth).Within(1.0));
            Assert.That(bounds.GetProperty("h").GetDouble(), Is.EqualTo(expectedHeight).Within(1.0));
        });
    }

    private static UnitySceneTestHost StartHost(int width = 541, int height = 857)
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
            AdditionalArguments = ["-screen-width", width.ToString(), "-screen-height", height.ToString(), "-screen-fullscreen", "0"],
        });
    }

    private static IDisposable CreateAssertionScope(UnitySceneTestHost host)
    {
        var diagnostics = host.CreateDiagnosticsSession(
            TestContext.CurrentContext.Test.FullName,
            Path.Combine(ProjectRoot, "artifacts", "gua"),
            captureScreenshot: true);
        return GuaAssertionScope.Use(new GuaAssertionOptions { DiagnosticsSession = diagnostics });
    }

    private static string FindProjectRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the Unity project above the NUnit output directory.");
    }
}
