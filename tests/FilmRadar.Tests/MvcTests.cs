using System.Net;
using System.Text.RegularExpressions;
using FilmRadar.Web.Data;
using FilmRadar.Web.Integrations.Tmdb;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FilmRadar.Tests;

public sealed class FilmRadarFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    internal FakeTmdb Tmdb { get; } = new();
    public FilmRadarFactory() => connection.Open();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<FilmRadarDbContext>(); services.RemoveAll<DbContextOptions<FilmRadarDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<FilmRadarDbContext>>();
            services.AddDbContext<FilmRadarDbContext>(o => o.UseSqlite(connection));
            services.RemoveAll<ITmdbClient>(); services.AddSingleton<ITmdbClient>(Tmdb);
        });
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}

public class MvcTests
{
    private static HttpClient Client(FilmRadarFactory factory) => factory.CreateClient(new() { AllowAutoRedirect = false });
    private static async Task<string> Token(HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Expected an antiforgery form token."); return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    [Theory]
    [InlineData("/")]
    [InlineData("/Movies/Search")]
    [InlineData("/Movies/Search?mode=discover")]
    [InlineData("/MyList")]
    [InlineData("/Profile/Edit")]
    [InlineData("/Recommendations")]
    [InlineData("/Recommendations?Input.Generate=true&Input.Mood=1")]
    [InlineData("/Home/About")]
    [InlineData("/Movies/Details/101")]
    public async Task MainPagesRender(string path)
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        var response = await client.GetAsync(path); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("FilmRadar", await response.Content.ReadAsStringAsync());
    }
    [Theory]
    [InlineData("/MyList/Add")]
    [InlineData("/MyList/AddWatched")]
    [InlineData("/MyList/Delete/1")]
    [InlineData("/MyList/Edit/1")]
    [InlineData("/Profile/Edit")]
    [InlineData("/Profile/RefreshGenres")]
    public async Task PostWithoutAntiforgeryIsRejected(string path)
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent([]))).StatusCode);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AddWatchedOpensEditorAndPreservesExistingPersonalData(int existingStatus)
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        using var scope = factory.Services.CreateScope();
        var movies = scope.ServiceProvider.GetRequiredService<FilmRadar.Web.Services.IUserMovieService>();
        var db = scope.ServiceProvider.GetRequiredService<FilmRadarDbContext>();
        if (existingStatus > 0)
        {
            var id = (await movies.AddAsync(101, default)).Id!.Value;
            await movies.UpdateAsync(id, new(existingStatus, existingStatus == 2 ? 4 : null, true, "Keep my note", existingStatus == 2 ? new DateOnly(2020, 1, 1) : null), default);
        }
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Movies/Details/101"));
        if (existingStatus < 2) Assert.Contains("action=\"/MyList/AddWatched\"", html);
        var token = await Token(client, existingStatus == 2 ? "/Profile/Edit" : "/Movies/Details/101");
        var response = await client.PostAsync("/MyList/AddWatched", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["tmdbId"] = "101"
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var movie = await db.SavedMovies.AsNoTracking().SingleAsync();
        Assert.Equal(FilmRadar.Web.Domain.WatchStatus.Watched, movie.Status);
        Assert.NotNull(movie.WatchedOn);
        Assert.Equal($"/MyList/Edit/{movie.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(response.Headers.Location)).StatusCode);
        if (existingStatus > 0)
        {
            Assert.True(movie.IsFavorite); Assert.Equal("Keep my note", movie.Notes);
        }
        Assert.Equal(existingStatus == 2 ? 4 : (int?)null, (int?)movie.PersonalRating);
        if (existingStatus == 2) Assert.Equal(new DateOnly(2020, 1, 1), movie.WatchedOn);
    }

    [Fact]
    public async Task BrowserFlowAddsRatesZeroConfirmsResetAndDeletes()
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        var token = await Token(client, "/Movies/Details/101");
        var added = await client.PostAsync("/MyList/Add", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["tmdbId"] = "101" }));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<FilmRadarDbContext>();
        var id = (await db.SavedMovies.AsNoTracking().SingleAsync()).Id;
        token = await Token(client, $"/MyList/Edit/{id}");
        var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Input.StatusValue"] = "2", ["Input.PersonalRatingValue"] = "0", ["Input.Notes"] = "<script>alert('x')</script>" };
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/MyList/Edit/{id}", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.Equal(0, (int?)(await db.SavedMovies.AsNoTracking().SingleAsync()).PersonalRating);
        var editHtml = await client.GetStringAsync($"/MyList/Edit/{id}");
        Assert.DoesNotContain("<script>alert('x')</script>", editHtml); Assert.Contains("&lt;script&gt;", editHtml);
        fields["Input.StatusValue"] = "1";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/MyList/Edit/{id}", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.NotNull((await db.SavedMovies.AsNoTracking().SingleAsync()).PersonalRating);
        fields["Input.ConfirmReset"] = "true";
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/MyList/Edit/{id}", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.Null((await db.SavedMovies.AsNoTracking().SingleAsync()).PersonalRating);
        token = await Token(client, $"/MyList/Delete/{id}");
        Assert.Single(await db.SavedMovies.AsNoTracking().ToListAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/MyList/Delete/{id}", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }))).StatusCode);
        Assert.Empty(await db.SavedMovies.ToListAsync());
    }
    [Theory]
    [InlineData("/Movies/Search?Query=abc&Page=501")]
    [InlineData("/Movies/Search?Query=abc&Year=bad")]
    [InlineData("/Movies/Search?Query=abc&GenreId=35")]
    [InlineData("/Movies/Search?mode=discover&Query=abc")]
    [InlineData("/Movies/Search?mode=discover&GenreId=99999")]
    [InlineData("/Movies/Search?mode=discover&YearFrom=2025&YearTo=2000")]
    public async Task InvalidSearchNeverCallsProvider(string path)
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.Empty(factory.Tmdb.DiscoverCalls);
    }
    [Fact]
    public async Task TokenlessModeKeepsLocalPagesAvailable()
    {
        using var factory = new FilmRadarFactory(); factory.Tmdb.Failure = new(TmdbErrorKind.Configuration);
        using var client = Client(factory);
        foreach (var path in new[] { "/", "/MyList", "/Profile/Edit", "/Recommendations" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Movies/Search?Query=example"));
        Assert.Contains("Каталогът още не е свързан", html);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/Movies/Details/101")).StatusCode);
    }
    [Fact]
    public async Task InvalidProfileKeepsInputAndExistingDatabaseValues()
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        var token = await Token(client, "/Profile/Edit");
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.DisplayName"] = "Preserve this name",
            ["Input.YearFrom"] = "2025",
            ["Input.YearTo"] = "2000"
        };
        var response = await client.PostAsync("/Profile/Edit", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Preserve this name", await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        Assert.Equal("Киноман", (await scope.ServiceProvider.GetRequiredService<FilmRadarDbContext>().Profiles.SingleAsync()).DisplayName);
    }
    [Fact]
    public async Task UnknownMovieAndLocalIdReturn404()
    {
        using var factory = new FilmRadarFactory(); using var client = Client(factory);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Movies/Details/404")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/MyList/Edit/999")).StatusCode);
    }
}
