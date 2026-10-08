namespace FilmRadar.Web.Configuration;

public static class LocalConfigurationExtensions
{
    public static void AddLocalSettings(this ConfigurationManager configuration, string contentRoot, string[] args)
    {
        using var local = new ConfigurationManager();
        local.SetBasePath(contentRoot)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

        // Blank template values must not erase an existing User Secrets token.
        configuration.AddInMemoryCollection(local.AsEnumerable()
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value)));

        configuration.AddEnvironmentVariables();
        configuration.AddCommandLine(args);
    }
}
