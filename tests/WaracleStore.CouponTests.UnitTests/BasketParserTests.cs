using Shouldly;
using WaracleStore.CouponTests.Support.Data;

namespace WaracleStore.CouponTests.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public sealed class BasketParserTests
{
    [Test]
    public void Parses_compact_notation_with_quantities()
    {
        var lines = BasketParser.FromText("2 x Waracle Headset, 1 x Waracle Cap");

        lines.Count.ShouldBe(2);
        lines[0].Product.Id.ShouldBe("p-headset");
        lines[0].Quantity.ShouldBe(2);
        lines[0].Size.ShouldBe("One Size");
        lines[1].Product.Id.ShouldBe("p-cap");
        lines[1].LineTotal.ShouldBe(19.99m);
    }

    [Test]
    public void Product_names_are_matched_case_insensitively_and_with_curly_apostrophes()
    {
        BasketParser.FromText("1 x men’s logo t-shirt")[0].Product.Id.ShouldBe("p-mens-tee");
    }

    [Test]
    public void Empty_text_is_an_empty_basket() => BasketParser.FromText("").ShouldBeEmpty();

    [Test]
    public void Unknown_product_names_fail_with_the_known_names_listed()
    {
        var ex = Should.Throw<ArgumentException>(() => BasketParser.FromText("1 x Waracle Hoodie"));

        ex.Message.ShouldContain("Waracle Hoodie");
        ex.Message.ShouldContain("Waracle Cap");
    }

    [Test]
    public void Malformed_entries_are_rejected() =>
        Should.Throw<FormatException>(() => BasketParser.FromText("Waracle Cap"));

    [Test]
    public void Cart_seed_json_matches_the_apps_cart_item_shape()
    {
        var json = CartSeeder.ToStorageJson(BasketParser.FromText("1 x Waracle Cap"));

        json.ShouldBe("""[{"productId":"p-cap","name":"Waracle Cap","price":19.99,"image":"cap","size":"One Size","quantity":1}]""");
    }
}
