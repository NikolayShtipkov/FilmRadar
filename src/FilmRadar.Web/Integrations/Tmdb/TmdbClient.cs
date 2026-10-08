using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace FilmRadar.Web.Integrations.Tmdb;

public sealed class TmdbClient(HttpClient http, IOptions<TmdbOptions> options, ILogger<TmdbClient> logger) : ITmdbClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private Dictionary<string, string?> Common(int page = 1) => new()
    {
        ["language"] = options.Value.Language,
        ["region"] = options.Value.Region,
        ["include_adult"] = "false",
        ["page"] = Math.Clamp(page, 1, 500).ToString(CultureInfo.InvariantCulture)
    };

    public async Task<TmdbPagedResult<TmdbMovieSummaryDto>> GetPopularMoviesAsync(int page, CancellationToken ct) =>
        ValidatePage(await GetAsync<TmdbPagedResult<TmdbMovieSummaryDto>>("movie/popular", Common(page), false, ct));

    public async Task<TmdbPagedResult<TmdbMovieSummaryDto>> SearchMoviesAsync(TmdbSearchRequest request, CancellationToken ct)
    {
        var query = Common(request.Page);
        query["query"] = request.Query;
        if (request.Year is not null) query["primary_release_year"] = request.Year.Value.ToString(CultureInfo.InvariantCulture);
        return ValidatePage(await GetAsync<TmdbPagedResult<TmdbMovieSummaryDto>>("search/movie", query, false, ct));
    }

    public async Task<TmdbPagedResult<TmdbMovieSummaryDto>> DiscoverMoviesAsync(TmdbDiscoverRequest request, CancellationToken ct)
    {
        var query = Common(request.Page);
        query["sort_by"] = request.Sort;
        if (request.GenreIds.Count > 0) query["with_genres"] = string.Join('|', request.GenreIds.Distinct().Order());
        if (request.YearFrom is not null) query["primary_release_date.gte"] = $"{request.YearFrom:0000}-01-01";
        if (request.YearTo is not null) query["primary_release_date.lte"] = $"{request.YearTo:0000}-12-31";
        if (request.MinimumRating is not null) query["vote_average.gte"] = request.MinimumRating.Value.ToString(CultureInfo.InvariantCulture);
        return ValidatePage(await GetAsync<TmdbPagedResult<TmdbMovieSummaryDto>>("discover/movie", query, false, ct));
    }

    public async Task<TmdbMovieDetailsDto?> GetMovieDetailsAsync(int id, CancellationToken ct)
    {
        var movie = await GetAsync<TmdbMovieDetailsDto>($"movie/{id}", new() { ["language"] = options.Value.Language }, true, ct);
        if (movie != null && (movie.Id != id || string.IsNullOrWhiteSpace(movie.Title) || !ValidGenres(movie.Genres)))
            throw new TmdbApiException(TmdbErrorKind.InvalidPayload);
        return movie;
    }

    public async Task<IReadOnlyList<TmdbGenreDto>> GetMovieGenresAsync(CancellationToken ct)
    {
        var genres = (await GetAsync<TmdbGenresResponse>("genre/movie/list", new() { ["language"] = options.Value.Language }, false, ct))?.Genres;
        return ValidGenres(genres) ? genres! : throw new TmdbApiException(TmdbErrorKind.InvalidPayload);
    }

    private static bool ValidGenres(List<TmdbGenreDto>? genres) =>
        genres != null && genres.All(g => g != null && g.Id > 0 && !string.IsNullOrWhiteSpace(g.Name));

    private static TmdbPagedResult<TmdbMovieSummaryDto> ValidatePage(TmdbPagedResult<TmdbMovieSummaryDto>? page)
    {
        if (page is null || page.Page < 1 || page.TotalPages < 0 || page.Results is null ||
            page.Results.Any(m => m is null || m.Id < 1 || string.IsNullOrWhiteSpace(m.Title) || m.GenreIds is null))
            throw new TmdbApiException(TmdbErrorKind.InvalidPayload);
        return page;
    }

    private async Task<T?> GetAsync<T>(string path, Dictionary<string, string?> query, bool allowNotFound, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(options.Value.ReadAccessToken)) throw new TmdbApiException(TmdbErrorKind.Configuration);
        var stopwatch = Stopwatch.StartNew();
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString(path, query));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ReadAccessToken);
        try
        {
            using var response = await http.SendAsync(request, ct);
            logger.LogInformation("TMDB {Endpoint}: {StatusCode}, {ElapsedMs} ms", path, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
            if (response.StatusCode == HttpStatusCode.NotFound && allowNotFound) return default;
            if (!response.IsSuccessStatusCode) throw new TmdbApiException(response.StatusCode switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => TmdbErrorKind.Unauthorized,
                HttpStatusCode.TooManyRequests => TmdbErrorKind.RateLimited,
                _ => TmdbErrorKind.Unavailable
            });
            var result = await response.Content.ReadFromJsonAsync<T>(Json, ct);
            return result is null ? throw new TmdbApiException(TmdbErrorKind.InvalidPayload) : result;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TmdbApiException(TmdbErrorKind.Unavailable); }
        catch (HttpRequestException) { throw new TmdbApiException(TmdbErrorKind.Unavailable); }
        catch (JsonException) { throw new TmdbApiException(TmdbErrorKind.InvalidPayload); }
    }
}
