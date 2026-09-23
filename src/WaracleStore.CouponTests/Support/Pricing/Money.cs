using System.Globalization;
using System.Text.RegularExpressions;

namespace WaracleStore.CouponTests.Support.Pricing;

/// <summary>
/// GBP display helpers. The app renders amounts with <c>Intl.NumberFormat('en-GB')</c>
/// and prefixes discounts with an en dash on some pages and a minus sign on others,
/// so parsing tolerates every sign glyph and never depends on the machine culture.
/// </summary>
public static partial class Money
{
    private static readonly CultureInfo Gbp = CultureInfo.GetCultureInfo("en-GB");

    public static string Format(decimal amount) => amount.ToString("C2", Gbp);

    /// <summary>Parses text such as "£1,234.56", "–£30.00" or "−£5.00" into a signed decimal.</summary>
    public static decimal Parse(string text)
    {
        var match = AmountPattern().Match(text);
        if (!match.Success)
            throw new FormatException($"'{text}' does not contain a GBP amount.");

        var negative = match.Groups["sign"].Success;
        var magnitude = decimal.Parse(match.Groups["amount"].Value.Replace(",", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture);
        return negative ? -magnitude : magnitude;
    }

    [GeneratedRegex(@"(?<sign>[-–−])?\s*£\s*(?<amount>\d[\d,]*(?:\.\d{2})?)")]
    private static partial Regex AmountPattern();
}
