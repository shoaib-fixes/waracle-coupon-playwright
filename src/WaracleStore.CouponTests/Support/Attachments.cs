using System.Text.RegularExpressions;
using Allure.Net.Commons;
using NUnit.Framework;
using WaracleStore.CouponTests.Support.Config;

namespace WaracleStore.CouponTests.Support;

/// <summary>
/// One place for "keep this file and show it in every report": every artefact is attached to
/// the NUnit result (TRX, visible in CI summaries) and to the Allure report.
/// </summary>
public static partial class Attachments
{
    /// <summary>A file path under the artefacts directory whose name identifies the scenario and is unique per run.</summary>
    public static string PathFor(TestSettings settings, string scenarioTitle, string suffix)
    {
        Directory.CreateDirectory(settings.ResolvedArtifactsDirectory);
        return Path.Combine(settings.ResolvedArtifactsDirectory, Stem(scenarioTitle) + suffix);
    }

    /// <summary>Attach from inside a step: Allure files it under that step.</summary>
    public static void AddToCurrentStep(string name, string mimeType, string path)
    {
        TestContext.AddTestAttachment(path, name);
        AllureApi.AddAttachment(name, mimeType, path);
    }

    /// <summary>
    /// Attach from a hook: Allure reports after-scenario hooks as tear-down fixtures, so a plain
    /// <c>AllureApi.AddAttachment</c> there would file the trace under "Tear down". This copies
    /// the file into the results directory and attaches it to the test case itself.
    /// </summary>
    public static void AddToTestCase(string name, string mimeType, string path)
    {
        TestContext.AddTestAttachment(path, name);

        var source = $"{Guid.NewGuid():N}-attachment{Path.GetExtension(path)}";
        File.Copy(path, Path.Combine(AllureLifecycle.Instance.ResultsDirectory, source));
        AllureLifecycle.Instance.UpdateTestCase(testCase =>
            testCase.attachments.Add(new Attachment { name = name, type = mimeType, source = source }));
    }

    private static string Stem(string scenarioTitle)
    {
        var safe = UnsafeChars().Replace(scenarioTitle, "_").Trim('_');
        if (safe.Length > 80)
            safe = safe[..80];
        return $"{safe}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
    }

    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex UnsafeChars();
}
