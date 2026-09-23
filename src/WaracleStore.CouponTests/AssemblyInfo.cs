using NUnit.Framework;

// Scenario-level parallelism: every scenario (NUnit test case) may run concurrently.
// Safe because the suite holds no mutable static state: one browser per run,
// one isolated BrowserContext per scenario, and all scenario state lives in
// Reqnroll's scenario-scoped container.
[assembly: Parallelizable(ParallelScope.Children)]
