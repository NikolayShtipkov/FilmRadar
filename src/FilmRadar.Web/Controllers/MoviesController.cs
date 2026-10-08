using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FilmRadar.Web.Controllers;

public sealed class MoviesController(ITmdbClient tmdb, IUserMovieService movies, IGenreCatalogService genres) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Search(string mode = "title", CancellationToken ct = default)
    {
        var page = new BrowsePage { Mode = mode, Genres = await genres.GetAsync(ct) };
        if (mode is not ("title" or "discover")) { ModelState.AddModelError("", "Изберете режим за търсене."); page.Mode = "title"; return View(page); }
        TmdbPagedResult<TmdbMovieSummaryDto>? response = null;
        try
        {
            if (mode == "title")
            {
                await TryUpdateModelAsync(page.TitleInput, "");
                if (Request.Query.Keys.Any(k => new[] { "GenreId", "YearFrom", "YearTo", "MinimumRating", "Sort" }.Contains(k, StringComparer.OrdinalIgnoreCase)))
                    ModelState.AddModelError("", "Жанровите филтри са достъпни в режим „Откриване“.");
                page.Page = page.TitleInput.Page;
                page.Searched = Request.Query.ContainsKey("Query");
                if (page.Searched && string.IsNullOrWhiteSpace(page.TitleInput.Query)) ModelState.AddModelError("Query", "Въведете заглавие.");
                if (ModelState.IsValid && page.Searched)
                    response = await tmdb.SearchMoviesAsync(new(page.TitleInput.Query!.Trim(), page.TitleInput.Year, page.TitleInput.Page), ct);
            }
            else
            {
                await TryUpdateModelAsync(page.DiscoverInput, "");
                if (Request.Query.ContainsKey("Query") || Request.Query.ContainsKey("Year")) ModelState.AddModelError("", "Откриването използва филтри, без търсене по заглавие.");
                if (page.DiscoverInput.GenreId.HasValue && !page.Genres.Any(g => g.Id == page.DiscoverInput.GenreId)) ModelState.AddModelError("GenreId", "Непознат жанр.");
                page.Page = page.DiscoverInput.Page;
                page.Searched = true;
                if (ModelState.IsValid)
                    response = await tmdb.DiscoverMoviesAsync(new(page.DiscoverInput.GenreId is int id ? [id] : [],
                        page.DiscoverInput.YearFrom, page.DiscoverInput.YearTo, page.DiscoverInput.MinimumRating, page.DiscoverInput.Sort, page.Page), ct);
            }
            if (response != null)
            {
                page.TotalPages = Math.Min(response.TotalPages, 500);
                var states = await movies.GetStatesAsync(response.Results.Select(m => m.Id), ct);
                page.Movies = response.Results.Where(m => !m.Adult).Select(m => MoviePresentation.Card(m, states.GetValueOrDefault(m.Id))).ToArray();
                string PageUrl(int number)
                {
                    var query = Request.Query.ToDictionary(k => k.Key, v => (string?)v.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                    query["Page"] = number.ToString();
                    return QueryHelpers.AddQueryString(Url.Action(nameof(Search))!, query);
                }
                page.PreviousUrl = page.Page > 1 ? PageUrl(page.Page - 1) : null;
                page.NextUrl = page.Page < page.TotalPages ? PageUrl(page.Page + 1) : null;
            }
        }
        catch (TmdbApiException ex) { page.Error = ex.UserMessage; }
        return View(page);
    }

    [HttpGet("Movies/Details/{tmdbId:int}")]
    public async Task<IActionResult> Details(int tmdbId, CancellationToken ct)
    {
        if (tmdbId <= 0) return NotFound();
        try
        {
            var details = await tmdb.GetMovieDetailsAsync(tmdbId, ct);
            if (details is null || details.Adult) return NotFound();
            var states = await movies.GetStatesAsync([tmdbId], ct);
            return View(new DetailsPage(MoviePresentation.Card(details, states.GetValueOrDefault(tmdbId)), details.Overview,
                details.OriginalTitle, details.Runtime, details.Genres.Select(g => g.Name).ToArray()));
        }
        catch (TmdbApiException ex) { Response.StatusCode = 503; return View("Problem", new ProblemPage(ex.UserMessage)); }
    }
}
