namespace WaracleStore.CouponTests.Support;

/// <summary>Where this suite lives, for links in reports. Follows the repository when it is forked or renamed.</summary>
public static class Repository
{
    private const string DefaultUrl = "https://github.com/shoaib-fixes/waracle-coupon-playwright";

    public static string Url
    {
        get
        {
            var server = Environment.GetEnvironmentVariable("GITHUB_SERVER_URL");
            var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
            return string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(repository)
                ? DefaultUrl
                : $"{server.TrimEnd('/')}/{repository}";
        }
    }

    public static string ObservationsUrl => $"{Url}/blob/main/docs/ReleaseObservations.md";
}
