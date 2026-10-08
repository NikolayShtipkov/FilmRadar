using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FilmRadar.Web.Controllers;

public sealed class MyListController(IUserMovieService movies) : Controller
{
    public async Task<IActionResult> Index(string filter = "all", string sort = "recent", CancellationToken ct = default)
    {
        if (filter is not ("all" or "watchlist" or "watched" or "favorites") || sort is not ("recent" or "title" or "rating")) return BadRequest();
        return View(new ListPage((await movies.ListAsync(filter, sort, ct)).Select(MoviePresentation.Card).ToArray(), filter, sort));
    }
    [HttpPost]
    public async Task<IActionResult> Add(int tmdbId, CancellationToken ct)
    {
        try
        {
            var result = await movies.AddAsync(tmdbId, ct);
            if (result.Status == MutationStatus.NotFound) return NotFound();
            if (result.Status == MutationStatus.Invalid) return BadRequest();
            TempData["Message"] = result.Status == MutationStatus.Duplicate ? "Филмът вече е в списъка ви." : "Добавен в „За гледане“.";
        }
        catch (TmdbApiException ex) { TempData["Error"] = ex.UserMessage; }
        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var movie = await movies.GetAsync(id, ct);
        if (movie is null) return NotFound();
        return View(new EditPage
        {
            Id = id,
            Title = movie.Title,
            WasWatched = movie.Status == WatchStatus.Watched,
            Input = new EditMovieInput
            {
                StatusValue = (int)movie.Status,
                PersonalRatingValue = (int?)movie.Rating,
                IsFavorite = movie.IsFavorite,
                Notes = movie.Notes,
                WatchedOn = movie.WatchedOn
            }
        });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] EditMovieInput input, CancellationToken ct)
    {
        var movie = await movies.GetAsync(id, ct);
        if (movie is null) return NotFound();
        if (ModelState.IsValid)
        {
            var result = await movies.UpdateAsync(id, new(input.StatusValue!.Value, input.PersonalRatingValue,
                input.IsFavorite, input.Notes, input.WatchedOn, input.ConfirmReset), ct);
            if (result.Status == MutationStatus.NotFound) return NotFound();
            if (result.Status == MutationStatus.Success) { TempData["Message"] = "Промените са запазени."; return RedirectToAction(nameof(Index)); }
            ModelState.AddModelError("", result.Error!);
        }
        return View(new EditPage { Id = id, Title = movie.Title, WasWatched = movie.Status == WatchStatus.Watched, Input = input });
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var movie = await movies.GetAsync(id, ct);
        return movie is null ? NotFound() : View(MoviePresentation.Card(movie));
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        if ((await movies.DeleteAsync(id, ct)).Status == MutationStatus.NotFound) return NotFound();
        TempData["Message"] = "Филмът е премахнат от личния списък.";
        return RedirectToAction(nameof(Index));
    }
}
