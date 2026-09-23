namespace WaracleStore.CouponTests.Support.Data;

/// <summary>The web app's <c>localStorage</c> keys, seeded by scenarios so they start where the feature starts.</summary>
public static class StorageKeys
{
    /// <summary>JSON array of cart lines (<c>CartItem[]</c> in the app).</summary>
    public const string Cart = "waracle_cart";

    /// <summary>The customer's JWT; its presence is what makes the app treat the session as signed in.</summary>
    public const string Token = "waracle_token";
}
