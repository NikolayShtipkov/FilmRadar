using FilmRadar.Web.Configuration;
using Microsoft.Extensions.Configuration;

namespace FilmRadar.Tests;

public class LocalConfigurationTests
{
    [Theory]
    [InlineData(null, "existing-token")]
    [InlineData("", "existing-token")]
    [InlineData("local-token", "local-token")]
    public void OptionalLocalFilePreservesExistingValuesOrOverridesWithNonemptyToken(string? localToken, string expected)
    {
        var directory = Path.Combine(Path.GetTempPath(), "FilmRadar-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "appsettings.Local.json");
        try
        {
            if (localToken != null)
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new { Tmdb = new { ReadAccessToken = localToken } }));
            using var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Tmdb:ReadAccessToken"] = "existing-token" });
            configuration.AddLocalSettings(directory, []);
            Assert.Equal(expected, configuration["Tmdb:ReadAccessToken"]);

            configuration.AddLocalSettings(directory, ["--Tmdb:ReadAccessToken=command-line-token"]);
            Assert.Equal("command-line-token", configuration["Tmdb:ReadAccessToken"]);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            Directory.Delete(directory);
        }
    }
}
