using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmRadar.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    TmdbGenreId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.TmdbGenreId);
                    table.CheckConstraint("CK_Genre_Id", "TmdbGenreId > 0");
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PreferredReleaseYearFrom = table.Column<int>(type: "INTEGER", nullable: true),
                    PreferredReleaseYearTo = table.Column<int>(type: "INTEGER", nullable: true),
                    MinimumTmdbRating = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.CheckConstraint("CK_Profile_Id", "Id = 1");
                    table.CheckConstraint("CK_Profile_Name", "length(trim(DisplayName)) BETWEEN 2 AND 50");
                    table.CheckConstraint("CK_Profile_Rating", "MinimumTmdbRating IS NULL OR MinimumTmdbRating BETWEEN 0 AND 10");
                    table.CheckConstraint("CK_Profile_Years", "(PreferredReleaseYearFrom IS NULL OR PreferredReleaseYearFrom BETWEEN 1888 AND 2100) AND (PreferredReleaseYearTo IS NULL OR PreferredReleaseYearTo BETWEEN 1888 AND 2100) AND (PreferredReleaseYearFrom IS NULL OR PreferredReleaseYearTo IS NULL OR PreferredReleaseYearFrom <= PreferredReleaseYearTo)");
                });

            migrationBuilder.CreateTable(
                name: "SavedMovies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmdbMovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OriginalTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PosterPath = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TmdbVoteAverage = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    PersonalRating = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AddedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WatchedOn = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMovies", x => x.Id);
                    table.CheckConstraint("CK_Movie_Id", "TmdbMovieId > 0");
                    table.CheckConstraint("CK_Movie_Notes", "Notes IS NULL OR length(Notes) <= 1000");
                    table.CheckConstraint("CK_Movie_Rating", "PersonalRating IS NULL OR PersonalRating BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Movie_State", "(Status = 1 AND PersonalRating IS NULL AND WatchedOn IS NULL) OR (Status = 2 AND WatchedOn IS NOT NULL)");
                    table.CheckConstraint("CK_Movie_Status", "Status IN (1, 2)");
                    table.CheckConstraint("CK_Movie_Tmdb", "TmdbVoteAverage IS NULL OR TmdbVoteAverage BETWEEN 0 AND 10");
                    table.ForeignKey(
                        name: "FK_SavedMovies_Profiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferredGenre",
                columns: table => new
                {
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmdbGenreId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferredGenre", x => new { x.UserProfileId, x.TmdbGenreId });
                    table.ForeignKey(
                        name: "FK_UserPreferredGenre_Genres_TmdbGenreId",
                        column: x => x.TmdbGenreId,
                        principalTable: "Genres",
                        principalColumn: "TmdbGenreId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPreferredGenre_Profiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedMovieGenre",
                columns: table => new
                {
                    SavedMovieId = table.Column<int>(type: "INTEGER", nullable: false),
                    TmdbGenreId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedMovieGenre", x => new { x.SavedMovieId, x.TmdbGenreId });
                    table.ForeignKey(
                        name: "FK_SavedMovieGenre_Genres_TmdbGenreId",
                        column: x => x.TmdbGenreId,
                        principalTable: "Genres",
                        principalColumn: "TmdbGenreId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedMovieGenre_SavedMovies_SavedMovieId",
                        column: x => x.SavedMovieId,
                        principalTable: "SavedMovies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedMovieGenre_TmdbGenreId",
                table: "SavedMovieGenre",
                column: "TmdbGenreId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedMovies_UserProfileId_TmdbMovieId",
                table: "SavedMovies",
                columns: new[] { "UserProfileId", "TmdbMovieId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferredGenre_TmdbGenreId",
                table: "UserPreferredGenre",
                column: "TmdbGenreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedMovieGenre");

            migrationBuilder.DropTable(
                name: "UserPreferredGenre");

            migrationBuilder.DropTable(
                name: "SavedMovies");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Profiles");
        }
    }
}
