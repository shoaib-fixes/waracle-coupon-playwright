using System.Text.RegularExpressions;
using Reqnroll;

namespace WaracleStore.CouponTests.Support.Data;

public sealed record BasketLine(Product Product, int Quantity, string Size)
{
    public decimal LineTotal => Product.Price * Quantity;
}

/// <summary>
/// Turns the two Gherkin basket notations into <see cref="BasketLine"/>s:
/// a data table (Product | Quantity | Size) or the compact string "2 x Waracle Headset, 1 x Waracle Cap".
/// </summary>
public static partial class BasketParser
{
    public static IReadOnlyList<BasketLine> FromTable(Table table)
    {
        var lines = new List<BasketLine>();
        foreach (var row in table.Rows)
        {
            var product = Catalogue.ByName(row["Product"]);
            var quantity = row.ContainsKey("Quantity") ? int.Parse(row["Quantity"]) : 1;
            var size = row.ContainsKey("Size") && !string.IsNullOrWhiteSpace(row["Size"]) ? row["Size"] : product.DefaultSize;
            lines.Add(new BasketLine(product, quantity, size));
        }

        return lines;
    }

    public static IReadOnlyList<BasketLine> FromText(string basket)
    {
        if (string.IsNullOrWhiteSpace(basket))
            return [];

        return basket.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseEntry)
            .ToList();
    }

    private static BasketLine ParseEntry(string entry)
    {
        var match = EntryPattern().Match(entry);
        if (!match.Success)
            throw new FormatException($"'{entry}' is not in the form '<quantity> x <product name>'.");

        var product = Catalogue.ByName(match.Groups["name"].Value);
        return new BasketLine(product, int.Parse(match.Groups["qty"].Value), product.DefaultSize);
    }

    [GeneratedRegex(@"^(?<qty>\d+)\s*[x×]\s*(?<name>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex EntryPattern();
}
