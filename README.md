# Waracle Store – Coupon Codes (WS-101) test suite

[![Coupon E2E](https://github.com/shoaib-fixes/waracle-coupon-playwright/actions/workflows/e2e.yml/badge.svg)](https://github.com/shoaib-fixes/waracle-coupon-playwright/actions/workflows/e2e.yml)

Automated acceptance tests for the **WS-101 Coupon Codes at Checkout** release of the Waracle
Store web app, written in C# with Playwright, Reqnroll (Gherkin) and NUnit. Scenarios run in
parallel, record a Playwright trace on failure, and run in GitHub Actions on every push.

**Result on the current build:** 38 scenarios, 27 pass, 11 fail. Every failure is a release
defect, not a test problem. Those scenarios are tagged `@KnownDefect`, kept red on purpose and
excluded from the CI gate. See [Observations on the release](#observations-on-the-release).

## Running the tests

Prerequisites: [.NET SDK 10](https://dotnet.microsoft.com/download), Node.js 18+, Git.

```bash
# 1. Start the store (clones Waracle/qa-candidate-test next to this repo if needed,
#    installs it, starts the API on :4000 and the web app on :5173)
scripts/start-store.sh

# 2. In another terminal: build, install the browser once, run everything
dotnet build
pwsh src/WaracleStore.CouponTests/bin/Debug/net10.0/playwright.ps1 install chromium   # Windows: powershell -File ...
dotnet test
```

Useful variations:

```bash
dotnet test --filter "TestCategory!=KnownDefect"        # the CI gate: everything that should be green
dotnet test --filter "TestCategory=KnownDefect"         # only the scenarios that document release defects
dotnet test --filter "TestCategory=AC5"                 # one acceptance criterion (AC1 … AC6)
dotnet test --filter "TestCategory=Smoke"               # Smoke | Positive | Negative | Edge | Journey
dotnet test -- NUnit.NumberOfTestWorkers=8              # more (or fewer) parallel scenarios; default 4
TEST_Headless=false TEST_SlowMoMs=250 dotnet test       # watch it run
TEST_Browser=firefox dotnet test                        # chromium | firefox | webkit (install it first)
TEST_TraceMode=On dotnet test                           # keep a trace for every scenario, not only failures
```

Configuration lives in `src/WaracleStore.CouponTests/appsettings.json` and can be overridden by an
untracked `appsettings.Local.json` or `TEST_*` environment variables (`TEST_BaseUrl`,
`TEST_Credentials__Password`, …). Bad configuration fails once, at start-up, with every problem listed.

Failed scenarios leave `<scenario>.png` and `<scenario>.trace.zip` under
`src/WaracleStore.CouponTests/bin/Debug/net10.0/artifacts/` (also attached to the TRX result).
Open a trace with `npx playwright show-trace <file>.trace.zip` or at
[trace.playwright.dev](https://trace.playwright.dev).

## What is covered

| AC | Scenario (feature file) | Kind | Result |
|----|-------------------------|------|--------|
| AC-1 | The launch coupon can be applied from the cart (`ApplyCoupon`) | positive | pass |
| AC-1 | The coupon applied in the cart carries through to the checkout summary | positive | pass |
| AC-1, AC-6 | A signed-in customer applies WARACLE25 to a basket built from the catalogue (`EndToEndJourney`) | positive | pass |
| AC-2 | WARACLE25 reduces the subtotal by 25% – 3 baskets | positive | **fail – D1** |
| AC-2 | The discount is recalculated when the quantity changes after the coupon is applied | edge | **fail – D1** |
| AC-2 | The launch code is accepted regardless of letter case – 2 spellings | edge | pass |
| AC-2 | Spaces around the launch code are ignored | edge | pass |
| AC-3 | Standard shipping of £5.00 applies to any non-empty basket – 3 baskets (`OrderSummaryTotals`) | positive | pass |
| AC-3 | Shipping is not affected by the coupon | positive | pass |
| AC-3 | An empty basket has nothing to ship and no order summary | edge | pass |
| AC-4 | Order total = subtotal + shipping when no coupon is applied – 3 baskets | positive | pass |
| AC-4 | Order total = subtotal − discount + shipping with WARACLE25 applied – 2 baskets | positive | **fail – D1** |
| AC-4 | Clearing the coupon restores the undiscounted total | edge | pass |
| AC-4 | Removing the last item empties the basket and its summary | edge | pass |
| AC-5 | An invalid code applies no discount – 5 codes (`InvalidCoupon`) | negative | pass |
| AC-5 | An invalid code shows a clear message – 2 codes | negative | **fail – D2** |
| AC-5 | An empty code applies no discount | negative | pass |
| AC-5 | An empty code shows a clear message | negative | **fail – D3** |
| AC-5 | A code made only of spaces shows a clear message | negative | **fail – D3** |
| AC-5 | An invalid code entered after a valid one removes the discount | edge | pass |
| AC-5 | A very long code is rejected without breaking the cart | edge | pass |
| AC-6 | The confirmation shows the applied coupon, its discount and the total paid (`OrderConfirmation`) | positive | pass |
| AC-6 | The amounts on the confirmation follow the release pricing rules | positive | **fail – D1** |
| AC-6 | An order placed without a coupon shows no coupon line | negative | pass |
| AC-6 | The amount on the Pay button is the amount shown as paid | edge | pass |

AC-5 is split into "no discount" and "clear message" scenarios so that the half the release gets
right reports green independently of the half it gets wrong.

## Observations on the release

The short version; the full write-up with evidence and suggested fixes is in
[docs/ReleaseObservations.md](docs/ReleaseObservations.md).

- **D1 · Blocker.** The discount is a flat **£0.25**, not 25%: `discount = 0.25` is never multiplied
  by the subtotal. The same line is in the web, backend and mobile pricing modules. AC-2, AC-4 and
  AC-6 fail on every basket. The release cannot ship.
- **D2 · High.** An invalid code shows an informational toast, "Coupon entered", and nothing
  else. AC-5 asks for a clear message.
- **D3 · High.** An empty code shows no message at all; it silently clears the coupon.
- **D4 · Medium.** The release note promises coupons "at the cart and checkout"; the checkout has
  no coupon field.
- **D5 · Medium.** Refreshing the cart keeps the basket but drops the coupon.
- **D6 · Medium.** The exercise's `cd web → npm install → npm run dev` does not work: the mock API
  imports `bcryptjs`, declared only by the backend. Install from the monorepo root instead. This
  suite runs against the full stack (API + web), which is what will ship.
- **D7 · Medium.** Pricing is copy-pasted across three apps and has already drifted; one shared
  module with unit tests would have caught D1 before a UI existed.
- **D8 · Low.** The confirmation omits subtotal and shipping, and its "Order Details" button goes
  to the product list.
- **D9 · Spec gaps.** Case-sensitivity of the code (README says `Waracle25`, ACs say
  `WARACLE25`, the app accepts both) and the rounding rule for 25% of an odd subtotal are not
  specified. The suite pins the observed behaviour and flags both for a product decision.

## How the suite is put together

```
src/WaracleStore.CouponTests/
├── Features/            Gherkin, one file per area, tagged @AC1…@AC6, @Positive/@Negative/@Edge, @KnownDefect
├── StepDefinitions/     Session, Basket, Coupon, Summary, Checkout – thin; all page logic lives in Pages/
├── Pages/               CartPage, CheckoutPage, OrderConfirmationPage, LoginPage, ProductDetailsPage
│   └── Components/      OrderSummaryComponent (shared by cart + checkout), ToastComponent
├── Hooks/               BrowserHooks – run-level browser + sign-in, scenario-level context + tracing
└── Support/
    ├── Config/          TestSettings (typed, validated) + SettingsLoader (json → local json → TEST_* env)
    ├── Drivers/         PlaywrightRunner (one per run), BrowserDriver (one per scenario)
    ├── Data/            Catalogue, BasketParser, CartSeeder
    ├── Pricing/         PricingOracle – AC-2/3/4 as code, Money – GBP parse/format
    └── Transforms/      "£24.99" and summary tables straight into step arguments
tests/WaracleStore.CouponTests.UnitTests/   27 tests for the oracle, money parsing and basket parsing
```

Design decisions worth knowing about:

- **The expected numbers come from the acceptance criteria, not from the app.** `PricingOracle`
  implements AC-2/3/4 in a dozen lines and is unit-tested. Outline scenarios also carry explicit
  expected values so a reviewer can check the arithmetic by eye. A failure prints a diff of the
  whole order summary, e.g. `discount: expected £30.00, displayed £0.25`.
- **Scenarios start where the feature starts.** The demo customer is signed in once through the
  API per run; each scenario receives the token and its basket via `localStorage` before the app
  boots, so a scenario is on the cart page in about a second. One end-to-end journey still uses the
  login form and the product page, proving the seeded state matches what the UI itself produces.
- **Parallel by default, with no shared mutable state.** `[assembly: Parallelizable(ParallelScope.Children)]`
  runs scenarios concurrently across NUnit workers. One browser per run; one isolated browser
  context per scenario; all scenario state is in Reqnroll's scenario container. The API is
  stateless for the cart, and orders are per customer, so scenarios cannot interfere.
- **Traces instead of reports.** Every scenario is traced; on failure the trace (DOM snapshots,
  network, console, screenshots, step timeline) and a full-page screenshot are attached to the
  test result. Nothing else is logged, because the Gherkin steps already are the narrative.
- **Known defects stay red and stay visible.** Marking them `Ignore` would hide the release's
  state; letting them break the build would hide regressions elsewhere. CI runs them in a
  separate, non-blocking step and publishes both result sets. When the app is fixed they go green
  without touching the suite.
- **Web-first assertions everywhere.** Toasts vanish after 3 s and the summary re-prices
  asynchronously, so every check is a retrying `Expect(...)`, never a sleep.

### Testability notes

The web app has no `data-testid` hooks and the checkout labels are not associated with their
inputs, so those fields are located by placeholder and summary rows by their visible label. The
suite documents each such compromise in the page object. Details and recommended fixes are in
[docs/ReleaseObservations.md](docs/ReleaseObservations.md#testability-notes).

## Continuous integration

[`.github/workflows/e2e.yml`](.github/workflows/e2e.yml) runs on every push and pull request,
and on demand with a choice of browser and worker count:

1. **Unit tests** – the pricing oracle and parsers, no browser.
2. **E2E** – checks out `Waracle/qa-candidate-test` at a pinned commit, installs only the `web`
   and `backend` workspaces, starts both, waits for health, installs the browser (cached by
   Playwright version), then runs the **gate** (`TestCategory!=KnownDefect`, must pass) followed
   by the **known release defects** (`TestCategory=KnownDefect`, expected to fail, never blocks).
   TRX results, traces and screenshots are uploaded as artefacts and published as check runs.

## Out of scope

Kept out deliberately, per the exercise's "less important" list and the 90-minute guide:
API-level tests of the Express backend, the mobile app, accessibility and performance audits,
visual report polish, and exhaustive edge cases (multi-currency, concurrent sessions, order history).
