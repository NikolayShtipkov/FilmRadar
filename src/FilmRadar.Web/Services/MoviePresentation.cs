using System.Text.RegularExpressions;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.ViewModels;
using Microsoft.Extensions.Options;

namespace FilmRadar.Web.Services;

public sealed partial class MoviePresentation(IOptions<TmdbOptions> options)
{
    public string Poster(string? path) => path != null && PosterPattern().IsMatch(path)
        ? options.Value.ImageBaseUrl.TrimEnd('/') + path : "/images/poster-placeholder.svg";
    public static MovieCard Card(TmdbMovieSummaryDto dto, LocalMovie? state = null) =>
        new(dto.Id, dto.Title, dto.PosterPath, dto.ParsedDate, dto.VoteAverage, state?.Id, state?.Status, state?.Rating, state?.IsFavorite ?? false);
    public static MovieCard Card(LocalMovie movie) => new(movie.TmdbId, movie.Title, movie.PosterPath,
        movie.ReleaseDate, movie.TmdbRating, movie.Id, movie.Status, movie.Rating, movie.IsFavorite);
    [GeneratedRegex(@"^/[A-Za-z0-9_-]+\.(jpg|png|webp)$", RegexOptions.IgnoreCase)] private static partial Regex PosterPattern();
}
