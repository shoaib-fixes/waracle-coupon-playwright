using System.Text;
using Allure.Net.Commons;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using NUnit.Framework;
using Reqnroll;
using Shouldly;
using WaracleStore.CouponTests.Support.Config;
using WaracleStore.CouponTests.Support.Drivers;

namespace WaracleStore.CouponTests.StepDefinitions;

[Binding]
public sealed class AccessibilitySteps(BrowserDriver driver, TestSettings settings, ScenarioContext scenarioContext)
{
    private static readonly string[] Wcag21AaTags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"];
    private static readonly string[] FailingImpacts = ["critical", "serious"];

    [Then("the page has no serious or critical WCAG 2.1 AA violations")]
    public async Task ThenNoSeriousViolations()
    {
        var results = await driver.Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = Wcag21AaTags.ToList() },
        });

        var report = Describe(results);
        await AttachAsync(report);

        var blocking = results.Violations.Where(v => FailingImpacts.Contains(v.Impact)).ToList();
        if (blocking.Count == 0)
            return;

        throw new ShouldAssertException(
            $"{blocking.Count} serious/critical WCAG 2.1 AA violation(s) on {driver.Page.Url} " +
            $"({results.Violations.Length} violations in total; full scan attached):{Environment.NewLine}" +
            string.Join(Environment.NewLine, blocking.Select(v =>
                $"  - [{v.Impact}] {v.Id}: {v.Help} ({v.Nodes.Length} element(s), e.g. {v.Nodes[0].Target}) {v.HelpUrl}")));
    }

    private static string Describe(AxeResult results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# axe-core scan of {results.Url}");
        sb.AppendLine($"{results.Timestamp:u} · {results.Violations.Length} violations · {results.Passes.Length} rules passed · {results.Incomplete.Length} incomplete");
        sb.AppendLine();

        foreach (var v in results.Violations.OrderBy(v => ImpactRank(v.Impact)))
        {
            sb.AppendLine($"## [{v.Impact}] {v.Id} — {v.Help}");
            sb.AppendLine(v.HelpUrl);
            foreach (var node in v.Nodes)
            {
                sb.AppendLine($"- target: {node.Target}");
                sb.AppendLine($"  html:   {node.Html}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static int ImpactRank(string? impact) => impact switch
    {
        "critical" => 0,
        "serious" => 1,
        "moderate" => 2,
        "minor" => 3,
        _ => 4,
    };

    private async Task AttachAsync(string report)
    {
        Directory.CreateDirectory(settings.ResolvedArtifactsDirectory);
        var safeTitle = string.Concat(scenarioContext.ScenarioInfo.Title.Where(char.IsLetterOrDigit));
        var path = Path.Combine(settings.ResolvedArtifactsDirectory, $"axe_{safeTitle}_{DateTime.UtcNow:HHmmssfff}.md");
        await File.WriteAllTextAsync(path, report);

        TestContext.AddTestAttachment(path, "axe-core scan");
        AllureApi.AddAttachment("axe-core scan", "text/markdown", path);
    }
}
