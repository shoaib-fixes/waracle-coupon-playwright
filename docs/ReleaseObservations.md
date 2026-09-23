# WS-101 Coupon Codes – release observations

Findings from reading the Waracle Store source (commit `e123f7a`) and from running this suite
against it. Each defect names the scenarios that expose it; those scenarios are tagged
`@KnownDefect` and fail until the defect is fixed. When a fix lands they go green with no
change to the suite.

Severity: **Blocker** – the release cannot ship · **High** – an acceptance criterion is unmet ·
**Medium** – customer-visible or a maintenance risk · **Low** – polish.

## Defects

### D1 · Blocker · The discount is 25 pence, not 25 per cent  (AC-2, AC-4, AC-6)

`calculateDiscount` sets `discount = 0.25` and never multiplies by the subtotal, so every basket
gets exactly £0.25 off. The same line appears in all three copies of the pricing module:
`web/src/services/pricing.ts`, `backend/src/utils/pricing.js` and `mobile/src/services/pricing.ts`.

| Basket | Expected discount (25%) | Actual |
|--------|-------------------------|--------|
| 2 × Waracle Headset (£119.98) | £30.00 | £0.25 |
| 3 × Men's Logo T-Shirt (£74.97) | £18.74 | £0.25 |
| 1 × Waracle Cap (£19.99) | £5.00 | £0.25 |

Because AC-4 and AC-6 derive from the discount, both fail as a consequence; the total and the
confirmation are internally consistent but wrong.

Exposed by: *AC-2 – WARACLE25 reduces the subtotal by 25%* (×3), *AC-2 – The discount is
recalculated when the quantity changes*, *AC-4 – Order total … with WARACLE25 applied* (×2),
*AC-6 – The amounts on the confirmation follow the release pricing rules*.

Fix: `discount = subtotal * 0.25` in one shared module (see D7).

### D2 · High · An invalid code gives no clear message  (AC-5)

Applying an unrecognised code shows an informational toast reading **"Coupon entered"**. Nothing
says the code was rejected; the customer has to notice that no discount line appeared. The
toast text is chosen by a string comparison inside `CartPage.tsx`, not from the API's
`couponApplied` flag, so the UI and the pricing engine can disagree.

Exposed by: *AC-5 – An invalid code shows a clear message* (×2).

### D3 · High · An empty code gives no message at all  (AC-5)

Submitting an empty (or whitespace-only) code silently clears any applied coupon. AC-5 asks
for a clear message in this case too.

Exposed by: *AC-5 – An empty code shows a clear message*, *AC-5 – A code made only of spaces
shows a clear message*.

### D4 · Medium · No coupon entry at checkout

The release note says coupons are supported "at the cart and checkout". The checkout page only
displays a coupon applied earlier; there is no field to enter one. AC-1 only requires the cart,
so this is a scope question for the product owner rather than an AC failure.

### D5 · Medium · The coupon is lost on refresh, the basket is not

The basket is persisted in `localStorage`; the coupon lives only in React state. Refreshing the
cart page keeps the items and drops the discount, so the total silently goes back up.

### D6 · Medium · The standalone run instructions do not work

The exercise (and `web/README.md`) say `cd web → npm install → npm run dev`. That fails:
`web/src/utils/mockApi.ts` imports `bcryptjs`, which is declared only in `backend/package.json`.
Vite reports *Failed to resolve import "bcryptjs"* and the app renders blank. Installing from the
monorepo root hoists the package and the app works. `launch-web.sh` is also misnamed: it starts
the backend too and points the web app at it, so it never exercises the standalone mock.

This suite therefore runs against the full stack (API + web), which is also the configuration
the release will ship in.

### D7 · Medium · Pricing logic is copy-pasted three times and has already drifted

Web, backend and mobile each carry their own `calculateTotals`. D1 shipped in all three. The
mobile copy's `round2` already differs (it omits the `Number.EPSILON` guard the other two use),
so the three apps can disagree on the penny. One shared package with unit tests would have caught
D1 before any UI existed.

### D8 · Low · Confirmation page gaps  (AC-6)

The confirmation shows the coupon, the discount and "Total Paid", which satisfies AC-6, but not the
subtotal or shipping, so the customer cannot reconcile the total. The **Order Details** button
links to the product list. When the web app runs standalone, refreshing the confirmation shows
"Order not found" because orders live in memory.

### D9 · Low · Spec ambiguities the suite had to decide

- **Case and whitespace.** The ACs name `WARACLE25`; `web/README.md` says `Waracle25`; the code
  accepts any case and trims spaces. The suite pins the observed behaviour (*AC-2 – The launch
  code is accepted regardless of letter case*, *… Spaces around the launch code are ignored*) so a
  change is deliberate. If the code should be exact-match, those two scenarios become the defect.
- **Rounding.** The ACs do not say how 25% of an odd subtotal rounds. The oracle uses half-up to
  the penny (matches JavaScript's `Math.round`); e.g. 25% of £119.98 = £29.995 → £30.00.
- **Discount sign.** The cart and confirmation prefix the discount with an en dash (–), the
  checkout with a minus sign (−). Cosmetic, but it suggests three hand-rolled renderings.

## Testability notes

The web app has no `data-testid` hooks (the repository's only commit added `testID`s to the
*mobile* app). The suite copes with role, label and text locators, with two exceptions worth fixing
in the product:

- Checkout inputs have `<label>` elements that are not associated with their inputs (no
  `htmlFor`/`id`), so they are neither accessible nor locatable by label. The suite uses
  placeholders instead.
- Order-summary rows are anonymous `div`/`span` pairs. Rows are located by their visible label.
  A `data-testid="summary-total"` (or a `<dl>`) would make this robust to restyling.

Toasts disappear after 3.2 s; every message assertion is web-first (`Expect(...)`) for that reason.

## Suggested next steps for the release

1. Fix D1 in a single shared pricing module and add unit tests for AC-2/3/4 there.
2. Drive coupon feedback from the API response (`couponApplied`) and give D2/D3 real messages.
3. Decide D4, D5 and the case-sensitivity question with the product owner and update the ACs.
4. Add `bcryptjs` to `web/package.json` or drop the standalone mock's dependency on it.
