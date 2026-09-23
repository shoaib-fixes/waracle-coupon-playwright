// Allure Report 3 configuration. Picked up automatically when `allure generate` runs from the
// repository root (scripts/report.sh and the CI workflow both do).
const repository =
  process.env.GITHUB_SERVER_URL && process.env.GITHUB_REPOSITORY
    ? `${process.env.GITHUB_SERVER_URL}/${process.env.GITHUB_REPOSITORY}`
    : "https://github.com/shoaib-fixes/waracle-coupon-playwright";
const observations = `${repository}/blob/main/docs/ReleaseObservations.md`;

const isKnownDefect = (labels) => labels.some((l) => l.name === "tag" && l.value === "KnownDefect");

export default {
  name: "Waracle coupon suite",
  output: "./allure-report",

  // Failures are grouped so that an unexpected regression never hides among the expected reds.
  categories: [
    {
      id: "known-release-defects",
      name: "Known release defects",
      matchers: (d) => (d.status === "failed" || d.status === "broken") && isKnownDefect(d.labels),
      groupBy: [{ label: "feature" }],
      groupByMessage: false,
    },
    {
      id: "performance-budget",
      name: "Performance budget exceeded",
      matchers: { statuses: ["failed"], labels: { tag: /^Performance$/ } },
    },
    {
      id: "unexpected-failures",
      name: "Unexpected failures – investigate",
      matchers: (d) => d.status === "failed" && !isKnownDefect(d.labels),
    },
    {
      id: "test-errors",
      name: "Test errors",
      matchers: { statuses: ["broken"] },
    },
  ],

  // Resolutions mark each expected failure as a known issue, linked to its entry in the
  // observations document. The report then shows them as "known" rather than as plain failures.
  resolutions: {
    knownIssuesPath: "./known-issues.json",
    links: {
      "release-defect": {
        nameTemplate: "Release defect %s",
        urlTemplate: `${observations}#%s`,
      },
    },
    rules: [
      {
        resolution: "issue",
        messageRegexp: "AC-2/3/4|does not match the values in the feature file",
        issue: { id: "d1", type: "release-defect" },
        comment: "D1 – the discount is a flat £0.25 instead of 25% of the subtotal.",
      },
      {
        resolution: "issue",
        messageRegexp: "AC-5 expects a clear message when the coupon \"[^\"\\s][^\"]*\" is rejected",
        issue: { id: "d2", type: "release-defect" },
        comment: "D2 – an invalid code only shows the toast \"Coupon entered\".",
      },
      {
        resolution: "issue",
        messageRegexp: "AC-5 expects a clear message when the coupon \"\\s*\" is rejected",
        issue: { id: "d3", type: "release-defect" },
        comment: "D3 – an empty code shows no message at all.",
      },
      {
        resolution: "issue",
        messageRegexp: "WCAG 2\\.1 AA violation",
        issue: { id: "d10", type: "release-defect" },
        comment: "D10 – grey helper text fails WCAG 2.1 AA colour contrast.",
      },
    ],
  },

  plugins: {
    awesome: {
      options: {
        groupBy: ["epic", "feature"],
      },
    },
  },
};
