using FilmRadar.Web.Data;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Tests;

public class PersistenceTests
{
    [Fact]
    public async Task MigrationsAndInitializationAreIdempotent()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        await fixture.Db.Database.MigrateAsync();
        await new DatabaseInitializer(fixture.Db, TimeProvider.System).InitializeAsync(default);
        Assert.Single(await fixture.Db.Profiles.ToListAsync()); Assert.Equal(19, await fixture.Db.Genres.CountAsync());
        Assert.False(fixture.Db.Database.HasPendingModelChanges());
    }
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task WatchedRatingRoundTripsIncludingZeroAndNull(int? rating)
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var service = new UserMovieService(fixture.Db, new FakeTmdb(), TimeProvider.System);
        var added = await service.AddAsync(101, default);
        var updated = await service.UpdateAsync(added.Id!.Value, new(2, rating, true, "  note  ", null), default);
        fixture.Db.ChangeTracker.Clear();
        var movie = await service.GetAsync(added.Id.Value, default);
        Assert.Equal(MutationStatus.Success, updated.Status); Assert.Equal(rating, (int?)movie!.Rating);
        Assert.NotNull(movie.WatchedOn); Assert.Equal("note", movie.Notes); Assert.True(movie.IsFavorite);
        Assert.Equal(rating.HasValue ? 1 : 0, (await service.GetHistoryAsync(default)).Count);
    }
    [Fact]
    public async Task ResetNeedsConfirmationAndClearsOnlyRatingAndDate()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        var service = new UserMovieService(fixture.Db, new FakeTmdb(), TimeProvider.System);
        var id = (await service.AddAsync(101, default)).Id!.Value;
        await service.UpdateAsync(id, new(2, 4, true, "keep", null), default);
        Assert.Equal(MutationStatus.Invalid, (await service.UpdateAsync(id, new(1, null, true, "keep", null), default)).Status);
        Assert.Equal(PersonalRating.Top, (await service.GetAsync(id, default))!.Rating);
        await service.UpdateAsync(id, new(1, 4, true, "keep", null, true), default);
        var movie = (await service.GetAsync(id, default))!;
        Assert.Null(movie.Rating); Assert.Null(movie.WatchedOn); Assert.True(movie.IsFavorite); Assert.Equal("keep", movie.Notes);
    }
    [Fact]
    public async Task DuplicateAddDoesNotFetchDetailsAgain()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var tmdb = new FakeTmdb();
        var service = new UserMovieService(fixture.Db, tmdb, TimeProvider.System);
        var first = await service.AddAsync(101, default); var second = await service.AddAsync(101, default);
        Assert.Equal(MutationStatus.Duplicate, second.Status); Assert.Equal(first.Id, second.Id); Assert.Equal(1, tmdb.DetailCalls);
        Assert.Single(await fixture.Db.SavedMovies.ToListAsync());
    }
    [Theory]
    [InlineData(0, null)]
    [InlineData(3, null)]
    [InlineData(2, -1)]
    [InlineData(2, 5)]
    public async Task InvalidEnumsCannotBeSaved(int status, int? rating)
    {
        await using var fixture = await TestDatabase.CreateAsync(); var service = new UserMovieService(fixture.Db, new FakeTmdb(), TimeProvider.System);
        var id = (await service.AddAsync(101, default)).Id!.Value;
        Assert.Equal(MutationStatus.Invalid, (await service.UpdateAsync(id, new(status, rating, false, null, null), default)).Status);
    }
    [Fact]
    public async Task DatabaseRejectsRatingOnWatchlistEvenWithoutService()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        fixture.Db.SavedMovies.Add(new() { UserProfileId = 1, TmdbMovieId = 1, Title = "Bad", Status = WatchStatus.Watchlist, PersonalRating = PersonalRating.Terrible });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }
    [Fact]
    public async Task UniqueMovieConstraintIsEnforced()
    {
        await using var fixture = await TestDatabase.CreateAsync();
        for (var i = 0; i < 2; i++) fixture.Db.SavedMovies.Add(new() { UserProfileId = 1, TmdbMovieId = 1, Title = "Duplicate" });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
        fixture.Db.ChangeTracker.Clear(); Assert.Empty(await fixture.Db.SavedMovies.ToListAsync());
    }
    [Fact]
    public async Task DeleteCascadesJoinRowsButPreservesGenres()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var service = new UserMovieService(fixture.Db, new FakeTmdb(), TimeProvider.System);
        var id = (await service.AddAsync(101, default)).Id!.Value;
        await service.DeleteAsync(id, default);
        Assert.Empty(await fixture.Db.Set<SavedMovieGenre>().ToListAsync()); Assert.Equal(19, await fixture.Db.Genres.CountAsync());
    }
    [Fact]
    public async Task ProfileReplacesPreferencesAndRejectsUnknownGenres()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var service = new ProfileService(fixture.Db, TimeProvider.System);
        await service.UpdateAsync(new(" Film Fan ", [35, 18, 35], 2000, null, 7), default);
        await service.UpdateAsync(new("Film Fan", [878], null, 2025, null), default);
        fixture.Db.ChangeTracker.Clear(); var profile = await service.GetAsync(default);
        Assert.Equal([878], profile.GenreIds); Assert.Null(profile.YearFrom); Assert.Equal(2025, profile.YearTo);
        Assert.Equal(MutationStatus.Invalid, (await service.UpdateAsync(new("Film Fan", [999999], null, null, null), default)).Status);
        Assert.Equal([878], (await service.GetAsync(default)).GenreIds);
    }
    [Fact]
    public async Task GenreRefreshUpsertsWithoutRemovingBaselineAndSurvivesFailure()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var tmdb = new FakeTmdb();
        var service = new GenreCatalogService(fixture.Db, tmdb, TimeProvider.System);
        Assert.True(await service.RefreshAsync(default)); Assert.Equal(20, (await service.GetAsync(default)).Count);
        tmdb.Failure = new(TmdbErrorKind.Unavailable);
        Assert.False(await service.RefreshAsync(default)); Assert.Equal(20, (await service.GetAsync(default)).Count);
    }
    [Fact]
    public async Task FutureWatchDateAndOversizedNotesAreRejected()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var service = new UserMovieService(fixture.Db, new FakeTmdb(), TimeProvider.System);
        var id = (await service.AddAsync(101, default)).Id!.Value;
        Assert.Equal(MutationStatus.Invalid, (await service.UpdateAsync(id, new(2, 0, false, null, new DateOnly(2100, 1, 1)), default)).Status);
        Assert.Equal(MutationStatus.Invalid, (await service.UpdateAsync(id, new(1, null, false, new string('x', 1001), null), default)).Status);
    }
    [Fact]
    public async Task DemoSeedIsIdempotentAndIncludesZero()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var seeder = new DemoSeeder(fixture.Db, TimeProvider.System);
        await seeder.SeedAsync(default); await seeder.SeedAsync(default);
        Assert.Equal(6, await fixture.Db.SavedMovies.CountAsync());
        Assert.Contains(await fixture.Db.SavedMovies.ToListAsync(), m => m.PersonalRating == PersonalRating.Terrible);
    }
}
