using FilmRadar.Web.Data;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.Services.Recommendations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllersWithViews(o =>
{
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    o.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(name => $"Полето {name} трябва да е число.");
    o.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, name) => $"Невалидна стойност за {name}.");
});
builder.Services.AddSingleton(TimeProvider.System);
var dataDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(dataDirectory);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
var connection = builder.Configuration.GetConnectionString("FilmRadar")
    ?? new SqliteConnectionStringBuilder { DataSource = Path.Combine(dataDirectory, "filmradar.db"), ForeignKeys = true }.ToString();
builder.Services.AddDbContext<FilmRadarDbContext>(o => o.UseSqlite(connection));
builder.Services.AddOptions<TmdbOptions>().BindConfiguration("Tmdb")
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https", "TMDB BaseUrl must use HTTPS.")
    .Validate(o => Uri.TryCreate(o.ImageBaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https", "TMDB ImageBaseUrl must use HTTPS.")
    .Validate(o => o.TimeoutSeconds is >= 1 and <= 60, "TMDB timeout must be between 1 and 60 seconds.").ValidateOnStart();
builder.Services.AddHttpClient<ITmdbClient, TmdbClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<TmdbOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).RemoveAllLoggers();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddScoped<IGenreCatalogService, GenreCatalogService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IUserMovieService, UserMovieService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddSingleton<IRecommendationEngine, MockRecommendationEngine>();
builder.Services.AddSingleton<MoviePresentation>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsDevelopment())
        await scope.ServiceProvider.GetRequiredService<FilmRadarDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Demo:Seed"))
        await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(CancellationToken.None);
}
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program { }
