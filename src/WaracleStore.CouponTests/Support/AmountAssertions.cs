using Shouldly;
using WaracleStore.CouponTests.Support.Pricing;

namespace WaracleStore.CouponTests.Support;

/// <summary>
/// Compares several money amounts at once and reports every mismatch in one short message,
/// so a failing scenario reads like a diff of the order summary rather than a stack of asserts.
/// </summary>
public static class AmountAssertions
{
    public static void ShouldAllMatch(string context, params (string Name, decimal? Expected, decimal? Actual)[] amounts)
    {
        var mismatches = amounts
            .Where(a => a.Expected != a.Actual)
            .Select(a => $"{a.Name}: expected {Describe(a.Expected)}, actual {Describe(a.Actual)}")
            .ToList();

        if (mismatches.Count == 0)
            return;

        throw new ShouldAssertException(
            context + Environment.NewLine + string.Join(Environment.NewLine, mismatches.Select(m => "  - " + m)));
    }

    private static string Describe(decimal? amount) => amount is { } a ? Money.Format(a) : "none";
}
