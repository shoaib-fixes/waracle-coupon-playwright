using Microsoft.Extensions.Configuration;

namespace WaracleStore.CouponTests.Support.Config;

public static class SettingsLoader
{
    public const string EnvironmentPrefix = "TEST_";

    /// <summary>
    /// appsettings.json &lt; appsettings.Local.json (git-ignored) &lt; TEST_* environment variables.
    /// Later sources win, so CI can override anything without touching files.
    /// </summary>
    public static TestSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(EnvironmentPrefix)
            .Build();

        var settings = configuration.Get<TestSettings>()
                       ?? throw new InvalidOperationException("appsettings.json could not be bound to TestSettings.");

        settings.Validate();
        return settings;
    }
}
