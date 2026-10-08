using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FilmRadar.Web.Controllers;

public sealed class HomeController(ITmdbClient tmdb, IUserMovieService movies) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var page = new HomePage();
        try
        {
            var results = (await tmdb.GetPopularMoviesAsync(1, ct)).Results.Where(m => !m.Adult).Take(8).ToArray();
            var states = await movies.GetStatesAsync(results.Select(m => m.Id), ct);
            page.Movies = results.Select(m => MoviePresentation.Card(m, states.GetValueOrDefault(m.Id))).ToArray();
        }
        catch (TmdbApiException ex) { page.Error = ex.UserMessage; }
        return View(page);
    }
    public IActionResult About() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("Problem", new ProblemPage("Възникна неочаквана грешка. Опитайте отново по-късно.", 500));
    public IActionResult Status(int code) => View("Problem", new ProblemPage(code == 404 ? "Страницата или филмът не е намерен." : "Заявката не може да бъде изпълнена.", code));
}
