using System.Net;
using System.Text;
using FilmRadar.Web.Integrations.Tmdb;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FilmRadar.Tests;

public class TmdbClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request, ct); }
    private static TmdbClient Create(Handler handler, string? token = "test-token") => new(
        new HttpClient(handler) { BaseAddress = new("https://api.themoviedb.org/3/") },
        Options.Create(new TmdbOptions { ReadAccessToken = token }), NullLogger<TmdbClient>.Instance);
    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SearchUsesDedicatedEndpointAndEncodesTitle()
    {
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("/3/search/movie", request.RequestUri!.AbsolutePath);
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal("A & B / C", query["query"]); Assert.Equal("2020", query["primary_release_year"]);
            Assert.Equal("bg-BG", query["language"]); Assert.Equal("false", query["include_adult"]);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal("test-token", request.Headers.Authorization.Parameter);
            Assert.DoesNotContain("test-token", request.RequestUri.ToString());
            return Task.FromResult(Json("{\"page\":1,\"total_pages\":1,\"results\":[]}"));
        });
        await Create(handler).SearchMoviesAsync(new("A & B / C", 2020), default);
    }
    [Fact]
    public async Task DiscoverUsesOrGenresAndRemoteBounds()
    {
        using var handler = new Handler((request, _) =>
        {
            Assert.Equal("/3/discover/movie", request.RequestUri!.AbsolutePath);
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal("18|35", query["with_genres"]); Assert.Equal("2000-01-01", query["primary_release_date.gte"]);
            Assert.Equal("2025-12-31", query["primary_release_date.lte"]); Assert.Equal("7.5", query["vote_average.gte"]);
            Assert.Equal("2", query["page"]); Assert.False(query.ContainsKey("query"));
            return Task.FromResult(Json("{\"page\":2,\"total_pages\":3,\"results\":[]}"));
        });
        await Create(handler).DiscoverMoviesAsync(new([35, 18, 35], 2000, 2025, 7.5, Page: 2), default);
    }
    [Fact]
    public async Task MissingTokenNeverMakesNetworkRequest()
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Unexpected request"));
        var error = await Assert.ThrowsAsync<TmdbApiException>(() => Create(handler, null).GetPopularMoviesAsync(1, default));
        Assert.Equal(TmdbErrorKind.Configuration, error.Kind);
    }
    [Theory]
    [InlineData(401, TmdbErrorKind.Unauthorized)]
    [InlineData(403, TmdbErrorKind.Unauthorized)]
    [InlineData(429, TmdbErrorKind.RateLimited)]
    [InlineData(500, TmdbErrorKind.Unavailable)]
    public async Task HttpFailuresHaveSafeMessages(int status, TmdbErrorKind expected)
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        var error = await Assert.ThrowsAsync<TmdbApiException>(() => Create(handler).GetPopularMoviesAsync(1, default));
        Assert.Equal(expected, error.Kind); Assert.DoesNotContain("test-token", error.UserMessage);
    }
    [Fact]
    public async Task Detail404ReturnsNull()
    {
        using var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        Assert.Null(await Create(handler).GetMovieDetailsAsync(404, default));
    }
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"page\":1,\"results\":null}")]
    [InlineData("{\"page\":1,\"results\":[null]}")]
    public async Task MalformedPayloadMapsToControlledError(string json)
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json(json)));
        Assert.Equal(TmdbErrorKind.InvalidPayload, (await Assert.ThrowsAsync<TmdbApiException>(() => Create(handler).GetPopularMoviesAsync(1, default))).Kind);
    }
    [Fact]
    public async Task NetworkFailureMapsToUnavailable()
    {
        using var handler = new Handler((_, _) => throw new HttpRequestException("private infrastructure detail"));
        Assert.Equal(TmdbErrorKind.Unavailable, (await Assert.ThrowsAsync<TmdbApiException>(() => Create(handler).GetPopularMoviesAsync(1, default))).Kind);
    }
    [Fact]
    public async Task TimeoutMapsToUnavailable()
    {
        using var handler = new Handler((_, _) => throw new TaskCanceledException());
        Assert.Equal(TmdbErrorKind.Unavailable, (await Assert.ThrowsAsync<TmdbApiException>(() => Create(handler).GetPopularMoviesAsync(1, default))).Kind);
    }
    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        using var handler = new Handler((_, _) => throw new InvalidOperationException());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Create(handler).GetPopularMoviesAsync(1, cts.Token));
    }
    [Fact]
    public async Task EmptyDatesAndMissingPosterAreValid()
    {
        using var handler = new Handler((_, _) => Task.FromResult(Json("{\"id\":1,\"title\":\"Movie\",\"release_date\":\"\",\"genres\":[]}")));
        var details = await Create(handler).GetMovieDetailsAsync(1, default);
        Assert.Null(details!.ParsedDate); Assert.Null(details.PosterPath);
    }
}
